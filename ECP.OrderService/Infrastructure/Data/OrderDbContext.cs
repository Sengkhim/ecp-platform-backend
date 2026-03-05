using ECP.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECP.OrderService.Infrastructure.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<OrderEntity> Orders { get; set; }
    public DbSet<OrderItemEntity> OrderItems { get; set; }
    public DbSet<ShippingAddressEntity> ShippingAddresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { 
        base.OnModelCreating(modelBuilder);

            // Order configuration
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.OrderNumber)
                .IsUnique();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.SubTotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.TaxAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.ShippingCost)
                .HasPrecision(18, 2);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

            // OrderItem configuration
        modelBuilder.Entity<OrderItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.Discount)
                .HasPrecision(18, 2);

            entity.HasOne(e => e.OrderEntity)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
        });

        // ShippingAddress configuration
        
        // modelBuilder.Entity<ShippingAddress>(entity =>
        // {
        //     entity.HasKey(e => e.Id);
        //
        //     entity.HasOne(e => e.Order)
        //         .WithOne(o => o.ShippingAddress)
        //         .HasForeignKey<ShippingAddress>(e => e.OrderId)
        //         .OnDelete(DeleteBehavior.Cascade);
        //
        //     entity.HasIndex(e => e.OrderId)
        //         .IsUnique();
        // });
    }
}