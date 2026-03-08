using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.Enums;
using ECP.ProductService.Core.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECP.ProductService.Infrastructure.Persistence.Documents;

/// <summary>
/// MongoDB persistence document for Product.
/// Deliberately separate from the domain aggregate — persistence schema can
/// evolve independently without touching domain logic.
/// </summary>
public sealed class ProductDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid   Id          { get; set; }

    public string Name        { get; set; } = string.Empty;
    public string Slug        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand       { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid   CategoryId  { get; set; }

    // Price stored as flat fields — no nested document (simpler queries)
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal  Price        { get; set; }
    public string   Currency     { get; set; } = string.Empty;
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? SalePrice    { get; set; }
    public string?  SaleCurrency { get; set; }

    // Stock
    public int StockQty      { get; set; }
    public int StockReserved { get; set; }

    public string       Status     { get; set; } = string.Empty;
    public List<string> Tags       { get; set; } = [];
    public List<string> Images     { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int      Version   { get; set; }
}

/// <summary>
/// Converts between ProductDocument and Product domain aggregate.
/// All reflection is isolated here — the rest of the codebase stays clean.
/// </summary>
public static class ProductDocumentMapper
{
    // ── Domain → Document ─────────────────────────────────────────────────────
    public static ProductDocument ToDocument(Product p) => new()
    {
        Id           = p.Id.Value,
        Name         = p.Name,
        Slug         = p.Slug.Value,
        Description  = p.Description,
        Brand        = p.Brand,
        CategoryId   = p.CategoryId.Value,
        Price        = p.Price.Amount,
        Currency     = p.Price.Currency,
        SalePrice    = p.SalePrice?.Amount,
        SaleCurrency = p.SalePrice?.Currency,
        StockQty     = p.Stock.Quantity,
        StockReserved= p.Stock.Reserved,
        Status       = p.Status.ToString(),
        Tags         = p.Tags.ToList(),
        Images       = p.Images.ToList(),
        Attributes   = new Dictionary<string, string>(p.Attributes),
        CreatedAt    = p.CreatedAt,
        UpdatedAt    = p.UpdatedAt,
        Version      = p.Version,
    };

    // ── Document → Domain ─────────────────────────────────────────────────────
    public static Product ToDomain(ProductDocument d) =>
        ProductReconstituter.Reconstitute(
            id:          d.Id,
            name:        d.Name,
            slug:        d.Slug,
            description: d.Description,
            brand:       d.Brand,
            categoryId:  d.CategoryId,
            price:       Money.Of(d.Price, d.Currency),
            salePrice:   d.SalePrice.HasValue ? Money.Of(d.SalePrice.Value, d.SaleCurrency!) : null,
            stock:       StockInfo.Create(d.StockQty, d.StockReserved),
            status:      Enum.Parse<ProductStatus>(d.Status),
            tags:        d.Tags,
            images:      d.Images,
            attributes:  d.Attributes,
            createdAt:   d.CreatedAt,
            updatedAt:   d.UpdatedAt,
            version:     d.Version);
}

/// <summary>
/// Reconstitutes a Product aggregate directly from raw values,
/// bypassing the domain's Create() factory so no business rules re-run on load.
/// Reflection is contained here and nowhere else.
/// </summary>
internal static class ProductReconstituter
{
    private static readonly Type ProductType = typeof(Product);
    private const System.Reflection.BindingFlags All =
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Public;

    public static Product Reconstitute(
        Guid     id,
        string   name,
        string   slug,
        string   description,
        string   brand,
        Guid     categoryId,
        Money    price,
        Money?   salePrice,
        StockInfo stock,
        ProductStatus status,
        List<string> tags,
        List<string> images,
        Dictionary<string,string> attributes,
        DateTime createdAt,
        DateTime updatedAt,
        int      version)
    {
        var p = (Product)System.Runtime.CompilerServices
            .RuntimeHelpers.GetUninitializedObject(ProductType);

        SetProp(p, nameof(Product.Id),          ProductId.From(id));
        SetProp(p, nameof(Product.Name),         name);
        SetProp(p, nameof(Product.Slug),         Slug.Parse(slug));
        SetProp(p, nameof(Product.Description),  description);
        SetProp(p, nameof(Product.Brand),        brand);
        SetProp(p, nameof(Product.CategoryId),   CategoryId.From(categoryId));
        SetProp(p, nameof(Product.Price),        price);
        SetProp(p, nameof(Product.SalePrice),    salePrice);
        SetProp(p, nameof(Product.Stock),        stock);
        SetProp(p, nameof(Product.Status),       status);
        SetProp(p, nameof(Product.Tags),         tags.AsReadOnly());
        SetProp(p, nameof(Product.Images),       images.AsReadOnly());
        SetProp(p, nameof(Product.Attributes),   (IReadOnlyDictionary<string,string>)attributes);
        SetProp(p, nameof(Product.CreatedAt),    createdAt);
        SetProp(p, nameof(Product.UpdatedAt),    updatedAt);
        SetProp(p, nameof(Product.Version),      version);

        return p;
    }

    private static void SetProp(Product p, string name, object? value)
    {
        var field = ProductType.GetField($"<{name}>k__BackingField", All)
            ?? throw new InvalidOperationException($"Backing field for '{name}' not found.");
        field.SetValue(p, value);
    }
}
