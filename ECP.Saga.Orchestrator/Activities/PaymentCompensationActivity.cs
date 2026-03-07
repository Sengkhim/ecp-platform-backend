using Contracts;
using ECP.Saga.Orchestrator.Infrastructure;
using ECP.Saga.Orchestrator.StateData;
using MassTransit;

namespace ECP.Saga.Orchestrator.Activities;

public sealed class PaymentCompensationActivity :
    IStateMachineActivity<OrderState, PaymentFailed>
{
    private readonly ITopicProducer<OrderFailed> _producer;
    private readonly SagaErrorLogger _errorLogger;
    private readonly ILogger<PaymentCompensationActivity> _logger;

    public PaymentCompensationActivity(
        ITopicProducer<OrderFailed> producer,
        SagaErrorLogger errorLogger,
        ILogger<PaymentCompensationActivity> logger)
    {
        _producer    = producer;
        _errorLogger = errorLogger;
        _logger      = logger;
    }

    public void Probe(ProbeContext context) => context.CreateScope("payment-compensation");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderState, PaymentFailed> context,
        IBehavior<OrderState, PaymentFailed> next)
    {
        await CompensationCore.RunPaymentAsync(
            saga:              context.Saga,
            reason:            context.Message.Reason ?? "Payment processing failed",
            producer:          _producer,
            logger:            _logger,
            errorLogger:       _errorLogger,
            cancellationToken: context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<OrderState, PaymentFailed, TException> context,
        IBehavior<OrderState, PaymentFailed> next)
        where TException : Exception
    {
        CompensationCore.StampException(context.Saga, context.Exception);

        await _errorLogger.LogExceptionAsync(
            context.Saga.CorrelationId, context.Saga.OrderId, context.Saga.CurrentState,
            "PaymentCompensation.Faulted", context.Exception, context.CancellationToken);

        _logger.LogError(context.Exception,
            "PaymentCompensationActivity faulted for Order {OrderId}", context.Saga.OrderId);

        await next.Faulted(context);
    }
}
