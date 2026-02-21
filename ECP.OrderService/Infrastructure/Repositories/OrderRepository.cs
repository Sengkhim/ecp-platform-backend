using ECP.OrderService.Domain.Entities;
using ECP.OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECP.OrderService.Infrastructure.Repositories;

public class OrderRepository(OrderDbContext context)
{
    public async Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                // .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<OrderEntity?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                // .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        }

        public async Task<IEnumerable<OrderEntity>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                // .Include(o => o.ShippingAddress)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrderEntity>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                // .Include(o => o.ShippingAddress)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrderEntity>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                // .Include(o => o.ShippingAddress)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrderEntity> AddAsync(OrderEntity orderEntity, CancellationToken cancellationToken = default)
        {
            await context.Orders.AddAsync(orderEntity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return orderEntity;
        }

        public async Task UpdateAsync(OrderEntity orderEntity, CancellationToken cancellationToken = default)
        {
            orderEntity.UpdatedAt = DateTime.UtcNow;
            context.Orders.Update(orderEntity);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await context.Orders.FindAsync(new object[] { id }, cancellationToken);
            if (order != null)
            {
                context.Orders.Remove(order);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.Orders.AnyAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<int> CountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            return await context.Orders.CountAsync(o => o.Status == status, cancellationToken);
        }
    }