using System.ComponentModel.DataAnnotations;
using ECP.OrderService.Domain.Entities;

namespace ECP.OrderService.Modules.Order;

    public class CreateOrderDto
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(100)]
        public string? CustomerEmail { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Currency { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        public List<CreateOrderItemDto> Items { get; set; } = [];

        public CreateShippingAddressDto? ShippingAddress { get; set; }
    }

    public class CreateOrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Sku { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class CreateShippingAddressDto
    {
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

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }
    }

    public class OrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public ShippingAddressDto? ShippingAddress { get; set; }
    }

    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; }
        public string? Notes { get; set; }
    }

    public class ShippingAddressDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }