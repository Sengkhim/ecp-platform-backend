import {IsEnum, IsNotEmpty, IsString} from "class-validator";
import { OAuthProviderType } from '@/modules/auth/strategies/type';

export class AuthProviderDto {

    @IsNotEmpty()
    @IsString()
    userId: string;

    @IsNotEmpty()
    @IsEnum(OAuthProviderType)
    providerName: OAuthProviderType;

    @IsNotEmpty()
    @IsString()
    providerUserId: string;
}