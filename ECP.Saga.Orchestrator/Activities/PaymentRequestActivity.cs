using System.Security.Cryptography;
using System.Text;
using Contracts;
// using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;
// using Microsoft.Extensions.Logging;

namespace ECP.Saga.Orchestrator.Activities;
// <summary>
/// Generates deterministic UUID v5 values (RFC 4122 §4.3).
///
/// UUID v5 = SHA-1(namespace_bytes + name_bytes), truncated to 128 bits
/// with version/variant bits set per RFC 4122.
///
/// Properties that make it correct for payment idempotency keys:
///   - Same namespace + same name → always the same Guid (deterministic)
///   - Different namespace or name → different Guid (collision-resistant)
///   - No dependency on wall-clock time, random seed, or process state
///   - Survives process restarts, re-deployments, and broker replays
///
/// Usage:
///   var key = GuidV5.Create(MyNamespace, orderId.ToString());
/// </summary>
internal static class GuidV5
{
    /// <summary>
    /// Creates a deterministic UUID v5 from a fixed namespace and a name string.
    /// </summary>
    /// <param name="namespaceId">
    ///   A fixed, private Guid that scopes the key space.
    ///   Use a different namespace per domain concept (e.g. one for payments,
    ///   one for notifications) to prevent accidental key collisions.
    /// </param>
    /// <param name="name">
    ///   The unique name within the namespace (e.g. OrderId.ToString()).
    /// </param>
    public static Guid Create(Guid namespaceId, string name)
    {
        // 1. Namespace bytes in big-endian network order (RFC 4122 requirement)
        var namespaceBytes = ToNetworkBytes(namespaceId);

        // 2. Name bytes as UTF-8
        var nameBytes = Encoding.UTF8.GetBytes(name);

        // 3. SHA-1 hash of namespace || name
        Span<byte> buffer = stackalloc byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(buffer);
        nameBytes.CopyTo(buffer[namespaceBytes.Length..]);

        Span<byte> hash = stackalloc byte[20]; // SHA-1 = 20 bytes
        SHA1.HashData(buffer, hash);

        // 4. Take first 16 bytes
        Span<byte> guidBytes = stackalloc byte[16];
        hash[..16].CopyTo(guidBytes);

        // 5. Set version = 5 (0101 in high nibble of byte 6)
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);

