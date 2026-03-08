using ECP.ProductService.Application.DTOs;
using ECP.ProductService.Application.Mappings;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Interfaces.Repositories;

namespace ECP.ProductService.Infrastructure.GraphQL.DataLoaders;

/// <summary>
/// Batches multiple individual product-by-ID fetches into a single MongoDB query.
/// Eliminates the N+1 query problem when resolving products inside nested types
/// (e.g., order lines that each reference a product).
///
/// HotChocolate automatically groups concurrent GetProduct(id) calls within the
/// same execution tick and dispatches them as a single batch here.
/// </summary>
public sealed class ProductByIdDataLoader(
    IProductRepository repository,
    IBatchScheduler scheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, ProductDto>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, ProductDto>> LoadBatchAsync(
        IReadOnlyList<Guid> keys, CancellationToken ct)
    {
        var ids      = keys.Select(ProductId.From).ToList();
        var products = await repository.GetByIdsAsync(ids, ct);

        return products.ToDictionary(p => p.Id.Value, p => p.ToDto());
    }
}