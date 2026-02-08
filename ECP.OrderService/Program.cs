using Contracts;
using ECP.OrderService.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddConsumer<CheckInventoryConsumer>();
        rider.AddConsumer<OrderFailedConsumer>();

        // Producers: Registering them is enough. 
        // Kafka will auto-create these on the FIRST 'Produce' call 
        // provided the Docker variable is set.
        rider.AddProducer<OrderCreated>("order-created");
        rider.AddProducer<InventoryReserved>("inventory-reserved");
        rider.AddProducer<NotificationRequest>("notification-request");

        rider.UsingKafka((context, k) =>
        {
            k.Host("localhost:9092");

            // CONSUMER ENDPOINT 1
            k.TopicEndpoint<OrderFailed>("order-failed", "orchestrator", e =>
            {
                e.AutoStart = true;
                // BEST SOLUTION: This forces MassTransit to create the topic on start
                e.CreateIfMissing(m => 
                {
                    m.NumPartitions = 2;
                    m.ReplicationFactor = 1;
                });
                e.ConfigureConsumer<OrderFailedConsumer>(context);
            });
            
            // CONSUMER ENDPOINT 2
            k.TopicEndpoint<CheckInventory>("check-inventory", "orchestrator", e =>
            {
                e.AutoStart = true;
                e.CreateIfMissing(m => 
                {
                    m.NumPartitions = 2;
                });
                e.ConfigureConsumer<CheckInventoryConsumer>(context);
            });
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/orders", async (ITopicProducer<OrderCreated> producer) =>
{
    var orderId = Guid.NewGuid();
    var message = new OrderCreated(orderId, 100, Guid.NewGuid().ToString());
    await producer.Produce(message);
    Console.WriteLine($"🟢 Published OrderCreated for {orderId}");
    return Results.Accepted();
});

app.Run();