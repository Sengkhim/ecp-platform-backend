using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class SendNotificationActivity(
    ITopicProducer<NotificationRequest> producer,
    ILogger<SendNotificationActivity> logger) : IStateMachineActivity<OrderState>
{
    public void Probe(ProbeContext context) => context.CreateScope("send-notification");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    [Obsolete("Obsolete")]
    public async Task Execute(BehaviorContext<OrderState> context, IBehavior<OrderState> next)
    {
        try
        {
            var orderId = context.Instance.CorrelationId;
            await producer.Produce(new NotificationRequest(orderId, "Order Completed!"));
            logger.LogInformation("Notification sent for Order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification");
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
            await producer.Produce(new NotificationRequest(orderId, "Order Completed!"));
            logger.LogInformation("Notification sent for Order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification");
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
        =>  await next.Faulted(context);
}