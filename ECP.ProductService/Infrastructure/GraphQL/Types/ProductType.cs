using ECP.ProductService.Application.DTOs;
using HotChocolate.Types;

namespace ECP.ProductService.Infrastructure.GraphQL.Types;

public sealed class ProductType : ObjectType<ProductDto>
{
    protected override void Configure(IObjectTypeDescriptor<ProductDto> descriptor)
    {
        descriptor.Description("A product in the catalog.");

        descriptor.Field(x => x.Id).Description("Unique product identifier.");
        descriptor.Field(x => x.Name).Description("Product display name.");
        descriptor.Field(x => x.Slug).Description("URL-friendly slug.");
        descriptor.Field(x => x.Description).Description("Full product description.");
        descriptor.Field(x => x.Price).Description("Regular price.");
        descriptor.Field(x => x.Currency).Description("ISO 4217 currency code.");
        descriptor.Field(x => x.SalePrice).Description("Discounted sale price, if any.");
        descriptor.Field(x => x.Brand).Description("Manufacturer / brand name.");
        descriptor.Field(x => x.Status).Description("Product availability status.");
        descriptor.Field(x => x.StockAvailable).Description("Units available for purchase.");
        descriptor.Field(x => x.StockQuantity).Description("Total units in warehouse.");
        descriptor.Field(x => x.StockReserved).Description("Units reserved (in pending orders).");
        descriptor.Field(x => x.Tags).Description("Searchable tags.");
        descriptor.Field(x => x.Images).Description("Ordered list of image URLs.");
        descriptor.Field(x => x.Attributes).Description("Key-value product specifications.");
        descriptor.Field(x => x.Version).Description("Optimistic concurrency version.");
    }
}

public sealed class ProductSummaryType : ObjectType<ProductSummaryDto>
{
    protected override void Configure(IObjectTypeDescriptor<ProductSummaryDto> descriptor)
    {
        descriptor.Description("Lightweight product summary for lists and search results.");
    }
}

// ── Input types ───────────────────────────────────────────────────────────────

public record CreateProductInput(
    string   Name,
    string   Description,
    decimal  Price,
    string   Currency,
    Guid     CategoryId,
    string   Brand,
    int      InitialStock,
    List<string>?                 Tags       = null,
    List<string>?                 Images     = null,
    Dictionary<string, string>?   Attributes = null);

public record UpdateProductInput(
    Guid     Id,
    string   Name,
    string   Description,
    string   Brand,
    List<string>?                 Tags       = null,
    List<string>?                 Images     = null,
    Dictionary<string, string>?   Attributes = null);

public record UpdatePriceInput(
    Guid     Id,
    decimal  Price,
    string   Currency,
    decimal? SalePrice = null);

public record AdjustStockInput(
    Guid   Id,
    int    Delta,
    string Reason);

public record ReserveStockInput(Guid Id, int Quantity);
public record ReleaseStockInput(Guid Id, int Quantity);

public record SearchProductsInput(
    string?  Keyword    = null,
    Guid?    CategoryId = null,
    string?  Brand      = null,
    decimal? MinPrice   = null,
    decimal? MaxPrice   = null,
    string?  Status     = null,
    string   SortBy     = "createdAt",
    bool     SortDesc   = true,
    int      Skip       = 0,
    int      Take       = 20);