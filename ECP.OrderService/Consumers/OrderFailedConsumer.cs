using Contracts;
using MassTransit;

namespace ECP.OrderService.Consumers;

public class OrderFailedConsumer(
    ITopicProducer<NotificationRequest> producer,
    ILogger<OrderFailedConsumer> logger) : IConsumer<OrderFailed>
{
    public async Task Consume(ConsumeContext<OrderFailed> context)
    {
        logger.LogInformation("Order Failed: {OrderId}", context.Message.OrderId);

        var customerId = Guid.NewGuid();
        
        await producer.Produce(
            new NotificationRequest(context.Message.OrderId, customerId, context.Message.Reason, "MB"));
    }
}