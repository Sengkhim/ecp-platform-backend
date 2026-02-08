using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.OrderService.Consumers;
using ECP.OrderService.Infrastructure.Data;
using ECP.OrderService.Infrastructure.Repositories;
using ECP.OrderService.Modules.Order.Service;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL with EF Core
var connectionString = builder.Configuration.GetConnectionString("OrderConnections")
                       ?? "Host=localhost;Port=5432;Database=OrderDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Register repositories and services
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<OrderService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddConsumer<CheckInventoryConsumer>();
        rider.AddConsumer<OrderFailedConsumer>();
        
        rider.AddProducer<OrderCreatedEvent>("order-created");
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
            k.TopicEndpoint<CheckInventoryEvent>("check-inventory", "orchestrator", e =>
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

app.MapHealthChecks("/health");
app.UseCors("AllowAll");
app.MapControllers();
app.Run();