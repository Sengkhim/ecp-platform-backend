using Contracts;
using MassTransit;

namespace ECP.NotificationService;

public class NotificationConsumer(
    ILogger<NotificationConsumer> logger) : IConsumer<NotificationRequest>
{
    public Task Consume(ConsumeContext<NotificationRequest> context)
    {
        logger.LogInformation("Notification: {OrderId} - {Message}",
            context.Message.OrderId, context.Message.Message);

        // TODO: send email, push notification, etc.
        return Task.CompletedTask;
    }
}
