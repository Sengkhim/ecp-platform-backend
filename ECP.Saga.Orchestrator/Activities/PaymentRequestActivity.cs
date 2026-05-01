using System.Security.Cryptography;
using System.Text;
using Contracts;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Deterministic UUID v5 generator (RFC 4122 §4.3).
/// Same namespace + name always produces the same Guid — safe for payment idempotency keys.
/// </summary>
internal static class GuidV5
{
    public static Guid Create(Guid namespaceId, string name)
    {
        var nsBytes   = ToNetworkBytes(namespaceId);
        var nameBytes = Encoding.UTF8.GetBytes(name);

        Span<byte> buffer = stackalloc byte[nsBytes.Length + nameBytes.Length];
        nsBytes.CopyTo(buffer);
        nameBytes.CopyTo(buffer[nsBytes.Length..]);

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(buffer, hash);

        Span<byte> guid = stackalloc byte[16];
        hash[..16].CopyTo(guid);
        guid[6] = (byte)((guid[6] & 0x0F) | 0x50); // version 5
        guid[8] = (byte)((guid[8] & 0x3F) | 0x80); // RFC 4122 variant

        return FromNetworkBytes(guid);
    }

    private static byte[] ToNetworkBytes(Guid g)
    {
        var b = g.ToByteArray();
        Array.Reverse(b, 0, 4);
        Array.Reverse(b, 4, 2);
        Array.Reverse(b, 6, 2);
        return b;
    }

    private static Guid FromNetworkBytes(Span<byte> b)
    {
        Span<byte> le = stackalloc byte[16];
        b.CopyTo(le);
        le[..4].Reverse();
        le[4..6].Reverse();
        le[6..8].Reverse();
        return new Guid(le);
    }
}

public sealed class PaymentRequestActivity :
    IStateMachineActivity<OrderState, InventoryReserved>
{
    // Must never change — changing this invalidates all in-flight idempotency keys
    private static readonly Guid PaymentNamespace = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private readonly ITopicProducer<RequestPayment> _producer;
    private readonly SagaErrorLogger _errorLogger;
    private readonly ILogger<PaymentRequestActivity> _logger;

    public PaymentRequestActivity(
        ITopicProducer<RequestPayment> producer,
        SagaErrorLogger errorLogger,
        ILogger<PaymentRequestActivity> logger)
    {
        _producer    = producer;
        _errorLogger = errorLogger;
        _logger      = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("payment-request");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, InventoryReserved> context,
        IBehavior<OrderState, InventoryReserved> next)
    {
        var saga = context.Saga;

        saga.PaymentIdempotencyKey ??= GuidV5.Create(PaymentNamespace, saga.OrderId.ToString());
        saga.LastUpdatedAt          = DateTime.UtcNow;

        try
        {
            await _producer.Produce(
                new RequestPayment(
                    OrderId:            saga.OrderId,
                    CustomerId:         saga.CustomerId,
                    Amount:             saga.TotalAmount,
                    Currency:           saga.Currency,
                    PaymentMethod:      saga.PaymentMethod,
                    IdempotencyKey:     saga.PaymentIdempotencyKey.Value,
                    PaymentRequestedAt: DateTime.UtcNow),
                context.CancellationToken);

            _logger.LogInformation(
                "Payment request published. Order {OrderId} | Amount {Amount} {Currency} | IdempotencyKey {Key}",
                saga.OrderId, saga.TotalAmount, saga.Currency, saga.PaymentIdempotencyKey.Value);
        }
        catch (Exception ex)
        {
            CompensationCore.StampException(saga, ex);

            await _errorLogger.LogExceptionAsync(
                saga.CorrelationId, saga.OrderId, saga.CurrentState,
                "PaymentRequest", ex, context.CancellationToken);

            _logger.LogError(ex,
                "Failed to publish payment request for Order {OrderId}", saga.OrderId);

            throw;
        }

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryReserved, TException> context,
        IBehavior<OrderState, InventoryReserved> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "PaymentRequest.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "PaymentRequestActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }
}
