using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Application.Services;

public class OrderEventPublisher : IAsyncDisposable
{
    private readonly ILogger<OrderEventPublisher> _logger;
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderEventPublisher(ILogger<OrderEventPublisher> logger)
    {
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = "rabbitmq"
        };
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection != null && _connection.IsOpen &&
            _channel != null && _channel.IsOpen)
            return;

        _logger.LogInformation("Connecting to RabbitMQ...");

        _connection = await _factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "order-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        _logger.LogInformation("RabbitMQ publisher connected.");
    }

    public async Task PublishOrderCreatedAsync(object order)
    {
        await EnsureConnectedAsync();

        var json = JsonSerializer.Serialize(order);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: "order-created",
            mandatory: false,
            body: body
        );

        _logger.LogInformation("OrderCreated event published.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();
    }
}
