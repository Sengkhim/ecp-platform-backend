using System.Text.Json;
using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Publishes a <see cref="CheckInventoryEvent"/> command to the inventory service
/// when a new order is created.
///
/// The inventory service will check stock levels and respond with either
/// <see cref="InventoryReserved"/> (success) or <see cref="InventoryFailed"/> (failure).
/// </summary>
public sealed class CheckInventoryActivity :
    IStateMachineActivity<OrderState, OrderCreatedEvent>
{
    private readonly ITopicProducer<CheckInventoryEvent> _producer;
    private readonly ILogger<CheckInventoryActivity> _logger;

    public CheckInventoryActivity(
        ITopicProducer<CheckInventoryEvent> producer,
        ILogger<CheckInventoryActivity> logger)
    {
        _producer = producer;
        _logger   = logger;
    }

    public void Probe(ProbeContext context)
        => context.CreateScope("check-inventory");

    public void Accept(StateMachineVisitor visitor)
        => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, OrderCreatedEvent> context,
        IBehavior<OrderState, OrderCreatedEvent> next)
    {
        var saga = context.Saga;

        try
        {
            // Deserialize items from saga state to send to inventory service
            var items = JsonSerializer.Deserialize<List<OrderItemInfoEvent>>(
                saga.Items,
                OrderSagaJsonContext.Default.ListOrderItemInfoEvent)!;

            await _producer.Produce(
                new CheckInventoryEvent(saga.OrderId, items),
                context.CancellationToken);

            _logger.LogInformation(
                "Inventory check requested for Order {OrderId} with {ItemCount} items",
                saga.OrderId,
                items.Count);
        }
        catch (Exception ex)
        {
            saga.LastExceptionDetail = $"[{ex.GetType().Name}] {ex.Message}";
            saga.LastUpdatedAt       = DateTime.UtcNow;

            _logger.LogError(ex,
                "Failed to request inventory check for Order {OrderId}",
                saga.OrderId);

            throw;
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, OrderCreatedEvent, TException> context,
        IBehavior<OrderState, OrderCreatedEvent> next)
        where TException : Exception
    {
        context.Saga.LastExceptionDetail =
            $"[{context.Exception.GetType().Name}] {context.Exception.Message}";
        context.Saga.LastUpdatedAt = DateTime.UtcNow;

        _logger.LogError(context.Exception,
            "CheckInventoryActivity faulted for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}