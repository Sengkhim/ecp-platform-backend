namespace ECP.ProductService.Core.Exceptions;

public class DomainException(string message) : Exception(message);

public class ProductNotFoundException(string id) : Exception($"Product '{id}' was not found.");

public class ProductAlreadyExistsException(string name) : Exception($"A product with the name '{name}' already exists.");

public class InsufficientStockException(string productId, int requested, int available)
    : Exception($"Insufficient stock for product '{productId}'. Requested: {requested}, Available: {available}.");

public class ValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(errors);
}