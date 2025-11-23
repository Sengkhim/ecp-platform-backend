using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class PaymentRequestActivity(
    ILogger<OrderActivity> logger,
    ITopicProducer<ProcessPaymentRequest> producer) : IStateMachineActivity<OrderState>
{
    public void Probe(ProbeContext context) => context.CreateScope("payment-request-activity");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    [Obsolete("Obsolete")]
    public async Task Execute(BehaviorContext<OrderState> context, IBehavior<OrderState> next)
    {
        try
        {
            var orderId = context.Instance.CorrelationId;
            await producer.Produce(new ProcessPaymentRequest(orderId, context.Saga.Amount));
            logger.LogInformation("Staring payment request {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed payment request!");
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
            await producer.Produce(new ProcessPaymentRequest(orderId, context.Saga.Amount));
            logger.LogInformation("Staring payment request {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed payment request!");
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