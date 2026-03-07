using ECP.Saga.Orchestrator.Extension;
using ECP.Saga.Orchestrator.Persistence;
using ECP.Saga.Orchestrator.StateData;
using MongoDB.Bson.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddKafkaComponent(builder.Configuration);
builder.Services.AddSingleton<BsonClassMap<OrderState>, OrderStateClassMap>();
BsonClassMap.RegisterClassMap(new OrderStateClassMap());  
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();