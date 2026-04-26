using Invernadero.Contracts.Alarmas;
using MS4.Notificaciones.Datos;
using MS4.Notificaciones.Estrategias;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MS4.Notificaciones.Mensajeria;

public sealed class AlarmasSubscriber(
    ILogger<AlarmasSubscriber>          logger,
    IConfiguration                      config,
    IServiceScopeFactory                scopeFactory,
    IEnumerable<INotificacionStrategy>  strategies) : BackgroundService
{
    private readonly IReadOnlyList<INotificacionStrategy> _strategies = strategies.ToList();
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

        var queueName = config["RabbitMQ:QueueName"] ?? "ms4.notificaciones.queue";
        var consumer  = new EventingBasicConsumer(_channel!);
        consumer.Received += OnMessageReceived;
        _channel!.BasicConsume(queueName, autoAck: false, consumer: consumer);

        logger.LogInformation("MS4 suscrito — esperando en queue '{Queue}'", queueName);

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

            _connection = factory.CreateConnection("MS4.Notificaciones");
            _channel    = _connection.CreateModel();

            var exchangeAlarmas = config["RabbitMQ:ExchangeAlarmas"] ?? "invernadero.alarmas";
            var queueName       = config["RabbitMQ:QueueName"]       ?? "ms4.notificaciones.queue";

            _channel.ExchangeDeclare(exchangeAlarmas, ExchangeType.Fanout, durable: true, autoDelete: false);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queueName, exchangeAlarmas, routingKey: string.Empty);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

            logger.LogInformation("RabbitMQ conectado — queue '{Queue}' ↔ exchange '{Exchange}'",
                queueName, exchangeAlarmas);
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
            var alarma = AlarmaEvent.Parser.ParseFrom(args.Body.ToArray());

            logger.LogInformation("📨 Procesando alarma {Tipo} sensor={SensorId}",
                alarma.Tipo, alarma.SensorId);

            var email    = config["DestinatariosMock:Email"]     ?? "admin@invernadero.com";
            var telefono = config["DestinatariosMock:Telefono"]  ?? "+5216441234567";
            var pushTok  = config["DestinatariosMock:PushToken"] ?? "fcm_token_demo_123";

            string GetDestinatario(string canal) => canal switch
            {
                "Email" => email,
                "SMS"   => telefono,
                "Push"  => pushTok,
                _       => string.Empty
            };

            // Ejecutar las 3 estrategias en paralelo (bloqueo deliberado: callback es void)
            var tareas     = _strategies.Select(s => s.EnviarAsync(alarma, GetDestinatario(s.Canal))).ToArray();
            var resultados = Task.WhenAll(tareas).GetAwaiter().GetResult();

            // Persistir — si falla la BD, el catch lanza NACK+requeue
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificacionesDbContext>();

            for (int i = 0; i < _strategies.Count; i++)
            {
                var s    = _strategies[i];
                var r    = resultados[i];
                var dest = GetDestinatario(s.Canal);

                if (r.Exitoso)
                    logger.LogInformation("{Prefix} Enviado a {Dest} — OK", LogPrefix(s.Canal), dest);
                else
                    logger.LogWarning("{Prefix} ERROR: {Error}", LogPrefix(s.Canal), r.MensajeError);

                db.NotificacionesEnviadas.Add(new NotificacionEnviada
                {
                    AlarmaId       = Guid.Parse(alarma.AlarmaId),
                    SensorId       = alarma.SensorId,
                    InvernaderoId  = alarma.InvernaderoId,
                    TipoAlarma     = alarma.Tipo.ToString(),
                    Canal          = s.Canal,
                    Destinatario   = dest,
                    Estado         = r.Exitoso ? "Enviado" : "Error",
                    MensajeError   = r.MensajeError,
                    TimestampEnvio = DateTime.UtcNow
                });
            }

            db.SaveChanges();
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando alarma — nack+requeue");
            _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static string LogPrefix(string canal) => canal switch
    {
        "Email" => "[EMAIL]",
        "SMS"   => "[MOCK SMS]",
        "Push"  => "[MOCK PUSH]",
        _       => $"[{canal}]"
    };

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
