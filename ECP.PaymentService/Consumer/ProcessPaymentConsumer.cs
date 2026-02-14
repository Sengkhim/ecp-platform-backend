using Contracts;
using MassTransit;

namespace ECP.PaymentService.Consumer;

public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
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

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var message = context.Message;
        var payment = new ProcessPayment(message.OrderId, message.Amount, message.Currency, message.PaymentMethod);
        
        _logger.LogInformation("💳Payment completed for Order {Payment}", payment.ToString());
        
        await _producer.Produce(payment);
        // await _failedProducer.Produce(new PaymentFailed(context.Message.OrderId, "R"));
    }
    
    // public async Task Consume(ConsumeContext<ProcessPayment> context)
    // {
    //     try
    //     {
    //         var payment = context.Message;
    //
    //         // Call payment gateway or internal logic
    //         bool success = await ProcessPaymentGateway(payment);
    //
    //         if (success)
    //         {
    //             await _processedProducer.Produce(new PaymentProcessed(payment.OrderId));
    //         }
    //         else
    //         {
    //             await _failedProducer.Produce(new PaymentFailed(payment.OrderId, "Payment failed reason"));
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         await _failedProducer.Produce(new PaymentFailed(context.Message.OrderId, ex.Message));
    //     }
    // }
}