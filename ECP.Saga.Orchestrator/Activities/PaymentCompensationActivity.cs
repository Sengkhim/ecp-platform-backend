using Contracts;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public class PaymentCompensationActivity :
    IStateMachineActivity<OrderState, PaymentFailed>
{
    private readonly ITopicProducer<RefundPayment> _producer;
    private readonly ILogger<PaymentCompensationActivity> _logger;

    public PaymentCompensationActivity(
        ITopicProducer<RefundPayment> producer,
        ILogger<PaymentCompensationActivity> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("payment-compensation");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, PaymentFailed> context,
        IBehavior<OrderState, PaymentFailed> next)
    {
        var saga = context.Saga;
        
        var paymentId = Guid.NewGuid();       
        var refundAmount = saga.TotalAmount;    
        var refundTime = DateTime.UtcNow;

        if (saga.PaymentRefunded)
        {
            var refundCommand = new RefundPayment(
                saga.OrderId,
                paymentId,
                refundAmount,
                refundTime);

            await _producer.Produce(refundCommand);
        }

        _logger.LogWarning(
            "Payment compensation triggered for Order {OrderId}, Payment {PaymentId}, Amount {Amount}",
            saga.OrderId,
            paymentId,
            refundAmount);

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, PaymentFailed, TException> context,
        IBehavior<OrderState, PaymentFailed> next)
        where TException : Exception
    {
        _logger.LogError(context.Exception,
            "Payment compensation failed for Order {OrderId}",
            context.Saga.OrderId);

        return next.Faulted(context);
    }
}
