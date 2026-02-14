using MassTransit;

namespace ECP.Saga.Orchestrator.StateData;

/// <summary>
/// Persisted saga state for an order workflow.
/// Each property maps to a column in the saga repository (e.g. EF Core / Redis).
///
/// Lifecycle:
///   Initial → AwaitingInventory → AwaitingPayment → Completed
///                                                  ↘ Failed (from any step)
/// </summary>
public class OrderState : SagaStateMachineInstance, ISagaVersion
{
    // -------------------------------------------------------------------------
    // MassTransit required
    // -------------------------------------------------------------------------

    /// <summary>Saga correlation identifier — maps 1:1 with OrderId.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Current state name persisted as a string.</summary>
    public string CurrentState { get; set; } = string.Empty;

    // -------------------------------------------------------------------------
    // Order core data
    // -------------------------------------------------------------------------
    public Guid   OrderId       { get; set; }
    public Guid   CustomerId    { get; set; }
    public string CustomerName  { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderNumber   { get; set; } = string.Empty;
    public decimal TotalAmount  { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Currency      { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; }

    /// <summary>
    /// Order items serialized as JSON using source-generated serializer.
    /// Stored as a single column to avoid N-child-row joins at high throughput.
    /// </summary>
    public string Items { get; set; } = string.Empty;

    // -------------------------------------------------------------------------
    // Payment idempotency tracking
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stable idempotency key sent with every <see cref="Contracts.RequestPayment"/> command.
    /// Derived deterministically from <see cref="OrderId"/> via GuidV5 so it is
    /// identical on every replay — survives process restarts and broker retries.
    /// The payment provider uses this key to deduplicate re-submissions and return
    /// the original result without processing the charge a second time.
    /// </summary>
    public Guid? PaymentIdempotencyKey { get; set; }

    // -------------------------------------------------------------------------
    // Timeout scheduler tokens
    // Nullable — only set while waiting for an external service response.
    // Cleared (Unschedule) as soon as the response arrives.
    // -------------------------------------------------------------------------

    /// <summary>Token used to cancel the inventory timeout if inventory responds in time.</summary>
    public Guid? InventoryTimeoutTokenId { get; set; }

    /// <summary>Token used to cancel the payment timeout if payment responds in time.</summary>
    public Guid? PaymentTimeoutTokenId { get; set; }

    // -------------------------------------------------------------------------
    // Error tracking
    // Populated on any failure so the Failed state is self-describing
    // without needing to join against a separate error log table.
    // -------------------------------------------------------------------------

    /// <summary>Step name where the failure occurred (e.g. "CheckInventory").</summary>
    public string? FailedStep { get; set; }

    /// <summary>Human-readable failure reason for ops dashboards / support.</summary>
    public string? FailureReason { get; set; }

    /// <summary>UTC timestamp of when the saga entered the Failed state.</summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Serialized exception detail (type + message, no stack trace).
    /// Populated only when an activity itself throws — not for business failures.
    /// Kept compact to avoid bloating the saga row.
    /// </summary>
    public string? LastExceptionDetail { get; set; }

    // -------------------------------------------------------------------------
    // Audit
    // -------------------------------------------------------------------------

    /// <summary>UTC timestamp of the last state transition.</summary>
    public DateTime? LastUpdatedAt { get; set; }

    public int Version { get; set; }
}