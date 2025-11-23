using ECP.Saga.Orchestrator.StateData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace ECP.Saga.Orchestrator.Persistence;

public class OrderStateClassMap : BsonClassMap<OrderState>
{
    public OrderStateClassMap()
    {
        MapProperty(x => x.CorrelationId).SetIdGenerator(GuidGenerator.Instance)
            .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));

        MapProperty(x => x.CurrentState);
        MapProperty(x => x.Amount);
        MapProperty(x => x.ProductId);
        MapProperty(x => x.Version);
        
    }
}
