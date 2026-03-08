namespace ECP.ProductService.Application.DTOs;

public record ProductDto(
    Guid     Id,
    string   Name,
    string   Slug,
    string   Description,
    decimal  Price,
    string   Currency,
    decimal? SalePrice,
    Guid     CategoryId,
    string   Brand,
    int      StockQuantity,
    int      StockReserved,
    int      StockAvailable,
    string   Status,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Images,
    IReadOnlyDictionary<string, string> Attributes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int      Version);

public record ProductSummaryDto(
    Guid    Id,
    string  Name,
    string  Slug,
    decimal Price,
    string  Currency,
    decimal? SalePrice,
    string  Brand,
    string  Status,
    int     StockAvailable,
    string? PrimaryImage);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    long             Total,
    int              Skip,
    int              Take)
{
    public bool HasMore => Skip + Items.Count < Total;
}