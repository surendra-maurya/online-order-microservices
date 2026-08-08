using OrderService.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Application.Services
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqPublisher()
        {
            _factory = new ConnectionFactory
            {
                HostName = "rabbitmq"
            };
        }

        public async Task PublishAsync<T>(string queueName, T message)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body);
        }
    }
}
