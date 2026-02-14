using System.Text.Json;
using ECP.OrderService.Application.Contracts.Events;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class CheckInventoryActivity :
    IStateMachineActivity<OrderState, OrderCreatedEvent>
{
    private readonly ITopicProducer<CheckInventoryEvent> _producer;
    private readonly ILogger<CheckInventoryActivity> _logger;

    public CheckInventoryActivity(
        ITopicProducer<CheckInventoryEvent> producer,
        ILogger<CheckInventoryActivity> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("check-inventory");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, OrderCreatedEvent> context,
        IBehavior<OrderState, OrderCreatedEvent> next)
    {
        var saga = context.Saga;

        var items = string.IsNullOrWhiteSpace(saga.Items)
            ? []
            : JsonSerializer.Deserialize<List<OrderItemInfoEvent>>(saga.Items)
              ?? [];

        var checkInventoryEvent = new CheckInventoryEvent(
            saga.OrderId,
            items);

        await _producer.Produce(checkInventoryEvent);

        _logger.LogInformation(
            "Inventory check requested for Order {OrderId} with {ItemCount} items",
            saga.OrderId,
            items.Count);

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, OrderCreatedEvent, TException> context,
        IBehavior<OrderState, OrderCreatedEvent> next)
        where TException : Exception
    {
        _logger.LogError(context.Exception,
            "Inventory check failed for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}