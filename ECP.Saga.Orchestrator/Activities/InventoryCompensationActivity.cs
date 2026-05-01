using Contracts;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public sealed class InventoryCompensationActivity :
    IStateMachineActivity<OrderState, InventoryFailed>
{
    private readonly ITopicProducer<OrderFailed> _producer;
    private readonly SagaErrorLogger _errorLogger;
    private readonly ILogger<InventoryCompensationActivity> _logger;

    public InventoryCompensationActivity(
        ITopicProducer<OrderFailed> producer,
        SagaErrorLogger errorLogger,
        ILogger<InventoryCompensationActivity> logger)
    {
        _producer    = producer;
        _errorLogger = errorLogger;
        _logger      = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("inventory-compensation");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, InventoryFailed> context,
        IBehavior<OrderState, InventoryFailed> next)
    {
        await CompensationCore.RunInventoryAsync(
            saga:             context.Saga,
            reason:           context.Message.Reason ?? "Inventory reservation failed",
            producer:         _producer,
            logger:           _logger,
            errorLogger:      _errorLogger,
            cancellationToken: context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryFailed, TException> context,
        IBehavior<OrderState, InventoryFailed> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "InventoryCompensation.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "InventoryCompensationActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }
}
