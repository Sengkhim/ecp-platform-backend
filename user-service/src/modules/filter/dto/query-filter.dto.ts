import { IsOptional, IsString, IsEmail } from 'class-validator';
import { ApiPropertyOptional } from '@nestjs/swagger';
import { BaseFilter } from '@/modules/filter/base.filter';

export class FilterQueryDto implements BaseFilter {

    @ApiPropertyOptional({ description: "Filter by email", example: "user@example.com" })
    @IsOptional()
    @IsEmail()
    email: string;

    @ApiPropertyOptional({ description: "Filter by first name", example: "John" })
    @IsOptional()
    @IsString()
    firstName: string;

    @ApiPropertyOptional({ description: "Filter by last name", example: "Doe" })
    @IsOptional()
    @IsString()
    lastName: string;

    @ApiPropertyOptional({ description: "Filter by role", example: "admin" })
    @IsOptional()
    @IsString()
    role: string;
}
