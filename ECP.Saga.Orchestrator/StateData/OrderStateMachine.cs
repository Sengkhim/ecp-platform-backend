using System.Text.Json;
using System.Text.Json.Serialization;
using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.Saga.Orchestrator.Activities;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECP.Saga.Orchestrator.StateData;

// -----------------------------------------------------------------------------
// Source-generated JSON serializer context.
//
// Replaces the default reflective JsonSerializer in the hot path (Initially block).
// Benefits at high throughput:
//   - Zero reflection overhead on first use
//   - Uses pooled ArrayBufferWriter<byte> internally
//   - Trim-safe for AOT/NativeAOT publishing
// -----------------------------------------------------------------------------
[JsonSerializable(typeof(List<OrderItemInfoEvent>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class OrderSagaJsonContext : JsonSerializerContext { }

/// <summary>
/// Orchestrates the order fulfillment saga:
///
///   Initial
///     └─ OrderCreated ──► AwaitingInventory
///                              ├─ InventoryReserved ──► AwaitingPayment
///                              │                             ├─ PaymentProcessed ──► Completed ──► (finalized)
///                              │                             ├─ PaymentFailed    ──► Failed    ──► (finalized)
///                              │                             └─ PaymentTimeout   ──► Failed    ──► (finalized)
///                              ├─ InventoryFailed  ──► Failed ──► (finalized)
///                              └─ InventoryTimeout ──► Failed ──► (finalized)
///
/// High-throughput design decisions:
///   1. Source-generated JSON serializer — no reflection on the hot path.
///   2. ILogger structured logging — no Console.WriteLine global lock contention.
///   3. Ignore() guards on all out-of-order events — prevents requeue storms.
///   4. Idempotency guard on Initially — broker retries cannot overwrite saga state.
///   5. Schedule/Unschedule timeouts — saga instances cannot pin open forever.
///   6. .Finalize() on terminal states — saga rows are deleted, table stays bounded.
///   7. Failure metadata written to OrderState — ops can query the saga repo directly.
/// </summary>
[UsedImplicitly]
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // -------------------------------------------------------------------------
    // States
    // -------------------------------------------------------------------------
    public State AwaitingInventory { get; private set; } = null!;
    public State AwaitingPayment   { get; private set; } = null!;
    public State Completed         { get; private set; } = null!;
    public State Failed            { get; private set; } = null!;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------
    public Event<OrderCreatedEvent> OrderCreatedEvent      { get; private set; } = null!;
    public Event<InventoryReserved> InventoryReservedEvent { get; private set; } = null!;
    public Event<InventoryFailed>   InventoryFailedEvent   { get; private set; } = null!;
    public Event<ProcessPayment>    PaymentProcessedEvent  { get; private set; } = null!;
    public Event<PaymentFailed>     PaymentFailedEvent     { get; private set; } = null!;

    // -------------------------------------------------------------------------
    // Schedules
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fires <see cref="InventoryTimeout"/> if the inventory service does not
    /// respond within 30 seconds. Triggers <see cref="InventoryTimeoutCompensationActivity"/>.
    /// </summary>
    public Schedule<OrderState, InventoryTimeout> InventoryTimeoutSchedule { get; private set; } = null!;

    /// <summary>
    /// Fires <see cref="PaymentTimeout"/> if the payment service does not
    /// respond within 60 seconds. Triggers <see cref="PaymentTimeoutCompensationActivity"/>.
    /// </summary>
    public Schedule<OrderState, PaymentTimeout> PaymentTimeoutSchedule { get; private set; } = null!;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------
    private readonly ILogger<OrderStateMachine> _logger;

    public OrderStateMachine(ILogger<OrderStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        ConfigureCorrelation();
        ConfigureSchedules();
        ConfigureInitial();
        ConfigureAwaitingInventory();
        ConfigureAwaitingPayment();
        ConfigureGlobalGuards();
        ConfigureFinalization();
    }

    // -------------------------------------------------------------------------
    // Correlation
    // -------------------------------------------------------------------------
    private void ConfigureCorrelation()
    {
        Event(() => OrderCreatedEvent,
            x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Event(() => InventoryReservedEvent,
            x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Event(() => InventoryFailedEvent,
            x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Event(() => PaymentProcessedEvent,
            x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Event(() => PaymentFailedEvent,
            x => x.CorrelateById(ctx => ctx.Message.OrderId));
    }

    // -------------------------------------------------------------------------
    // Schedules
    // -------------------------------------------------------------------------
    private void ConfigureSchedules()
    {
        Schedule(() => InventoryTimeoutSchedule,
            instance => instance.InventoryTimeoutTokenId,
            s =>
            {
                s.Delay    = TimeSpan.FromSeconds(30);
                s.Received = r => r.CorrelateById(ctx => ctx.Message.OrderId);
            });

        Schedule(() => PaymentTimeoutSchedule,
            instance => instance.PaymentTimeoutTokenId,
            s =>
            {
                s.Delay    = TimeSpan.FromSeconds(60);
                s.Received = r => r.CorrelateById(ctx => ctx.Message.OrderId);
            });
    }

    // -------------------------------------------------------------------------
    // Initial
    // -------------------------------------------------------------------------
    private void ConfigureInitial()
    {
        Initially(
            When(OrderCreatedEvent)
                // Idempotency guard: CorrelationId is set by MassTransit before this runs,
                // but OrderId is only set by our .Then() below.
                // On a broker retry the saga row already exists and OrderId will be non-empty,
                // so we skip re-hydration and let the DuringAny guard log the duplicate.
                .If(ctx => ctx.Saga.OrderId == Guid.Empty, binder => binder
                    .Then(ctx =>
                    {
                        var msg  = ctx.Message;
                        var saga = ctx.Saga;

                        saga.OrderId        = msg.OrderId;
                        saga.CustomerId     = msg.CustomerId;
                        saga.CustomerName   = msg.CustomerName;
                        saga.CustomerEmail  = msg.CustomerEmail;
                        saga.OrderNumber    = msg.OrderNumber;
                        saga.TotalAmount    = msg.TotalAmount;
                        saga.CreatedAt      = msg.CreatedAt;
                        saga.PaymentMethod  = msg.PaymentMethod;
                        saga.Currency       = msg.Currency;
                        saga.LastUpdatedAt  = DateTime.UtcNow;

                        // Source-generated serializer — zero reflection, no runtime
                        // type discovery, pooled buffer writer.
                        saga.Items = JsonSerializer.Serialize(
                            msg.Items,
                            OrderSagaJsonContext.Default.ListOrderItemInfoEvent);
                    })
                    .Activity(x => x.OfType<CheckInventoryActivity>())
                    .Schedule(InventoryTimeoutSchedule,
                        ctx => ctx.Init<InventoryTimeout>(new { ctx.Saga.OrderId }))
                    .TransitionTo(AwaitingInventory))
        );
    }

    // -------------------------------------------------------------------------
    // AwaitingInventory
    // -------------------------------------------------------------------------
    private void ConfigureAwaitingInventory()
    {
        During(AwaitingInventory,

            When(InventoryReservedEvent)
                .Unschedule(InventoryTimeoutSchedule)
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .Activity(x => x.OfType<PaymentRequestActivity>())
                .Schedule(PaymentTimeoutSchedule,
                    ctx => ctx.Init<PaymentTimeout>(new { ctx.Saga.OrderId }))
                .TransitionTo(AwaitingPayment),

            When(InventoryFailedEvent)
                .Unschedule(InventoryTimeoutSchedule)
                // InventoryCompensationActivity stamps FailedStep + FailureReason into saga
                .Activity(x => x.OfType<InventoryCompensationActivity>())
                .TransitionTo(Failed),

            When(InventoryTimeoutSchedule.Received)
                .Then(ctx =>
                {
                    _logger.LogWarning(
                        "Inventory timeout elapsed for Order {OrderId}. Transitioning to Failed.",
                        ctx.Saga.OrderId);
                })
                // InventoryTimeoutCompensationActivity stamps FailedStep + FailureReason into saga
                .Activity(x => x.OfType<InventoryTimeoutCompensationActivity>())
                .TransitionTo(Failed),

            // Discard payment events that arrive before inventory has settled.
            // Without Ignore(), MassTransit's behaviour is transport-dependent:
            // some transports requeue indefinitely → unbounded requeue storm.
            Ignore(PaymentProcessedEvent),
            Ignore(PaymentFailedEvent)
        );
    }

    // -------------------------------------------------------------------------
    // AwaitingPayment
    // -------------------------------------------------------------------------
    private void ConfigureAwaitingPayment()
    {
        During(AwaitingPayment,

            When(PaymentProcessedEvent)
                .Unschedule(PaymentTimeoutSchedule)
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .Activity(x => x.OfType<SendNotificationActivity>())
                .TransitionTo(Completed),

            When(PaymentFailedEvent)
                .Unschedule(PaymentTimeoutSchedule)
                // PaymentCompensationActivity stamps FailedStep + FailureReason into saga
                .Activity(x => x.OfType<PaymentCompensationActivity>())
                .TransitionTo(Failed),

            When(PaymentTimeoutSchedule.Received)
                .Then(ctx =>
                {
                    _logger.LogWarning(
                        "Payment timeout elapsed for Order {OrderId}. Transitioning to Failed.",
                        ctx.Saga.OrderId);
                })
                // PaymentTimeoutCompensationActivity stamps FailedStep + FailureReason into saga
                .Activity(x => x.OfType<PaymentTimeoutCompensationActivity>())
                .TransitionTo(Failed),

            // Discard duplicate inventory events — inventory already settled.
            Ignore(InventoryReservedEvent),
            Ignore(InventoryFailedEvent)
        );
    }

    // -------------------------------------------------------------------------
    // Global Guards
    // -------------------------------------------------------------------------
    private void ConfigureGlobalGuards()
    {
        DuringAny(
            When(OrderCreatedEvent)
                .If(ctx => ctx.Saga.CurrentState != Initial.Name,
                    x => x.Then(ctx =>
                        _logger.LogWarning(
                            "Duplicate OrderCreated received for Order {OrderId} " +
                            "in state {State}. Message discarded.",
                            ctx.Saga.OrderId,
                            ctx.Saga.CurrentState)))
        );
    }

    // -------------------------------------------------------------------------
    // Finalization
    // -------------------------------------------------------------------------
    private void ConfigureFinalization()
    {
        // SetCompletedWhenFinalized instructs the saga repository to DELETE the row
        // once Finalize() is called. Without this + .Finalize(), terminal saga rows
        // accumulate indefinitely — a major problem at high throughput.
        SetCompletedWhenFinalized();

        WhenEnter(Completed, x => x
            .Then(ctx =>
            {
                ctx.Saga.LastUpdatedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Order {OrderId} completed successfully. Customer: {CustomerId}",
                    ctx.Saga.OrderId,
                    ctx.Saga.CustomerId);
            })
            .Finalize());

        WhenEnter(Failed, x => x
            .Then(ctx =>
            {
                ctx.Saga.LastUpdatedAt = DateTime.UtcNow;
                _logger.LogWarning(
                    "Order {OrderId} failed at step {FailedStep}. Reason: {Reason}",
                    ctx.Saga.OrderId,
                    ctx.Saga.FailedStep   ?? "unknown",
                    ctx.Saga.FailureReason ?? "unknown");
            })
            .Finalize());
    }
}