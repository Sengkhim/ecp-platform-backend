
using System.Text.Json;
using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.Saga.Orchestrator.Activities;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State? AwaitingInventory { get; private set; }
    public State? AwaitingPayment { get; private set; }
    public State? Completed { get; private set; }
    public State? Failed { get; private set; }

    public Event<OrderCreatedEvent>? OrderCreatedEvent { get; private set; }
    public Event<InventoryReserved>? InventoryReservedEvent { get; private set; }
    public Event<InventoryFailed>? InventoryFailedEvent { get; private set; }
    public Event<ProcessPayment>? PaymentProcessedEvent { get; private set; }
    public Event<PaymentFailed>? PaymentFailedEvent { get; private set; }

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreatedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryReservedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryFailedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        // ── Initial
        Initially(
            When(OrderCreatedEvent)
                .Then(ctx =>
                {
                    var saga = ctx.Saga;
                    saga.CustomerId = ctx.Message.CustomerId;
                    saga.OrderNumber = ctx.Message.OrderNumber;
                    saga.TotalAmount = ctx.Message.TotalAmount;
                    saga.Currency = ctx.Message.Currency;
                    saga.PaymentMethod = ctx.Message.PaymentMethod;
                    saga.Items = JsonSerializer.Serialize(ctx.Message.Items);
                })
                .Activity(x => x.OfType<CheckInventoryActivity>())
                .TransitionTo(AwaitingInventory)
        );

        // ── AwaitingInventory
        During(AwaitingInventory,
            When(InventoryReservedEvent)
                .If(ctx => !ctx.Saga.PaymentRequested, binder =>
                    binder
                        .Then(ctx => ctx.Saga.PaymentRequested = true)
                        .Activity(x => x.OfType<PaymentRequestActivity>())
                        .TransitionTo(AwaitingPayment)
                ),

            When(InventoryFailedEvent)
                .Activity(x => x.OfType<InventoryCompensationActivity>())
                .TransitionTo(Failed)
        );

        // ── AwaitingPayment
        During(AwaitingPayment,
            When(PaymentProcessedEvent)
                .If(ctx => ctx.Saga.CurrentState == AwaitingPayment?.Name, binder =>
                    binder.Activity(x => x.OfType<SendNotificationActivity>())
                          .TransitionTo(Completed)
                ),

            When(PaymentFailedEvent)
                .Activity(x => x.OfType<PaymentCompensationActivity>())
                .TransitionTo(Failed)
        );

        // ── Finalization
        SetCompletedWhenFinalized();
    }
}

