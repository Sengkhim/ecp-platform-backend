export abstract class BaseORMFilter {
    protected filter<Type extends Record<string, any>>(filter: Type): Record<string, any> {
        const conditions: any[] = Object.entries(filter ?? {})
            .flatMap(([key, value]) => this.parseCondition(key, value))
            .filter(Boolean);

        return conditions.length > 0 ? { AND: conditions } : {};
    }

    private parseCondition(key: string, value: any): Record<string, any> | null {
        if (value === null || value === undefined) return null;
        if (Array.isArray(value)) return { [key]: { in: value } };
        if (typeof value === 'object' && !(value instanceof Date)) {
            return { [key]: this.filter(value) };
        }
        return { [key]: value };
    }
}