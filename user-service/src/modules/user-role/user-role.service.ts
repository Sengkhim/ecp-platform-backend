import {Injectable, NotFoundException, Scope} from '@nestjs/common';
import {Role, User, UserRole} from "@prisma/client";
import { PrismaService } from 'prisma/prisma.service';
import { AssignRoleDto } from '@/modules/user-role/dto/assign-role.dto';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';

@Injectable({ scope: Scope.REQUEST })
export class UserRoleService {
    constructor(private readonly prismaService: PrismaService) {}
    
    async create(request: AssignRoleDto) {

        const { userId, roleId } = request;

        const user: User | null = await this
            .prismaService
            .user
            .findUnique({ where: { id: userId } });
        
        const role: Role | null = await this
            .prismaService
            .role
            .findUnique({ where: { id: roleId } });

        if (!user || !role) {
            throw new NotFoundException('User or Role not found');
        }
        
        const data: UserRole = await this
            .prismaService
            .userRole
            .create({
                data: request
            });
        
        return ResponseImpl.success(data);
    }

    async update(userId: string, roleId: string, request: AssignRoleDto) {
     
        const existingUserRole: UserRole | null = await this
            .prismaService
            .userRole
            .findUnique({
                where: { userId_roleId: { userId, roleId } },
            });

        if (!existingUserRole) {
            throw new NotFoundException('UserRole not found');
        }
        
        const data: UserRole = await this.prismaService.userRole.update({
            where: { userId_roleId: { userId, roleId } },
            data: {
                ...request, 
            },
        });
        
        return ResponseImpl.success(data);
    }
}
