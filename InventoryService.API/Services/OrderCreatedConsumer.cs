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
    private readonly IConfiguration _configuration;

    private const string QueueName = "order-created";

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCreatedConsumer> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
                    Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };

                connection = await factory.CreateConnectionAsync();
                channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Process one message at a time so the database update and ACK stay ordered.
                await channel.BasicQosAsync(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(channel);

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

                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing order-created message");

                        await channel.BasicNackAsync(
                            ea.DeliveryTag,
                            false,
                            requeue: false);
                    }
                };

                var consumerTag = await channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation(
                    "InventoryService connected to RabbitMQ and consuming queue {QueueName} with consumer {ConsumerTag}.",
                    QueueName,
                    consumerTag);

                // Keep this hosted service alive. If RabbitMQ closes the connection,
                // the loop below exits and the outer loop reconnects automatically.
                while (!stoppingToken.IsCancellationRequested &&
                       connection.IsOpen &&
                       channel.IsOpen)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ consumer stopped. Retrying in 5 seconds. Error: {Message}",
                    ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                if (channel != null)
                {
                    try { await channel.CloseAsync(); } catch { }
                    await channel.DisposeAsync();
                }

                if (connection != null)
                {
                    try { await connection.CloseAsync(); } catch { }
                    await connection.DisposeAsync();
                }
            }
        }
    }
}

public class OrderEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
