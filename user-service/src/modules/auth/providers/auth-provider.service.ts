import { IAuthProviderService } from '@/modules/auth/providers/service/auth-provider.service';
import { PrismaService } from 'prisma/prisma.service';
import { AuthProviderDto } from '@/modules/auth/dto/auth-provider.dto';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';


export class AuthProviderService implements IAuthProviderService {
    
    constructor(private readonly prismaService: PrismaService) {}
    
    async create(request: AuthProviderDto) {
        const data = await this
            .prismaService
            .oAuthProvider
            .create({
                data: request
            });
        
        return ResponseImpl.success(data);
    }
}
