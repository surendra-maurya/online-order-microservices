using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Application.Services;

public class OrderEventPublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public OrderEventPublisher()
    {
        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq"
        };

        // 7.x API
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "order-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();
    }

    public void PublishOrderCreated(object order)
    {
        var json = JsonSerializer.Serialize(order);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublishAsync(
            exchange: "",
            routingKey: "order-created",
            mandatory: false,
            body: body
        ).GetAwaiter().GetResult();
    }
}
