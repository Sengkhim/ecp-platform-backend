using ECP.ProductService.Core.Domain.ValueObjects;
using ECP.ProductService.Infrastructure.Persistence.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ECP.ProductService.Infrastructure.Persistence.Configuration;


/// <summary>
/// Registers ALL MongoDB BSON mappings once at startup before any
/// MongoClient or collection is touched.
///
/// Rules:
///   1. No [BsonRepresentation] attributes on documents — they conflict
///      with custom serializers registered here.
///   2. Guid stored as string. Decimal stored as Decimal128.
///   3. ProductDocument explicitly class-mapped so MongoDB never auto-maps
///      domain value objects.
/// </summary>
public static class MongoDbConfiguration
{
    private static bool _registered;
    private static readonly Lock Lock = new();

    public static void RegisterSerializers()
    {
        lock (Lock)
        {
            if (_registered) return;

            // ── 1. Primitive overrides ────────────────────────────────────────
            // Guid → string (human-readable in MongoDB)
            BsonSerializer.RegisterSerializer(
                new GuidSerializer(GuidRepresentation.Standard));

            // decimal → Decimal128 (preserves precision)
            BsonSerializer.RegisterSerializer(
                new DecimalSerializer(BsonType.Decimal128));

            BsonSerializer.RegisterSerializer(
                new NullableSerializer<decimal>(
                    new DecimalSerializer(BsonType.Decimal128)));

            // ── 2. Value object serializers ───────────────────────────────────
            // These ensure MongoDB never tries to auto-map domain structs
            BsonSerializer.RegisterSerializer(new ProductIdSerializer());
            BsonSerializer.RegisterSerializer(new CategoryIdSerializer());
            BsonSerializer.RegisterSerializer(new SlugSerializer());

            // ── 3. Explicit class map for ProductDocument ─────────────────────
            // Prevents auto-mapping; every field mapped by name explicitly.
            if (!BsonClassMap.IsClassMapRegistered(typeof(ProductDocument)))
            {
                BsonClassMap.RegisterClassMap<ProductDocument>(cm =>
                {
                    cm.AutoMap();           // map all public properties by convention
                    cm.SetIgnoreExtraElements(true); // forward-compatible reads
                    cm.MapIdMember(c => c.Id);
                });
            }

            _registered = true;
        }
    }
}

// ── Serializers for domain value objects ─────────────────────────────────────
// Each reads/writes as a plain string. MongoDB stores a string,
// we rehydrate the value object on read.

public sealed class ProductIdSerializer : SerializerBase<ProductId>
{
    public override ProductId Deserialize(
        BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => ProductId.From(Guid.Parse(ctx.Reader.ReadString()));

    public override void Serialize(
        BsonSerializationContext ctx, BsonSerializationArgs args, ProductId value)
        => ctx.Writer.WriteString(value.Value.ToString());
}

public sealed class CategoryIdSerializer : SerializerBase<CategoryId>
{
    public override CategoryId Deserialize(
        BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => CategoryId.From(Guid.Parse(ctx.Reader.ReadString()));

    public override void Serialize(
        BsonSerializationContext ctx, BsonSerializationArgs args, CategoryId value)
        => ctx.Writer.WriteString(value.Value.ToString());
}

public sealed class SlugSerializer : SerializerBase<Slug>
{
    public override Slug Deserialize(
        BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => Slug.Parse(ctx.Reader.ReadString());

    public override void Serialize(
        BsonSerializationContext ctx, BsonSerializationArgs args, Slug value)
        => ctx.Writer.WriteString(value.Value);
}