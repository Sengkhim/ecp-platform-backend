using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Publishes a <see cref="NotificationRequest"/> to the notification service
/// when an order completes successfully.
///
/// The notification service will send confirmation emails, SMS, push notifications,
/// or whatever channels are configured for the customer.
/// </summary>
public sealed class SendNotificationActivity :
    IStateMachineActivity<OrderState, ProcessPayment>
{
    private readonly ITopicProducer<NotificationRequest> _producer;
    private readonly ILogger<SendNotificationActivity> _logger;

    public SendNotificationActivity(
        ITopicProducer<NotificationRequest> producer,
        ILogger<SendNotificationActivity> logger)
    {
        _producer = producer;
        _logger   = logger;
    }

    public void Probe(ProbeContext context)
        => context.CreateScope("send-notification");

    public void Accept(StateMachineVisitor visitor)
        => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, ProcessPayment> context,
        IBehavior<OrderState, ProcessPayment> next)
    {
        var saga = context.Saga;

        try
        {
            await _producer.Produce(
                new NotificationRequest(
                    saga.OrderId,
                    saga.CustomerId,
                    saga.CustomerEmail,
                    NotificationType: "OrderCompleted"), context.CancellationToken);

            _logger.LogInformation(
                "Notification sent for completed Order {OrderId} to {CustomerEmail}",
                saga.OrderId,
                saga.CustomerEmail);
        }
        catch (Exception ex)
        {
            saga.LastExceptionDetail = $"[{ex.GetType().Name}] {ex.Message}";
            saga.LastUpdatedAt       = DateTime.UtcNow;

            _logger.LogError(ex,
                "Failed to send notification for Order {OrderId}",
                saga.OrderId);

            throw;
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, ProcessPayment, TException> context,
        IBehavior<OrderState, ProcessPayment> next)
        where TException : Exception
    {
        context.Saga.LastExceptionDetail =
            $"[{context.Exception.GetType().Name}] {context.Exception.Message}";
        context.Saga.LastUpdatedAt = DateTime.UtcNow;

        _logger.LogError(context.Exception,
            "SendNotificationActivity faulted for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}