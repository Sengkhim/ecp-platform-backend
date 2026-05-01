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
///   1. Guid stored as BsonType.String (human-readable, no global GuidSerializer conflict).
///   2. decimal stored as Decimal128.
///   3. ProductDocument explicitly class-mapped so AutoMap only sees primitives.
///   4. NO value object serializers registered globally — ProductDocument only
///      contains primitives (Guid, string, decimal, int, DateTime), so MongoDB
///      never encounters ProductId / CategoryId / Slug on the wire.
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

            // ── decimal → Decimal128 ──────────────────────────────────────────
            BsonSerializer.RegisterSerializer(
                new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(
                new NullableSerializer<decimal>(
                    new DecimalSerializer(BsonType.Decimal128)));

            // ── ProductDocument class map ─────────────────────────────────────
            // Guid fields use GuidRepresentation.Standard explicitly on each
            // member to avoid the "GuidRepresentation is Unspecified" error.
            // Do NOT register a global GuidSerializer — it fights with the
            // class map member-level setting.
            if (!BsonClassMap.IsClassMapRegistered(typeof(ProductDocument)))
            {
                BsonClassMap.RegisterClassMap<ProductDocument>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);

                    // Id: Guid stored as standard UUID string
                    cm.MapIdMember(c => c.Id)
                        .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));

                    // CategoryId: also a Guid
                    cm.MapMember(c => c.CategoryId)
                        .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));

                    // decimal fields
                    cm.MapMember(c => c.Price)
                        .SetSerializer(new DecimalSerializer(BsonType.Decimal128));
                    cm.MapMember(c => c.SalePrice)
                        .SetSerializer(new NullableSerializer<decimal>(
                            new DecimalSerializer(BsonType.Decimal128)));
                });
            }

            _registered = true;
        }
    }
}