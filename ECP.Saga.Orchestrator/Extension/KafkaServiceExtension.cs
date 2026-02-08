using Contracts;
using ECP.OrderService.Application.Contracts.Events;
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
            m.AddSagaStateMachine();
            m.AddConsumers(typeof(Program).Assembly);
            m.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            m.AddRider(rider =>
            {
                rider.KafkaConfigure();
                rider.UsingKafka((context, k) => k.EndpointConfigure(context));
            });
        });
    }

    private static void AddSagaStateMachine(this IBusRegistrationConfigurator m)
    {
        m.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .MongoDbRepository(r =>
            {
                r.DatabaseName = "sagas";
                r.CollectionName = "sagas";
                r.Connection = "mongodb://root:pass168@127.0.0.1:27017/sagas?authSource=admin";
            });
    }

    private static void KafkaConfigure(this IRiderRegistrationConfigurator rider)
    {
        rider.AddProducer<ProcessPayment>("process-payment");
        rider.AddProducer<CheckInventoryEvent>("check-inventory");
        rider.AddProducer<OrderFailed>("order-failed");
        rider.AddProducer<NotificationRequest>("notification-request");
        rider.AddConsumers(typeof(Program).Assembly);
        rider.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .MongoDbRepository(r =>
            {
                r.DatabaseName = "sagas";
                r.CollectionName = "sagas";
                r.Connection = "mongodb://root:pass168@127.0.0.1:27017/sagas?authSource=admin";
            });
    }

    private static void EndpointConfigure(
        this IKafkaFactoryConfigurator k, IRiderRegistrationContext context)
    {
        k.Host("localhost:9092");

        // 1. Order Created
        k.TopicEndpoint<OrderCreatedEvent>("order-created", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2); // Forces creation
            e.ConfigureSaga<OrderState>(context);
        });

        // 2. Inventory Reserved
        k.TopicEndpoint<InventoryReserved>("inventory-reserved", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(context);
        });

        // 3. Inventory Failed
        k.TopicEndpoint<InventoryFailed>("inventory-failed", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(context);
        });

        // 4. Process Payment
        k.TopicEndpoint<ProcessPayment>("process-payment", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(context);
        });

        // 5. Payment Failed
        k.TopicEndpoint<PaymentFailed>("payment-failed", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(context);
        });
    }
}