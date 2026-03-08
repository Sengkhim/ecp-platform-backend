using ECP.ProductService.Core.Exceptions;
using ValidationException = ECP.ProductService.Core.Exceptions.ValidationException;

namespace ECP.ProductService.Infrastructure.GraphQL.Filters;

/// <summary>
/// Translates domain and application exceptions into structured GraphQL errors.
/// Prevents raw stack traces from leaking to clients in production.
/// </summary>
public sealed class ProductErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            ProductNotFoundException ex => error
                .WithCode("PRODUCT_NOT_FOUND")
                .WithMessage(ex.Message),

            ProductAlreadyExistsException ex => error
                .WithCode("PRODUCT_ALREADY_EXISTS")
                .WithMessage(ex.Message),

            InsufficientStockException ex => error
                .WithCode("INSUFFICIENT_STOCK")
                .WithMessage(ex.Message),

            DomainException ex => error
                .WithCode("DOMAIN_ERROR")
                .WithMessage(ex.Message),

            ValidationException ex => error
                .WithCode("VALIDATION_ERROR")
                .WithMessage("One or more validation errors occurred.")
                .SetExtension("errors", ex.Errors) ,

            _ => error // unhandled — default HC behaviour (masked in prod)
        };
    }
}