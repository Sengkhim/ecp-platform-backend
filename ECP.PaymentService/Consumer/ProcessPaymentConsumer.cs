using Contracts;
using MassTransit;

namespace ECP.PaymentService.Consumer;

public class ProcessPaymentConsumer : IConsumer<ProcessPaymentRequest>
{
    private readonly ILogger<ProcessPaymentConsumer> _logger;
    private readonly ITopicProducer<ProcessPayment> _producer;
    private readonly ITopicProducer<PaymentFailed> _failedProducer;

    public ProcessPaymentConsumer(
        ILogger<ProcessPaymentConsumer> logger,
        ITopicProducer<ProcessPayment> producer, 
        ITopicProducer<PaymentFailed> failedProducer)
    {
        _logger = logger;
        _producer = producer;
        _failedProducer = failedProducer;
    }

    public async Task Consume(ConsumeContext<ProcessPaymentRequest> context)
    {
        _logger.LogInformation("💳 Payment completed for Order {MessageOrderId}", context.Message.OrderId);
        await _producer.Produce(new ProcessPayment(context.Message.OrderId, 34));
        // await _failedProducer.Produce(new PaymentFailed(context.Message.OrderId, "R"));
    }
}