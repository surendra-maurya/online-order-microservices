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
    private readonly ILogger<OrderCreatedConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "rabbitmq"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: "order-created",
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var order = JsonSerializer.Deserialize<OrderEvent>(json)!;

                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                        var item = await db.InventoryItems
                            .FirstOrDefaultAsync(
                                x => x.ProductId == order.ProductId,
                                stoppingToken);

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
                        await db.SaveChangesAsync(stoppingToken);

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing order-created message");

                        await _channel.BasicNackAsync(
                            ea.DeliveryTag,
                            false,
                            requeue: false);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: "order-created",
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("InventoryService connected to RabbitMQ.");

                break; // Exit retry loop
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "RabbitMQ not ready. Retrying in 5 seconds. Error: {Message}",
                    ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping InventoryService RabbitMQ consumer.");

        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();

        await base.StopAsync(cancellationToken);
    }
}

public class OrderEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
