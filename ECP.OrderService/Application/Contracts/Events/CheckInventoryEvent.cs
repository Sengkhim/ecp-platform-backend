namespace ECP.OrderService.Application.Contracts.Events;

public record CheckInventoryEvent(
    Guid OrderId,
    List<OrderItemInfoEvent>? Items
);