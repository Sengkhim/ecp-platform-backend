import {Global, INestApplication, Injectable, PipeTransform, ValidationPipe} from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import {DocumentBuilder, OpenAPIObject, SwaggerModule} from "@nestjs/swagger";

@Global()
@Injectable()
export class ApplicationService {
    
    private readonly port: number = 3000;
    private app: INestApplication;
    constructor(private configService: ConfigService) {}
    
    build(app: INestApplication): ApplicationService {
        this.app = app;
        return this;
    }

    getGithubClientId(): string {
        return this.configService.get<string>('GITHUB_CLIENT_ID')!;
    }

    getGithubClientSecret(): string {
        return this.configService.get<string>('GITHUB_CLIENT_SECRET')!;
    }

    getGithubCallbackURL(): string {
        return this.configService.get<string>('GITHUB_CLIENT_CALL_BACK_URL')!;
    }

    getGoogleClientId(): string {
        return this.configService.get<string>('GOOGLE_CLIENT_ID')!;
    }

    getGoogleClientSecret(): string {
        return this.configService.get<string>('GOOGLE_CLIENT_SECRET')!;
    }

    getGoogleCallbackURL(): string {
        return this.configService.get<string>('GOOGLE_CLIENT_CALL_BACK_URL')!;
    }
    
    getAppPort(): number {
        return this.configService.get<number>('APP_PORT', this.port); 
    }
    
    getAppMode(): "development" | "production" {
        return this.configService.get("APP_MODE") || "development";
    }
    
    isDevelopment(): boolean {
        return this
            .configService
            .get("APP_MODE") === "development";
    }

    getJwtSecret(): string {
        return this.configService.get("JWT_SECRET")!;
    }
    
    getExpires(): string {
        return this.configService.get("EXPIRES_IN") || "2d";
    }
    
    useValidate(...pipes: PipeTransform[]): void {
        // Enable global validation for incoming requests
        this.app.useGlobalPipes(
            new ValidationPipe({
                transform: true,  // Automatically transforms the request body to DTO instances
                whitelist: true,  // Strips properties that are not in the DTO class
                forbidNonWhitelisted: true,  // Throws an error if extra properties are found
                ...pipes,  // Pass any additional pipes if needed
            }),
        );
    }
    
    useSwagger(): void {
        if (this.getAppMode() === "development") {
            const config: Omit<OpenAPIObject, "paths"> = new DocumentBuilder()
                .setTitle('User Service API')
                .setDescription('API for managing users')
                .setVersion('1.0')
                .build();

            SwaggerModule.setup(
                'api/swagger', 
                this.app, SwaggerModule.createDocument(this.app, config),
                {
                    explorer: true,
                    swaggerOptions: {
                        schemes: ['http']
                    }
            });
        }
    }
    
    async run(log?: () => void): Promise<void> {
        const port: number = this.getAppPort();
        const listener = log ?? (() => console.info(`Listening on : http://localhost:${port}/api`));
        await this.app.listen(port, listener);
    }
}
