using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.PaymentService.Consumer;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Extension;

public static class KafkaServiceExtension
{
    public static void AddKafkaComponent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddActivities();
        services.AddMassTransit(m =>
        {
            m.SetKebabCaseEndpointNameFormatter();
            m.AddDelayedMessageScheduler();
            m.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .MongoDbRepository(r =>
                {
                    r.DatabaseName   = "sagas";
                    r.CollectionName = "sagas";
                    r.Connection = configuration["MongoDB:SagaConnection"]
                                   ?? "mongodb://root:pass168@127.0.0.1:27017/sagas?authSource=admin";                });
            
            m.AddConsumer<ProcessPaymentConsumer>();
            m.UsingInMemory((ctx, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();
                cfg.ConfigureEndpoints(ctx);
            });
            
            m.AddRider(rider =>
            {
                rider.AddSagaStateMachine<OrderStateMachine, OrderState>();
                rider.AddConsumer<ProcessPaymentConsumer>();
                ConfigureKafkaProducers(rider);
                rider.UsingKafka((context, configurator) => 
                    ConfigureKafkaEndpoints(context, configurator, configuration));
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
        k.Host(configuration["Kafka:BootstrapServers"] ?? "kafka.ecp-dev.svc.cluster.local");
        // k.Host("localhost:9092");
        
        k.TopicEndpoint<OrderCreatedEvent>("order-created", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
        
        k.TopicEndpoint<InventoryReserved>("inventory-reserved", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
        
        k.TopicEndpoint<InventoryFailed>("inventory-failed", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
        
        k.TopicEndpoint<ProcessPayment>("process-payment", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
        
        k.TopicEndpoint<PaymentFailed>("payment-failed", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });

        k.TopicEndpoint<RequestPayment>("request-payment", "orchestrator", e =>
        {
            e.AutoStart = true;
            e.CreateIfMissing(t => t.NumPartitions = 2);
            e.ConfigureSaga<OrderState>(riderCtx);
        });
    }
}