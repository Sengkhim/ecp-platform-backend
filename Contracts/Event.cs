namespace Contracts;

// The Initial Event
// public record OrderCreated(
//     Guid OrderId, 
//     Guid CustomerId,
//     decimal Amount, 
//     string ProductId, 
//     int Quantity,
//     DateTime CreatedAt);

// Inventory Related
// public record CheckInventory(
//     Guid OrderId, 
//     string ProductId, 
//     int Quantity,
//     DateTime Timestamp);

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


// Orchestration / Final States
// public record OrderCompleted(
//     Guid OrderId, 
//     Guid CustomerId,
//     string OrderNumber,
//     decimal TotalAmount,
//     DateTime CompletedAt);

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