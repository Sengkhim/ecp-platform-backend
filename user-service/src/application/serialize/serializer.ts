export class Serializer {
    static toResponse<TResponse>(json: any): TResponse {
        return JSON.parse(json);
    }

    static toJson<TResponse>(value: TResponse): string {
        return JSON.stringify(value);
    }
}