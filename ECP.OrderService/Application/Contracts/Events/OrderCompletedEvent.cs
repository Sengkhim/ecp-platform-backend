namespace ECP.OrderService.Application.Contracts.Events;

public class OrderCompletedEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CompletedAt { get; set; }
}