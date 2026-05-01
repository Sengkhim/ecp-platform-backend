using Contracts;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

/// <summary>
/// Shared core for all compensation activities.
///
/// Every failure path:
///   1. Stamps FailedStep / FailureReason / FailedAt into the saga row (persisted to MongoDB)
///   2. Writes a document to the "saga_errors" collection via SagaErrorLogger
///   3. Publishes an OrderFailed event to Kafka for downstream consumers
///   4. Re-throws if the Kafka publish itself fails so MassTransit moves to error queue
/// </summary>
public static class CompensationCore
{
    // -------------------------------------------------------------------------
    // Inventory
    // -------------------------------------------------------------------------

    public static async Task RunInventoryAsync(
        OrderState              saga,
        string                  reason,
        ITopicProducer<OrderFailed> producer,
        ILogger                 logger,
        SagaErrorLogger?        errorLogger        = null,
        CancellationToken       cancellationToken  = default)
    {
        const string step = "CheckInventory";
        StampFailure(saga, step, reason);

        // Write business failure to MongoDB saga_errors
        if (errorLogger is not null)
            await errorLogger.LogFailureAsync(
                saga.CorrelationId, saga.OrderId, saga.CurrentState,
                step, reason, cancellationToken);

        try
        {
            await producer.Produce(
                new OrderFailed(saga.OrderId, reason, step, DateTime.UtcNow),
                cancellationToken);

            logger.LogWarning(
                "Inventory compensation published. Order {OrderId} cancelled. Reason: {Reason}",
                saga.OrderId, reason);
        }
        catch (Exception ex)
        {
            StampException(saga, ex);

            // Write Kafka publish failure to MongoDB saga_errors
            if (errorLogger is not null)
                await errorLogger.LogExceptionAsync(
                    saga.CorrelationId, saga.OrderId, saga.CurrentState,
                    $"{step}.PublishFailed", ex, cancellationToken);

            logger.LogError(ex,
                "Failed to publish OrderFailed during inventory compensation. Order {OrderId}",
                saga.OrderId);

            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Payment
    // -------------------------------------------------------------------------

    public static async Task RunPaymentAsync(
        OrderState              saga,
        string                  reason,
        ITopicProducer<OrderFailed> producer,
        ILogger                 logger,
        SagaErrorLogger?        errorLogger        = null,
        CancellationToken       cancellationToken  = default)
    {
        const string step = "ProcessPayment";
        StampFailure(saga, step, reason);

        // Write business failure to MongoDB saga_errors
        if (errorLogger is not null)
            await errorLogger.LogFailureAsync(
                saga.CorrelationId, saga.OrderId, saga.CurrentState,
                step, reason, cancellationToken);

        try
        {
            await producer.Produce(
                new OrderFailed(saga.OrderId, reason, step, DateTime.UtcNow),
                cancellationToken);

            logger.LogWarning(
                "Payment compensation published. Order {OrderId} cancelled. Reason: {Reason}",
                saga.OrderId, reason);
        }
        catch (Exception ex)
        {
            StampException(saga, ex);

            // Write Kafka publish failure to MongoDB saga_errors
            if (errorLogger is not null)
                await errorLogger.LogExceptionAsync(
                    saga.CorrelationId, saga.OrderId, saga.CurrentState,
                    $"{step}.PublishFailed", ex, cancellationToken);

            logger.LogError(ex,
                "Failed to publish OrderFailed during payment compensation. Order {OrderId}",
                saga.OrderId);

            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public static void StampFailure(OrderState saga, string step, string reason)
    {
        saga.FailedStep    = step;
        saga.FailureReason = reason;
        saga.FailedAt      = DateTime.UtcNow;
        saga.LastUpdatedAt = DateTime.UtcNow;
    }

    public static void StampException(OrderState saga, Exception ex)
    {
        saga.LastExceptionDetail = $"[{ex.GetType().Name}] {ex.Message}";
        saga.LastUpdatedAt       = DateTime.UtcNow;
    }
}
