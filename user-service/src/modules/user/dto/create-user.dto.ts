import {
    IsString,
    IsEmail,
    IsNotEmpty,
    MinLength,
    MaxLength,
    IsOptional,
    ValidateNested
} from 'class-validator';
import { Type } from 'class-transformer';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { CreateProfileDto } from '@/modules/user/dto/create-profile.dto';

export class CreateUserDto {
    @ApiProperty({ example: 'user@example.com', description: 'User email address' })
    @IsEmail()
    @IsNotEmpty()
    email: string;

    @ApiProperty({ example: 'StrongPass123', description: 'User password (8-20 characters)' })
    @IsString()
    @IsNotEmpty()
    @MinLength(8)
    @MaxLength(20)
    password: string;

    @ApiProperty({ example: 'John', description: 'Username of the user' })
    @IsString()
    @IsNotEmpty()
    username: string;

    @ApiProperty({ example: 'Doe', description: 'displayName of the user' })
    @IsString()
    @IsNotEmpty()
    displayName: string;

    @ApiPropertyOptional({ type: () => CreateProfileDto, description: 'User profile details' })
    @IsOptional()
    @ValidateNested()
    @Type(() => CreateProfileDto)
    profile?: CreateProfileDto;
}
