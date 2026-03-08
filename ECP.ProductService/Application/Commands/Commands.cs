using ECP.ProductService.Application.DTOs;
using MediatR;

namespace ECP.ProductService.Application.Commands;

// ── Create ────────────────────────────────────────────────────────────────────

public record CreateProductCommand(
    string   Name,
    string   Description,
    decimal  Price,
    string   Currency,
    Guid     CategoryId,
    string   Brand,
    int      InitialStock,
    IEnumerable<string>?               Tags       = null,
    IEnumerable<string>?               Images     = null,
    IDictionary<string, string>?       Attributes = null) : IRequest<ProductDto>;

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateProductCommand(
    Guid     Id,
    string   Name,
    string   Description,
    string   Brand,
    IEnumerable<string>?               Tags       = null,
    IEnumerable<string>?               Images     = null,
    IDictionary<string, string>?       Attributes = null) : IRequest<ProductDto>;

public record UpdateProductPriceCommand(
    Guid     Id,
    decimal  Price,
    string   Currency,
    decimal? SalePrice = null) : IRequest<ProductDto>;

// ── Stock ─────────────────────────────────────────────────────────────────────

public record AdjustStockCommand(
    Guid   Id,
    int    Delta,
    string Reason) : IRequest<ProductDto>;

public record ReserveStockCommand(
    Guid Id,
    int  Quantity) : IRequest<ProductDto>;

public record ReleaseStockCommand(
    Guid Id,
    int  Quantity) : IRequest<ProductDto>;

// ── Status ────────────────────────────────────────────────────────────────────

public record ActivateProductCommand(Guid Id)   : IRequest<ProductDto>;
public record DeactivateProductCommand(Guid Id) : IRequest<ProductDto>;
public record ArchiveProductCommand(Guid Id)    : IRequest<ProductDto>;

// ── Delete ────────────────────────────────────────────────────────────────────

public record DeleteProductCommand(Guid Id) : IRequest<bool>;