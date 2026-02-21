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
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid   OrderId       { get; set; }
    public Guid   CustomerId    { get; set; }
    public string CustomerName  { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderNumber   { get; set; } = string.Empty;
    public decimal TotalAmount  { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Currency      { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; }
    public string Items { get; set; } = string.Empty;
    public Guid? PaymentIdempotencyKey { get; set; }
    
    public Guid? InventoryTimeoutTokenId { get; set; }
    public Guid? PaymentTimeoutTokenId { get; set; }

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
    
    /// <summary>UTC timestamp of the last state transition.</summary>
    public DateTime? LastUpdatedAt { get; set; }

    public int Version { get; set; }
}