using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Compensation activity triggered when the inventory service explicitly
/// returns an <see cref="InventoryFailed"/> event.
///
/// Must implement <c>IStateMachineActivity&lt;OrderState, InventoryFailed&gt;</c>
/// because MassTransit binds the activity to the triggering event's data context.
/// Use <see cref="InventoryTimeoutCompensationActivity"/> for the timeout path
/// where no <see cref="InventoryFailed"/> message data is present.
/// </summary>
public sealed class InventoryCompensationActivity :
    IStateMachineActivity<OrderState, InventoryFailed>
{
    private readonly ITopicProducer<OrderFailed> _producer;
    private readonly ILogger<InventoryCompensationActivity> _logger;

    public InventoryCompensationActivity(
        ITopicProducer<OrderFailed> producer,
        ILogger<InventoryCompensationActivity> logger)
    {
        _producer = producer;
        _logger   = logger;
    }

    public void Probe(ProbeContext context)
        => context.CreateScope("inventory-compensation");

    public void Accept(StateMachineVisitor visitor)
        => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, InventoryFailed> context,
        IBehavior<OrderState, InventoryFailed> next)
    {
        await CompensationCore.RunInventoryAsync(
            saga:   context.Saga,
            reason: context.Message.Reason ?? "Inventory reservation failed",
            producer: _producer,
            logger: _logger,
            cancellationToken: context.CancellationToken);

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryFailed, TException> context,
        IBehavior<OrderState, InventoryFailed> next)
        where TException : Exception
    {
        // The compensation activity itself threw — stamp and re-throw.
        // MassTransit will move the message to the error queue.
        context.Saga.LastExceptionDetail =
            $"[{context.Exception.GetType().Name}] {context.Exception.Message}";
        context.Saga.LastUpdatedAt = DateTime.UtcNow;

        _logger.LogError(context.Exception,
            "InventoryCompensationActivity faulted for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}

// using Contracts;
// using ECP.Saga.Orchestrator.StateData;
// using MassTransit;
//
// namespace ECP.Saga.Orchestrator.Activities;
//
// // ✅ Used when: InventoryFailed event fires (has message data)
// public class InventoryCompensationActivity :
//     IStateMachineActivity<OrderState, InventoryFailed>
// {
//     private readonly ITopicProducer<OrderFailed> _producer;
//     private readonly ILogger<InventoryCompensationActivity> _logger;
//
//     public InventoryCompensationActivity(
//         ITopicProducer<OrderFailed> producer,
//         ILogger<InventoryCompensationActivity> logger)
//     {
//         _producer = producer;
//         _logger   = logger;
//     }
//
//     public void Probe(ProbeContext context) => context.CreateScope("inventory-compensation");
//     public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
//
//     public async Task Execute(
//         BehaviorContext<OrderState, InventoryFailed> context,
//         IBehavior<OrderState, InventoryFailed> next)
//     {
//         await CompensateAsync(context.Saga, reason: "Inventory reservation failed");
//         await next.Execute(context);
//     }
//
//     public Task Faulted<TException>(
//         BehaviorExceptionContext<OrderState, InventoryFailed, TException> context,
//         IBehavior<OrderState, InventoryFailed> next)
//         where TException : Exception
//     {
//         _logger.LogError(context.Exception,
//             "Inventory compensation faulted for Order {OrderId}", context.Saga.OrderId);
//         return next.Faulted(context);
//     }
//
//     internal Task CompensateAsync(OrderState saga, string reason)
//         => CompensationCore.RunAsync(saga, reason, "CheckInventory", _producer, _logger);
// }
//
// // ✅ Used when: InventoryTimeoutSchedule fires (no InventoryFailed message data)
// public class InventoryTimeoutCompensationActivity :
//     IStateMachineActivity<OrderState>
// {
//     private readonly ITopicProducer<OrderFailed> _producer;
//     private readonly ILogger<InventoryCompensationActivity> _logger;
//
//     public InventoryTimeoutCompensationActivity(
//         ITopicProducer<OrderFailed> producer,
//         ILogger<InventoryCompensationActivity> logger)
//     {
//         _producer = producer;
//         _logger   = logger;
//     }
//
//     public void Probe(ProbeContext context) => context.CreateScope("inventory-timeout-compensation");
//     public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
//
//     public async Task Execute(
//         BehaviorContext<OrderState> context,
//         IBehavior<OrderState> next)
//     {
//         await CompensationCore.RunAsync(
//             context.Saga, reason: "Inventory service timed out",
//             step: "CheckInventory", _producer, _logger);
//         await next.Execute(context);
//     }
//
//     public Task Faulted<TException>(
//         BehaviorExceptionContext<OrderState, TException> context,
//         IBehavior<OrderState> next)
//         where TException : Exception
//     {
//         _logger.LogError(context.Exception,
//             "Inventory timeout compensation faulted for Order {OrderId}", context.Saga.OrderId);
//         return next.Faulted(context);
//     }
// }