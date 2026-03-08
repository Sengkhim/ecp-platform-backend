using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Mappings;
using ECP.ProductService.Application.Queries;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Exceptions;
using ECP.ProductService.Core.Interfaces.Cache;
using ECP.ProductService.Core.Interfaces.Repositories;
using MediatR;

namespace ECP.ProductService.Application.Handlers;

public sealed class GetProductByIdHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public Task<ProductDto?> Handle(GetProductByIdQuery q, CancellationToken ct)
        => cache.GetOrSetAsync<ProductDto>(
            CacheKey.ById(q.Id),
            async _ =>
            {
                var p = await repo.GetByIdAsync(ProductId.From(q.Id), ct);
                return p?.ToDto()!;
            },
            ttl: TimeSpan.FromMinutes(10), ct: ct)!;
}

public sealed class GetProductBySlugHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<GetProductBySlugQuery, ProductDto?>
{
    public Task<ProductDto?> Handle(GetProductBySlugQuery q, CancellationToken ct)
        => cache.GetOrSetAsync<ProductDto>(
            CacheKey.BySlug(q.Slug),
            async _ =>
            {
                var p = await repo.GetBySlugAsync(Slug.Parse(q.Slug), ct);
                return p?.ToDto()!;
            },
            ttl: TimeSpan.FromMinutes(10), ct: ct)!;
}

public sealed class GetProductsByCategoryHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<GetProductsByCategoryQuery, PagedResult<ProductSummaryDto>>
{
    public Task<PagedResult<ProductSummaryDto>> Handle(GetProductsByCategoryQuery q, CancellationToken ct)
        => cache.GetOrSetAsync(
            CacheKey.ByCategory(q.CategoryId, q.Skip, q.Take),
            async _ =>
            {
                var products = await repo.GetByCategoryAsync(
                    CategoryId.From(q.CategoryId), q.Skip, q.Take, ct);

                var items = products.Select(p => p.ToSummaryDto()).ToList();
                return new PagedResult<ProductSummaryDto>(items, items.Count, q.Skip, q.Take);
            },
            ttl: TimeSpan.FromMinutes(5), ct: ct);
}

public sealed class SearchProductsHandler(
    IProductRepository repo,
    ICacheService cache)
    : IRequestHandler<SearchProductsQuery, PagedResult<ProductSummaryDto>>
{
    public Task<PagedResult<ProductSummaryDto>> Handle(SearchProductsQuery q, CancellationToken ct)
    {
        var cacheKey = CacheKey.Search(StableHash(q));

        return cache.GetOrSetAsync(
            cacheKey,
            async _ =>
            {
                var (products, total) = await repo.SearchAsync(new ProductSearchCriteria(
                    q.Keyword, q.CategoryId, q.Brand,
                    q.MinPrice, q.MaxPrice, q.Status,
                    q.SortBy, q.SortDesc, q.Skip, q.Take), ct);

                var items = products.Select(p => p.ToSummaryDto()).ToList();
                return new PagedResult<ProductSummaryDto>(items, total, q.Skip, q.Take);
            },
            ttl: TimeSpan.FromMinutes(2), ct: ct);
    }

    private static string StableHash(SearchProductsQuery q)
    {
        var json  = JsonSerializer.Serialize(q);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16];
    }
}

public sealed class GetProductsByIdsHandler(IProductRepository repo)
    : IRequestHandler<GetProductsByIdsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsByIdsQuery q, CancellationToken ct)
    {
        var ids      = q.Ids.Select(ProductId.From).ToList();
        var products = await repo.GetByIdsAsync(ids, ct);
        return products.Select(p => p.ToDto()).ToList();
    }
}
