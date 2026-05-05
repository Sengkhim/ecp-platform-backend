import { UserSession } from '@prisma/client';
import { CreateUserSessionDto } from '@/modules/user-session/dto/create-user-session.dto';
import { IResponse } from '@/wrapper/inteface/response';

export interface IUserSessionService {

    create(request: CreateUserSessionDto): Promise<IResponse<UserSession>>;
}