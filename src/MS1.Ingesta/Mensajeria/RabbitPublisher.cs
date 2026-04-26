using Google.Protobuf;
using Invernadero.Contracts.Mediciones;
using RabbitMQ.Client;

namespace MS1.Ingesta.Mensajeria;

public sealed class RabbitPublisher : IDisposable
{
    private readonly ILogger<RabbitPublisher> _logger;
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly string _exchange;

    public RabbitPublisher(ILogger<RabbitPublisher> logger, IConfiguration config)
    {
        _logger   = logger;
        _exchange = config["RabbitMQ:ExchangeMediciones"] ?? "invernadero.mediciones";

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            Port     = config.GetValue<int>("RabbitMQ:Port", 5672),
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection("MS1.Ingesta");
        _channel    = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Fanout, durable: true, autoDelete: false);

        _logger.LogInformation("RabbitMQ conectado — exchange '{Exchange}'", _exchange);
    }

    public void Publish(MedicionEvent medicion)
    {
        var body = medicion.ToByteArray();
        _channel.BasicPublish(_exchange, routingKey: string.Empty, basicProperties: null, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
