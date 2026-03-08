using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.ValueObjects;

namespace ECP.ProductService.Core.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(CategoryId categoryId, int skip, int take, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, long Total)> SearchAsync(ProductSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<ProductId> ids, CancellationToken ct = default);
    Task InsertAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(ProductId id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, ProductId? excludeId = null, CancellationToken ct = default);
}

public record ProductSearchQuery(
    string?  Keyword      = null,
    Guid?    CategoryId   = null,
    string?  Brand        = null,
    decimal? MinPrice     = null,
    decimal? MaxPrice     = null,
    string?  Status       = null,
    string   SortBy       = "createdAt",
    bool     SortDesc     = true,
    int      Skip         = 0,
    int      Take         = 20);