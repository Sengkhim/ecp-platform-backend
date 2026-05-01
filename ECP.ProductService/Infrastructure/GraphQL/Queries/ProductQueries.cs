using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Queries;
using ECP.ProductService.Infrastructure.GraphQL.DataLoaders;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using HotChocolate.Types;
using MediatR;

namespace ECP.ProductService.Infrastructure.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ProductQueries
{
    // HC strips "Get" prefix by default → field name becomes "product"
    // Use [GraphQLName] to make names explicit and predictable.

    [GraphQLName("product")]
    [GraphQLDescription("Fetch a product by its unique ID.")]
    public Task<ProductDto?> GetProduct(
        Guid id,
        ProductByIdDataLoader loader,
        CancellationToken ct)
        => loader.LoadAsync(id, ct)!;

    [GraphQLName("productBySlug")]
    [GraphQLDescription("Fetch a product by its URL slug.")]
    public Task<ProductDto?> GetProductBySlug(
        string slug,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductBySlugQuery(slug), ct);

    [GraphQLName("productsByCategory")]
    [GraphQLDescription("Browse products in a category (paginated, newest first).")]
    public Task<PagedResult<ProductSummaryDto>> GetProductsByCategory(
        Guid categoryId,
        int  skip = 0,
        int  take = 20,
        ISender mediator = default!,
        CancellationToken ct = default)
        => mediator.Send(new GetProductsByCategoryQuery(categoryId, skip, take), ct);

    [GraphQLName("products")]
    [GraphQLDescription("Get all products (paginated). Use searchProducts for filtering.")]
    public Task<PagedResult<ProductSummaryDto>> GetAllProducts(
        int skip = 0,
        int take = 20,
        ISender mediator = default!,
        CancellationToken ct = default)
        => mediator.Send(new GetAllProductsQuery(skip, take), ct);

    [GraphQLName("searchProducts")]
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

    [GraphQLName("productsByIds")]
    [GraphQLDescription("Batch-fetch multiple products by ID list.")]
    public Task<IReadOnlyList<ProductDto>> GetProductsByIds(
        List<Guid> ids,
        ISender mediator,
        CancellationToken ct)
        => mediator.Send(new GetProductsByIdsQuery(ids), ct);
}