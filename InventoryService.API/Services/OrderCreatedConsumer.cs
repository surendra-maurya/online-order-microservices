using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderCreatedConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("order-created", true, false, false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var order = JsonSerializer.Deserialize<OrderEvent>(json);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var item = await db.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == order.ProductId);

            if (item == null)
            {
                item = new InventoryItem
                {
                    ProductId = order.ProductId,
                    AvailableStock = 0
                };
                db.InventoryItems.Add(item);
            }

            item.AvailableStock -= order.Quantity;
            await db.SaveChangesAsync();
        };

        await channel.BasicConsumeAsync("order-created", true, consumer);
    }
}

public class OrderEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
