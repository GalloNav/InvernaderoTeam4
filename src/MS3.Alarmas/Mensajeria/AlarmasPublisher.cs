using Google.Protobuf;
using Invernadero.Contracts.Alarmas;
using RabbitMQ.Client;

namespace MS3.Alarmas.Mensajeria;

public sealed class AlarmasPublisher(
    IConfiguration           config,
    ILogger<AlarmasPublisher> logger) : IDisposable
{
    private readonly object _lock     = new();
    private readonly string _exchange = config["RabbitMQ:ExchangeAlarmas"] ?? "invernadero.alarmas";
    private IConnection?    _connection;
    private IModel?         _channel;

    public void EnsureConnected()
    {
        lock (_lock)
        {
            if (_channel?.IsOpen == true) return;

            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:HostName"] ?? "localhost",
                Port     = config.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = config["RabbitMQ:User"]     ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection("MS3.Alarmas.Publisher");
            _channel    = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Fanout, durable: true, autoDelete: false);

            logger.LogInformation("AlarmasPublisher conectado → exchange '{Exchange}'", _exchange);
        }
    }

    public void Publish(AlarmaEvent alarmaEvent)
    {
        EnsureConnected();
        lock (_lock)
        {
            var bytes = alarmaEvent.ToByteArray();
            _channel!.BasicPublish(_exchange, routingKey: string.Empty, basicProperties: null, body: bytes);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
