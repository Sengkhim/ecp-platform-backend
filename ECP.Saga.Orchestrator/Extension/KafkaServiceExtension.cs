using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.PaymentService.Consumer;
using ECP.Saga.Orchestrator.Persistence;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;
using MongoDB.Bson.Serialization;

namespace ECP.Saga.Orchestrator.Extension;

public static class KafkaServiceExtension
{
    public static void AddKafkaComponent(this IServiceCollection services, IConfiguration configuration)
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(OrderState)))
            BsonClassMap.RegisterClassMap(new OrderStateClassMap());

        services.AddActivities();

        services.AddMassTransit(m =>
        {
            m.SetKebabCaseEndpointNameFormatter();
            m.AddDelayedMessageScheduler();
            m.AddConsumer<ProcessPaymentConsumer>();

            m.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .MongoDbRepository(r =>
                {
                    r.DatabaseName   = "sagas";
                    r.CollectionName = "sagas";
                    r.Connection     = configuration["MongoDB__SagaConnection"]
                                    ?? "mongodb://root:pass168@mongodb.ecp-dev.svc.cluster.local:27017/sagas?authSource=admin";
                });

            m.UsingInMemory((ctx, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();

                // Retry on MongoDB concurrency conflicts — two messages racing
                // to update the same saga instance triggers MongoDbConcurrencyException.
                // Exponential backoff retries the full saga transition safely.
                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(
                        retryLimit:    8,
                        minInterval:   TimeSpan.FromMilliseconds(50),
                        maxInterval:   TimeSpan.FromSeconds(5),
                        intervalDelta: TimeSpan.FromMilliseconds(100));

                    r.Handle<MongoDbConcurrencyException>();
                });

                // Hold outbound Kafka messages until MongoDB save succeeds.
                // Prevents Kafka publish succeeding but MongoDB failing — which
                // would leave downstream services acting on an unsaved saga state.
                cfg.UseInMemoryOutbox(ctx);

                cfg.ConfigureEndpoints(ctx);
            });

            m.AddRider(rider =>
            {
                ConfigureKafkaProducers(rider);

                rider.AddConsumer<KafkaForwardConsumer<OrderCreatedEvent>>();
                rider.AddConsumer<KafkaForwardConsumer<InventoryReserved>>();
                rider.AddConsumer<KafkaForwardConsumer<InventoryFailed>>();
                rider.AddConsumer<KafkaForwardConsumer<ProcessPayment>>();
                rider.AddConsumer<KafkaForwardConsumer<PaymentFailed>>();
                rider.AddConsumer<KafkaForwardConsumer<RequestPayment>>();
                rider.AddConsumer<ProcessPaymentConsumer>();

                rider.UsingKafka((context, k) =>
                    ConfigureKafkaEndpoints(context, k, configuration));
            });
        });
    }

    private static void ConfigureKafkaProducers(IRiderRegistrationConfigurator rider)
    {
        rider.AddProducer<RequestPayment>("request-payment");
        rider.AddProducer<ProcessPayment>("process-payment");
        rider.AddProducer<CheckInventoryEvent>("check-inventory");
        rider.AddProducer<OrderFailed>("order-failed");
        rider.AddProducer<NotificationRequest>("notification-request");
        rider.AddProducer<PaymentFailed>("payment-failed");
    }

    private static void ConfigureKafkaEndpoints(
        IRiderRegistrationContext riderCtx,
        IKafkaFactoryConfigurator k,
        IConfiguration configuration)
    {
        k.Host(configuration["Kafka__BootstrapServers"]
            ?? configuration["Kafka:BootstrapServers"]
            ?? "kafka.ecp-dev.svc.cluster.local:9092");

        // k.Host("kafka.ecp-dev.svc.cluster.local:9092");

        k.TopicEndpoint<OrderCreatedEvent>("order-created", "orchestrator", e =>
        {
            e.AutoStart     = true;
            e.PublishFaults = false;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureConsumer<KafkaForwardConsumer<OrderCreatedEvent>>(riderCtx);
        });

        k.TopicEndpoint<InventoryReserved>("inventory-reserved", "orchestrator", e =>
        {
            e.AutoStart     = true;
            e.PublishFaults = false;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureConsumer<KafkaForwardConsumer<InventoryReserved>>(riderCtx);
        });

        k.TopicEndpoint<InventoryFailed>("inventory-failed", "orchestrator", e =>
        {
            e.AutoStart     = true;
            e.PublishFaults = false;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureConsumer<KafkaForwardConsumer<InventoryFailed>>(riderCtx);
        });

        k.TopicEndpoint<ProcessPayment>("process-payment", "orchestrator", e =>
        {
            e.AutoStart     = true;
            e.PublishFaults = false;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureConsumer<KafkaForwardConsumer<ProcessPayment>>(riderCtx);
        });

        k.TopicEndpoint<PaymentFailed>("payment-failed", "orchestrator", e =>
        {
            e.AutoStart     = true;
            e.PublishFaults = false;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureConsumer<KafkaForwardConsumer<PaymentFailed>>(riderCtx);
        });

        k.TopicEndpoint<RequestPayment>("request-payment", "orchestrator", e =>
        {
            e.AutoStart     = true;
            e.PublishFaults = false;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureConsumer<KafkaForwardConsumer<RequestPayment>>(riderCtx);
        });
    }
}