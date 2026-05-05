import { IsOptional, IsString, IsEnum, IsDateString } from "class-validator";
import { ApiPropertyOptional } from "@nestjs/swagger";

export class CreateProfileDto {

    @ApiPropertyOptional({ example: "https://example.com/profile.jpg", description: "Profile picture URL" })
    @IsOptional()
    @IsString()
    profilePicture?: string;

    @ApiPropertyOptional({ example: "Software developer", description: "Short bio about the user" })
    @IsOptional()
    @IsString()
    bio?: string;

    @ApiPropertyOptional({ example: "+1234567890", description: "User phone number" })
    @IsOptional()
    @IsString()
    phoneNumber?: string;

    @ApiPropertyOptional({ example: "123 Main St, New York, USA", description: "User address" })
    @IsOptional()
    @IsString()
    address?: string;

    @ApiPropertyOptional({ example: "1990-01-01", description: "User's date of birth (ISO 8601)" })
    @IsOptional()
    @IsDateString()
    dateOfBirth?: string;

    @ApiPropertyOptional({ example: "M", enum: ["M", "F"], description: "User gender" })
    @IsOptional()
    @IsEnum(["M", "F"])
    gender?: "M" | "F";
}
