using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Internal shared core for all compensation activities.
///
/// Design rationale:
/// - MassTransit requires typed activities (<c>IStateMachineActivity&lt;TState, TData&gt;</c>)
///   AND state-only activities (<c>IStateMachineActivity&lt;TState&gt;</c>) to be separate classes
///   because the triggering event type is part of the generic contract.
/// - Rather than duplicating business logic across each pair, both variants
///   delegate here — one place to maintain, one place to test.
///
/// Error strategy:
/// - Failures are written into <see cref="OrderState"/> directly so they are
///   persisted alongside the saga row. This means ops/support can query the
///   saga repository (EF/Redis) and see exactly what failed without needing
///   a separate log aggregation query.
/// - The exception detail (type + message, no stack trace) is stored in
///   <see cref="OrderState.LastExceptionDetail"/> to keep the row compact.
/// - Structured logging is emitted at Warning/Error level for alerting pipelines.
/// </summary>
public static class CompensationCore
{
    // -------------------------------------------------------------------------
    // Inventory
    // -------------------------------------------------------------------------

    public static async Task RunInventoryAsync(
        OrderState saga,
        string reason,
        ITopicProducer<OrderFailed> producer,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        StampFailure(saga, step: "CheckInventory", reason);

        try
        {
            await producer.Produce(
                new OrderFailed(
                    saga.OrderId,
                    reason,
                    "CheckInventory",
                    DateTime.UtcNow),
                cancellationToken);

            logger.LogWarning(
                "Inventory compensation published. Order {OrderId} cancelled. Reason: {Reason}",
                saga.OrderId, reason);
        }
        catch (Exception ex)
        {
            // Producing the OrderFailed event itself failed.
            // Stamp the exception into the saga row so it is persisted
            // even if the broker is unavailable.
            StampException(saga, ex);

            logger.LogError(ex,
                "Failed to publish OrderFailed during inventory compensation. Order {OrderId}",
                saga.OrderId);

            // Re-throw so MassTransit can move the message to the error queue
            // and the saga row is not silently left in a broken state.
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Payment
    // -------------------------------------------------------------------------

    public static async Task RunPaymentAsync(
        OrderState saga,
        string reason,
        ITopicProducer<OrderFailed> producer,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        StampFailure(saga, step: "ProcessPayment", reason);

        try
        {
            await producer.Produce(
                new OrderFailed(
                    saga.OrderId,
                    reason,
                    "ProcessPayment",
                    DateTime.UtcNow),
                cancellationToken);

            logger.LogWarning(
                "Payment compensation published. Order {OrderId} cancelled. Reason: {Reason}",
                saga.OrderId, reason);
        }
        catch (Exception ex)
        {
            StampException(saga, ex);

            logger.LogError(ex,
                "Failed to publish OrderFailed during payment compensation. Order {OrderId}",
                saga.OrderId);

            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers — write failure metadata into the saga row
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stamps failure metadata into the saga row before the produce call.
    /// Written first so that even if Produce() throws, the row still reflects
    /// what was attempted.
    /// </summary>
    private static void StampFailure(OrderState saga, string step, string reason)
    {
        saga.FailedStep     = step;
        saga.FailureReason  = reason;
        saga.FailedAt       = DateTime.UtcNow;
        saga.LastUpdatedAt  = DateTime.UtcNow;
    }

    /// <summary>
    /// Stamps a compact exception summary (type + message only, no stack trace)
    /// into the saga row. Avoids bloating the row while still providing enough
    /// context for triage without opening a log aggregator.
    /// </summary>
    private static void StampException(OrderState saga, Exception ex)
    {
        saga.LastExceptionDetail =
            $"[{ex.GetType().Name}] {ex.Message}";
        saga.LastUpdatedAt = DateTime.UtcNow;
    }
}