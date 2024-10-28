using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace WebApplicationRegistrationByEmail.Services
{
    public class MessageQueueService
    {
        private readonly string _hostname;
        private readonly int _port;
        private readonly string _queueName;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public MessageQueueService(IOptions<RabbitMQSettings> config)
        {
            _hostname = config.Value.Host;
            _port = config.Value.Port;
            _queueName = config.Value.QueueName;

            var factory = new ConnectionFactory { HostName = _hostname, Port = _port };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false);
        }

        public void SendEmailMessage(string email, string code)
        {
            var message = new { Email = email, Code = code };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            _channel.BasicPublish(exchange: "", routingKey: _queueName, basicProperties: null, body: body);
        }
    }

    public class RabbitMQSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string QueueName { get; set; }
    }
}
