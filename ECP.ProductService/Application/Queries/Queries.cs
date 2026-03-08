using ECP.ProductService.Application.DTOs;
using MediatR;

namespace ECP.ProductService.Application.Queries;

public record GetProductByIdQuery(Guid Id)       : IRequest<ProductDto?>;
public record GetProductBySlugQuery(string Slug) : IRequest<ProductDto?>;

public record GetProductsByCategoryQuery(
    Guid CategoryId,
    int  Skip = 0,
    int  Take = 20) : IRequest<PagedResult<ProductSummaryDto>>;

public record SearchProductsQuery(
    string?  Keyword    = null,
    Guid?    CategoryId = null,
    string?  Brand      = null,
    decimal? MinPrice   = null,
    decimal? MaxPrice   = null,
    string?  Status     = null,
    string   SortBy     = "createdAt",
    bool     SortDesc   = true,
    int      Skip       = 0,
    int      Take       = 20) : IRequest<PagedResult<ProductSummaryDto>>;

public record GetProductsByIdsQuery(IEnumerable<Guid> Ids) : IRequest<IReadOnlyList<ProductDto>>;