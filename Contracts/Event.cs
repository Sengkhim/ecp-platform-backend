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
    string PaymentMethod,
    Guid CustomerId,
    Guid IdempotencyKey,
    DateTime? PaymentRequestedAt);

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
    
    
/// <summary>
/// Emitted by MassTransit scheduler when inventory service
/// does not respond within the configured deadline.
/// </summary>
public record InventoryTimeout
{
    public Guid OrderId { get; init; }
}

/// <summary>
/// Emitted by MassTransit scheduler when payment service
/// does not respond within the configured deadline.
/// </summary>
public record PaymentTimeout
{
    public Guid OrderId { get; init; }
}

/// <summary>
/// Command published by <c>PaymentRequestActivity</c> to trigger payment processing.
///
/// <para>
/// <see cref="IdempotencyKey"/> is a stable, deterministic <see cref="Guid"/>
/// derived from <see cref="OrderId"/>. The payment provider must use this key
/// to deduplicate re-submitted requests and return the original result without
/// processing the charge again.
/// </para>
/// </summary>  
public record RequestPayment(
    Guid    OrderId,
    Guid    CustomerId,
    decimal Amount,
    string  Currency,
    string  PaymentMethod,
    Guid IdempotencyKey,
    DateTime PaymentRequestedAt
);