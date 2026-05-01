using MassTransit;

namespace ECP.Saga.Orchestrator.Extension;

/// <summary>
/// Bridges Kafka messages to the InMemory bus so the saga state machine
/// can persist state to MongoDB.
///
/// CRITICAL: Copies CorrelationId + all headers from the original Kafka message.
/// Without this, MassTransit generates a new CorrelationId on re-publish and
/// cannot match the existing saga instance — state transition is silently dropped.
///
/// FLOW:
///   Kafka Topic
///     -> KafkaForwardConsumer<T>
///     -> IPublishEndpoint (InMemory bus, headers preserved)
///     -> OrderStateMachine (matches by CorrelationId)
///     -> MongoDB (state saved)
/// </summary>
public sealed class KafkaForwardConsumer<T> : IConsumer<T> where T : class
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<KafkaForwardConsumer<T>> _logger;

    public KafkaForwardConsumer(
        IPublishEndpoint publishEndpoint,
        ILogger<KafkaForwardConsumer<T>> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Consume(ConsumeContext<T> context)
    {
        _logger.LogInformation(
            "[Kafka->InMemory] Forwarding {MessageType} | CorrelationId={CorrelationId} | MessageId={MessageId}",
            typeof(T).Name, context.CorrelationId, context.MessageId);

        await _publishEndpoint.Publish<T>(context.Message, ctx =>
        {
            if (context.CorrelationId.HasValue)
                ctx.CorrelationId = context.CorrelationId;

            if (context.MessageId.HasValue)
                ctx.MessageId = context.MessageId;

            if (context.ConversationId.HasValue)
                ctx.ConversationId = context.ConversationId;

            foreach (var (key, value) in context.Headers.GetAll())
                ctx.Headers.Set(key, value);

        }, context.CancellationToken);

        _logger.LogInformation(
            "[Kafka->InMemory] Forwarded {MessageType} successfully", typeof(T).Name);
    }
}
