import { Injectable} from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { Strategy } from 'passport-github2';
import { OAuthProvider } from "@prisma/client";
import { IUserSessionService } from '@/modules/user-session/service/user-session.service';
import { IAuthProviderService } from '@/modules/auth/providers/service/auth-provider.service';
import { UserService } from '@/modules/user/user.service';
import { PrismaService } from 'prisma/prisma.service';
import { ApplicationService } from '@/application/services/application.service';
import { UserSessionService } from '@/modules/user-session/user-session.service';
import { AuthProviderService } from '@/modules/auth/providers/auth-provider.service';
import { CreateProfileDto } from '@/modules/user/dto/create-profile.dto';
import { GithubProfile } from '@/modules/auth/providers/response/github.response';
import { IResponse } from '@/wrapper/inteface/response';
import { OAuthProviderType } from '@/modules/auth/strategies/type';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';
@Injectable()
export class GithubStrategy extends PassportStrategy(Strategy, 'github')  {
   
    private readonly pwd: string = Math.random().toString(36).slice(-8);
    private readonly sessionService: IUserSessionService;
    private readonly authProvider: IAuthProviderService;
    private readonly userService: UserService;
   
    constructor(
        private readonly prisma: PrismaService,
        appService: ApplicationService,
    ) {
        super({
            clientID: appService.getGithubClientId(),
            clientSecret: appService.getGithubClientSecret(),
            callbackURL: appService.getGithubCallbackURL(),
            scope: ['user:email'],
        });
        
        this.sessionService = new UserSessionService(this.prisma);
        this.authProvider = new AuthProviderService(this.prisma);
        this.userService = new UserService(this.prisma);
    }

    getProfile(profile: GithubProfile): CreateProfileDto {
        return {
            profilePicture: profile.photos?.[0]?.value,
            bio: profile._json.bio,
            address: profile._json.location
        };
    }

    async validate(accessToken: string, _: string, profile: GithubProfile) {
        const { id, emails } = profile;
        const email = emails?.[0]?.value || '';

        let user: IResponse<Record<string, any>> = await this
            .userService
            .findIncludeOauth(email, OAuthProviderType.GITHUB);

        if (!user?.data) {
            user = await this.userService.create({
                email,
                password: this.pwd,
                username: profile.username,
                displayName: profile.displayName,
                profile: this.getProfile(profile),
            });
        }

        const oauthProvider = user.data?.oauthProviders?.find(
            (provider: OAuthProvider) => provider.providerName === OAuthProviderType.GITHUB
        );

        const userId: string = user.data.id;
        
        if (!oauthProvider) {
            await this.authProvider.create({
                providerName: OAuthProviderType.GITHUB,
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
