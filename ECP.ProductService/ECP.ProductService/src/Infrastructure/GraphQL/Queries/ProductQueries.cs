using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Queries;
using ECP.ProductService.Infrastructure.GraphQL.DataLoaders;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace ECP.ProductService.Infrastructure.GraphQL.Queries;

[QueryType]
public sealed class ProductQueries
{
    /// <summary>Fetch a single product by UUID. Uses DataLoader for batching.</summary>
    [GraphQLDescription("Fetch a product by its unique ID.")]
    public Task<ProductDto?> GetProduct(
        Guid id,
        ProductByIdDataLoader loader,
        CancellationToken ct)
        => loader.LoadAsync(id, ct)!;

    /// <summary>Fetch a single product by its URL slug.</summary>
    [GraphQLDescription("Fetch a product by its URL slug.")]
    public Task<ProductDto?> GetProductBySlug(
        string slug,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductBySlugQuery(slug), ct);

    /// <summary>Browse products within a category, paginated.</summary>
    [GraphQLDescription("Browse products in a category (paginated, newest first).")]
    public Task<PagedResult<ProductSummaryDto>> GetProductsByCategory(
        Guid categoryId,
        int  skip,
        int  take,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductsByCategoryQuery(categoryId, skip, take), ct);

    /// <summary>Full-text + faceted search across the product catalog.</summary>
    [GraphQLDescription("Search products with full-text keyword, filters, and sorting.")]
    public Task<PagedResult<ProductSummaryDto>> SearchProducts(
        SearchProductsInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new SearchProductsQuery(
            Keyword:    input.Keyword,
            CategoryId: input.CategoryId,
            Brand:      input.Brand,
            MinPrice:   input.MinPrice,
            MaxPrice:   input.MaxPrice,
            Status:     input.Status,
            SortBy:     input.SortBy,
            SortDesc:   input.SortDesc,
            Skip:       input.Skip,
            Take:       input.Take), ct);

    /// <summary>Batch-fetch multiple products. Use the DataLoader for efficiency.</summary>
    [GraphQLDescription("Batch-fetch multiple products by ID list.")]
    public Task<IReadOnlyList<ProductDto>> GetProductsByIds(
        List<Guid> ids,
        [Service] ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductsByIdsQuery(ids), ct);
}
