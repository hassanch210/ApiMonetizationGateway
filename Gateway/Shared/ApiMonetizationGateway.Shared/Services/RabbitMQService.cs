using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ApiMonetizationGateway.Shared.Services;

public class RabbitMQService : IMessageQueueService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMQService(IConnection connection)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<T>(string queueName, T message) where T : class
    {
        await DeclareQueueAsync(queueName);
        
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body
        );

        await Task.CompletedTask;
    }

    public async Task<T?> ConsumeAsync<T>(string queueName) where T : class
    {
        await DeclareQueueAsync(queueName);
        
        var result = _channel.BasicGet(queueName, autoAck: true);
        if (result == null)
            return null;

        var json = Encoding.UTF8.GetString(result.Body.ToArray());
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class
    {
        DeclareQueueAsync(queueName).Wait();
        
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                if (message != null)
                {
                    await handler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (Exception)
            {
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );
    }

    public async Task DeclareQueueAsync(string queueName, bool durable = true)
    {
        _channel.QueueDeclare(
            queue: queueName,
            durable: durable,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
        
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}