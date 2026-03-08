using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Exceptions;
using ECP.ProductService.Core.Interfaces.Repositories;
using ECP.ProductService.Infrastructure.Persistence.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ECP.ProductService.Infrastructure.Persistence.Repositories;

public sealed class MongoProductRepository : IProductRepository
{
    private readonly IMongoCollection<ProductDocument> _col;

    public MongoProductRepository(IMongoDatabase db)
    {
        _col = db.GetCollection<ProductDocument>("products");
        EnsureIndexes();
    }

    // ── Reads ─────────────────────────────────────────────────────────────────

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
    {
        var doc = await _col.Find(x => x.Id == id.Value).FirstOrDefaultAsync(ct);
        return doc is null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    public async Task<Product?> GetBySlugAsync(Slug slug, CancellationToken ct = default)
    {
        var doc = await _col.Find(x => x.Slug == slug.Value).FirstOrDefaultAsync(ct);
        return doc is null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        CategoryId categoryId, int skip, int take, CancellationToken ct = default)
    {
        var docs = await _col
            .Find(x => x.CategoryId == categoryId.Value && x.Status != "Archived")
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip).Limit(take)
            .ToListAsync(ct);

        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Product>, long)> SearchAsync(
        ProductSearchCriteria c, CancellationToken ct = default)
    {
        var filter = BuildFilter(c);
        var sort   = BuildSort(c.SortBy, c.SortDesc);

        var total = await _col.CountDocumentsAsync(filter, cancellationToken: ct);
        var docs  = await _col.Find(filter).Sort(sort)
            .Skip(c.Skip).Limit(c.Take)
            .ToListAsync(ct);

        return (docs.Select(ProductDocumentMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyList<ProductId> ids, CancellationToken ct = default)
    {
        var guids = ids.Select(x => x.Value).ToList();
        var docs  = await _col.Find(x => guids.Contains(x.Id)).ToListAsync(ct);
        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    // ── Writes ────────────────────────────────────────────────────────────────

    public Task InsertAsync(Product product, CancellationToken ct = default)
        => _col.InsertOneAsync(ProductDocumentMapper.ToDocument(product), cancellationToken: ct);

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        var doc    = ProductDocumentMapper.ToDocument(product);

        // Optimistic concurrency: update only when version matches the pre-increment value
        var filter = Builders<ProductDocument>.Filter.Where(
            x => x.Id == product.Id.Value && x.Version == product.Version - 1);

        var result = await _col.ReplaceOneAsync(filter, doc, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConcurrencyException(product.Id.Value.ToString());
    }

    public Task DeleteAsync(ProductId id, CancellationToken ct = default)
        => _col.DeleteOneAsync(x => x.Id == id.Value, ct);

    public async Task<bool> ExistsByNameAsync(
        string name, ProductId? excludeId = null, CancellationToken ct = default)
    {
        // Case-insensitive exact-name match
        var filter = Builders<ProductDocument>.Filter
            .Regex(x => x.Name, new BsonRegularExpression(
                $"^{System.Text.RegularExpressions.Regex.Escape(name.Trim())}$", "i"));

        if (excludeId is not null)
            filter &= Builders<ProductDocument>.Filter.Ne(x => x.Id, excludeId.Value);

        return await _col.CountDocumentsAsync(filter, cancellationToken: ct) > 0;
    }

    // ── Filter builder ────────────────────────────────────────────────────────

    private static FilterDefinition<ProductDocument> BuildFilter(ProductSearchCriteria c)
    {
        var b       = Builders<ProductDocument>.Filter;
        var filters = new List<FilterDefinition<ProductDocument>>();

        if (!string.IsNullOrWhiteSpace(c.Keyword))
            filters.Add(b.Or(
                b.Regex(x => x.Name,        new BsonRegularExpression(c.Keyword, "i")),
                b.Regex(x => x.Description, new BsonRegularExpression(c.Keyword, "i")),
                b.Regex(x => x.Brand,       new BsonRegularExpression(c.Keyword, "i"))));

        if (c.CategoryId.HasValue)
            filters.Add(b.Eq(x => x.CategoryId, c.CategoryId.Value));

        if (!string.IsNullOrWhiteSpace(c.Brand))
            filters.Add(b.Regex(x => x.Brand, new BsonRegularExpression(c.Brand, "i")));

        if (c.MinPrice.HasValue)
            filters.Add(b.Gte(x => x.Price, c.MinPrice.Value));

        if (c.MaxPrice.HasValue)
            filters.Add(b.Lte(x => x.Price, c.MaxPrice.Value));

        if (!string.IsNullOrWhiteSpace(c.Status))
            filters.Add(b.Eq(x => x.Status, c.Status));
        else
            filters.Add(b.Ne(x => x.Status, "Archived")); // never return archived by default

        return filters.Count > 0 ? b.And(filters) : b.Empty;
    }

    private static SortDefinition<ProductDocument> BuildSort(string sortBy, bool desc)
    {
        var b = Builders<ProductDocument>.Sort;
        return sortBy.ToLowerInvariant() switch
        {
            "name"      => desc ? b.Descending(x => x.Name)      : b.Ascending(x => x.Name),
            "price"     => desc ? b.Descending(x => x.Price)     : b.Ascending(x => x.Price),
            "updatedat" => desc ? b.Descending(x => x.UpdatedAt) : b.Ascending(x => x.UpdatedAt),
            _           => desc ? b.Descending(x => x.CreatedAt) : b.Ascending(x => x.CreatedAt),
        };
    }

    // ── Index bootstrap ───────────────────────────────────────────────────────

    private void EnsureIndexes()
    {
        var k = Builders<ProductDocument>.IndexKeys;
        _col.Indexes.CreateMany([
            new CreateIndexModel<ProductDocument>(
                k.Ascending(x => x.Slug),
                new CreateIndexOptions { Unique = true, Name = "idx_slug_unique" }),
            new CreateIndexModel<ProductDocument>(
                k.Ascending(x => x.CategoryId).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "idx_category_created" }),
            new CreateIndexModel<ProductDocument>(
                k.Text(x => x.Name).Text(x => x.Description).Text(x => x.Brand),
                new CreateIndexOptions { Name = "idx_text_search" }),
            new CreateIndexModel<ProductDocument>(
                k.Ascending(x => x.Status),
                new CreateIndexOptions { Name = "idx_status" }),
            new CreateIndexModel<ProductDocument>(
                k.Ascending(x => x.Brand),
                new CreateIndexOptions { Name = "idx_brand" }),
            new CreateIndexModel<ProductDocument>(
                k.Ascending(x => x.Price),
                new CreateIndexOptions { Name = "idx_price" }),
        ]);
    }
}
