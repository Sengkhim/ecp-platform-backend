import { IsNotEmpty, IsOptional, IsString } from "class-validator";
import { ApiPropertyOptional } from "@nestjs/swagger";

export class CreateRoleDto {

    @ApiPropertyOptional({ example: "admin", description: "Name of the role" })
    @IsNotEmpty()
    @IsString()
    name: string;

    @ApiPropertyOptional({ example: "Administrator with full privileges", description: "Description of the role" })
    @IsOptional()
    @IsString()
    description?: string;  
}
