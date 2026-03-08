namespace ECP.ProductService.Core.Domain.ValueObjects;

/// <summary>Strongly-typed product ID. Prevents passing wrong IDs across boundaries.</summary>
public readonly record struct ProductId(Guid Value)
{
    public static ProductId   New()             => new(Guid.NewGuid());
    public static ProductId   From(Guid g)      => new(g);
    public static ProductId   Parse(string s)   => new(Guid.Parse(s));
    public static bool        TryParse(string s, out ProductId id)
    {
        if (Guid.TryParse(s, out var g)) { id = new(g); return true; }
        id = default; return false;
    }
    public override string ToString() => Value.ToString();
}

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId From(Guid g)    => new(g);
    public static CategoryId Parse(string s) => new(Guid.Parse(s));
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Money value object. Enforces non-negative amount and valid 3-letter ISO currency code.
/// Arithmetic operations check currency compatibility.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    private static readonly HashSet<string> ValidCurrencies =
    [
        "USD","EUR","GBP","KHR","THB","SGD","JPY","CNY","AUD","CAD","HKD","MYR","PHP","VND","IDR"
    ];

    private Money() { Amount = 0; Currency = "USD"; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        var code = (currency ?? "").ToUpperInvariant().Trim();

        if (!ValidCurrencies.Contains(code))
            throw new ArgumentException($"'{currency}' is not a supported currency code.", nameof(currency));

        Amount   = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = code;
    }

    public static Money   Of(decimal amount, string currency) => new(amount, currency);
    public static Money   Zero(string currency)               => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (Amount < other.Amount) throw new InvalidOperationException("Result would be negative.");
        return new Money(Amount - other.Amount, Currency);
    }

    public bool IsLessThan(Money other)    { EnsureSameCurrency(other); return Amount < other.Amount; }
    public bool IsGreaterThan(Money other) { EnsureSameCurrency(other); return Amount > other.Amount; }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}.");
    }

    public bool Equals(Money? other)  => other is not null && Amount == other.Amount && Currency == other.Currency;
    public override bool Equals(object? obj) => obj is Money m && Equals(m);
    public override int  GetHashCode()       => HashCode.Combine(Amount, Currency);
    public override string ToString()        => $"{Amount:F2} {Currency}";
}

/// <summary>Encapsulates stock levels. Prevents reserved > total invariant.</summary>
public sealed class StockInfo
{
    public int Quantity  { get; }
    public int Reserved  { get; }
    public int Available => Quantity - Reserved;
    public bool IsLowStock => Available > 0 && Available <= 5;

    private StockInfo() { }
    private StockInfo(int quantity, int reserved) { Quantity = quantity; Reserved = reserved; }

    public static StockInfo Create(int quantity, int reserved = 0)
    {
        if (quantity < 0)        throw new ArgumentException("Quantity cannot be negative.");
        if (reserved < 0)        throw new ArgumentException("Reserved cannot be negative.");
        if (reserved > quantity) throw new ArgumentException("Reserved cannot exceed quantity.");
        return new(quantity, reserved);
    }

    public StockInfo WithAdjustedQuantity(int delta)
    {
        var newQty = Quantity + delta;
        if (newQty < 0) throw new InvalidOperationException($"Stock would go negative. Current: {Quantity}, Delta: {delta}.");
        return new(newQty, Math.Min(Reserved, newQty));
    }

    public StockInfo WithReserved(int reserved)   => Create(Quantity, reserved);
    public override string ToString()              => $"Qty:{Quantity} Reserved:{Reserved} Available:{Available}";
}

/// <summary>Slug value object — immutable, validated, URL-safe.</summary>
public readonly record struct Slug(string Value)
{
    private static readonly System.Text.RegularExpressions.Regex ValidSlugRegex =
        new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static Slug From(string raw)
    {
        var slug = System.Text.RegularExpressions.Regex
            .Replace(raw.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');

        if (string.IsNullOrEmpty(slug)) throw new ArgumentException("Cannot generate slug from input.");
        return new(slug);
    }

    public static Slug Parse(string value)
    {
        if (!ValidSlugRegex.IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid slug.");
        return new(value);
    }

    public override string ToString() => Value;
}
