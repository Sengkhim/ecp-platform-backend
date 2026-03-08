namespace ECP.ProductService.Core.Common;

/// <summary>
/// Discriminated union result type.
/// Use for expected failures (NotFound, Conflict) so callers handle errors explicitly.
/// Use exceptions only for unexpected failures (infrastructure down, bugs).
/// </summary>
public sealed class Result<T>
{
    public T?     Value   { get; }
    public Error? Error   { get; }
    public bool   IsOk    => Error is null;
    public bool   IsError => Error is not null;

    private Result(T value)       { Value = value; }
    private Result(Error error)   { Error = error; }

    public static Result<T> Ok(T value)      => new(value);
    public static Result<T> Fail(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onOk, Func<Error, TOut> onError)
        => IsOk ? onOk(Value!) : onError(Error!);

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsOk ? Result<TOut>.Ok(mapper(Value!)) : Result<TOut>.Fail(Error!);
}

public sealed record Error(string Code, string Message)
{
    public static readonly Error NotFound       = new("NOT_FOUND",       "Resource not found.");
    public static readonly Error Conflict       = new("CONFLICT",        "Resource already exists.");
    public static readonly Error InsufficientStock = new("INSUFFICIENT_STOCK", "Not enough stock available.");
    public static readonly Error ConcurrencyConflict = new("CONCURRENCY_CONFLICT", "The resource was modified by another request.");

    public static Error NotFoundWith(string msg)  => new("NOT_FOUND", msg);
    public static Error ConflictWith(string msg)  => new("CONFLICT", msg);
    public static Error DomainError(string msg)   => new("DOMAIN_ERROR", msg);
    public static Error Validation(string msg)    => new("VALIDATION_ERROR", msg);
}
