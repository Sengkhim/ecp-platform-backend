using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class InventoryCompensationActivity :
    IStateMachineActivity<OrderState, InventoryFailed>
{
    private readonly ITopicProducer<OrderFailed> _producer;
    private readonly ILogger<InventoryCompensationActivity> _logger;

    public InventoryCompensationActivity(
        ITopicProducer<OrderFailed> producer,
        ILogger<InventoryCompensationActivity> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("inventory-compensation");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, InventoryFailed> context,
        IBehavior<OrderState, InventoryFailed> next)
    {
        var saga = context.Saga;

        var cancelCommand = new OrderFailed(
            saga.OrderId,
            "Inventory reservation failed", "CheckInventory", DateTime.Now);

        await _producer.Produce(cancelCommand);

        _logger.LogWarning(
            "Inventory failed. Order {OrderId} is being cancelled.",
            saga.OrderId);

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryFailed, TException> context,
        IBehavior<OrderState, InventoryFailed> next)
        where TException : Exception
    {
        _logger.LogError(context.Exception,
            "Inventory compensation failed for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}
