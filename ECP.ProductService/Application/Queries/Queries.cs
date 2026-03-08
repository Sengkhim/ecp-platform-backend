using ECP.ProductService.Application.DTOs;
using MediatR;

namespace ECP.ProductService.Application.Queries;

public sealed record GetProductByIdQuery(Guid Id)       : IRequest<ProductDto?>;
public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDto?>;

public sealed record GetProductsByCategoryQuery(
    Guid CategoryId,
    int  Skip = 0,
    int  Take = 20
) : IRequest<PagedResult<ProductSummaryDto>>;

public sealed record SearchProductsQuery(
    string?  Keyword    = null,
    Guid?    CategoryId = null,
    string?  Brand      = null,
    decimal? MinPrice   = null,
    decimal? MaxPrice   = null,
    string?  Status     = null,
    string   SortBy     = "createdAt",
    bool     SortDesc   = true,
    int      Skip       = 0,
    int      Take       = 20
) : IRequest<PagedResult<ProductSummaryDto>>;

/// <summary>Used by GraphQL DataLoader to batch-load products in one round trip.</summary>
public sealed record GetProductsByIdsQuery(IReadOnlyList<Guid> Ids)
    : IRequest<IReadOnlyList<ProductDto>>;
