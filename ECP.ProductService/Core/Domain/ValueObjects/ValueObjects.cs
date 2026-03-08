using ECP.ProductService.Core.Exceptions;

namespace ECP.ProductService.Core.Domain.ValueObjects;

public sealed record ProductId(Guid Value)
{
    public static ProductId New()  => new(Guid.NewGuid());
    public static ProductId From(Guid value) => new(value);
    public static ProductId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

public sealed record CategoryId(Guid Value)
{
    public static CategoryId From(Guid value)   => new(value);
    public static CategoryId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

public sealed record Money
{
    public decimal  Amount   { get; }
    public string   Currency { get; }

    private Money(string currency)
    {
        Currency = currency;
    }

    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Price cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code (e.g. USD).");

        Amount   = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Of(decimal amount, string currency) => new(amount, currency);
    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        return Currency != other.Currency 
            ? throw new DomainException("Cannot add amounts of different currencies.") 
            : new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}

public sealed record StockInfo
{
    public int Quantity { get; }
    public int Reserved { get; }
    public int Available => Quantity - Reserved;

    private StockInfo() { }

    private StockInfo(int quantity, int reserved)
    {
        Quantity = quantity;
        Reserved = reserved;
    }

    public static StockInfo Create(int quantity, int reserved = 0)
    {
        if (quantity < 0)  throw new DomainException("Stock quantity cannot be negative.");
        if (reserved < 0)  throw new DomainException("Reserved quantity cannot be negative.");
        return reserved > quantity 
            ? throw new DomainException("Reserved cannot exceed total quantity.") 
            : new StockInfo(quantity, reserved);
    }
}