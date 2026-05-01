using ECP.ProductService.Application.Commands;
using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Mappings;
using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Exceptions;
using ECP.ProductService.Core.Interfaces.Cache;
using ECP.ProductService.Core.Interfaces.Repositories;
using MediatR;

namespace ECP.ProductService.Application.Handlers;

// ── CreateProduct ─────────────────────────────────────────────────────────────

public sealed class CreateProductHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        if (await repo.ExistsByNameAsync(cmd.Name, null, ct))
            throw new ProductNameConflictException(cmd.Name);

        var product = Product.Create(
            name:         cmd.Name,
            description:  cmd.Description,
            price:        Money.Of(cmd.Price, cmd.Currency),
            categoryId:   CategoryId.From(cmd.CategoryId),
            brand:        cmd.Brand,
            initialStock: cmd.InitialStock,
            tags:         cmd.Tags,
            images:       cmd.Images,
            attributes:   cmd.Attributes);

        await repo.InsertAsync(product, ct);
        await cache.RemoveByPrefixAsync(CacheKey.ProductPrefix, ct);

        return product.ToDto();
    }
}

// ── UpdateProduct ─────────────────────────────────────────────────────────────

public sealed class UpdateProductHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var id      = ProductId.From(cmd.Id);
        var product = await repo.GetByIdAsync(id, ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        // Name uniqueness — exclude self from check
        if (product.Name != cmd.Name
            && await repo.ExistsByNameAsync(cmd.Name, id, ct))
            throw new ProductNameConflictException(cmd.Name);

        var oldSlug = product.Slug.Value;
        product.UpdateDetails(cmd.Name, cmd.Description, cmd.Brand, cmd.Tags, cmd.Images, cmd.Attributes);

        await repo.UpdateAsync(product, ct);
        await InvalidateCacheAsync(cmd.Id, oldSlug, ct);

        return product.ToDto();
    }

    private async Task InvalidateCacheAsync(Guid id, string oldSlug, CancellationToken ct)
    {
        await Task.WhenAll(
            cache.RemoveAsync(CacheKey.ById(id), ct),
            cache.RemoveAsync(CacheKey.BySlug(oldSlug), ct),
            cache.RemoveByPrefixAsync(CacheKey.ProductPrefix, ct));
    }
}

// ── UpdatePrice ───────────────────────────────────────────────────────────────

public sealed class UpdatePriceHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<UpdatePriceCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdatePriceCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        var salePrice = cmd.SalePrice.HasValue
            ? Money.Of(cmd.SalePrice.Value, cmd.Currency) : null;

        product.UpdatePrice(Money.Of(cmd.Price, cmd.Currency), salePrice);

        await repo.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKey.ById(cmd.Id), ct);

        return product.ToDto();
    }
}

// ── AdjustStock ───────────────────────────────────────────────────────────────

public sealed class AdjustStockHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<AdjustStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.AdjustStock(cmd.Delta, cmd.Reason);

        await repo.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKey.ById(cmd.Id), ct);

        return product.ToDto();
    }
}

// ── ReserveStock ──────────────────────────────────────────────────────────────

public sealed class ReserveStockHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<ReserveStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ReserveStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.ReserveStock(cmd.Quantity);

        await repo.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKey.ById(cmd.Id), ct);

        return product.ToDto();
    }
}

// ── ReleaseStock ──────────────────────────────────────────────────────────────

public sealed class ReleaseStockHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<ReleaseStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ReleaseStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.ReleaseStock(cmd.Quantity);

        await repo.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKey.ById(cmd.Id), ct);

        return product.ToDto();
    }
}

// ── Status commands ───────────────────────────────────────────────────────────

public sealed class PublishProductHandler(IProductRepository repo, ICacheService cache)
    : IRequestHandler<PublishProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(PublishProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.Publish();
        await repo.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKey.ById(cmd.Id), ct);
        return product.ToDto();
    }
}

public sealed class DeactivateProductHandler(IProductRepository repo, ICacheService cache)
    : IRequestHandler<DeactivateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(DeactivateProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.Deactivate();
        await repo.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKey.ById(cmd.Id), ct);
        return product.ToDto();
    }
}

public sealed class ArchiveProductHandler(IProductRepository repo, ICacheService cache)
    : IRequestHandler<ArchiveProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ArchiveProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.Archive();
        await repo.UpdateAsync(product, ct);
        await Task.WhenAll(
            cache.RemoveAsync(CacheKey.ById(cmd.Id), ct),
            cache.RemoveAsync(CacheKey.BySlug(product.Slug.Value), ct));
        return product.ToDto();
    }
}

// ── DeleteProduct ─────────────────────────────────────────────────────────────

public sealed class DeleteProductHandler(IProductRepository repo, ICacheService cache)
    : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand cmd, CancellationToken ct)
    {
        var id      = ProductId.From(cmd.Id);
        var product = await repo.GetByIdAsync(id, ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        await repo.DeleteAsync(id, ct);
        await Task.WhenAll(
            cache.RemoveAsync(CacheKey.ById(cmd.Id), ct),
            cache.RemoveAsync(CacheKey.BySlug(product.Slug.Value), ct),
            cache.RemoveByPrefixAsync(CacheKey.ProductPrefix, ct));

        return true;
    }
}
