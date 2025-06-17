using System;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using Objects.DTOs;
using Objects.Enums;
using Objects;
using System.Text.Json;

namespace OrderService.Services;

public class OrderService
{
    private readonly OrderDbContext OrderDbContext_;


    public OrderService(OrderDbContext context)
    {
        OrderDbContext_ = context;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto request)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = await OrderDbContext_.Database.BeginTransactionAsync();
        try
        {
            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Amount = request.Amount,
                Description = request.Description,
                Status = StatusEnum.New
            };
            if (newOrder.Amount <= 0)
            {
                throw new Exception("Error: can't amount be not positive.");
            }
            await OrderDbContext_.Orders.AddAsync(newOrder);

            var payment = new PaymentRequest
            {
                OrderId = newOrder.Id,
                UserId = newOrder.UserId,
                Amount = newOrder.Amount,
                MessageId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(payment),
                CreatedUtc = DateTime.UtcNow
            };
            await OrderDbContext_.OutboxMessages.AddAsync(outboxMessage);
            await OrderDbContext_.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return new OrderDto
            {
                Id = newOrder.Id,
                UserId = newOrder.UserId,
                Amount = newOrder.Amount,
                Description = newOrder.Description,
                Status = newOrder.Status
            };
        }
        catch (Exception exception)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw new Exception($"Error:{exception}");
        }
    }


    public async Task<OrderDto> GetOrderByIdAsync(Guid orderId, Guid userId)
    {
        var order = await OrderDbContext_.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Error: can't find order {orderId}.");
        }
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Amount = order.Amount,
            Description = order.Description,
            Status = order.Status
        };
    }


    public async Task<List<OrderDto>> GetOrdersForUserAsync(Guid userId)
    {
        var orders = await OrderDbContext_.Orders
            .Where(o => o.UserId == userId)
            .ToListAsync();

        List<OrderDto> orderDtos = orders.Select(order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Amount = order.Amount,
            Description = order.Description,
            Status = order.Status
        }).ToList();
        return orderDtos;
    }


    public async Task UpdateStatusAsync(Guid orderId, StatusEnum status)
    {
        var order = await OrderDbContext_.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Error: can't find order {orderId}.");
        }

        order.Status = status;
        await OrderDbContext_.SaveChangesAsync();
    }
}
