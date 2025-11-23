using Contracts;
using ECP.Saga.Orchestrator.Activities;
using MassTransit;

namespace ECP.Saga.Orchestrator.StateData;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{ 
    public State AwaitingInventory { get; set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    
    public Event<OrderCreated> OrderCreatedEvent { get; private set; } = null!;
    public Event<InventoryReserved> InventoryReservedEvent { get; private set; } = null!;
    public Event<InventoryFailed> InventoryFailedEvent { get; private set; } = null!;
    public Event<ProcessPayment> PaymentProcessedEvent { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = null!;
    
    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreatedEvent, x
            => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryReservedEvent, x
            => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryFailedEvent, x
            => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessedEvent, x
            => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailedEvent, x
            => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(When(OrderCreatedEvent)
              .Activity(x => x.OfInstanceType<CheckInventoryActivity>())
              .TransitionTo(AwaitingInventory)
        );

        During(AwaitingInventory, When(InventoryReservedEvent)
                .Activity(x => x.OfInstanceType<PaymentRequestActivity>())
                .TransitionTo(AwaitingPayment),
        
            When(InventoryFailedEvent)
                .Activity(x => x.OfInstanceType<OrderActivity>())
                .TransitionTo(Failed)
        );
        
        During(AwaitingPayment, When(PaymentProcessedEvent)
                .Activity(x => x.OfInstanceType<SendNotificationActivity>())
                .TransitionTo(Completed),
            
            When(PaymentFailedEvent)
                .Activity(x => x.OfInstanceType<OrderActivity>())
                .TransitionTo(Failed)
        );
    }
}

