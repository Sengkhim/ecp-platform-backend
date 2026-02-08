namespace ECP.OrderService.Modules.Order.Messages;

// Events published by the Order module
    public interface OrderCreated
    {
        Guid OrderId { get; }
        string OrderNumber { get; }
        Guid CustomerId { get; }
        string CustomerName { get; }
        string? CustomerEmail { get; }
        decimal TotalAmount { get; }
        DateTime CreatedAt { get; }
        List<OrderItemInfo> Items { get; }
    }

    public interface OrderStatusChanged
    {
        Guid OrderId { get; }
        string OrderNumber { get; }
        string PreviousStatus { get; }
        string NewStatus { get; }
        DateTime ChangedAt { get; }
        string? Reason { get; }
    }

    public interface OrderCompleted
    {
        Guid OrderId { get; }
        string OrderNumber { get; }
        Guid CustomerId { get; }
        decimal TotalAmount { get; }
        DateTime CompletedAt { get; }
    }

    public interface OrderCancelled
    {
        Guid OrderId { get; }
        string OrderNumber { get; }
        Guid CustomerId { get; }
        string? CancellationReason { get; }
        DateTime CancelledAt { get; }
    }

    public interface OrderShipped
    {
        Guid OrderId { get; }
        string OrderNumber { get; }
        Guid CustomerId { get; }
        ShippingInfo ShippingAddress { get; }
        DateTime ShippedAt { get; }
    }

    // Commands consumed by the Order module
    public interface ProcessPayment
    {
        Guid OrderId { get; }
        decimal Amount { get; }
        string PaymentMethod { get; }
    }

    public interface CancelOrder
    {
        Guid OrderId { get; }
        string Reason { get; }
    }

    // Supporting types
    public class OrderItemInfo
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ShippingInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }