using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Queries;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace ECP.ProductService.Infrastructure.GraphQL.Queries;

[QueryType]
public sealed class ProductQueries
{
    [GraphQLDescription("Get a product by its unique ID.")]
    public async Task<ProductDto?> GetProduct(
        Guid id,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new GetProductByIdQuery(id), ct);

    [GraphQLDescription("Get a product by its URL slug.")]
    public async Task<ProductDto?> GetProductBySlug(
        string slug,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new GetProductBySlugQuery(slug), ct);

    [GraphQLDescription("Get all products in a category (paginated).")]
    public async Task<PagedResult<ProductSummaryDto>> GetProductsByCategory(
        Guid categoryId,
        int  skip,
        int  take,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new GetProductsByCategoryQuery(categoryId, skip, take), ct);

    [GraphQLDescription("Full-text search across products with filtering and sorting.")]
    public async Task<PagedResult<ProductSummaryDto>> SearchProducts(
        SearchProductsInput input,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new SearchProductsQuery(
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

    [GraphQLDescription("Batch-load multiple products by IDs (for DataLoader scenarios).")]
    public async Task<IReadOnlyList<ProductDto>> GetProductsByIds(
        List<Guid> ids,
        [Service] ISender mediator,
        CancellationToken ct)
        => await mediator.Send(new GetProductsByIdsQuery(ids), ct);
}