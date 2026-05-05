import { BadRequestException, Injectable, Scope } from '@nestjs/common';
import { CreateUserDto } from './dto/create-user.dto';
import * as bcrypt from 'bcrypt';
import { Prisma, User } from '@prisma/client';
import { BaseORMFilter } from '@/modules/filter/base/base-ORM-filter';
import { PrismaService } from 'prisma/prisma.service';
import { CreateProfileDto } from './dto/create-profile.dto';
import { IResponse } from '@/wrapper/inteface/response';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';
import { BaseFilter } from '@/modules/filter/base.filter';

@Injectable({ scope: Scope.REQUEST })
export class UserService extends BaseORMFilter {
    
    constructor(private readonly prismaService: PrismaService) {
        super();
    }

    async validateUser(email: string, password: string): Promise<User | null> {
        const user: User | null = await this
            .prismaService
            .user
            .findUnique({ where: { email } });

        if (!user) return null;

        const isPasswordValid: boolean = await bcrypt.compare(password, user.passwordHash);

        if (!isPasswordValid) return null;

        return user;
    }
    
    private async hashPassword(password: string): Promise<string> {
        const saltRounds: number = 10;
        return await bcrypt.hash(password, saltRounds);
    }

    toProfile(profile?: CreateProfileDto): Record<string, any> | undefined {
        if (!profile) return undefined;

        const { 
            profilePicture, 
            bio, 
            phoneNumber, 
            address, 
            dateOfBirth,
            gender
        } = profile;

        return {
            create: {
                profilePicture,
                bio,
                phoneNumber,
                address,
                dateOfBirth: new Date(dateOfBirth ?? Date.now()),
                gender
            }
        };
    }


    create(request: CreateUserDto): Promise<IResponse<Record<string, any>>> {
        return this.prismaService.$transaction(async (transaction) => {
            const { 
                username,
                displayName, 
                email, 
                password,
                profile 
            } = request;

            if (await this.validateUser(email, password)) {
                throw new BadRequestException("User already exists");
            }

            const passwordHash: string = await this.hashPassword(password);

            const user: Record<string, any> = await transaction.user.create({
                data: {
                    username,
                    displayName,
                    email,
                    passwordHash,
                    profile: this.toProfile(profile)
                }
            });

            return ResponseImpl.success(user);
        });
    }
    
    async findAll(filter: Partial<BaseFilter>): Promise<IResponse<Record<string, any>[]>> {
        const where: Prisma.UserWhereInput = this.filter(filter);

        const data: Record<string, any>[] = await this
            .prismaService.user.findMany({ 
                where,
                select: {
                    id: true,
                    username: true,
                    displayName: true,
                    email: true,
                    profile: {
                        select: {
                            gender: true,
                            bio: true,
                            profilePicture: true,
                            phoneNumber: true,
                            address: true,
                            dateOfBirth: true
                        }
                    },
                    roles: {
                        select: {
                            role: true
                        }
                    }
                }
            });
        
        return ResponseImpl.success(data);
    }

    async findOne(id: string) {
        const data: Record<string, any> = await this
            .prismaService
            .user
            .findMany({
                where: { id: id },
                select: {
                    id: true,
                    username: true,
                    displayName: true,
                    email: true,
                    profile: {
                        select: {
                            gender: true,
                            bio: true,
                            profilePicture: true,
                            phoneNumber: true,
                            address: true,
                            dateOfBirth: true
                        }
                    },
                    roles: {
                        select: {
                            role: true
                        }
                    }
                }
            });
        
        return ResponseImpl.success(data)
    }
    
    async findIncludeOauth(email: string, providerName: string) {
        const data: any = await this
            .prismaService
            .user
            .findUnique({
                where: { email },
                include: {
                    oauthProviders: {
                        where: { providerName }
                    }
                },
            });
        return ResponseImpl.success(data);
    }
}
