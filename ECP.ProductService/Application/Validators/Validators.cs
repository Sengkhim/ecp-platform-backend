using ECP.ProductService.Application.Commands;
using FluentValidation;

namespace ECP.ProductService.Application.Validators;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    private static readonly string[] SupportedCurrencies =
        ["USD","EUR","GBP","KHR","THB","SGD","JPY","CNY","AUD","CAD"];

    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()          .WithMessage("Product name is required.")
            .MaximumLength(200)  .WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(5000) .WithMessage("Description must not exceed 5000 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0)      .WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => SupportedCurrencies.Contains(c?.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}.");

        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Brand)
            .NotEmpty()          .WithMessage("Brand is required.")
            .MaximumLength(100)  .WithMessage("Brand must not exceed 100 characters.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.");

        RuleForEach(x => x.Tags)
            .MaximumLength(50).WithMessage("Each tag must not exceed 50 characters.")
            .When(x => x.Tags is not null);
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(5000);
    }
}

public sealed class UpdatePriceValidator : AbstractValidator<UpdatePriceCommand>
{
    public UpdatePriceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        RuleFor(x => x.SalePrice)
            .LessThan(x => x.Price).WithMessage("Sale price must be less than regular price.")
            .When(x => x.SalePrice.HasValue);
    }
}

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Stock delta cannot be zero.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300)
            .WithMessage("Reason is required for every stock adjustment.");
    }
}

public sealed class ReserveStockValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive.");
    }
}
