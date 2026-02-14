using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class PaymentRequestActivity :
    IStateMachineActivity<OrderState, InventoryReserved>
{
    private readonly ILogger<PaymentRequestActivity> _logger;
    private readonly ITopicProducer<ProcessPayment> _producer;

    public PaymentRequestActivity(
        ILogger<PaymentRequestActivity> logger,
        ITopicProducer<ProcessPayment> producer)
    {
        _logger = logger;
        _producer = producer;
    }

    public void Probe(ProbeContext context) => context.CreateScope("payment-request");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, InventoryReserved> context,
        IBehavior<OrderState, InventoryReserved> next)
    {
        var saga = context.Saga;

        if (!saga.PaymentRequested)
        {
            saga.PaymentRequested = true;

            var paymentCommand = new ProcessPayment(
                saga.OrderId,
                saga.TotalAmount,
                saga.Currency,
                saga.PaymentMethod
            );

            await _producer.Produce(paymentCommand);
        }

        _logger.LogInformation(
            "Payment request sent for Order {OrderId}, Amount {Amount} {Currency}, Method {Method}",
            saga.OrderId,
            saga.TotalAmount,
            saga.Currency,
            saga.PaymentMethod);

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, InventoryReserved, TException> context,
        IBehavior<OrderState, InventoryReserved> next)
        where TException : Exception
    {
        _logger.LogError(context.Exception,
            "Payment request failed for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}