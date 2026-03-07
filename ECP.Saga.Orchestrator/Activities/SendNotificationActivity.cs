using Contracts;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public sealed class SendNotificationActivity :
    IStateMachineActivity<OrderState, ProcessPayment>
{
    private readonly ITopicProducer<NotificationRequest> _producer;
    private readonly SagaErrorLogger _errorLogger;
    private readonly ILogger<SendNotificationActivity> _logger;

    public SendNotificationActivity(
        ITopicProducer<NotificationRequest> producer,
        SagaErrorLogger errorLogger,
        ILogger<SendNotificationActivity> logger)
    {
        _producer    = producer;
        _errorLogger = errorLogger;
        _logger      = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("send-notification");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

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
                    NotificationType: "OrderCompleted"),
                context.CancellationToken);

            _logger.LogInformation(
                "Notification sent for completed Order {OrderId} to {CustomerEmail}",
                saga.OrderId, saga.CustomerEmail);
        }
        catch (Exception ex)
        {
            CompensationCore.StampException(saga, ex);

            await _errorLogger.LogExceptionAsync(
                saga.CorrelationId, saga.OrderId, saga.CurrentState,
                "SendNotification", ex, context.CancellationToken);

            _logger.LogError(ex,
                "Failed to send notification for Order {OrderId}", saga.OrderId);

            throw;
        }

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, ProcessPayment, TException> context,
        IBehavior<OrderState, ProcessPayment> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "SendNotification.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "SendNotificationActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }
}
