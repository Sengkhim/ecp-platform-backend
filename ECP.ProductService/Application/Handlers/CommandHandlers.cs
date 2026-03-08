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

public sealed class CreateProductHandler(
    IProductRepository repository,
    ICacheService cache)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        if (await repository.ExistsByNameAsync(cmd.Name, null, ct))
            throw new ProductAlreadyExistsException(cmd.Name);

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

        await repository.InsertAsync(product, ct);

        await cache.RemoveByPatternAsync(CacheKeys.ProductPattern, ct);

        return product.ToDto();
    }
}


public sealed class UpdateProductHandler(
    IProductRepository repository,
    ICacheService cache)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        if (await repository.ExistsByNameAsync(cmd.Name, ProductId.From(cmd.Id), ct))
            throw new ProductAlreadyExistsException(cmd.Name);

        product.UpdateDetails(cmd.Name, cmd.Description, cmd.Brand, cmd.Tags, cmd.Images, cmd.Attributes);

        await repository.UpdateAsync(product, ct);
        await InvalidateCacheAsync(product, ct);

        return product.ToDto();
    }

    private async Task InvalidateCacheAsync(Product product, CancellationToken ct)
    {
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);
        await cache.RemoveAsync(CacheKeys.ProductBySlug(product.Slug), ct);
        await cache.RemoveByPatternAsync(CacheKeys.ProductPattern, ct);
    }
}

public sealed class UpdateProductPriceHandler(
    IProductRepository repository,
    ICacheService cache)
    : IRequestHandler<UpdateProductPriceCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductPriceCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        var salePrice = cmd.SalePrice.HasValue
            ? Money.Of(cmd.SalePrice.Value, cmd.Currency)
            : null;

        product.UpdatePrice(Money.Of(cmd.Price, cmd.Currency), salePrice);

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class AdjustStockHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<AdjustStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.AdjustStock(cmd.Delta, cmd.Reason);

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class ReserveStockHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<ReserveStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ReserveStockCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.ReserveStock(cmd.Quantity);

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class ReleaseStockHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<ReleaseStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ReleaseStockCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.ReleaseStock(cmd.Quantity);

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class ActivateProductHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<ActivateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ActivateProductCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.Activate();

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class DeactivateProductHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<DeactivateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(DeactivateProductCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.Deactivate();

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class ArchiveProductHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<ArchiveProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(ArchiveProductCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        product.Archive();

        await repository.UpdateAsync(product, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);

        return product.ToDto();
    }
}

public sealed class DeleteProductHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(ProductId.From(cmd.Id), ct)
            ?? throw new ProductNotFoundException(cmd.Id.ToString());

        await repository.DeleteAsync(product.Id, ct);
        await cache.RemoveAsync(CacheKeys.Product(product.Id.Value.ToString()), ct);
        await cache.RemoveAsync(CacheKeys.ProductBySlug(product.Slug), ct);
        await cache.RemoveByPatternAsync(CacheKeys.ProductPattern, ct);

        return true;
    }
}