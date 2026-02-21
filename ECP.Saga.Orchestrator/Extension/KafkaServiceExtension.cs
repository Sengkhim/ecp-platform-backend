using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.PaymentService.Consumer;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Extension;

public static class KafkaServiceExtension
{
    public static void AddKafkaComponent(this IServiceCollection services)
    {
        services.AddActivities();

        services.AddMassTransit(m =>
        {
            m.SetKebabCaseEndpointNameFormatter();

            // ── Scheduler ─────────────────────────────────────────────────────
            m.AddDelayedMessageScheduler();

            // ── Saga ──────────────────────────────────────────────────────────
            // Registered HERE with the full repository configuration.
            // Also registered below on the rider (without repository) so the
            // rider's IRiderRegistrationContext can resolve it for ConfigureSaga.
            // This is NOT a double registration — the rider entry is a reference
            // that points to the same DI-registered saga instance. The MongoDB
            // repository is only configured once here.
            m.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .MongoDbRepository(r =>
                {
                    r.DatabaseName   = "sagas";
                    r.CollectionName = "sagas";
                    r.Connection     = "mongodb://root:pass168@127.0.0.1:27017/sagas?authSource=admin";
                });

            // ── Consumers ────────────────────────────────────────────────────
            m.AddConsumer<ProcessPaymentConsumer>();

            // ── In-memory bus ─────────────────────────────────────────────────
            // UseDelayedMessageScheduler() wires MessageSchedulerContext into
            // every pipeline execution — must be called before ConfigureEndpoints.
            m.UsingInMemory((ctx, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();
                cfg.ConfigureEndpoints(ctx);
            });

            // ── Kafka rider ───────────────────────────────────────────────────
            m.AddRider(rider =>
            {
                // Re-register saga on the rider WITHOUT repository config.
                // This gives the rider's IRiderRegistrationContext a handle to
                // the saga so ConfigureSaga<OrderState>(riderCtx) works below.
                // The repository is NOT configured here — it is shared from above.
                rider.AddSagaStateMachine<OrderStateMachine, OrderState>();

                // Re-register consumer on the rider for the same reason.
                rider.AddConsumer<ProcessPaymentConsumer>();

                ConfigureKafkaProducers(rider);

                // riderCtx is IRiderRegistrationContext — it now knows about the
                // saga and consumer because we registered them on the rider above.
                rider.UsingKafka(ConfigureKafkaEndpoints);
            });
        });
    }

    // ── Kafka producers ───────────────────────────────────────────────────────
    // One AddProducer<T> per ITopicProducer<T> injected anywhere in the app.
    private static void ConfigureKafkaProducers(IRiderRegistrationConfigurator rider)
    {
        rider.AddProducer<RequestPayment>("request-payment");
        rider.AddProducer<ProcessPayment>("process-payment");
        rider.AddProducer<CheckInventoryEvent>("check-inventory");
        rider.AddProducer<OrderFailed>("order-failed");
        rider.AddProducer<NotificationRequest>("notification-request");
        rider.AddProducer<PaymentFailed>("payment-failed");
    }

    // ── Kafka topic endpoints ─────────────────────────────────────────────────
    // riderCtx is IRiderRegistrationContext — resolves saga + consumer because
    // both were registered on the rider above.
    //
    // ConfigureSaga<T>    → saga state machine handles this topic
    // ConfigureConsumer<T> → regular consumer handles this topic
    private static void ConfigureKafkaEndpoints(
        IRiderRegistrationContext riderCtx,
        IKafkaFactoryConfigurator k)
    {
        k.Host("localhost:9092");

        // 1. Order Created → saga
        k.TopicEndpoint<OrderCreatedEvent>("order-created", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });

        // 2. Inventory Reserved → saga
        k.TopicEndpoint<InventoryReserved>("inventory-reserved", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });

        // 3. Inventory Failed → saga
        k.TopicEndpoint<InventoryFailed>("inventory-failed", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });

        // 4. Payment Processed (success response from payment service) → saga
        k.TopicEndpoint<ProcessPayment>("process-payment", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });

        // 5. Payment Failed → saga
        k.TopicEndpoint<PaymentFailed>("payment-failed", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
        // 6. Request Payment → ProcessPaymentConsumer (NOT the saga)
        // Command published by the saga to trigger payment processing.
        // Isolated in its own consumer group from the saga's orchestrator group.
        k.TopicEndpoint<RequestPayment>("request-payment", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
    }
}