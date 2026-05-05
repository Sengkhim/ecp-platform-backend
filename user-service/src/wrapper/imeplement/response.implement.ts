import { IResponse } from '@/wrapper/inteface/response';

export class ResponseImpl<T> implements IResponse<T> {
    data: T;
    code: number;
    limit: number;
    pages: number;
    totalPages: number;

    constructor(data: T, code: number, limit?: number, page?: number, totalPages?: number) {
        this.data = data;
        this.code = code;
        this.limit = limit || 10;
        this.pages = page || 1;
        this.totalPages = totalPages || 0;
    }

    static success<T>(data: T, statusCode: number = 200): IResponse<T> {
        return new ResponseImpl<T>(data, statusCode);
    }

    static paginate<T>(data: T, limit: number, page: number, totalPages: number): IResponse<T> {
        return new ResponseImpl<T>(data, 200, limit, page, totalPages);
    }

    static paginateV2<T>(data: T, totalPages: number, filter: Record<string, any>): IResponse<T> {
        return new ResponseImpl<T>(data, 200, filter.limit, filter.page, totalPages);
    }
}