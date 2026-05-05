import { Module } from '@nestjs/common';
import { ApplicationService } from '@/application/services/application.service';
import { ConfigModule } from '@nestjs/config';

@Module({
    imports: [ConfigModule.forRoot()],
    controllers: [],
    providers: [
        ApplicationService
    ],
})
export class AppModule {}
