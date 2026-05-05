export interface IResponse<T> {
    data: T;
    code: number;
    limit: number;
    pages: number;
    totalPages: number;
}