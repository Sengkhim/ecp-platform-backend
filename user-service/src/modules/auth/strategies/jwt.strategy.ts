import {Injectable} from "@nestjs/common";
import {PassportStrategy} from "@nestjs/passport";
import { ExtractJwt, Strategy} from 'passport-jwt';
import {User} from "@prisma/client";
import { PrismaService } from "prisma/prisma.service";
import { ApplicationService } from "@/application/services/application.service";
import { JwtPayload } from "../dto/jwt-payload.dto";

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
    constructor(
        private readonly prisma: PrismaService,
        appService: ApplicationService,
    ) {

        super({
            ignoreExpiration: false,
            jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
            secretOrKey: appService.getJwtSecret()
        });
    }

    async validate(payload: JwtPayload) {
        const user: User | null = await this.prisma.user.findUnique({
            where: { id: payload.userId },
        });

        if (!user) {
            throw new Error('Invalid token');
        }

        return user;
    }
}
