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

    public OrderService(IProductClient productClient,
                        OrderDbContext db,
                        OrderEventPublisher publisher)
    {
        _productClient = productClient;
        _db = db;
        _publisher = publisher;
    }


    public async Task<int> CreateOrderAsync(CreateOrderDto dto)
    {
        try
        {
            var available = await _productClient
            .IsProductAvailableAsync(dto.ProductId, dto.Quantity);

            if (!available)
                throw new Exception("Product not available");

            var order = new Order
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _publisher.PublishOrderCreated(new
            {
                order.Id,
                order.ProductId,
                order.Quantity,
                order.CreatedAt
            });

            return order.Id;
        }
        catch (Exception ex) {
            throw ex;
        }
    }
}
