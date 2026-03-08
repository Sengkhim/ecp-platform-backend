using ECP.ProductService.Core.Domain.Enums;
using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Core.Exceptions;

namespace ECP.ProductService.Core.Domain.Entities;

/// <summary>
/// Product aggregate root.
/// All state changes go through domain methods — no public setters.
/// </summary>
public sealed class Product
{
    public ProductId    Id          { get; private set; }
    public string       Name        { get; private set; }
    public string       Slug        { get; private set; }
    public string       Description { get; private set; }
    public Money        Price       { get; private set; }
    public Money?       SalePrice   { get; private set; }
    public CategoryId   CategoryId  { get; private set; }
    public string       Brand       { get; private set; }
    public StockInfo    Stock       { get; private set; }
    public ProductStatus Status     { get; private set; }
    public IReadOnlyList<string> Tags   { get; private set; }
    public IReadOnlyList<string> Images { get; private set; }
    public IReadOnlyDictionary<string, string> Attributes { get; private set; }
    public DateTime     CreatedAt   { get; private set; }
    public DateTime     UpdatedAt   { get; private set; }
    public int          Version     { get; private set; }

    private Product() { }

    public static Product Create(
        string name,
        string description,
        Money price,
        CategoryId categoryId,
        string brand,
        int initialStock,
        IEnumerable<string>? tags       = null,
        IEnumerable<string>? images     = null,
        IDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");

        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("Product brand is required.");

        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative.");

        var now = DateTime.UtcNow;

        return new Product
        {
            Id          = ProductId.New(),
            Name        = name.Trim(),
            Slug        = GenerateSlug(name),
            Description = description?.Trim() ?? string.Empty,
            Price       = price,
            CategoryId  = categoryId,
            Brand       = brand.Trim(),
            Stock       = StockInfo.Create(initialStock),
            Status      = initialStock > 0 ? ProductStatus.Active : ProductStatus.OutOfStock,
            Tags        = (tags ?? []).ToList().AsReadOnly(),
            Images      = (images ?? []).ToList().AsReadOnly(),
            Attributes  = new Dictionary<string, string>(attributes ?? new Dictionary<string, string>()),
            CreatedAt   = now,
            UpdatedAt   = now,
            Version     = 1,
        };
    }

    public void UpdateDetails(
        string name,
        string description,
        string brand,
        IEnumerable<string>? tags       = null,
        IEnumerable<string>? images     = null,
        IDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");

        Name        = name.Trim();
        Slug        = GenerateSlug(name);
        Description = description?.Trim() ?? string.Empty;
        Brand       = brand.Trim();
        Tags        = (tags ?? []).ToList().AsReadOnly();
        Images      = (images ?? []).ToList().AsReadOnly();
        Attributes  = new Dictionary<string, string>(attributes ?? new Dictionary<string, string>());
        UpdatedAt   = DateTime.UtcNow;
        Version++;
    }

    public void UpdatePrice(Money price, Money? salePrice = null)
    {
        if (salePrice is not null && salePrice.Amount >= price.Amount)
            throw new DomainException("Sale price must be less than the regular price.");

        Price     = price;
        SalePrice = salePrice;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void AdjustStock(int delta, string reason)
    {
        var newQuantity = Stock.Quantity + delta;

        if (newQuantity < 0)
            throw new DomainException($"Insufficient stock. Available: {Stock.Quantity}, Requested: {Math.Abs(delta)}.");

        Stock     = StockInfo.Create(newQuantity, Stock.Reserved);
        Status    = ResolveStatus();
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Reserve quantity must be positive.");

        var available = Stock.Quantity - Stock.Reserved;
        if (quantity > available)
            throw new DomainException($"Cannot reserve {quantity} units. Available: {available}.");

        Stock     = StockInfo.Create(Stock.Quantity, Stock.Reserved + quantity);
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void ReleaseStock(int quantity)
    {
        var newReserved = Math.Max(0, Stock.Reserved - quantity);
        Stock     = StockInfo.Create(Stock.Quantity, newReserved);
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Activate()
    {
        if (Stock.Quantity == 0)
            throw new DomainException("Cannot activate a product with zero stock.");

        Status    = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Deactivate()
    {
        Status    = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Archive()
    {
        Status    = ProductStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private ProductStatus ResolveStatus()
    {
        if (Status == ProductStatus.Archived || Status == ProductStatus.Inactive)
            return Status;

        return Stock.Quantity > 0 ? ProductStatus.Active : ProductStatus.OutOfStock;
    }

    private static string GenerateSlug(string name)
        => System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');
}