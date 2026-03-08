using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.Enums;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Interfaces.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ECP.ProductService.Infrastructure.Persistence;

// ── Document ──────────────────────────────────────────────────────────────────

public sealed class ProductDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid   Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Slug        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price      { get; set; }
    public string Currency    { get; set; } = string.Empty;
    public decimal? SalePrice { get; set; }
    public string? SaleCurrency { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid   CategoryId  { get; set; }
    public string Brand       { get; set; } = string.Empty;
    public int    StockQty    { get; set; }
    public int    StockReserved { get; set; }
    public string Status      { get; set; } = string.Empty;
    public List<string> Tags  { get; set; } = [];
    public List<string> Images { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version        { get; set; }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

public static class ProductDocumentMapper
{
    public static ProductDocument ToDocument(Product p) => new()
    {
        Id          = p.Id.Value,
        Name        = p.Name,
        Slug        = p.Slug,
        Description = p.Description,
        Price       = p.Price.Amount,
        Currency    = p.Price.Currency,
        SalePrice   = p.SalePrice?.Amount,
        SaleCurrency = p.SalePrice?.Currency,
        CategoryId  = p.CategoryId.Value,
        Brand       = p.Brand,
        StockQty    = p.Stock.Quantity,
        StockReserved = p.Stock.Reserved,
        Status      = p.Status.ToString(),
        Tags        = p.Tags.ToList(),
        Images      = p.Images.ToList(),
        Attributes  = new Dictionary<string, string>(p.Attributes),
        CreatedAt   = p.CreatedAt,
        UpdatedAt   = p.UpdatedAt,
        Version     = p.Version,
    };

    public static Product ToDomain(ProductDocument doc)
    {
        // Use reflection-free factory that reconstructs the aggregate
        return ProductFactory.Reconstitute(
            id:          Guid.Parse(doc.Id.ToString()),
            name:        doc.Name,
            slug:        doc.Slug,
            description: doc.Description,
            price:       Money.Of(doc.Price, doc.Currency),
            salePrice:   doc.SalePrice.HasValue ? Money.Of(doc.SalePrice.Value, doc.SaleCurrency!) : null,
            categoryId:  CategoryId.From(doc.CategoryId),
            brand:       doc.Brand,
            stock:       StockInfo.Create(doc.StockQty, doc.StockReserved),
            status:      Enum.Parse<ProductStatus>(doc.Status),
            tags:        doc.Tags,
            images:      doc.Images,
            attributes:  doc.Attributes,
            createdAt:   doc.CreatedAt,
            updatedAt:   doc.UpdatedAt,
            version:     doc.Version);
    }
}

// ── Repository ────────────────────────────────────────────────────────────────

public sealed class MongoProductRepository : IProductRepository
{
    private readonly IMongoCollection<ProductDocument> _collection;

    public MongoProductRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ProductDocument>("products");
        EnsureIndexes();
    }

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
    {
        var doc = await _collection.Find(x => x.Id == id.Value).FirstOrDefaultAsync(ct);
        return doc is null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var doc = await _collection.Find(x => x.Slug == slug).FirstOrDefaultAsync(ct);
        return doc is null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        CategoryId categoryId, int skip, int take, CancellationToken ct = default)
    {
        var docs = await _collection
            .Find(x => x.CategoryId == categoryId.Value)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip).Limit(take)
            .ToListAsync(ct);

        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Product> Items, long Total)> SearchAsync(
        ProductSearchQuery query, CancellationToken ct = default)
    {
        var filter = BuildSearchFilter(query);
        var sort   = BuildSort(query.SortBy, query.SortDesc);

        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var docs  = await _collection.Find(filter)
            .Sort(sort)
            .Skip(query.Skip).Limit(query.Take)
            .ToListAsync(ct);

        return (docs.Select(ProductDocumentMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids, CancellationToken ct = default)
    {
        var guids = ids.Select(x => x.Value).ToList();
        var docs  = await _collection.Find(x => guids.Contains(x.Id)).ToListAsync(ct);
        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    public async Task InsertAsync(Product product, CancellationToken ct = default)
    {
        var doc = ProductDocumentMapper.ToDocument(product);
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        var doc = ProductDocumentMapper.ToDocument(product);
        // Optimistic concurrency: only update if version matches
        var filter = Builders<ProductDocument>.Filter.Where(
            x => x.Id == product.Id.Value && x.Version == product.Version - 1);

        var result = await _collection.ReplaceOneAsync(filter, doc, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new Exception($"Concurrency conflict updating product {product.Id.Value}.");
    }

    public async Task DeleteAsync(ProductId id, CancellationToken ct = default)
        => await _collection.DeleteOneAsync(x => x.Id == id.Value, ct);

    public async Task<bool> ExistsByNameAsync(string name, ProductId? excludeId = null, CancellationToken ct = default)
    {
        var filter = Builders<ProductDocument>.Filter.Regex(
            x => x.Name, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i"));

        if (excludeId is not null)
            filter &= Builders<ProductDocument>.Filter.Ne(x => x.Id, excludeId.Value);

        return await _collection.CountDocumentsAsync(filter, cancellationToken: ct) > 0;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static FilterDefinition<ProductDocument> BuildSearchFilter(ProductSearchQuery q)
    {
        var builder = Builders<ProductDocument>.Filter;
        var filters = new List<FilterDefinition<ProductDocument>>();

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            filters.Add(builder.Or(
                builder.Regex(x => x.Name,        new BsonRegularExpression(q.Keyword, "i")),
                builder.Regex(x => x.Description, new BsonRegularExpression(q.Keyword, "i")),
                builder.Regex(x => x.Brand,       new BsonRegularExpression(q.Keyword, "i"))));
        }

        if (q.CategoryId.HasValue)
            filters.Add(builder.Eq(x => x.CategoryId, q.CategoryId.Value));

        if (!string.IsNullOrWhiteSpace(q.Brand))
            filters.Add(builder.Regex(x => x.Brand, new BsonRegularExpression(q.Brand, "i")));

        if (q.MinPrice.HasValue)
            filters.Add(builder.Gte(x => x.Price, q.MinPrice.Value));

        if (q.MaxPrice.HasValue)
            filters.Add(builder.Lte(x => x.Price, q.MaxPrice.Value));

        if (!string.IsNullOrWhiteSpace(q.Status))
            filters.Add(builder.Eq(x => x.Status, q.Status));

        return filters.Count > 0 ? builder.And(filters) : builder.Empty;
    }

    private static SortDefinition<ProductDocument> BuildSort(string sortBy, bool desc)
    {
        var builder = Builders<ProductDocument>.Sort;
        return sortBy.ToLower() switch
        {
            "name"      => desc ? builder.Descending(x => x.Name)      : builder.Ascending(x => x.Name),
            "price"     => desc ? builder.Descending(x => x.Price)     : builder.Ascending(x => x.Price),
            "updatedat" => desc ? builder.Descending(x => x.UpdatedAt) : builder.Ascending(x => x.UpdatedAt),
            _           => desc ? builder.Descending(x => x.CreatedAt) : builder.Ascending(x => x.CreatedAt),
        };
    }

    private void EnsureIndexes()
    {
        var keys = Builders<ProductDocument>.IndexKeys;
        _collection.Indexes.CreateMany([
            new CreateIndexModel<ProductDocument>(keys.Ascending(x => x.Slug),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<ProductDocument>(keys.Ascending(x => x.CategoryId)),
            new CreateIndexModel<ProductDocument>(keys.Text(x => x.Name).Text(x => x.Description).Text(x => x.Brand)),
            new CreateIndexModel<ProductDocument>(keys.Ascending(x => x.Status)),
            new CreateIndexModel<ProductDocument>(keys.Ascending(x => x.Brand)),
            new CreateIndexModel<ProductDocument>(keys.Descending(x => x.CreatedAt)),
        ]);
    }
}