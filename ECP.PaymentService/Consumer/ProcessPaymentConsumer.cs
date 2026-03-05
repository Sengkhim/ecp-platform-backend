using Contracts;
using MassTransit;

namespace ECP.PaymentService.Consumer;

/// <summary>
/// Thrown by the payment gateway stub (or real client) when the provider
/// explicitly declines the payment — e.g. insufficient funds, card expired,
/// fraud block.
///
/// This is a business outcome, not a technical fault:
///   - The consumer catches this and publishes <c>PaymentFailed</c> to the saga.
///   - The message is NOT moved to the error queue.
///   - The saga compensates and transitions to Failed cleanly.
///
/// Contrast with any other <see cref="Exception"/>: a network timeout or
/// gateway 500 is a technical fault — the message IS moved to the error queue
/// for retry policy evaluation.
/// </summary>
public sealed class PaymentDeclinedException : Exception
{
    public string Reason { get; }

    public PaymentDeclinedException(string reason)
        : base($"Payment declined: {reason}")
    {
        Reason = reason;
    }
}

/// <summary>
/// Consumes <see cref="RequestPayment"/> commands published by
/// <c>PaymentRequestActivity</c> and drives the payment gateway.
///
/// Responsibility boundary:
///   - Receive the payment command from the saga orchestrator.
///   - Call the payment provider (stubbed here — inject your gateway client).
///   - Publish <see cref="ProcessPayment"/> on success  → saga transitions to Completed.
///   - Publish <see cref="PaymentFailed"/>  on failure  → saga transitions to Failed.
///   - Never re-publish <see cref="RequestPayment"/> — that would create an infinite loop.
///
/// Idempotency:
///   Every <see cref="RequestPayment"/> carries an <see cref="RequestPayment.IdempotencyKey"/>
///   derived deterministically from <see cref="RequestPayment.OrderId"/>.
///   This key MUST be forwarded to the payment provider so it can deduplicate
///   retries and avoid double-charging the customer.
/// </summary>
public sealed class ProcessPaymentConsumer : IConsumer<RequestPayment>
{
    private readonly ITopicProducer<ProcessPayment> _successProducer;
    private readonly ITopicProducer<PaymentFailed>  _failedProducer;
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(
        ITopicProducer<ProcessPayment> successProducer,
        ITopicProducer<PaymentFailed>  failedProducer,
        ILogger<ProcessPaymentConsumer> logger)
    {
        _successProducer = successProducer;
        _failedProducer  = failedProducer;
        _logger          = logger;
    }

    public async Task Consume(ConsumeContext<RequestPayment> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        _logger.LogInformation(
            "Processing payment for Order {OrderId} | Amount {Amount} {Currency} | " +
            "Method {PaymentMethod} | IdempotencyKey {IdempotencyKey}",
            msg.OrderId, msg.Amount, msg.Currency,
            msg.PaymentMethod, msg.IdempotencyKey);

        try
        {
            // ── Call your payment gateway here ────────────────────────────────
            // Pass msg.IdempotencyKey to the provider so it can deduplicate
            // retries. Example (Stripe):
            //
            //   await _stripeClient.ChargeAsync(new ChargeOptions
            //   {
            //       Amount        = (long)(msg.Amount * 100),
            //       Currency      = msg.Currency,
            //       PaymentMethod = msg.PaymentMethod,
            //       IdempotencyKey = msg.IdempotencyKey.ToString()   // ← required
            //   }, cancellationToken: ct);
            //
            // Replace the stub below with your real gateway call.
            // ─────────────────────────────────────────────────────────────────
            await ProcessWithGatewayAsync(msg, ct);

            // ── Notify the saga: payment succeeded ────────────────────────────
            // Publishes ProcessPayment which the state machine correlates via
            // OrderId and handles in When(PaymentProcessedEvent).
            await _successProducer.Produce(
                new ProcessPayment(
                    msg.OrderId,
                    msg.Amount,
                    msg.Currency,
                    msg.PaymentMethod,
                    msg.CustomerId,
                    msg.IdempotencyKey,
                    msg.PaymentRequestedAt),
                ct);

            _logger.LogInformation(
                "Payment succeeded for Order {OrderId}",
                msg.OrderId);
        }
        catch (PaymentDeclinedException ex)
        {
            // Business failure — payment was explicitly declined by the provider.
            // Publish PaymentFailed so the saga compensates gracefully.
            // Do NOT re-throw: the message should NOT be moved to the error queue
            // because this is an expected business outcome, not a technical fault.
            _logger.LogWarning(
                "Payment declined for Order {OrderId}. Reason: {Reason}",
                msg.OrderId, ex.Reason);

            await PublishFailedAsync(msg.OrderId, msg.Amount, ex.Reason, ct);
        }
        catch (Exception ex)
        {
            // Technical failure — gateway unreachable, timeout, unexpected error.
            // Log the exception, publish PaymentFailed so the saga compensates,
            // then re-throw so MassTransit moves the message to the error queue
            // for ops visibility and retry policy evaluation.
            _logger.LogError(ex,
                "Payment processing failed for Order {OrderId}",
                msg.OrderId);

            await PublishFailedAsync(msg.OrderId, msg.Amount,$"Payment processing error: {ex.Message}", ct);

            throw;
        }
    }

    // ── Gateway stub ──────────────────────────────────────────────────────────
    // Replace with your real payment gateway client injection + call.
    // This method should throw <see cref="PaymentDeclinedException"/> for
    // business declines and any other exception for technical failures.
    private static Task ProcessWithGatewayAsync(RequestPayment msg, CancellationToken ct)
    {
        // TODO: inject and call your payment gateway here.
        // Always forward msg.IdempotencyKey to prevent double-charging on retries.
        return Task.CompletedTask;
    }

    private Task PublishFailedAsync(Guid orderId, decimal amount, string reason, CancellationToken ct)
        => _failedProducer.Produce(new PaymentFailed(orderId,  amount, reason, DateTime.UtcNow), ct);
}