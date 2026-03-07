using System.Text.Json;
using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.Saga.Orchestrator.Activities;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public sealed class CheckInventoryActivity :
    IStateMachineActivity<OrderState, OrderCreatedEvent>
{
    private readonly ITopicProducer<CheckInventoryEvent> _producer;
    private readonly SagaErrorLogger _errorLogger;
    private readonly ILogger<CheckInventoryActivity> _logger;

    public CheckInventoryActivity(
        ITopicProducer<CheckInventoryEvent> producer,
        SagaErrorLogger errorLogger,
        ILogger<CheckInventoryActivity> logger)
    {
        _producer    = producer;
        _errorLogger = errorLogger;
        _logger      = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("check-inventory");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, OrderCreatedEvent> context,
        IBehavior<OrderState, OrderCreatedEvent> next)
    {
        var saga = context.Saga;

        try
        {
            var items = JsonSerializer.Deserialize(
                saga.Items,
                OrderSagaJsonContext.Default.ListOrderItemInfoEvent)!;

            await _producer.Produce(
                new CheckInventoryEvent(saga.OrderId, items),
                context.CancellationToken);

            _logger.LogInformation(
                "Inventory check requested for Order {OrderId} with {ItemCount} items",
                saga.OrderId, items.Count);
        }
        catch (Exception ex)
        {
            CompensationCore.StampException(saga, ex);

            await _errorLogger.LogExceptionAsync(
                saga.CorrelationId, saga.OrderId, saga.CurrentState,
                "CheckInventory", ex, context.CancellationToken);

            _logger.LogError(ex,
                "Failed to request inventory check for Order {OrderId}", saga.OrderId);

            throw;
        }

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, OrderCreatedEvent, TException> context,
        IBehavior<OrderState, OrderCreatedEvent> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "CheckInventory.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "CheckInventoryActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }
}
