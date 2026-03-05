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
        // ── Inventory flow ────────────────────────────────────────────────
        services.AddScoped<CheckInventoryActivity>();
        services.AddScoped<InventoryCompensationActivity>();
        services.AddScoped<InventoryTimeoutCompensationActivity>();

        // ── Payment flow ──────────────────────────────────────────────────
        services.AddScoped<PaymentRequestActivity>();
        services.AddScoped<PaymentCompensationActivity>();
        services.AddScoped<PaymentTimeoutCompensationActivity>();

        // ── Completion ────────────────────────────────────────────────────
        services.AddScoped<SendNotificationActivity>();
    }
}