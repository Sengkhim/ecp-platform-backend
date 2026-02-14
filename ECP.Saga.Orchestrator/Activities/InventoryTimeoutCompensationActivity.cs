using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Compensation activity triggered when the inventory service does not
/// respond within the scheduled deadline (<see cref="Contracts.InventoryTimeout"/>).
///
/// Must implement <c>IStateMachineActivity&lt;OrderState&gt;</c> (state-only)
/// because the timeout schedule fires with no <c>InventoryFailed</c> message data.
/// Use <see cref="InventoryCompensationActivity"/> for the explicit failure path.
/// </summary>
public sealed class InventoryTimeoutCompensationActivity : IStateMachineActivity<OrderState, InventoryTimeout>
{
    private readonly ITopicProducer<OrderFailed> _producer;
    private readonly ILogger<InventoryTimeoutCompensationActivity> _logger;

    public InventoryTimeoutCompensationActivity(
        ITopicProducer<OrderFailed> producer,
        ILogger<InventoryTimeoutCompensationActivity> logger)
    {
        _producer = producer;
        _logger   = logger;
    }

    public void Probe(ProbeContext context)
        => context.CreateScope("inventory-timeout-compensation");

    public void Accept(StateMachineVisitor visitor)
        => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState> context,
        IBehavior<OrderState> next)
    {
        await CompensationCore.RunInventoryAsync(
            saga:   context.Saga,
            reason: "Inventory service timed out",
            producer: _producer,
            logger: _logger,
            cancellationToken: context.CancellationToken);

        await next.Execute(context);
    }

    public Task Execute<T>(BehaviorContext<OrderState, T> context, IBehavior<OrderState, T> next) where T : class
    {
        throw new NotImplementedException();
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, TException> context,
        IBehavior<OrderState> next)
        where TException : Exception
    {
        context.Saga.LastExceptionDetail =
            $"[{context.Exception.GetType().Name}] {context.Exception.Message}";
        context.Saga.LastUpdatedAt = DateTime.UtcNow;

        _logger.LogError(context.Exception,
            "InventoryTimeoutCompensationActivity faulted for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }

    public Task Faulted<T, TException>(BehaviorExceptionContext<OrderState, T, TException> context, IBehavior<OrderState, T> next) where T : class where TException : Exception
    {
        throw new NotImplementedException();
    }

    public Task Execute(BehaviorContext<OrderState, InventoryTimeout> context, IBehavior<OrderState, InventoryTimeout> next)
    {
        throw new NotImplementedException();
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, InventoryTimeout, TException> context, IBehavior<OrderState, InventoryTimeout> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}