using ECP.ProductService.Core.Domain.Enums;
using ECP.ProductService.Core.Domain.Events;
using ECP.ProductService.Core.Domain.ValueObjects;

namespace ECP.ProductService.Core.Domain.Entities;

/// <summary>
/// Product aggregate root.
///
/// Rules:
///   - All state changes go through domain methods. No public setters.
///   - Business invariants are enforced in every method before state mutates.
///   - Domain events are collected and dispatched AFTER the aggregate is saved
///     so they reflect committed state, not optimistic state.
///   - Version is incremented on every mutation for optimistic concurrency.
/// </summary>
public sealed class Product
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public ProductId  Id         { get; private set; }
    public Slug       Slug       { get; private set; }

    // ── Catalog data ──────────────────────────────────────────────────────────
    public string     Name        { get; private set; } = string.Empty;
    public string     Description { get; private set; } = string.Empty;
    public string     Brand       { get; private set; } = string.Empty;
    public CategoryId CategoryId  { get; private set; }

    // ── Pricing ───────────────────────────────────────────────────────────────
    public Money      Price     { get; private set; } = null!;
    public Money?     SalePrice { get; private set; }

    // ── Stock ─────────────────────────────────────────────────────────────────
    public StockInfo  Stock { get; private set; } = null!;

    // ── Classification ────────────────────────────────────────────────────────
    public ProductStatus            Status     { get; private set; }
    public IReadOnlyList<string>    Tags       { get; private set; } = [];
    public IReadOnlyList<string>    Images     { get; private set; } = [];
    public IReadOnlyDictionary<string, string> Attributes { get; private set; }
        = new Dictionary<string, string>();

    // ── Audit ─────────────────────────────────────────────────────────────────
    public DateTime CreatedAt  { get; private set; }
    public DateTime UpdatedAt  { get; private set; }
    public int      Version    { get; private set; }

    // ── Domain events (dispatched after save, not part of persistence) ────────
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    // ── Private constructor — use factory methods ─────────────────────────────
    private Product() { }

    // -------------------------------------------------------------------------
    // Factory
    // -------------------------------------------------------------------------

    public static Product Create(
        string   name,
        string   description,
        Money    price,
        CategoryId categoryId,
        string   brand,
        int      initialStock,
        IEnumerable<string>?               tags       = null,
        IEnumerable<string>?               images     = null,
        IDictionary<string, string>?       attributes = null)
    {
        GuardName(name);
        GuardBrand(brand);

        if (initialStock < 0)
            throw new ArgumentException("Initial stock cannot be negative.", nameof(initialStock));

        var now = DateTime.UtcNow;

        var product = new Product
        {
            Id          = ProductId.New(),
            Slug        = Slug.From(name),
            Name        = name.Trim(),
            Description = description.Trim(),
            Brand       = brand.Trim(),
            CategoryId  = categoryId,
            Price       = price,
            SalePrice   = null,
            Stock       = StockInfo.Create(initialStock),
            Status      = initialStock > 0 ? ProductStatus.Active : ProductStatus.Draft,
            Tags        = NormaliseTags(tags),
            Images      = images?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)[],
            Attributes  = new Dictionary<string, string>(attributes ?? new Dictionary<string, string>()),
            CreatedAt   = now,
            UpdatedAt   = now,
            Version     = 1,
        };

        product._domainEvents.Add(new ProductCreatedEvent(
            product.Id.Value, product.Name, product.Brand,
            product.CategoryId.Value, product.Price.Amount, product.Price.Currency));

        return product;
    }

    // -------------------------------------------------------------------------
    // Catalog updates
    // -------------------------------------------------------------------------

    public void UpdateDetails(
        string   name,
        string   description,
        string   brand,
        IEnumerable<string>?               tags       = null,
        IEnumerable<string>?               images     = null,
        IDictionary<string, string>?       attributes = null)
    {
        GuardNotArchived();
        GuardName(name);
        GuardBrand(brand);

        Name        = name.Trim();
        Slug        = Slug.From(name);
        Description = description.Trim();
        Brand       = brand.Trim();
        Tags        = NormaliseTags(tags);
        Images      = images?.ToList().AsReadOnly() ?? Images;
        Attributes  = new Dictionary<string, string>(attributes ?? new Dictionary<string, string>());
        Bump();

        _domainEvents.Add(new ProductUpdatedEvent(Id.Value, Name));
    }

    // -------------------------------------------------------------------------
    // Pricing
    // -------------------------------------------------------------------------

    public void UpdatePrice(Money price, Money? salePrice = null)
    {
        GuardNotArchived();

        if (salePrice is not null && !salePrice.IsLessThan(price))
            throw new InvalidOperationException("Sale price must be strictly less than regular price.");

        var old = Price.Amount;
        Price     = price;
        SalePrice = salePrice;
        Bump();

        if (old != price.Amount)
            _domainEvents.Add(new ProductPriceChangedEvent(Id.Value, old, price.Amount, price.Currency));
    }

    // -------------------------------------------------------------------------
    // Stock management
    // -------------------------------------------------------------------------

    public void AdjustStock(int delta, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required for stock adjustments.", nameof(reason));

        var oldQty = Stock.Quantity;
        Stock      = Stock.WithAdjustedQuantity(delta);
        Status     = ResolveActiveStatus();
        Bump();

        _domainEvents.Add(new StockAdjustedEvent(
            Id.Value, oldQty, Stock.Quantity, delta, reason));

        if (Stock.IsLowStock)
            _domainEvents.Add(new LowStockWarningEvent(Id.Value, Name, Stock.Available));
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Reserve quantity must be positive.", nameof(quantity));

        if (quantity > Stock.Available)
            throw new InvalidOperationException(
                $"Cannot reserve {quantity}. Available: {Stock.Available}.");

        Stock = Stock.WithReserved(Stock.Reserved + quantity);
        Bump();

        _domainEvents.Add(new StockReservedEvent(Id.Value, quantity));
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Release quantity must be positive.", nameof(quantity));

        var newReserved = Math.Max(0, Stock.Reserved - quantity);
        Stock = Stock.WithReserved(newReserved);
        Bump();

        _domainEvents.Add(new StockReleasedEvent(Id.Value, quantity));
    }

    // -------------------------------------------------------------------------
    // Status transitions
    // -------------------------------------------------------------------------

    public void Publish()
    {
        if (Status == ProductStatus.Archived)
            throw new InvalidOperationException("Cannot publish an archived product.");

        if (Stock.Quantity == 0)
            throw new InvalidOperationException("Cannot publish a product with zero stock.");

        ChangeStatus(ProductStatus.Active);
    }

    public void Deactivate()
    {
        if (Status == ProductStatus.Archived)
            throw new InvalidOperationException("Cannot deactivate an archived product.");

        ChangeStatus(ProductStatus.Inactive);
    }

    public void Archive()
    {
        if (Status == ProductStatus.Archived) return; // idempotent
        ChangeStatus(ProductStatus.Archived);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void ChangeStatus(ProductStatus newStatus)
    {
        var old = Status.ToString();
        Status  = newStatus;
        Bump();
        _domainEvents.Add(new ProductStatusChangedEvent(Id.Value, old, newStatus.ToString()));
    }

    private ProductStatus ResolveActiveStatus()
    {
        if (Status is ProductStatus.Archived or ProductStatus.Inactive)
            return Status;
        return Stock.Quantity > 0 ? ProductStatus.Active : ProductStatus.OutOfStock;
    }

    private void Bump()
    {
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    private static void GuardName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))       throw new ArgumentException("Name is required.");
        if (name.Length > 200)                      throw new ArgumentException("Name must not exceed 200 characters.");
    }

    private static void GuardBrand(string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))       throw new ArgumentException("Brand is required.");
        if (brand.Length > 100)                     throw new ArgumentException("Brand must not exceed 100 characters.");
    }

    private void GuardNotArchived()
    {
        if (Status == ProductStatus.Archived)
            throw new InvalidOperationException("Cannot modify an archived product.");
    }

    private static IReadOnlyList<string> NormaliseTags(IEnumerable<string>? tags)
        => (tags ?? [])
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList()
            .AsReadOnly();
}
