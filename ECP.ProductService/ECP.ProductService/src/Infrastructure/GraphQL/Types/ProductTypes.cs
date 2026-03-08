using ECP.ProductService.Application.DTOs;
using HotChocolate.Types;

namespace ECP.ProductService.Infrastructure.GraphQL.Types;

// ── Object types ──────────────────────────────────────────────────────────────

public sealed class ProductType : ObjectType<ProductDto>
{
    protected override void Configure(IObjectTypeDescriptor<ProductDto> d)
    {
        d.Description("A product in the catalog with full detail.");

        d.Field(x => x.Id)             .Description("Unique product identifier (UUID).");
        d.Field(x => x.Name)           .Description("Display name.");
        d.Field(x => x.Slug)           .Description("URL-safe identifier, derived from name.");
        d.Field(x => x.Description)    .Description("Full product description.");
        d.Field(x => x.Brand)          .Description("Manufacturer or brand name.");
        d.Field(x => x.CategoryId)     .Description("Category UUID.");
        d.Field(x => x.Price)          .Description("Regular selling price.");
        d.Field(x => x.Currency)       .Description("ISO 4217 currency code (e.g. USD).");
        d.Field(x => x.SalePrice)      .Description("Discounted sale price, null if not on sale.");
        d.Field(x => x.StockQuantity)  .Description("Total units physically in warehouse.");
        d.Field(x => x.StockReserved)  .Description("Units reserved by pending orders.");
        d.Field(x => x.StockAvailable) .Description("Units available for new orders.");
        d.Field(x => x.IsLowStock)     .Description("True when available stock ≤ 5.");
        d.Field(x => x.Status)         .Description("Draft | Active | Inactive | OutOfStock | Archived");
        d.Field(x => x.Tags)           .Description("Normalised, deduplicated lowercase tags.");
        d.Field(x => x.Images)         .Description("Ordered list of image URLs.");
        d.Field(x => x.Attributes)     .Description("Key-value product specifications.");
        d.Field(x => x.Version)        .Description("Optimistic concurrency version number.");
        d.Field(x => x.CreatedAt)      .Description("UTC creation timestamp.");
        d.Field(x => x.UpdatedAt)      .Description("UTC last-updated timestamp.");
    }
}

public sealed class ProductSummaryType : ObjectType<ProductSummaryDto>
{
    protected override void Configure(IObjectTypeDescriptor<ProductSummaryDto> d)
    {
        d.Description("Lightweight product summary for lists and search results.");
    }
}

public sealed class PagedProductResultType : ObjectType<PagedResult<ProductSummaryDto>>
{
    protected override void Configure(IObjectTypeDescriptor<PagedResult<ProductSummaryDto>> d)
    {
        d.Description("Paginated list of product summaries.");
        d.Field(x => x.Items)     .Description("Products on the current page.");
        d.Field(x => x.Total)     .Description("Total number of matching products.");
        d.Field(x => x.Skip)      .Description("Number of products skipped.");
        d.Field(x => x.Take)      .Description("Requested page size.");
        d.Field(x => x.HasMore)   .Description("True if more pages exist.");
        d.Field(x => x.PageCount) .Description("Total number of pages.");
    }
}

// ── Mutation inputs ───────────────────────────────────────────────────────────

public record CreateProductInput(
    string  Name,
    string  Description,
    decimal Price,
    string  Currency,
    Guid    CategoryId,
    string  Brand,
    int     InitialStock,
    List<string>?              Tags       = null,
    List<string>?              Images     = null,
    Dictionary<string,string>? Attributes = null);

public record UpdateProductInput(
    Guid    Id,
    string  Name,
    string  Description,
    string  Brand,
    List<string>?              Tags       = null,
    List<string>?              Images     = null,
    Dictionary<string,string>? Attributes = null);

public record UpdatePriceInput(
    Guid     Id,
    decimal  Price,
    string   Currency,
    decimal? SalePrice = null);

public record AdjustStockInput(Guid Id, int Delta, string Reason);
public record ReserveStockInput(Guid Id, int Quantity);
public record ReleaseStockInput(Guid Id, int Quantity);

// ── Query filter input ────────────────────────────────────────────────────────

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
