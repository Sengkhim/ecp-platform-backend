using ECP.ProductService.Core.Exceptions;
using HotChocolate;

namespace ECP.ProductService.Infrastructure.GraphQL.Filters;

/// <summary>
/// Translates domain and application exceptions into structured GraphQL errors.
///
/// Guarantees:
///   - Domain exceptions surface with readable codes and messages.
///   - No stack traces or internal details leak to clients.
///   - Unhandled exceptions get a generic INTERNAL_ERROR code (HC default masking).
/// </summary>
public sealed class ProductErrorFilter : IErrorFilter
{
    public IError OnError(IError error) => error.Exception switch
    {
        ProductNotFoundException ex     => Clean(error, ex.Code, ex.Message),
        ProductNameConflictException ex => Clean(error, ex.Code, ex.Message),
        InsufficientStockException ex   => Clean(error, ex.Code, ex.Message),
        ConcurrencyException ex         => Clean(error, ex.Code, ex.Message),
        ProductArchivedExcepion ex      => Clean(error, ex.Code, ex.Message),

        AppValidationException ex => error
            .WithCode(ex.Code)
            .WithMessage(ex.Message)
            .SetExtension("validationErrors", ex.Errors)
            .RemoveException(),

        ArgumentException ex => Clean(error, "INVALID_ARGUMENT", ex.Message),
        InvalidOperationException ex => Clean(error, "DOMAIN_RULE_VIOLATION", ex.Message),

        _ => error // let HotChocolate apply its default masking
    };

    private static IError Clean(IError error, string code, string message)
        => error.WithCode(code).WithMessage(message).RemoveException();
}
