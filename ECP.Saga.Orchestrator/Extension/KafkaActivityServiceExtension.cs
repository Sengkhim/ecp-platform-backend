using ECP.Saga.Orchestrator.Activities;
using ECP.Saga.Orchestrator.Infrastructure;

namespace ECP.Saga.Orchestrator.Extension;

public static class KafkaActivityServiceExtension
{
    public static void AddActivities(this IServiceCollection services)
    {
        // SagaErrorLogger — singleton, owns the MongoClient (thread-safe, connection-pooled)
        services.AddSingleton<SagaErrorLogger>();

        // Inventory flow
        services.AddScoped<CheckInventoryActivity>();
        services.AddScoped<InventoryCompensationActivity>();
        services.AddScoped<InventoryTimeoutCompensationActivity>();

        // Payment flow
        services.AddScoped<PaymentRequestActivity>();
        services.AddScoped<PaymentCompensationActivity>();
        services.AddScoped<PaymentTimeoutCompensationActivity>();

        // Completion
        services.AddScoped<SendNotificationActivity>();
    }
}
