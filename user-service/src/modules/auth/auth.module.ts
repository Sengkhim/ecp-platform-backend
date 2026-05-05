import { PassportModule } from '@nestjs/passport';
import { Module } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { AuthController } from '@/modules/auth/auth.controller';
import { JwtStrategy } from '@/modules/auth/strategies/jwt.strategy';
import { GoogleStrategy } from '@/modules/auth/strategies/google.strategy';
import { AuthService } from '@/modules/auth/auth.service';

@Module({
    imports: [
        PassportModule.register({ defaultStrategy: 'github' }),
        PassportModule.register({ defaultStrategy: 'google' })
    ],
    controllers: [AuthController],
    providers: [
        JwtService,
        JwtStrategy,
        GoogleStrategy,
        AuthService,
    ],
    exports: [PassportModule]
})
export class AuthModule {}
