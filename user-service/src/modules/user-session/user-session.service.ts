import {CreateUserSessionDto} from './dto/create-user-session.dto';
import {UserSession} from "@prisma/client";
import { IUserSessionService } from '@/modules/user-session/service/user-session.service';
import { PrismaService } from 'prisma/prisma.service';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';

export class UserSessionService implements IUserSessionService {
    constructor(private readonly prismaService: PrismaService) {}
    
    async create(request: CreateUserSessionDto) {
        const expiresAt: Date = new Date();
        expiresAt.setHours(expiresAt.getHours() + 1); 

        const data: UserSession = await this
            .prismaService
            .userSession
            .create({
                data: {
                    userId: request.userId,
                    token: request.token,
                    expiresAt
                }
            });
        
        return ResponseImpl.success(data);
    }
}
