using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Mappings;
using ECP.ProductService.Application.Queries;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Interfaces.Cache;
using ECP.ProductService.Core.Interfaces.Repositories;
using MediatR;

namespace ECP.ProductService.Application.Handlers;

public sealed class GetProductByIdHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery query, CancellationToken ct)
    {
        var cacheKey = CacheKeys.Product(query.Id.ToString());

        return await cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var product = await repository.GetByIdAsync(ProductId.From(query.Id), ct);
                return product?.ToDto()!;
            },
            expiry: TimeSpan.FromMinutes(10),
            ct: ct);
    }
}

public sealed class GetProductBySlugHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<GetProductBySlugQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductBySlugQuery query, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ProductBySlug(query.Slug);

        return await cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var product = await repository.GetBySlugAsync(query.Slug, ct);
                return product?.ToDto()!;
            },
            expiry: TimeSpan.FromMinutes(10),
            ct: ct);
    }
}

public sealed class GetProductsByCategoryHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<GetProductsByCategoryQuery, PagedResult<ProductSummaryDto>>
{
    public async Task<PagedResult<ProductSummaryDto>> Handle(GetProductsByCategoryQuery query, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ProductCategory(query.CategoryId.ToString(), query.Skip, query.Take);

        return await cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var products = await repository.GetByCategoryAsync(
                    CategoryId.From(query.CategoryId), query.Skip, query.Take, ct);

                var items = products.Select(p => p.ToSummaryDto()).ToList();
                return new PagedResult<ProductSummaryDto>(items, items.Count, query.Skip, query.Take);
            },
            expiry: TimeSpan.FromMinutes(5),
            ct: ct);
    }
}

public sealed class SearchProductsHandler(IProductRepository repository, ICacheService cache)
    : IRequestHandler<SearchProductsQuery, PagedResult<ProductSummaryDto>>
{
    public async Task<PagedResult<ProductSummaryDto>> Handle(SearchProductsQuery query, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ProductSearch(HashQuery(query));

        return await cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var (products, total) = await repository.SearchAsync(new ProductSearchQuery(
                    Keyword:    query.Keyword,
                    CategoryId: query.CategoryId,
                    Brand:      query.Brand,
                    MinPrice:   query.MinPrice,
                    MaxPrice:   query.MaxPrice,
                    Status:     query.Status,
                    SortBy:     query.SortBy,
                    SortDesc:   query.SortDesc,
                    Skip:       query.Skip,
                    Take:       query.Take), ct);

                var items = products.Select(p => p.ToSummaryDto()).ToList();
                return new PagedResult<ProductSummaryDto>(items, total, query.Skip, query.Take);
            },
            expiry: TimeSpan.FromMinutes(2),
            ct: ct);
    }

    private static string HashQuery(SearchProductsQuery q)
    {
        var json  = JsonSerializer.Serialize(q);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16];
    }
}

public sealed class GetProductsByIdsHandler(IProductRepository repository)
    : IRequestHandler<GetProductsByIdsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsByIdsQuery query, CancellationToken ct)
    {
        var ids      = query.Ids.Select(ProductId.From).ToList();
        var products = await repository.GetByIdsAsync(ids, ct);
        return products.Select(p => p.ToDto()).ToList();
    }
}