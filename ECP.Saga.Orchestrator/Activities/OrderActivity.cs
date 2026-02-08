using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class OrderActivity(
    ILogger<OrderActivity> logger,
    ITopicProducer<OrderFailed> producer)
    : IStateMachineActivity<OrderState>
{
    public void Probe(ProbeContext context) => context.CreateScope("ordering");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    [Obsolete("Obsolete")]
    public async Task Execute(BehaviorContext<OrderState> context, IBehavior<OrderState> next)
    {
        try
        {
            var orderId = context.Instance.CorrelationId;
            var reason = string.Empty;
            var failStep = string.Empty;
            var timestamp = DateTime.Now;

            var orderFailed = new OrderFailed(
                orderId, reason, failStep, timestamp);
            
            await producer.Produce(orderFailed);
            
            logger.LogInformation("Order fail sent for Order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed ordering.");
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
            var reason = string.Empty;
            var failStep = string.Empty;
            var timestamp = DateTime.Now;

            var orderFailed = new OrderFailed(
                orderId, reason, failStep, timestamp);
            
            await producer.Produce(orderFailed);
            
            logger.LogInformation("Order fail sent for Order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed ordering.");
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