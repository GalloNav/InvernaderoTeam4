using Google.Protobuf.WellKnownTypes;
using Invernadero.Contracts.Alarmas;
using Invernadero.Contracts.Mediciones;
using MS3.Alarmas.Datos;
using MS3.Alarmas.Servicios;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MS3.Alarmas.Mensajeria;

public sealed class MedicionesSubscriber(
    ILogger<MedicionesSubscriber> logger,
    IConfiguration                config,
    IServiceScopeFactory          scopeFactory,
    ICacheUmbrales                cache,
    EvaluadorUmbrales             evaluador,
    AlarmasPublisher              publisher) : BackgroundService
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

        var queueName = config["RabbitMQ:QueueName"] ?? "ms3.alarmas.queue";
        var consumer  = new EventingBasicConsumer(_channel!);
        consumer.Received += OnMessageReceived;
        _channel!.BasicConsume(queueName, autoAck: false, consumer: consumer);

        logger.LogInformation("MS3 suscrito — esperando en queue '{Queue}'", queueName);

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

            _connection = factory.CreateConnection("MS3.Alarmas.Subscriber");
            _channel    = _connection.CreateModel();

            var exchangeMediciones = config["RabbitMQ:ExchangeMediciones"] ?? "invernadero.mediciones";
            var queueName          = config["RabbitMQ:QueueName"]          ?? "ms3.alarmas.queue";

            _channel.ExchangeDeclare(exchangeMediciones, ExchangeType.Fanout, durable: true, autoDelete: false);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queueName, exchangeMediciones, routingKey: string.Empty);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

            logger.LogInformation("RabbitMQ conectado — queue '{Queue}' <-> exchange '{Exchange}'",
                queueName, exchangeMediciones);
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

            logger.LogInformation(
                "Medición recibida sensor={SensorId} T={Temp:F1} H={Hum:F1}",
                evt.SensorId, evt.Temperatura, evt.Humedad);

            var umbral = cache.GetUmbralEfectivo(evt.SensorId, evt.InvernaderoId);
            if (umbral is null)
            {
                logger.LogWarning(
                    "Sin umbral configurado para sensor={SensorId} invernadero={InvId} — ignorando",
                    evt.SensorId, evt.InvernaderoId);
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            var disparadas = evaluador.Evaluar(evt.Temperatura, evt.Humedad, umbral);

            if (disparadas.Count == 0)
            {
                logger.LogInformation("-> Dentro de umbrales");
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AlarmasDbContext>();

            foreach (var (tipo, valorMedido, umbralConfig) in disparadas)
            {
                var alarmaId = Guid.NewGuid();

                logger.LogInformation(
                    "[ALARMA] sensor={SensorId} tipo={Tipo} valor={Valor:F1} umbral={Umbral:F1}",
                    evt.SensorId, tipo, valorMedido, umbralConfig);

                var alarmaEvent = new AlarmaEvent
                {
                    AlarmaId          = alarmaId.ToString(),
                    SensorId          = evt.SensorId,
                    InvernaderoId     = evt.InvernaderoId,
                    Tipo              = tipo,
                    ValorMedido       = valorMedido,
                    UmbralConfigurado = umbralConfig,
                    TimestampDisparada = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                publisher.Publish(alarmaEvent);
                logger.LogInformation("Publicada al exchange invernadero.alarmas");

                db.AlarmasHistoricas.Add(new AlarmaHistorica
                {
                    AlarmaId           = alarmaId,
                    SensorId           = evt.SensorId,
                    InvernaderoId      = evt.InvernaderoId,
                    TipoAlarma         = tipo.ToString(),
                    ValorMedido        = valorMedido,
                    UmbralConfigurado  = umbralConfig,
                    TimestampDisparada = DateTime.UtcNow
                });
            }

            db.SaveChanges();
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
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
