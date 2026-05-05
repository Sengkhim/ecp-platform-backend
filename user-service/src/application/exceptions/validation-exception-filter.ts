import { ExceptionFilter, Catch, ArgumentsHost } from '@nestjs/common';
import { Response } from 'express';
import {HttpArgumentsHost} from "@nestjs/common/interfaces";

@Catch()
export class ValidationExceptionFilter implements ExceptionFilter {
    catch(exception: any, host: ArgumentsHost) {
        const ctx: HttpArgumentsHost = host.switchToHttp();
        const response: Response = ctx.getResponse<Response>();
        const status: number = exception.getStatus();
        
        const errorResponse: Record<string, any> = {
            statusCode: status,
            message: exception.message || 'Validation failed',
            errors: exception.response || [],
        };

        response.status(status).json(errorResponse);
    }
}
