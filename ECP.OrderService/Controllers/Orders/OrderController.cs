using ECP.OrderService.Domain.Entities;
using ECP.OrderService.Modules.Order;
using Microsoft.AspNetCore.Mvc;

namespace ECP.OrderService.Controllers.Orders;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController(Modules.Order.Service.OrderService orderService) : ControllerBase
{
    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        [FromBody] CreateOrderDto createOrderDto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await orderService.CreateOrderAsync(createOrderDto, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderByIdAsync(id, cancellationToken);
            
        if (order == null)
            return NotFound(new { message = $"Order with ID {id} not found" });

        return Ok(order);
    }

     /// <summary>
     /// Get order by order number
     /// </summary>
     [HttpGet("by-number/{orderNumber}")]
     [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
     public async Task<ActionResult<OrderDto>> GetOrderByNumber(
         string orderNumber,
         CancellationToken cancellationToken)
     {
         var order = await orderService.GetOrderByNumberAsync(orderNumber, cancellationToken);
            
         if (order == null)
             return NotFound(new { message = $"Order {orderNumber} not found" });

         return Ok(order);
     }

     /// <summary>
     /// Get all orders for a customer
     /// </summary>
     [HttpGet("customer/{customerId:guid}")]
     [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
     public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomer(
         Guid customerId,
         CancellationToken cancellationToken)
     {
         var orders = await orderService.GetOrdersByCustomerAsync(customerId, cancellationToken);
         return Ok(orders);
     }

     /// <summary>
     /// Get all orders with a specific status
     /// </summary>
     [HttpGet("status/{status}")]
     [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
     public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByStatus(
         OrderStatus status,
         CancellationToken cancellationToken)
     {
         var orders = await orderService.GetOrdersByStatusAsync(status, cancellationToken);
         return Ok(orders);
     }

     /// <summary>
     /// Get all orders with pagination
     /// </summary>
     [HttpGet]
     [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
     public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders(
         [FromQuery] int skip = 0,
         [FromQuery] int take = 50,
         CancellationToken cancellationToken = default)
     {
         var orders = await orderService.GetAllOrdersAsync(skip, take, cancellationToken);
         return Ok(orders);
     }
     
     /// <summary>
     /// Update order status
     /// </summary>
     [HttpPatch("{id:guid}/status")]
     [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
     [ProducesResponseType(StatusCodes.Status400BadRequest)]
     public async Task<ActionResult<OrderDto>> UpdateOrderStatus(
         Guid id,
         [FromBody] UpdateOrderStatusDto updateStatusDto,
         CancellationToken cancellationToken)
     {
         if (!ModelState.IsValid)
             return BadRequest(ModelState);

         try
         {
             var order = await orderService.UpdateOrderStatusAsync(id, updateStatusDto, cancellationToken);
             return Ok(order);
         }
         catch (InvalidOperationException ex)
         {
             return NotFound(new { message = ex.Message });
         }
     }

     /// <summary>
     /// Complete an order
     /// </summary>
     [HttpPost("{id:guid}/complete")]
     [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
     [ProducesResponseType(StatusCodes.Status400BadRequest)]
     public async Task<ActionResult<OrderDto>> CompleteOrder(
         Guid id,
         CancellationToken cancellationToken)
     {
         try
         {
             var order = await orderService.CompleteOrderAsync(id, cancellationToken);
             return Ok(order);
         }
         catch (InvalidOperationException ex)
         {
             return BadRequest(new { message = ex.Message });
         }
     }

     /// <summary>
     /// Cancel an order
     /// </summary>
     [HttpPost("{id:guid}/cancel")]
     [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
     [ProducesResponseType(StatusCodes.Status400BadRequest)]
     public async Task<ActionResult<OrderDto>> CancelOrder(
         Guid id,
         [FromBody] CancelOrderRequest request,
         CancellationToken cancellationToken)
     {
         try
         {
             var order = await orderService.CancelOrderAsync(id, request.Reason, cancellationToken);
             return Ok(order);
         }
         catch (InvalidOperationException ex)
         {
             return BadRequest(new { message = ex.Message });
         }
     }
     

     // /// <summary>
     // /// Delete an order
     // /// </summary>
     // [HttpDelete("{id:guid}")]
     // [ProducesResponseType(StatusCodes.Status204NoContent)]
     // [ProducesResponseType(StatusCodes.Status404NotFound)]
     // public async Task<IActionResult> DeleteOrder(
     //     Guid id,
     //     CancellationToken cancellationToken)
     // {
     //     try
     //     {
     //         await orderService.DeleteOrderAsync(id, cancellationToken);
     //         return NoContent();
     //     }
     //     catch (InvalidOperationException ex)
     //     {
     //         return NotFound(new { message = ex.Message });
     //     }
     // }

     // /// <summary>
     // /// Get order statistics by status
     // /// </summary>
     // [HttpGet("statistics")]
     // [ProducesResponseType(typeof(Dictionary<OrderStatus, int>), StatusCodes.Status200OK)]
     // public async Task<ActionResult<Dictionary<OrderStatus, int>>> GetOrderStatistics(
     //     CancellationToken cancellationToken)
     // {
     //     var statistics = await orderService.GetOrderStatisticsAsync(cancellationToken);
     //     return Ok(statistics);
     // }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}