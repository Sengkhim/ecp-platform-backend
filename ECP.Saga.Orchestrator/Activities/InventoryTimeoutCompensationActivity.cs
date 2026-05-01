using Contracts;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public sealed class InventoryTimeoutCompensationActivity :
    IStateMachineActivity<OrderState, InventoryTimeout>
{
    private readonly ITopicProducer<OrderFailed> _producer;
    private readonly SagaErrorLogger _errorLogger;
    private readonly ILogger<InventoryTimeoutCompensationActivity> _logger;

    public InventoryTimeoutCompensationActivity(
        ITopicProducer<OrderFailed> producer,
        SagaErrorLogger errorLogger,
        ILogger<InventoryTimeoutCompensationActivity> logger)
    {
        _producer    = producer;
        _errorLogger = errorLogger;
        _logger      = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("inventory-timeout-compensation");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    // Primary typed path — fired by InventoryTimeoutSchedule.Received
    public async Task Execute(
        BehaviorContext<OrderState, InventoryTimeout> context,
        IBehavior<OrderState, InventoryTimeout> next)
    {
        await CompensationCore.RunInventoryAsync(
            saga:              context.Saga,
            reason:            "Inventory service timed out",
            producer:          _producer,
            logger:            _logger,
            errorLogger:       _errorLogger,
            cancellationToken: context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryTimeout, TException> context,
        IBehavior<OrderState, InventoryTimeout> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "InventoryTimeout.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "InventoryTimeoutCompensationActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }

    // Generic overloads required by interface — delegate cleanly
    public async Task Execute(BehaviorContext<OrderState> context, IBehavior<OrderState> next)
    {
        await CompensationCore.RunInventoryAsync(
            saga:              context.Saga,
            reason:            "Inventory service timed out",
            producer:          _producer,
            logger:            _logger,
            errorLogger:       _errorLogger,
            cancellationToken: context.CancellationToken);

        await next.Execute(context);
    }

    public Task Execute<T>(BehaviorContext<OrderState, T> context, IBehavior<OrderState, T> next)
        where T : class
        => next.Execute(context);

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, TException> context,
        IBehavior<OrderState> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "InventoryTimeout.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "InventoryTimeoutCompensationActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }

    public async Task Faulted<T, TException>(
        BehaviorExceptionContext<OrderState, T, TException> context,
        IBehavior<OrderState, T> next)
        where T : class
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "InventoryTimeout.Faulted.Generic", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "InventoryTimeoutCompensationActivity faulted (generic) for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }
}
