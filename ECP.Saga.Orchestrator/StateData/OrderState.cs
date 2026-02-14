using MassTransit;

namespace ECP.Saga.Orchestrator.StateData;

public class OrderState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public int Version { get; set; }
    
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Currency { get; set; } 
    public required string PaymentMethod { get; set; }
    
    // Idempotency flags
    public bool PaymentRequested { get; set; } = false;
    public bool PaymentRefunded { get; set; } = false;

    // Optional: store PaymentId for compensation
    public Guid PaymentId { get; set; }
    
    // Json
    public string Items { get; set; } = string.Empty;

}