        // 6. Set variant = RFC 4122 (10xxxxxx in byte 8)
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        // 7. Reconstruct as Guid — convert back from network byte order
        return FromNetworkBytes(guidBytes);
    }

    // ── Byte order helpers ────────────────────────────────────────────────────
    // .NET's Guid uses little-endian layout for the first three components.
    // RFC 4122 specifies big-endian (network) order.
    // We must convert before hashing and convert back after.

    private static byte[] ToNetworkBytes(Guid guid)
    {
        var b = guid.ToByteArray(); // .NET little-endian layout

        // Reverse the three little-endian fields to big-endian
        Array.Reverse(b, 0, 4);  // Data1 (int)
        Array.Reverse(b, 4, 2);  // Data2 (short)
        Array.Reverse(b, 6, 2);  // Data3 (short)
        // Data4 (8 bytes) is already big-endian in both layouts

        return b;
    }

    private static Guid FromNetworkBytes(Span<byte> b)
    {
        // Make a mutable copy
        Span<byte> le = stackalloc byte[16];
        b.CopyTo(le);

        // Reverse back from big-endian to .NET little-endian
        le[..4].Reverse();
        le[4..6].Reverse();
        le[6..8].Reverse();

        return new Guid(le);
    }
}
/// <summary>
/// Publishes a <see cref="RequestPayment"/> command to the payment topic
/// when inventory has been successfully reserved.
///
/// ── Duplicate payment prevention ──────────────────────────────────────────
///
/// Duplication source: MassTransit uses at-least-once delivery. The broker
/// can redeliver <c>InventoryReserved</c>, or the process can crash after
/// this activity publishes but before the saga row is saved — causing a replay.
///
/// Defence: every <see cref="RequestPayment"/> command carries a stable
/// <see cref="RequestPayment.IdempotencyKey"/> derived deterministically from
/// <see cref="OrderState.OrderId"/> via GuidV5 (RFC 4122 UUID v5).
///
///   Same OrderId → always the same key, on every run, on every host,
///   after any restart.
///
/// The payment provider must honour this key by returning the original
/// result for any re-submission — no second charge is processed.
///
/// <see cref="OrderState.PaymentIdempotencyKey"/> persists the key in the saga
/// row so the same value is reused on replay without recomputing it.
/// The <c>??=</c> assignment is safe: GuidV5 is deterministic, so even if
/// the row was not saved on a previous run, the recomputed value is identical.
///
/// ── Interface choice ──────────────────────────────────────────────────────
/// Implements <c>IStateMachineActivity&lt;OrderState, InventoryReserved&gt;</c>
/// because it is wired from <c>When(InventoryReservedEvent)</c>. MassTransit
/// requires the data type to match the triggering event exactly.
/// </summary>
public sealed class PaymentRequestActivity :
    IStateMachineActivity<OrderState, InventoryReserved>
{
    // Fixed namespace for GuidV5 derivation — arbitrary but must never change.
    // Changing this would generate different keys for existing orders on replay.
    private static readonly Guid PaymentNamespace = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private readonly ITopicProducer<RequestPayment> _producer;
    private readonly ILogger<PaymentRequestActivity> _logger;

    public PaymentRequestActivity(
        ITopicProducer<RequestPayment> producer,
        ILogger<PaymentRequestActivity> logger)
    {
        _producer = producer;
        _logger   = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("payment-request");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, InventoryReserved> context,
        IBehavior<OrderState, InventoryReserved> next)
    {
        var saga = context.Saga;

        // Derive the idempotency key and persist it.
        // ??= is safe here: GuidV5 is deterministic — if the saga row was not
        // saved on a previous run the recomputed value is bit-for-bit identical.
        saga.PaymentIdempotencyKey ??= GuidV5.Create(PaymentNamespace, saga.OrderId.ToString());
        saga.LastUpdatedAt          = DateTime.UtcNow;

        try
        {
            await _producer.Produce(
                new RequestPayment(
                    OrderId:        saga.OrderId,
                    CustomerId:     saga.CustomerId,
                    Amount:         saga.TotalAmount,
                    Currency:       saga.Currency,
                    PaymentMethod:  saga.PaymentMethod,
                    IdempotencyKey: saga.PaymentIdempotencyKey.Value,
                    PaymentRequestedAt: DateTime.UtcNow),
                context.CancellationToken);

            _logger.LogInformation(
                "Payment request published. Order {OrderId} | " +
                "Amount {Amount} {Currency} | IdempotencyKey {IdempotencyKey}",
                saga.OrderId,
                saga.TotalAmount,
                saga.Currency,
                saga.PaymentIdempotencyKey.Value);
        }
        catch (Exception ex)
        {
            // Stamp exception into the saga row before re-throwing.
            // The row is persisted by MassTransit even on fault, so ops can
            // query the saga repository and see what failed without needing
            // to open a log aggregator.
            saga.LastExceptionDetail = $"[{ex.GetType().Name}] {ex.Message}";
            saga.LastUpdatedAt       = DateTime.UtcNow;

            _logger.LogError(ex,
                "Failed to publish payment request for Order {OrderId}",
                saga.OrderId);

            // Re-throw so MassTransit faults the behavior chain and moves
            // the message to the error queue.
            throw;
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryReserved, TException> context,
        IBehavior<OrderState, InventoryReserved> next)
        where TException : Exception
    {
        context.Saga.LastExceptionDetail =
            $"[{context.Exception.GetType().Name}] {context.Exception.Message}";
        context.Saga.LastUpdatedAt = DateTime.UtcNow;

        _logger.LogError(context.Exception,
            "PaymentRequestActivity faulted for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}