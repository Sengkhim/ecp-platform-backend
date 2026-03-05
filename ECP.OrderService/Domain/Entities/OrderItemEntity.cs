using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECP.OrderService.Domain.Entities;

public class OrderItemEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid OrderId { get; init; }

    [Required]
    public Guid ProductId { get; init; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Sku { get; init; }

    [Required]
    public int Quantity { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }

    // Navigation properties
    public virtual OrderEntity? OrderEntity { get; init; }
}