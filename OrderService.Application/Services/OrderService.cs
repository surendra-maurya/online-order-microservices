using Microsoft.Extensions.Logging;
using OrderService.Application.Clients;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Application.Services;

public class OrderService : IOrderService
{
    private readonly IProductClient _productClient;
    private readonly OrderDbContext _db;
    private readonly OrderEventPublisher _publisher;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IProductClient productClient,
        OrderDbContext db,
        OrderEventPublisher publisher,
        ILogger<OrderService> logger)
    {
        _productClient = productClient;
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<int> CreateOrderAsync(CreateOrderDto dto)
    {
        // Validate product availability (sync communication)
        var available = await _productClient
            .IsProductAvailableAsync(dto.ProductId, dto.Quantity);

        if (!available)
            throw new InvalidOperationException("Product not available");

        // Create order (local transaction)
        var order = new Order
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // DB COMMIT FIRST

        _logger.LogInformation(
            "Order {OrderId} created successfully. Publishing OrderCreated event.",
            order.Id);

        // Publish event AFTER successful DB commit (Saga start)
        await _publisher.PublishOrderCreatedAsync(new
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity
        });

        return order.Id;
    }
}
