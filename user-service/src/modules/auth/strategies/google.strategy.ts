import { Injectable } from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { Strategy } from 'passport-google-oauth20';
import { IUserSessionService } from '@/modules/user-session/service/user-session.service';
import { IAuthProviderService } from '@/modules/auth/providers/service/auth-provider.service';
import { UserService } from '@/modules/user/user.service';
import { PrismaService } from 'prisma/prisma.service';
import { ApplicationService } from '@/application/services/application.service';
import { UserSessionService } from '@/modules/user-session/user-session.service';
import { AuthProviderService } from '@/modules/auth/providers/auth-provider.service';
import { GoogleProfile } from '@/modules/auth/providers/response/googleProfile';
import { CreateProfileDto } from '@/modules/user/dto/create-profile.dto';
import { IResponse } from '@/wrapper/inteface/response';
import { OAuthProviderType } from '@/modules/auth/strategies/type';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';

@Injectable()
export class GoogleStrategy extends PassportStrategy(Strategy, 'google') {

    private readonly pwd: string = Math.random().toString(36).slice(-8);
    private readonly sessionService: IUserSessionService;
    private readonly authProvider: IAuthProviderService;
    private readonly userService: UserService;
    
    constructor(
        private readonly prisma: PrismaService,
        appService: ApplicationService
    ) {
        super({
            clientID: appService.getGoogleClientId(),
            clientSecret: appService.getGoogleClientSecret(),
            callbackURL: appService.getGoogleCallbackURL(),
            scope: ['email', 'profile'],
        });

        this.sessionService = new UserSessionService(this.prisma);
        this.authProvider = new AuthProviderService(this.prisma);
        this.userService = new UserService(this.prisma);
    }

    getProfile(profile: GoogleProfile): CreateProfileDto {
        return {
            profilePicture: profile.photos?.[0]?.value,
        };
    }

    async validate(accessToken: string, _: string, profile: GoogleProfile) {
        const {id, emails } = profile;
        const email: string = emails?.[0]?.value || '';

        let user: IResponse<Record<string, any>> = await this
            .userService
            .findIncludeOauth(email, OAuthProviderType.GOOGLE);

        if (!user?.data) {
            user = await this.userService.create({
                email,
                password: this.pwd,
                username: `${profile.name.familyName} ${profile.name.givenName}`,
                displayName: profile.displayName,
                profile: this.getProfile(profile),
            });
        }

        const oauthProvider = user.data?.oauthProviders?.find(
            (provider: any) => provider.providerName === OAuthProviderType.GOOGLE
        );

        const userId: string = user.data.id;
        
        if (!oauthProvider) {
            await this.authProvider.create({
                providerName: OAuthProviderType.GOOGLE,
                providerUserId: id,
                userId
            });
        }

        await this.sessionService.create({
            userId,
            token: accessToken,
        });

        return ResponseImpl.success({ email });
    }
}
