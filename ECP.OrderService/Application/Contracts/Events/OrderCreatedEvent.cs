namespace ECP.OrderService.Application.Contracts.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string PaymentMethod { get; init; }  = string.Empty;
    public string Currency { get; init; }  = string.Empty;
    public List<OrderItemInfoEvent> Items { get; init; } = [];
}