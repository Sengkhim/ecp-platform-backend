using MassTransit;

namespace ECP.Saga.Orchestrator.StateData;

public class OrderState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int Version { get; set; }
}
