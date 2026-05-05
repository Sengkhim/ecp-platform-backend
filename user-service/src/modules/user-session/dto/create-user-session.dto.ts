import {IsNotEmpty, IsString} from "class-validator";

export class CreateUserSessionDto {

    @IsNotEmpty()
    @IsString()
    userId: string;

    @IsNotEmpty()
    @IsString()
    token: string;
}
