namespace Contracts;

public record InventoryReserved(
    Guid OrderId, 
    DateTime ExpiryDate);

public record InventoryFailed(
    Guid OrderId, 
    string ProductId, 
    string Reason, 
    string ErrorCode);

// Payment Related
public record ProcessPayment(
    Guid OrderId, 
    decimal Amount, 
    string Currency, 
    string PaymentMethod);

public record PaymentFailed(
    Guid OrderId, 
    decimal Amount, 
    string Reason, 
    DateTime FailureTime);

public record RefundPayment(Guid OrderId, Guid PaymentId, decimal Amount, DateTime RefundTime);

public record OrderFailed(
    Guid OrderId, 
    string Reason, 
    string FailedStep, // e.g., "Inventory" or "Payment"
    DateTime Timestamp);

// Notifications
public record NotificationRequest(
    Guid OrderId, 
    Guid CustomerId,
    string Message, 
    string NotificationType); // e.g., "Email", "SMS"