using Confluent.Kafka;
using Contracts;
using ECP.NotificationService;
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
        rider.AddConsumer<NotificationConsumer>();
        rider.UsingKafka((context, k) =>
        {
            k.Host("localhost:9092");

            k.TopicEndpoint<NotificationRequest>(
                "notification-request",
                "notification-service-group",
                e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.AutoStart = true;
                    e.CreateIfMissing(m => 
                    {
                        m.NumPartitions = 2;
                        m.ReplicationFactor = 1;
                    });
                    e.ConfigureConsumer<NotificationConsumer>(context);
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

app.Run();