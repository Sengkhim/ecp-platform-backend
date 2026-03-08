namespace ECP.ProductService.Core.Exceptions;

public abstract class ProductServiceException : Exception
{
    public string Code { get; }
    protected ProductServiceException(string code, string message) : base(message) => Code = code;
}

public sealed class ProductNotFoundException : ProductServiceException
{
    public ProductNotFoundException(string id)
        : base("PRODUCT_NOT_FOUND", $"Product '{id}' was not found.") { }
}

public sealed class ProductNameConflictException : ProductServiceException
{
    public ProductNameConflictException(string name)
        : base("PRODUCT_NAME_CONFLICT", $"A product named '{name}' already exists.") { }
}

public sealed class InsufficientStockException : ProductServiceException
{
    public InsufficientStockException(string productId, int requested, int available)
        : base("INSUFFICIENT_STOCK",
            $"Insufficient stock for '{productId}'. Requested: {requested}, Available: {available}.") { }
}

public sealed class ProductArchivedExcepion : ProductServiceException
{
    public ProductArchivedExcepion(string id)
        : base("PRODUCT_ARCHIVED", $"Product '{id}' is archived and cannot be modified.") { }
}

public sealed class ConcurrencyException : ProductServiceException
{
    public ConcurrencyException(string id)
        : base("CONCURRENCY_CONFLICT",
            $"Product '{id}' was modified by another request. Please retry.") { }
}

public sealed class AppValidationException : ProductServiceException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public AppValidationException(IDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
