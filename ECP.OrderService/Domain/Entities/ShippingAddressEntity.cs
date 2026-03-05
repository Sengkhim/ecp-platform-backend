using System.ComponentModel.DataAnnotations;

namespace ECP.OrderService.Domain.Entities;

public class ShippingAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // Navigation properties
    public virtual OrderEntity OrderEntity { get; set; } = null!;
}