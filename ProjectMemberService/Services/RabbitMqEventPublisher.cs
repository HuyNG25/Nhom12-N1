using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ProjectMemberService.Services
{
    public class RabbitMqEventPublisher : IEventPublisher, IDisposable
    {
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private bool _isConnected = false;

        public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
        {
            _logger = logger;
            TryConnect(configuration);
        }

        private void TryConnect(IConfiguration configuration)
        {
            try
            {
                var rabbitHost = configuration["RabbitMq:Host"] ?? "localhost";
                var factory = new ConnectionFactory
                {
                    HostName = rabbitHost,
                    UserName = "guest",
                    Password = "guest",
                    Port = 5672,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
                };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _isConnected = true;
                _logger.LogInformation("Connected to RabbitMQ at {Host}", rabbitHost);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning("Could not connect to RabbitMQ: {Message}. Events will be logged only.", ex.Message);
            }
        }

        public Task PublishAsync<T>(string eventName, T eventData)
        {
            var message = JsonSerializer.Serialize(eventData);
            _logger.LogInformation("Publishing event [{EventName}]: {Message}", eventName, message);

            if (_isConnected && _channel != null && _channel.IsOpen)
            {
                try
                {
                    // Declare a fanout exchange for the event
                    _channel.ExchangeDeclare(exchange: eventName, type: ExchangeType.Fanout, durable: true, autoDelete: false);

                    var body = Encoding.UTF8.GetBytes(message);
                    var props = _channel.CreateBasicProperties();
                    props.ContentType = "application/json";
                    props.DeliveryMode = 2; // persistent

                    _channel.BasicPublish(
                        exchange: eventName,
                        routingKey: string.Empty,
                        basicProperties: props,
                        body: body
                    );

                    _logger.LogInformation("Event [{EventName}] published to RabbitMQ successfully.", eventName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish event [{EventName}] to RabbitMQ.", eventName);
                }
            }
            else
            {
                _logger.LogWarning("RabbitMQ not connected. Event [{EventName}] was logged but not published to broker.", eventName);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
        }
    }
}
