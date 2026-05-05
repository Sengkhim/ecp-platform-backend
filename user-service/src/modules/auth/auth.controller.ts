import {Body, Controller, Get, HttpCode, Post, Req, UseGuards} from '@nestjs/common';
import {AuthService} from './auth.service';
import {ApiTags} from "@nestjs/swagger";
import { AuthGuard } from '@nestjs/passport';
import { GithubAuthGuard } from '@/application/guards/github-auth.guard';
import { LoginDto } from '@/modules/auth/dto/login.dto';

@ApiTags('Authentication')
@Controller('auth')
export class AuthController {
    constructor(private readonly authService: AuthService) {}

    @Get('github')
    @UseGuards(GithubAuthGuard)
    @HttpCode(200)
    githubLogin() {}

    @Get('github/callback')
    @UseGuards(GithubAuthGuard)
    @HttpCode(200)
    githubLoginCallback(@Req() req: Request) {
        const { email } = req["user"]["data"]as LoginDto;
        return this.authService.login({ email });
    }

    @Get('google')
    @UseGuards(AuthGuard('google'))
    async googleLogin() {}

    @Get('google/callback')
    @UseGuards(AuthGuard('google'))
    googleLoginCallback(@Req() req: Request) {
        const { email } = req["user"]["data"] as LoginDto;
        return this.authService.login({ email });
    }
    
    @Post('login')
    @HttpCode(201)
    login(@Body() request: LoginDto) {
        return this.authService.login(request);
    }
}
