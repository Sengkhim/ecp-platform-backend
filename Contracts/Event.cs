namespace Contracts;

public record OrderCreated(Guid OrderId, decimal Amount, string ProductId);
public record InventoryReserved(Guid OrderId);
public record InventoryFailed(Guid OrderId, string Reason);
public record PaymentFailed(Guid OrderId, string Reason);
public record OrderCompleted(Guid OrderId);
public record OrderFailed(Guid OrderId, string Reason);
public record ProcessPaymentRequest(Guid OrderId, decimal Amount);
public record CheckInventory(Guid OrderId, string ProductId);
public record ProcessPayment(Guid OrderId, decimal Amount);
public record NotificationRequest(Guid OrderId, string Message);