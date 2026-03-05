using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECP.OrderService.Domain.Entities;

public class OrderEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; init; } = string.Empty;

    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? CustomerEmail { get; init; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    // Navigation properties
    public ICollection<OrderItemEntity> Items { get; init; } = new List<OrderItemEntity>();

    // public virtual ShippingAddress? ShippingAddress { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Completed = 5,
    Cancelled = 6,
    Refunded = 7
}