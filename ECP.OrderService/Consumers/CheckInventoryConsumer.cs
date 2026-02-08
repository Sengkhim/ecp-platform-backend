using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using MassTransit;

namespace ECP.OrderService.Consumers;

public class CheckInventoryConsumer(
    ITopicProducer<InventoryReserved> producer) : IConsumer<CheckInventoryEvent>
{
    /// <summary>
    /// Try to consume the e then check product in stock ref=InventoryService.message
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<CheckInventoryEvent> context)
    {
        Console.WriteLine($"✅ Received CheckInventory: {context.Message.OrderId} | {context.Message.Items?.ToString()}");
        await producer.Produce(new InventoryReserved(context.Message.OrderId, DateTime.Now)); 
    }
}