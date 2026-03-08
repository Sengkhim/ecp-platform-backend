namespace ECP.ProductService.Application.DTOs;

/// <summary>Full product detail — returned by ID/slug queries and all mutations.</summary>
public sealed record ProductDto(
    Guid     Id,
    string   Name,
    string   Slug,
    string   Description,
    string   Brand,
    Guid     CategoryId,
    decimal  Price,
    string   Currency,
    decimal? SalePrice,
    // stock
    int      StockQuantity,
    int      StockReserved,
    int      StockAvailable,
    bool     IsLowStock,
    // status
    string   Status,
    // classification
    IReadOnlyList<string>              Tags,
    IReadOnlyList<string>              Images,
    IReadOnlyDictionary<string,string> Attributes,
    // audit
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int      Version);

/// <summary>Lightweight summary — used in lists, search results, DataLoader batching.</summary>
public sealed record ProductSummaryDto(
    Guid     Id,
    string   Name,
    string   Slug,
    string   Brand,
    decimal  Price,
    string   Currency,
    decimal? SalePrice,
    string   Status,
    int      StockAvailable,
    string?  PrimaryImage);

/// <summary>Generic cursor-based paginated result.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    long             Total,
    int              Skip,
    int              Take)
{
    public int  PageCount => Take > 0 ? (int)Math.Ceiling((double)Total / Take) : 0;
    public bool HasMore   => Skip + Items.Count < Total;
}
