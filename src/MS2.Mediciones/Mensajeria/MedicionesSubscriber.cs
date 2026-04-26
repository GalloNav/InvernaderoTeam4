using Invernadero.Contracts.Mediciones;
using Microsoft.EntityFrameworkCore;
using MS2.Mediciones.Datos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MS2.Mediciones.Mensajeria;

public sealed class MedicionesSubscriber(
    ILogger<MedicionesSubscriber> logger,
    IConfiguration config,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private IConnection? _connection;
    private IModel?      _channel;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Reintento de conexión: RabbitMQ puede tardar en estar listo
        while (!ct.IsCancellationRequested)
        {
            if (TryConnect()) break;
            logger.LogWarning("RabbitMQ no disponible — reintentando en 3s...");
            await Task.Delay(3_000, ct);
        }

        if (ct.IsCancellationRequested) return;

        var queueName = config["RabbitMQ:QueueName"] ?? "ms2.mediciones.queue";
        var consumer  = new EventingBasicConsumer(_channel!);
        consumer.Received += OnMessageReceived;
        _channel!.BasicConsume(queueName, autoAck: false, consumer: consumer);

        logger.LogInformation("MS2 suscrito — esperando en queue '{Queue}'", queueName);

        // Mantener el BackgroundService vivo hasta cancelación
        await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
    }

    private bool TryConnect()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:HostName"] ?? "localhost",
                Port     = config.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = config["RabbitMQ:User"]     ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection("MS2.Mediciones");
            _channel    = _connection.CreateModel();

            var exchange  = config["RabbitMQ:ExchangeMediciones"] ?? "invernadero.mediciones";
            var queueName = config["RabbitMQ:QueueName"]          ?? "ms2.mediciones.queue";

            _channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true, autoDelete: false);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queueName, exchange, routingKey: string.Empty);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

            logger.LogInformation("RabbitMQ conectado — queue '{Queue}' ↔ exchange '{Exchange}'",
                queueName, exchange);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Conexión a RabbitMQ fallida: {Message}", ex.Message);
            return false;
        }
    }

    private void OnMessageReceived(object? sender, BasicDeliverEventArgs args)
    {
        try
        {
            var evt = MedicionEvent.Parser.ParseFrom(args.Body.ToArray());

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MedicionesDbContext>();

            db.Mediciones.Add(new Medicion
            {
                MedicionId    = evt.MedicionId,
                SensorId      = evt.SensorId,
                InvernaderoId = evt.InvernaderoId,
                Timestamp     = evt.Timestamp.ToDateTime(),
                Temperatura   = evt.Temperatura,
                Humedad       = evt.Humedad,
                Marca         = evt.Marca
            });
            db.SaveChanges();

            _channel!.BasicAck(args.DeliveryTag, multiple: false);

            logger.LogInformation(
                "Guardada medición sensor={SensorId} marca={Marca} T={Temp:F1}°C H={Hum:F1}%",
                evt.SensorId, evt.Marca, evt.Temperatura, evt.Humedad);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando mensaje — nack+requeue");
            _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
