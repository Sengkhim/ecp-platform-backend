using ECP.Saga.Orchestrator.Activities;

namespace ECP.Saga.Orchestrator.Extension;

public static class KafkaActivityServiceExtension
{
    /// <summary>
    /// Represent for configure activity external produce message.
    /// </summary>
    /// <param name="services"></param>
    public static void AddActivities(this IServiceCollection services)
    {
        services.AddScoped<SendNotificationActivity>();
        services.AddScoped<CheckInventoryActivity>();
        services.AddScoped<OrderActivity>();
        services.AddScoped<PaymentRequestActivity>();
    }
}