// using System.Text.Json;
// using Contracts;
// using ECP.OrderService.Application.Contracts.Events;
// using ECP.Saga.Orchestrator.Activities;
// using JetBrains.Annotations;
// using MassTransit;
//
// namespace ECP.Saga.Orchestrator.StateData;
//
// [UsedImplicitly]
// public class OrderStateMachine : MassTransitStateMachine<OrderState>
// {
//     // States
//     public State AwaitingInventory { get; private set; } = null!;
//     public State AwaitingPayment   { get; private set; } = null!;
//     public State Completed  { get; private set; } = null!;
//     public State Failed  { get; private set; } = null!;
//
//     // Events
//     public Event<OrderCreatedEvent> OrderCreatedEvent { get; private set; } = null!;
//     public Event<InventoryReserved> InventoryReservedEvent { get; private set; } = null!;
//     public Event<InventoryFailed> InventoryFailedEvent { get; private set; } = null!;
//     public Event<ProcessPayment> PaymentProcessedEvent { get; private set; } = null!;
//     public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = null!;
//
//     public OrderStateMachine()
//     {
//         // Map state property
//         InstanceState(x => x.CurrentState);
//
//         // Event correlation
//         ConfigureCorrelation();
//
//         // Initial State
//         ConfigureInitial();
//
//         // Awaiting Inventory
//         ConfigureAwaitingInventory();
//
//         // Awaiting Payment
//         ConfigureAwaitingPayment();
//
//         // Global guards
//         ConfigureGlobalGuards();
//
//         // Finalization
//         ConfigureFinalization();
//     }
//
//     private void ConfigureCorrelation()
//     {
//         Event(() => OrderCreatedEvent,
//             x => x.CorrelateById(ctx => ctx.Message.OrderId));
//
//         Event(() => InventoryReservedEvent,
//             x => x.CorrelateById(ctx => ctx.Message.OrderId));
//
//         Event(() => InventoryFailedEvent,
//             x => x.CorrelateById(ctx => ctx.Message.OrderId));
//
//         Event(() => PaymentProcessedEvent,
//             x => x.CorrelateById(ctx => ctx.Message.OrderId));
//
//         Event(() => PaymentFailedEvent,
//             x => x.CorrelateById(ctx => ctx.Message.OrderId));
//     }
//
//     private void ConfigureInitial()
//     {
//         Initially(
//             When(OrderCreatedEvent)
//                 .Then(ctx =>
//                 {
//                     var msg = ctx.Message;
//                     var saga = ctx.Saga;
//
//                     saga.OrderId       = msg.OrderId;
//                     saga.CustomerId    = msg.CustomerId;
//                     saga.CustomerName  = msg.CustomerName;
//                     saga.CustomerEmail = msg.CustomerEmail;
//                     saga.OrderNumber   = msg.OrderNumber;
//                     saga.TotalAmount   = msg.TotalAmount;
//                     saga.CreatedAt     = msg.CreatedAt;
//                     saga.CreatedAt     = msg.CreatedAt;
//                     saga.Items         = JsonSerializer.Serialize(msg.Items);
//                     
//                     saga.PaymentMethod = msg.PaymentMethod;
//                     saga.Currency      = msg.Currency;
//                 })
//                 .Activity(x => x.OfType<CheckInventoryActivity>())
//                 .TransitionTo(AwaitingInventory)
//         );
//     }
//
//     private void ConfigureAwaitingInventory()
//     {
//         During(AwaitingInventory,
//             
//             When(InventoryReservedEvent)
//                 .Activity(x => x.OfType<PaymentRequestActivity>())
//                 .TransitionTo(AwaitingPayment),
//
//             When(InventoryFailedEvent)
//                 .Activity(x => x.OfType<InventoryCompensationActivity>())
//                 .TransitionTo(Failed)
//
//             // 🔐 Ignore out-of-order payment events
//             // Ignore(PaymentProcessedEvent),
//             // Ignore(PaymentFailedEvent)
//         );
//     }
//
//     private void ConfigureAwaitingPayment()
//     {
//         During(AwaitingPayment,
//             When(PaymentProcessedEvent)
//                 .Activity(x => x.OfType<SendNotificationActivity>())
//                 .TransitionTo(Completed),
//
//             When(PaymentFailedEvent)
//                 .Activity(x => x.OfType<PaymentCompensationActivity>())
//                 .TransitionTo(Failed)
//         );
//
//         // During(AwaitingPayment,
//         //
//         //     When(PaymentProcessedEvent)
//         //         .Activity(x => x.OfType<SendNotificationActivity>())
//         //         .TransitionTo(Completed),
//         //
//         //     When(PaymentFailedEvent)
//         //         .Activity(x => x.OfType<PaymentCompensationActivity>())
//         //         .TransitionTo(Failed),
//         //
//         //     // 🔐 Ignore duplicate inventory events
//         //     Ignore(InventoryReservedEvent),
//         //     Ignore(InventoryFailedEvent)
//         // );
//     }
//
//     private void ConfigureGlobalGuards()
//     {
//         DuringAny(
//             When(OrderCreatedEvent)
//                 .If(ctx => ctx.Saga.CurrentState != Initial.Name,
//                     x => x.Then(ctx =>
//                         Console.WriteLine(
//                             $"[WARN] Duplicate OrderCreated for {ctx.Saga.OrderId}")))
//         );
//     }
//
//     private void ConfigureFinalization()
//     {
//         SetCompletedWhenFinalized();
//
//         WhenEnter(Completed,
//             x => x.Then(ctx =>
//                 Console.WriteLine(
//                     $"[INFO] Order {ctx.Saga.OrderId} completed successfully.")));
//
//         WhenEnter(Failed,
//             x => x.Then(ctx =>
//                 Console.WriteLine(
//                     $"[WARN] Order {ctx.Saga.OrderId} failed.")));
//     }
// }
