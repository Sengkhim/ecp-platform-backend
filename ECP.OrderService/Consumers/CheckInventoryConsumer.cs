using Contracts;
using MassTransit;

namespace ECP.OrderService.Consumers;

public class CheckInventoryConsumer(
    ITopicProducer<InventoryReserved> producer) : IConsumer<CheckInventory>
{
    /// <summary>
    /// Try to consume the e then check product in stock ref=InventoryService.message
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<CheckInventory> context)
    {
        Console.WriteLine($"✅ Received CheckInventory: {context.Message.OrderId} | {context.Message.ProductId}");
        await producer.Produce(new InventoryReserved(context.Message.OrderId)); 
    }
}