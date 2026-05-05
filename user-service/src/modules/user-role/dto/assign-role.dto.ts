import {ApiPropertyOptional} from "@nestjs/swagger";
import {IsOptional, IsString} from "class-validator";

export class AssignRoleDto {
    
    @ApiPropertyOptional()
    @IsString()
    userId: string;

    @ApiPropertyOptional()
    @IsOptional()
    @IsString()
    roleId: string;
}