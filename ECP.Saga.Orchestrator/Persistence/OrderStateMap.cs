using ECP.Saga.Orchestrator.StateData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ECP.Saga.Orchestrator.Persistence;

/// <summary>
/// MongoDB BSON class map for <see cref="OrderState"/>.
///
/// Registration rules applied consistently across all fields:
///
///   Guid (non-nullable)  → GuidSerializer(Standard)   — stored as BSON Binary subtype 4
///   Guid? (nullable)     → NullableSerializer wrapping GuidSerializer(Standard)
///   DateTime (non-null)  → DateTimeSerializer(DateOnly=false, Kind=Utc)
///   DateTime? (nullable) → NullableSerializer wrapping DateTimeSerializer(Utc)
///   string               → default StringSerializer  (BsonType.String is the BSON default)
///   decimal              → DecimalSerializer(Decimal128) — lossless, avoids float rounding
///
/// Design notes:
///   - GuidRepresentation.Standard writes UUID as BSON Binary subtype 4 (0x04).
///     This is the cross-driver standard and is required for MassTransit's
///     MongoDB saga repository to correlate documents by CorrelationId correctly.
///   - All DateTime fields are stored as UTC milliseconds (BSON Date).
///     DateTimeKind.Utc is enforced by the serializer — no silent timezone shifts.
///   - Nullable fields use NullableSerializer so missing/null BSON values
///     deserialize to null rather than throwing a deserialization exception.
///   - Items is stored as a plain BSON string (the JSON is already serialized
///     by the source-generated serializer in the state machine hot path).
/// </summary>
public class OrderStateClassMap : BsonClassMap<OrderState>
{
    // Reusable serializer instances — allocated once, shared across all documents.
    // BsonSerializer instances are thread-safe and stateless.
    private static readonly GuidSerializer      GuidStd     = new(GuidRepresentation.Standard);
    private static readonly DateTimeSerializer  DateUtc     = new(DateTimeKind.Utc);
    // DecimalSerializer(Decimal128) maps System.Decimal ↔ BSON Decimal128.
    // Do NOT use Decimal128Serializer — that maps Decimal128 ↔ Decimal128 and
    // throws "value type does not match member type System.Decimal" at startup.
    // DecimalSerializer with BsonType.Decimal128 is the correct bridge type.
    private static readonly DecimalSerializer DecimalBson = new(BsonType.Decimal128);

    public OrderStateClassMap()
    {
        AutoMap();

        // ── MassTransit required ──────────────────────────────────────────────

        MapIdProperty(x => x.CorrelationId)
            .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.GuidGenerator.Instance)
            .SetSerializer(GuidStd);

        MapProperty(x => x.CurrentState);

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

        // JSON string produced by the source-generated serializer in the state machine.
        MapProperty(x => x.Items)
            .SetSerializer(new StringSerializer(BsonType.String));

        // ── Payment idempotency tracking ──────────────────────────────────────

        // Nullable<Guid> — absent from the document until PaymentRequestActivity runs.
        MapProperty(x => x.PaymentIdempotencyKey)
            .SetSerializer(new NullableSerializer<Guid>(GuidStd));

        // ── Timeout scheduler tokens ──────────────────────────────────────────
        // Written by MassTransit Schedule(), cleared by Unschedule().
        // Must be Guid? so MassTransit can store and clear the scheduler token.

        MapProperty(x => x.InventoryTimeoutTokenId)
            .SetSerializer(new NullableSerializer<Guid>(GuidStd));

        MapProperty(x => x.PaymentTimeoutTokenId)
            .SetSerializer(new NullableSerializer<Guid>(GuidStd));

        // ── Error tracking ────────────────────────────────────────────────────
        // All nullable — only written when the saga enters the Failed state.

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