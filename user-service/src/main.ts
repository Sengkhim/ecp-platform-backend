import { NestFactory } from "@nestjs/core";
import { INestApplication } from '@nestjs/common';
import { ApplicationService } from './application/services/application.service';
import { AppModule } from '@/app.module';

async function main(): Promise<void> {
    const nest: INestApplication = await NestFactory.create(AppModule);
    const builder: ApplicationService = nest.get(ApplicationService);
    const app: ApplicationService = builder.build(nest);

    if (app.isDevelopment()) {
        app.useSwagger();
    }
    
    app.useValidate();
    await app.run();
}

void main();
