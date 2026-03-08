using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Queries;
using ECP.ProductService.Infrastructure.GraphQL.DataLoaders;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using MediatR;

namespace ECP.ProductService.Infrastructure.GraphQL.Queries;

// No [QueryType] attribute — registered explicitly via .AddQueryType<ProductQueries>()
// combined with a root Query type defined in RootQueryType.cs
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ProductQueries
{
    [GraphQLDescription("Fetch a product by its unique ID.")]
    public Task<ProductDto?> GetProduct(
        Guid id,
        ProductByIdDataLoader loader,
        CancellationToken ct)
        => loader.LoadAsync(id, ct)!;

    [GraphQLDescription("Fetch a product by its URL slug.")]
    public Task<ProductDto?> GetProductBySlug(
        string slug,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductBySlugQuery(slug), ct);

    [GraphQLDescription("Browse products in a category (paginated, newest first).")]
    public Task<PagedResult<ProductSummaryDto>> GetProductsByCategory(
        Guid categoryId,
        int  skip,
        int  take,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductsByCategoryQuery(categoryId, skip, take), ct);

    [GraphQLDescription("Search products with full-text keyword, filters, and sorting.")]
    public Task<PagedResult<ProductSummaryDto>> SearchProducts(
        SearchProductsInput input,
        ISender mediator,
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

    [GraphQLDescription("Batch-fetch multiple products by ID list.")]
    public Task<IReadOnlyList<ProductDto>> GetProductsByIds(
        List<Guid> ids,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductsByIdsQuery(ids), ct);
}