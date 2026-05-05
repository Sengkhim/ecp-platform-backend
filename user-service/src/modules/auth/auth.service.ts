import {Injectable, Scope, UnauthorizedException} from '@nestjs/common';
import {JwtService, JwtSignOptions} from "@nestjs/jwt";
import {User} from "@prisma/client";
import * as bcrypt from 'bcryptjs';
import { PrismaService } from 'prisma/prisma.service';
import { ApplicationService } from '@/application/services/application.service';
import { LoginDto } from '@/modules/auth/dto/login.dto';
import { IResponse } from '@/wrapper/inteface/response';
import { JwtPayload } from '@/modules/auth/dto/jwt-payload.dto';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';

@Injectable({ scope: Scope.REQUEST })
export class AuthService {
    constructor(
        private readonly prisma: PrismaService,
        private readonly jwtService: JwtService,
        private readonly appService: ApplicationService,
    ) {}

    async login(loginDto: Partial<LoginDto>): Promise<IResponse<string>> {
        const { email, password } = loginDto;
       
        const user: User | null = await this.prisma.user.findUnique({
            where: { email },
        });

        if (!user) {
            throw new UnauthorizedException('Invalid credentials');
        }

        // Check if the provided password matches the stored hash
        if (password) {
            const isMatch: boolean = await bcrypt.compare(password, user.passwordHash);
            if (!isMatch) {
                throw new UnauthorizedException('Invalid credentials');
            }
        }
        
        // Create a payload and sign it to generate the JWT token
        const payload: JwtPayload = { email: user.email, userId: user.id };
        const options: JwtSignOptions = {
            secret: this.appService.getJwtSecret(),
            expiresIn: this.appService.getExpires(),
            issuer: "USER_SERVICE",
            audience: "MICRO_SERVICE",
            algorithm: 'HS256',
        };
        
        const accessToken: string = await this
            .jwtService
            .signAsync(payload, options);

        return ResponseImpl.success(accessToken);
    }
}
