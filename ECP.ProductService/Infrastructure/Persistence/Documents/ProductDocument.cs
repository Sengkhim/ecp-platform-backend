using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Domain.Enums;
using ECP.ProductService.Core.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace ECP.ProductService.Infrastructure.Persistence.Documents;

/// <summary>
/// Pure POCO MongoDB document — NO BsonRepresentation attributes.
/// All type mappings are handled by explicit BsonClassMap registration
/// in MongoDbConfiguration to avoid conflicts with custom value object serializers.
/// </summary>
public sealed class ProductDocument
{
    [BsonId]
    public Guid    Id           { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Slug         { get; set; } = string.Empty;
    public string  Description  { get; set; } = string.Empty;
    public string  Brand        { get; set; } = string.Empty;
    public Guid    CategoryId   { get; set; }
    public decimal Price        { get; set; }
    public string  Currency     { get; set; } = string.Empty;
    public decimal? SalePrice   { get; set; }
    public string?  SaleCurrency { get; set; }
    public int     StockQty     { get; set; }
    public int     StockReserved { get; set; }
    public string  Status       { get; set; } = string.Empty;
    public List<string> Tags    { get; set; } = [];
    public List<string> Images  { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];
    public DateTime CreatedAt   { get; set; }
    public DateTime UpdatedAt   { get; set; }
    public int      Version     { get; set; }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

public static class ProductDocumentMapper
{
    public static ProductDocument ToDocument(Product p) => new()
    {
        Id            = p.Id.Value,
        Name          = p.Name,
        Slug          = p.Slug.Value,
        Description   = p.Description,
        Brand         = p.Brand,
        CategoryId    = p.CategoryId.Value,
        Price         = p.Price.Amount,
        Currency      = p.Price.Currency,
        SalePrice     = p.SalePrice?.Amount,
        SaleCurrency  = p.SalePrice?.Currency,
        StockQty      = p.Stock.Quantity,
        StockReserved = p.Stock.Reserved,
        Status        = p.Status.ToString(),
        Tags          = p.Tags.ToList(),
        Images        = p.Images.ToList(),
        Attributes    = new Dictionary<string, string>(p.Attributes),
        CreatedAt     = p.CreatedAt,
        UpdatedAt     = p.UpdatedAt,
        Version       = p.Version,
    };

    public static Product ToDomain(ProductDocument d) =>
        ProductReconstituter.Reconstitute(
            id:          d.Id,
            name:        d.Name,
            slug:        d.Slug,
            description: d.Description,
            brand:       d.Brand,
            categoryId:  d.CategoryId,
            price:       Money.Of(d.Price, d.Currency),
            salePrice:   d.SalePrice.HasValue
                             ? Money.Of(d.SalePrice.Value, d.SaleCurrency!) : null,
            stock:       StockInfo.Create(d.StockQty, d.StockReserved),
            status:      Enum.Parse<ProductStatus>(d.Status),
            tags:        d.Tags,
            images:      d.Images,
            attributes:  d.Attributes,
            createdAt:   d.CreatedAt,
            updatedAt:   d.UpdatedAt,
            version:     d.Version);
}

// ── Reconstituter ─────────────────────────────────────────────────────────────

internal static class ProductReconstituter
{
    private static readonly Type ProductType = typeof(Product);
    private const System.Reflection.BindingFlags All =
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Public;

    public static Product Reconstitute(
        Guid      id,          string   name,        string   slug,
        string    description, string   brand,       Guid     categoryId,
        Money     price,       Money?   salePrice,   StockInfo stock,
        ProductStatus status,  List<string> tags,    List<string> images,
        Dictionary<string,string> attributes,
        DateTime  createdAt,   DateTime updatedAt,   int version)
    {
        var p = (Product)System.Runtime.CompilerServices
            .RuntimeHelpers.GetUninitializedObject(ProductType);

        Set(p, nameof(Product.Id),         ProductId.From(id));
        Set(p, nameof(Product.Name),        name);
        Set(p, nameof(Product.Slug),        Slug.Parse(slug));
        Set(p, nameof(Product.Description), description);
        Set(p, nameof(Product.Brand),       brand);
        Set(p, nameof(Product.CategoryId),  CategoryId.From(categoryId));
        Set(p, nameof(Product.Price),       price);
        Set(p, nameof(Product.SalePrice),   salePrice);
        Set(p, nameof(Product.Stock),       stock);
        Set(p, nameof(Product.Status),      status);
        Set(p, nameof(Product.Tags),        tags.AsReadOnly());
        Set(p, nameof(Product.Images),      images.AsReadOnly());
        Set(p, nameof(Product.Attributes),  (IReadOnlyDictionary<string,string>)attributes);
        Set(p, nameof(Product.CreatedAt),   createdAt);
        Set(p, nameof(Product.UpdatedAt),   updatedAt);
        Set(p, nameof(Product.Version),     version);

        return p;
    }

    private static void Set(Product p, string propName, object? value)
    {
        var field = ProductType.GetField($"<{propName}>k__BackingField", All)
            ?? throw new InvalidOperationException(
                $"Backing field '<{propName}>k__BackingField' not found on Product.");
        field.SetValue(p, value);
    }
}