using Confluent.Kafka;
using Contracts;
using ECP.PaymentService.Consumer;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingInMemory((context, cfg) =>  cfg.ConfigureEndpoints(context));
    x.AddRider(rider =>
    {
        rider.AddProducer<ProcessPayment>("process-payment");
        rider.AddProducer<PaymentFailed>("payment-failed");
        rider.AddConsumer(typeof(ProcessPaymentConsumer));
        rider.UsingKafka((context, k) =>
        {
            k.Host("localhost:9092");
            
            k.TopicEndpoint<ProcessPaymentRequest>(
                "process-payment-request",
                "orchestrator",
                e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<ProcessPaymentConsumer>(context);
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