using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class CheckInventoryActivity(
    ITopicProducer<CheckInventory> producer, 
    ILogger<CheckInventoryActivity> logger) : IStateMachineActivity<OrderState>
{
    public void Probe(ProbeContext context) => context.CreateScope("CheckInventory");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    [Obsolete("Obsolete")]
    public async Task Execute(BehaviorContext<OrderState> context, IBehavior<OrderState> next)
    {
        try
        {
            var orderId = context.Instance.CorrelationId;
            await producer.Produce(new CheckInventory(orderId, context.Saga.ProductId));
            logger.LogInformation("Check Inventory {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Check Inventory.");
            throw;
        }

        await next.Execute(context);
    }

    [Obsolete("Obsolete")]
    public async Task Execute<T>(
        BehaviorContext<OrderState, T> context, 
        IBehavior<OrderState, T> next) where T : class
    {
        try
        {
            var orderId = context.Instance.CorrelationId;
            await producer.Produce(new CheckInventory(orderId, context.Saga.ProductId));
            logger.LogInformation("Check Inventory {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Check Inventory.");
            throw;
        }

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, TException> context, 
        IBehavior<OrderState> next) where TException : Exception
        => await next.Faulted(context);

    public async Task Faulted<T, TException>(
        BehaviorExceptionContext<OrderState, T, TException> context, 
        IBehavior<OrderState, T> next) where T : class where TException : Exception
        => await next.Faulted(context);
}