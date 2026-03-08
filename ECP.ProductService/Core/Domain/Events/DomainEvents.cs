namespace ECP.ProductService.Core.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Raised inside the aggregate, dispatched by the repository after save.
/// Keeps the domain model free of infrastructure dependencies.
/// </summary>
public interface IDomainEvent
{
    Guid     EventId   { get; }
    DateTime OccuredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid     EventId   { get; } = Guid.NewGuid();
    public DateTime OccuredAt { get; } = DateTime.UtcNow;
}

// ── Product lifecycle ─────────────────────────────────────────────────────────

public record ProductCreatedEvent(
    Guid   ProductId,
    string Name,
    string Brand,
    Guid   CategoryId,
    decimal Price,
    string Currency) : DomainEvent;

public record ProductUpdatedEvent(
    Guid   ProductId,
    string Name) : DomainEvent;

public record ProductStatusChangedEvent(
    Guid   ProductId,
    string OldStatus,
    string NewStatus) : DomainEvent;

public record ProductPriceChangedEvent(
    Guid    ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string  Currency) : DomainEvent;

// ── Stock events ──────────────────────────────────────────────────────────────

public record StockAdjustedEvent(
    Guid   ProductId,
    int    OldQuantity,
    int    NewQuantity,
    int    Delta,
    string Reason) : DomainEvent;

public record StockReservedEvent(
    Guid ProductId,
    int  Quantity) : DomainEvent;

public record StockReleasedEvent(
    Guid ProductId,
    int  Quantity) : DomainEvent;

public record LowStockWarningEvent(
    Guid   ProductId,
    string ProductName,
    int    AvailableStock) : DomainEvent;
