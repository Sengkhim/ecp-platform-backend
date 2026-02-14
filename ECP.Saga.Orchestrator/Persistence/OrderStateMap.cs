using ECP.Saga.Orchestrator.StateData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ECP.Saga.Orchestrator.Persistence;

public class OrderStateClassMap : BsonClassMap<OrderState>
{
    public OrderStateClassMap()
    {
        MapProperty(x => x.CorrelationId)
            .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.GuidGenerator.Instance)
            .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));

        MapProperty(x => x.CurrentState);
        
        MapProperty(x => x.OrderId)
            .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
        
        MapProperty(x => x.CustomerId)
            .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
        
        MapProperty(x => x.CustomerName);
        MapProperty(x => x.CustomerEmail);
        MapProperty(x => x.OrderNumber);
        MapProperty(x => x.TotalAmount);
        MapProperty(x => x.CreatedAt);
        MapProperty(x => x.Version);
        
        MapProperty(x => x.PaymentMethod);
        MapProperty(x => x.Currency);

        // Items stored as JSON string
        MapProperty(x => x.Items)
            .SetSerializer(new StringSerializer(BsonType.String));
    }
}
