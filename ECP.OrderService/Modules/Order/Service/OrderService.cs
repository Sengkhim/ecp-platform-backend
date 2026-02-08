using ECP.OrderService.Application.Contracts.Events;
using ECP.OrderService.Domain.Entities;
using ECP.OrderService.Infrastructure.Repositories;
// using ECP.OrderService.Modules.Order.Messages;
using MassTransit;

namespace ECP.OrderService.Modules.Order.Service;

public class OrderService(
    OrderRepository orderRepository, ITopicProducer<OrderCreatedEvent> producer, ILogger<OrderService> logger)
{
    public async Task<OrderDto> CreateOrderAsync(
        CreateOrderDto createOrderDto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating new order for customer {CustomerId}", createOrderDto.CustomerId);

        // Calculate totals
        decimal subTotal = 0;
        var orderItems = new List<OrderItemEntity>();
        
        foreach (var itemDto in createOrderDto.Items)
        {
            var totalPrice = (itemDto.UnitPrice * itemDto.Quantity) - itemDto.Discount;
            subTotal += totalPrice;

            orderItems.Add(new OrderItemEntity
            {
                Id = Guid.NewGuid(),
                ProductId = itemDto.ProductId,
                ProductName = itemDto.ProductName,
                Sku = itemDto.Sku,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Discount = itemDto.Discount,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes
            });
        }

        // Calculate tax (10% for example)
        var taxAmount = subTotal * 0.10m;
        const decimal shippingCost = 15.00m; // Fixed shipping cost
        var totalAmount = subTotal + taxAmount + shippingCost;

        // Create order
        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = createOrderDto.CustomerId,
            CustomerName = createOrderDto.CustomerName,
            CustomerEmail = createOrderDto.CustomerEmail,
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            ShippingCost = shippingCost,
            TotalAmount = totalAmount,
            Notes = createOrderDto.Notes,
            CreatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        // Add shipping address if provided
        // if (createOrderDto.ShippingAddress != null)
        // {
        //     order.ShippingAddress = new ShippingAddress
        //     {
        //         Id = Guid.NewGuid(),
        //         OrderId = order.Id,
        //         FullName = createOrderDto.ShippingAddress.FullName,
        //         AddressLine1 = createOrderDto.ShippingAddress.AddressLine1,
        //         AddressLine2 = createOrderDto.ShippingAddress.AddressLine2,
        //         City = createOrderDto.ShippingAddress.City,
        //         State = createOrderDto.ShippingAddress.State,
        //         PostalCode = createOrderDto.ShippingAddress.PostalCode,
        //         Country = createOrderDto.ShippingAddress.Country,
        //         PhoneNumber = createOrderDto.ShippingAddress.PhoneNumber
        //     };
        // }

        // Save to database
        var createdOrder = await orderRepository.AddAsync(order, cancellationToken);

        logger.LogInformation("Order {OrderNumber} created successfully with ID {OrderId}", 
            createdOrder.OrderNumber, createdOrder.Id);
        
        // Publish OrderCreated event
        await producer.Produce(new OrderCreatedEvent
        {
            OrderId = createdOrder.Id,
            OrderNumber = createdOrder.OrderNumber,
            CustomerId = createdOrder.CustomerId,
            CustomerName = createdOrder.CustomerName,
            CustomerEmail = createdOrder.CustomerEmail,
            TotalAmount = createdOrder.TotalAmount,
            CreatedAt = createdOrder.CreatedAt,

            Items = createdOrder.Items.Select(i => new OrderItemInfoEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        }, cancellationToken);

        return MapToDto(createdOrder);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetByStatusAsync(status, cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetAllAsync(skip, take, cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto updateStatusDto, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {id} not found");

        var previousStatus = order.Status;
        order.Status = updateStatusDto.Status;

        // Update specific status timestamps
        if (updateStatusDto.Status == OrderStatus.Completed)
            order.CompletedAt = DateTime.UtcNow;
        else if (updateStatusDto.Status == OrderStatus.Cancelled)
        {
            order.CancelledAt = DateTime.UtcNow;
            order.CancellationReason = updateStatusDto.Reason;
        }

        await orderRepository.UpdateAsync(order, cancellationToken);

        logger.LogInformation("Order {OrderNumber} status changed from {PreviousStatus} to {NewStatus}",
            order.OrderNumber, previousStatus, order.Status);

        // Publish OrderStatusChanged event
        // await publishEndpoint.Publish<OrderStatusChanged>(new
        // {
        //     OrderId = order.Id,
        //     OrderNumber = order.OrderNumber,
        //     PreviousStatus = previousStatus.ToString(),
        //     NewStatus = order.Status.ToString(),
        //     ChangedAt = DateTime.UtcNow,
        //     Reason = updateStatusDto.Reason
        // }, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> CompleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {id} not found");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled order");

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order, cancellationToken);

        logger.LogInformation("Order {OrderNumber} completed", order.OrderNumber);

        // Publish OrderCompleted event
        // await producer.Produce()<OrderCompletedEvent>(new
        // {
        //     OrderId = order.Id,
        //     OrderNumber = order.OrderNumber,
        //     CustomerId = order.CustomerId,
        //     TotalAmount = order.TotalAmount,
        //     CompletedAt = order.CompletedAt.Value
        // }, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> CancelOrderAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {id} not found");

        if (order.Status is OrderStatus.Completed or OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a completed or delivered order");

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancellationReason = reason;

        await orderRepository.UpdateAsync(order, cancellationToken);

        logger.LogInformation("Order {OrderNumber} cancelled. Reason: {Reason}", order.OrderNumber, reason);

        // Publish OrderCancelled event
        // await publishEndpoint.Publish<OrderCancelled>(new
        // {
        //     OrderId = order.Id,
        //     OrderNumber = order.OrderNumber,
        //     CustomerId = order.CustomerId,
        //     CancellationReason = reason,
        //     CancelledAt = order.CancelledAt.Value
        // }, cancellationToken);

        return MapToDto(order);
    }

    // public async Task<OrderDto> ShipOrderAsync(Guid id, CancellationToken cancellationToken = default)
    // {
    //     var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
    //     if (order == null)
    //         throw new InvalidOperationException($"Order with ID {id} not found");
    //
    //     if (order.Status == OrderStatus.Cancelled)
    //         throw new InvalidOperationException("Cannot ship a cancelled order");
    //
    //     // if (order.ShippingAddress == null)
    //     //     throw new InvalidOperationException("Cannot ship order without shipping address");
    //
    //     order.Status = OrderStatus.Shipped;
    //
    //     await _orderRepository.UpdateAsync(order, cancellationToken);
    //
    //     _logger.LogInformation("Order {OrderNumber} shipped", order.OrderNumber);
    //
    //     // Publish OrderShipped event
    //     await _publishEndpoint.Publish<OrderShipped>(new
    //     {
    //         OrderId = order.Id,
    //         OrderNumber = order.OrderNumber,
    //         CustomerId = order.CustomerId,
    //         ShippingAddress = new ShippingInfo
    //         {
    //             FullName = order.ShippingAddress.FullName,
    //             AddressLine1 = order.ShippingAddress.AddressLine1,
    //             AddressLine2 = order.ShippingAddress.AddressLine2,
    //             City = order.ShippingAddress.City,
    //             State = order.ShippingAddress.State,
    //             PostalCode = order.ShippingAddress.PostalCode,
    //             Country = order.ShippingAddress.Country,
    //             PhoneNumber = order.ShippingAddress.PhoneNumber
    //         },
    //         ShippedAt = DateTime.UtcNow
    //     }, cancellationToken);
    //
    //     return MapToDto(order);
    // }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static OrderDto MapToDto(OrderEntity order) =>
        new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            ShippingCost = order.ShippingCost,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Discount = i.Discount,
                Notes = i.Notes
            }).ToList(),
            // ShippingAddress = order.ShippingAddress != null ? new ShippingAddressDto
            // {
            //     Id = order.ShippingAddress.Id,
            //     FullName = order.ShippingAddress.FullName,
            //     AddressLine1 = order.ShippingAddress.AddressLine1,
            //     AddressLine2 = order.ShippingAddress.AddressLine2,
            //     City = order.ShippingAddress.City,
            //     State = order.ShippingAddress.State,
            //     PostalCode = order.ShippingAddress.PostalCode,
            //     Country = order.ShippingAddress.Country,
            //     PhoneNumber = order.ShippingAddress.PhoneNumber
            // } : null
        };
}