using ECP.Saga.Orchestrator.StateData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ECP.Saga.Orchestrator.Persistence;

/// <summary>
/// MongoDB BSON class map for <see cref="OrderState"/>.
///
/// Maps all 21 properties with appropriate serializers for type safety and performance.
/// Includes ISagaVersion.Version for optimistic concurrency control.
/// </summary>
public class OrderStateClassMap : BsonClassMap<OrderState>
{
    private static readonly GuidSerializer     GuidStd     = new(GuidRepresentation.Standard);
    private static readonly DateTimeSerializer DateUtc     = new(DateTimeKind.Utc);
    private static readonly DecimalSerializer  DecimalBson = new(BsonType.Decimal128);

    public OrderStateClassMap()
    {
        AutoMap();

        // ── MassTransit required ──────────────────────────────────────────────
        MapIdProperty(x => x.CorrelationId)
            .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.GuidGenerator.Instance)
            .SetSerializer(GuidStd);

        MapProperty(x => x.CurrentState);

        // ── ISagaVersion - optimistic concurrency ─────────────────────────────
        // MassTransit increments this on every saga state transition to prevent
        // concurrent updates from racing. MongoDB checks Version on update and
        // throws if it changed since read (indicating another process modified it).
        MapProperty(x => x.Version);

        // ── Order core data ───────────────────────────────────────────────────
        MapProperty(x => x.OrderId)
            .SetSerializer(GuidStd);

        MapProperty(x => x.CustomerId)
            .SetSerializer(GuidStd);

        MapProperty(x => x.CustomerName);
        MapProperty(x => x.CustomerEmail);
        MapProperty(x => x.OrderNumber);

        MapProperty(x => x.TotalAmount)
            .SetSerializer(DecimalBson);

        MapProperty(x => x.PaymentMethod);
        MapProperty(x => x.Currency);

        MapProperty(x => x.CreatedAt)
            .SetSerializer(DateUtc);

        // JSON string produced by source-generated serializer in state machine
        MapProperty(x => x.Items)
            .SetSerializer(new StringSerializer(BsonType.String));

        // ── Payment idempotency tracking ──────────────────────────────────────
        MapProperty(x => x.PaymentIdempotencyKey)
            .SetSerializer(new NullableSerializer<Guid>(GuidStd));

        // ── Timeout scheduler tokens ──────────────────────────────────────────
        MapProperty(x => x.InventoryTimeoutTokenId)
            .SetSerializer(new NullableSerializer<Guid>(GuidStd));

        MapProperty(x => x.PaymentTimeoutTokenId)
            .SetSerializer(new NullableSerializer<Guid>(GuidStd));

        // ── Error tracking ────────────────────────────────────────────────────
        MapProperty(x => x.FailedStep);
        MapProperty(x => x.FailureReason);

        MapProperty(x => x.FailedAt)
            .SetSerializer(new NullableSerializer<DateTime>(DateUtc));

        MapProperty(x => x.LastExceptionDetail);

        // ── Audit ─────────────────────────────────────────────────────────────
        MapProperty(x => x.LastUpdatedAt)
            .SetSerializer(new NullableSerializer<DateTime>(DateUtc));
    }
}