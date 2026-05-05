import { OAuthProvider } from '@prisma/client';
import { AuthProviderDto } from '@/modules/auth/dto/auth-provider.dto';
import { IResponse } from '@/wrapper/inteface/response';

export interface IAuthProviderService {

    create(request: AuthProviderDto): Promise<IResponse<OAuthProvider>>;
}