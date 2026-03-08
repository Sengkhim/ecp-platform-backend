using ECP.ProductService.Application.DTOs;
using MediatR;

namespace ECP.ProductService.Application.Commands;

// ── Create ────────────────────────────────────────────────────────────────────
public sealed record CreateProductCommand(
    string   Name,
    string   Description,
    decimal  Price,
    string   Currency,
    Guid     CategoryId,
    string   Brand,
    int      InitialStock,
    List<string>?               Tags       = null,
    List<string>?               Images     = null,
    Dictionary<string,string>?  Attributes = null
) : IRequest<ProductDto>;

// ── Update details ────────────────────────────────────────────────────────────
public sealed record UpdateProductCommand(
    Guid   Id,
    string Name,
    string Description,
    string Brand,
    List<string>?               Tags       = null,
    List<string>?               Images     = null,
    Dictionary<string,string>?  Attributes = null
) : IRequest<ProductDto>;

// ── Pricing ───────────────────────────────────────────────────────────────────
public sealed record UpdatePriceCommand(
    Guid     Id,
    decimal  Price,
    string   Currency,
    decimal? SalePrice = null
) : IRequest<ProductDto>;

// ── Stock ─────────────────────────────────────────────────────────────────────
public sealed record AdjustStockCommand(Guid Id, int Delta, string Reason)  : IRequest<ProductDto>;
public sealed record ReserveStockCommand(Guid Id, int Quantity)              : IRequest<ProductDto>;
public sealed record ReleaseStockCommand(Guid Id, int Quantity)              : IRequest<ProductDto>;

// ── Status ────────────────────────────────────────────────────────────────────
public sealed record PublishProductCommand(Guid Id)    : IRequest<ProductDto>;
public sealed record DeactivateProductCommand(Guid Id) : IRequest<ProductDto>;
public sealed record ArchiveProductCommand(Guid Id)    : IRequest<ProductDto>;

// ── Delete ────────────────────────────────────────────────────────────────────
public sealed record DeleteProductCommand(Guid Id) : IRequest<bool>;
