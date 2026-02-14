using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class SendNotificationActivity :
    IStateMachineActivity<OrderState, ProcessPayment>
{
    private readonly ITopicProducer<NotificationRequest> _producer;
    private readonly ILogger<SendNotificationActivity> _logger;

    public SendNotificationActivity(
        ITopicProducer<NotificationRequest> producer,
        ILogger<SendNotificationActivity> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("send-notification");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, ProcessPayment> context,
        IBehavior<OrderState, ProcessPayment> next)
    {
        var saga = context.Saga;

        var notification = new NotificationRequest(
            saga.OrderId,
            saga.CustomerId,
            $"Your order {saga.OrderNumber} has been successfully created.",
            "OrderCompleted");

        await _producer.Produce(notification);

        _logger.LogInformation(
            "Notification sent for Order {OrderId} to Customer {CustomerId}",
            saga.OrderId,
            saga.CustomerId);

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, ProcessPayment, TException> context,
        IBehavior<OrderState, ProcessPayment> next)
        where TException : Exception
    {
        _logger.LogError(context.Exception,
            "Notification failed for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}