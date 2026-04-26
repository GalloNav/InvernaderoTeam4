using ApiGateway.Dtos;
using ApiGateway.Hubs;
using Invernadero.Contracts.Alarmas;
using Invernadero.Contracts.Mediciones;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ApiGateway.Mensajeria;

public sealed class EventosSubscriber(
    ILogger<EventosSubscriber> logger,
    IConfiguration             config,
    IHubContext<EventosHub>    hubContext) : BackgroundService
{
    private IConnection? _connection;
    private IModel?      _channel;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (TryConnect()) break;
            logger.LogWarning("RabbitMQ no disponible — reintentando en 3s...");
            await Task.Delay(3_000, ct);
        }

        if (ct.IsCancellationRequested) return;

        logger.LogInformation("EventosSubscriber activo — enviando por SignalR /hubs/eventos");

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

            _connection = factory.CreateConnection("ApiGateway.EventosSubscriber");
            _channel    = _connection.CreateModel();

            var exMediciones = config["RabbitMQ:ExchangeMediciones"] ?? "invernadero.mediciones";
            var exAlarmas    = config["RabbitMQ:ExchangeAlarmas"]    ?? "invernadero.alarmas";

            _channel.ExchangeDeclare(exMediciones, ExchangeType.Fanout, durable: true, autoDelete: false);
            _channel.ExchangeDeclare(exAlarmas,    ExchangeType.Fanout, durable: true, autoDelete: false);

            // Queues exclusivas con sufijo GUID — se eliminan al cerrar la conexión
            var suffix         = Guid.NewGuid().ToString("N")[..8];
            var qMediciones    = $"gateway.mediciones.{suffix}";
            var qAlarmas       = $"gateway.alarmas.{suffix}";

            _channel.QueueDeclare(qMediciones, durable: false, exclusive: true, autoDelete: true);
            _channel.QueueBind(qMediciones, exMediciones, routingKey: string.Empty);

            _channel.QueueDeclare(qAlarmas, durable: false, exclusive: true, autoDelete: true);
            _channel.QueueBind(qAlarmas, exAlarmas, routingKey: string.Empty);

            var consMediciones = new EventingBasicConsumer(_channel);
            consMediciones.Received += OnMedicionReceived;
            _channel.BasicConsume(qMediciones, autoAck: true, consumer: consMediciones);

            var consAlarmas = new EventingBasicConsumer(_channel);
            consAlarmas.Received += OnAlarmaReceived;
            _channel.BasicConsume(qAlarmas, autoAck: true, consumer: consAlarmas);

            logger.LogInformation("RabbitMQ conectado — queues '{QM}' y '{QA}'", qMediciones, qAlarmas);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Conexión a RabbitMQ fallida: {Message}", ex.Message);
            return false;
        }
    }

    private async void OnMedicionReceived(object? sender, BasicDeliverEventArgs args)
    {
        try
        {
            var evt = MedicionEvent.Parser.ParseFrom(args.Body.ToArray());
            var dto = new MedicionDto(
                evt.SensorId,
                evt.InvernaderoId,
                evt.Temperatura,
                evt.Humedad,
                evt.Marca,
                evt.Timestamp?.ToDateTime() ?? DateTime.UtcNow
            );

            await hubContext.Clients.All.SendAsync("NuevaMedicion", dto);
            logger.LogInformation("📡 SignalR push → NuevaMedicion sensor={SensorId}", dto.SensorId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando MedicionEvent para SignalR");
        }
    }

    private async void OnAlarmaReceived(object? sender, BasicDeliverEventArgs args)
    {
        try
        {
            var evt = AlarmaEvent.Parser.ParseFrom(args.Body.ToArray());
            var dto = new AlarmaDto(
                evt.SensorId,
                evt.InvernaderoId,
                evt.Tipo.ToString(),
                evt.ValorMedido,
                evt.UmbralConfigurado,
                evt.TimestampDisparada?.ToDateTime() ?? DateTime.UtcNow
            );

            await hubContext.Clients.All.SendAsync("NuevaAlarma", dto);
            logger.LogInformation("📡 SignalR push → NuevaAlarma sensor={SensorId} tipo={Tipo}",
                dto.SensorId, dto.Tipo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando AlarmaEvent para SignalR");
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
