using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ECP.Saga.Orchestrator.Infrastructure;

/// <summary>
/// Writes saga error events to a dedicated MongoDB collection ("saga_errors").
///
/// WHY A SEPARATE COLLECTION instead of embedding errors in the saga row:
///   - Saga rows are DELETED by SetCompletedWhenFinalized() when the saga
///     transitions to Completed or Failed. After finalization the row is gone,
///     taking any embedded error history with it.
///   - A dedicated collection retains full error history for ops dashboards,
///     alerting pipelines, and post-mortem analysis regardless of finalization.
///   - Keeping the saga row compact improves MongoDB write throughput.
///
/// RESILIENCE: All methods swallow exceptions internally. A logging failure
/// must never affect saga flow — the error is printed to the console only.
/// </summary>
public sealed class SagaErrorLogger
{
    private readonly IMongoCollection<SagaErrorDocument> _collection;
    private readonly ILogger<SagaErrorLogger> _logger;

    public SagaErrorLogger(IConfiguration configuration, ILogger<SagaErrorLogger> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDB:SagaConnection"]
                            ?? "mongodb://root:pass168@mongodb.ecp-dev.svc.cluster.local:27017/sagas?authSource=admin";

        var client   = new MongoClient(connectionString);
        var database = client.GetDatabase("sagas");
        _collection  = database.GetCollection<SagaErrorDocument>("saga_errors");

        EnsureIndexes();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Logs a business-level saga failure (e.g. InventoryFailed, PaymentFailed, timeout).
    /// </summary>
    public async Task LogFailureAsync(
        Guid   correlationId,
        Guid   orderId,
        string currentState,
        string failedStep,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await InsertSafeAsync(new SagaErrorDocument
        {
            CorrelationId = correlationId,
            OrderId       = orderId,
            CurrentState  = currentState,
            ErrorStep     = failedStep,
            ErrorMessage  = reason,
            ErrorKind     = SagaErrorKind.BusinessFailure,
            OccurredAt    = DateTime.UtcNow,
        }, cancellationToken);
    }

    /// <summary>
    /// Logs an unexpected exception thrown inside a saga activity or faulted handler.
    /// </summary>
    public async Task LogExceptionAsync(
        Guid      correlationId,
        Guid      orderId,
        string    currentState,
        string    failedStep,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        await InsertSafeAsync(new SagaErrorDocument
        {
            CorrelationId   = correlationId,
            OrderId         = orderId,
            CurrentState    = currentState,
            ErrorStep       = failedStep,
            ErrorMessage    = exception.Message,
            ErrorKind       = SagaErrorKind.UnhandledException,
            ExceptionType   = exception.GetType().FullName,
            ExceptionDetail = exception.ToString(),
            OccurredAt      = DateTime.UtcNow,
        }, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task InsertSafeAsync(SagaErrorDocument doc, CancellationToken ct)
    {
        try
        {
            await _collection.InsertOneAsync(doc, cancellationToken: ct);

            _logger.LogDebug(
                "[SagaErrorLogger] Logged {Kind} for Order {OrderId} at step {Step}",
                doc.ErrorKind, doc.OrderId, doc.ErrorStep);
        }
        catch (Exception ex)
        {
            // Never throw from the error logger — swallow and warn to console only
            _logger.LogWarning(ex,
                "[SagaErrorLogger] Failed to write error document for CorrelationId={CorrelationId}",
                doc.CorrelationId);
        }
    }

    private void EnsureIndexes()
    {
        try
        {
            // Fast lookup by saga instance
            var byCorrelation = Builders<SagaErrorDocument>.IndexKeys
                .Ascending(x => x.CorrelationId)
                .Descending(x => x.OccurredAt);

            // Fast lookup by order
            var byOrder = Builders<SagaErrorDocument>.IndexKeys
                .Ascending(x => x.OrderId)
                .Descending(x => x.OccurredAt);

            // TTL index — auto-delete error documents after 90 days
            var ttl = Builders<SagaErrorDocument>.IndexKeys
                .Ascending(x => x.OccurredAt);

            _collection.Indexes.CreateMany([
                new CreateIndexModel<SagaErrorDocument>(byCorrelation),
                new CreateIndexModel<SagaErrorDocument>(byOrder),
                new CreateIndexModel<SagaErrorDocument>(
                    ttl,
                    new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90) }),
            ]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SagaErrorLogger] Failed to ensure indexes on saga_errors");
        }
    }
}

// -------------------------------------------------------------------------
// Document schema
// -------------------------------------------------------------------------

public enum SagaErrorKind
{
    BusinessFailure,     // InventoryFailed, PaymentFailed, Timeout
    UnhandledException   // Activity or faulted handler threw
}

public sealed class SagaErrorDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.String)]
    public Guid CorrelationId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }

    public string        CurrentState    { get; set; } = string.Empty;
    public string        ErrorStep       { get; set; } = string.Empty;
    public string        ErrorMessage    { get; set; } = string.Empty;
    public SagaErrorKind ErrorKind       { get; set; }
    public string?       ExceptionType   { get; set; }
    public string?       ExceptionDetail { get; set; }
    public DateTime      OccurredAt      { get; set; }
}
