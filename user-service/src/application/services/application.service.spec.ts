import { Test, TestingModule } from '@nestjs/testing';
import { ApplicationService } from './application.service';
import {ConfigModule} from "@nestjs/config";

describe('ApplicationService', () => {
    let service: ApplicationService;

    beforeEach(async () => {
        const module: TestingModule = await Test.createTestingModule({
            imports: [ConfigModule],
            providers: [ApplicationService],
        }).compile();

        service = module.get<ApplicationService>(ApplicationService);
    });

    it('should be defined', () => {
        expect(service).toBeDefined();
    });
});
