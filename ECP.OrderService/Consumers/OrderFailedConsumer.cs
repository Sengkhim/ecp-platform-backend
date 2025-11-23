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
        await producer.Produce(new NotificationRequest(context.Message.OrderId, context.Message.Reason));
    }
}