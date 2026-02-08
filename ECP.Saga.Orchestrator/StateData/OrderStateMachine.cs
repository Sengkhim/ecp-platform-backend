using System.Text.Json;
// using Contracts;
using ECP.OrderService.Application.Contracts.Events;
using ECP.Saga.Orchestrator.Activities;
using MassTransit;

namespace ECP.Saga.Orchestrator.StateData;

// ReSharper disable once ClassNeverInstantiated.Global
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{ 
    public State? AwaitingInventory { get; set; }
    public State? AwaitingPayment { get; private set; } 
    public State? Completed { get; private set; } 
    public State? Failed { get; private set; }
    
    public Event<OrderCreatedEvent>? OrderCreatedEvent { get; set; }
    // public Event<InventoryReserved> InventoryReservedEvent { get; set; }
    // public Event<InventoryFailed> InventoryFailedEvent { get; set; }
    // public Event<ProcessPayment> PaymentProcessedEvent { get; set; }
    // public Event<PaymentFailed> PaymentFailedEvent { get; set; }
    
    public OrderStateMachine()
    {

        InstanceState(x => x.CurrentState);

        Event(() => OrderCreatedEvent, x
            => x.CorrelateById(ctx => ctx.Message.OrderId));
        
        // Event(() => InventoryReservedEvent, x
        //     => x.CorrelateById(ctx => ctx.Message.OrderId));
        //
        // Event(() => InventoryFailedEvent, x
        //     => x.CorrelateById(ctx => ctx.Message.OrderId));
        //
        // Event(() => PaymentProcessedEvent, x
        //     => x.CorrelateById(ctx => ctx.Message.OrderId));
        //
        // Event(() => PaymentFailedEvent, x
        //     => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(When(OrderCreatedEvent)
            .Then(context => 
            {
                context.Saga.CustomerId = context.Message.CustomerId;
                context.Saga.CustomerName = context.Message.CustomerName;
                context.Saga.OrderId = context.Message.OrderId;
                context.Saga.OrderNumber = context.Message.OrderNumber;
                context.Saga.CustomerEmail = context.Message.CustomerEmail;
                context.Saga.TotalAmount = context.Message.TotalAmount;
                context.Saga.CreatedAt = context.Message.CreatedAt;
                context.Saga.Items = JsonSerializer.Serialize(context.Message.Items);

            })
            .Activity(x => x.OfInstanceType<CheckInventoryActivity>())
            .TransitionTo(AwaitingInventory)
        );

        // During(AwaitingInventory, When(InventoryReservedEvent)
        //         .Activity(x => x.OfInstanceType<PaymentRequestActivity>())
        //         .TransitionTo(AwaitingPayment),
        //
        //     When(InventoryFailedEvent)
        //         .Activity(x => x.OfInstanceType<OrderActivity>())
        //         .TransitionTo(Failed)
        // );
        //
        // During(AwaitingPayment, When(PaymentProcessedEvent)
        //         .Activity(x => x.OfInstanceType<SendNotificationActivity>())
        //         .TransitionTo(Completed),
        //     
        //     When(PaymentFailedEvent)
        //         .Activity(x => x.OfInstanceType<OrderActivity>())
        //         .TransitionTo(Failed)
        // );
        //
    }
}

