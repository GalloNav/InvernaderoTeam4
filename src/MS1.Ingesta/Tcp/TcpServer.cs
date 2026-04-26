using System.Net;
using System.Net.Sockets;
using MS1.Ingesta.Adaptadores;
using MS1.Ingesta.Mensajeria;

namespace MS1.Ingesta.Tcp;

public class TcpServer(ILogger<TcpServer> logger, IConfiguration config, RabbitPublisher publisher)
{
    private readonly int _port = config.GetValue<int>("Tcp:Port", 6000);

    public async Task StartAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        logger.LogInformation("TCP escuchando en puerto {Port}", _port);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error aceptando conexión TCP");
            }
        }

        listener.Stop();
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();

                // Leer primer byte para identificar la marca
                var markerBuf = new byte[1];
                if (await stream.ReadAsync(markerBuf, ct) == 0) return;
                var marker = markerBuf[0];

                var frameSize = AdapterFactory.GetFrameSize(marker);
                if (frameSize < 0)
                {
                    logger.LogWarning("Byte desconocido 0x{Marker:X2} — conexión descartada", marker);
                    return;
                }

                // Leer el resto del frame según tamaño conocido por la marca
                var frame      = new byte[frameSize];
                frame[0]       = marker;
                var totalRead  = 1;
                while (totalRead < frameSize)
                {
                    var read = await stream.ReadAsync(frame.AsMemory(totalRead, frameSize - totalRead), ct);
                    if (read == 0) break;
                    totalRead += read;
                }

                if (totalRead < frameSize)
                {
                    logger.LogWarning("Frame incompleto: esperado {Expected}B, recibido {Got}B", frameSize, totalRead);
                    return;
                }

                try
                {
                    var adapter = AdapterFactory.GetAdapter(marker);
                    if (adapter.TryParse(frame, out var medicion) && medicion is not null)
                    {
                        publisher.Publish(medicion);
                        logger.LogInformation(
                            "Medición recibida — sensor={SensorId} marca={Marca} T={Temp:F1}°C H={Hum:F1}%",
                            medicion.SensorId, medicion.Marca, medicion.Temperatura, medicion.Humedad);
                    }
                }
                catch (NotImplementedException)
                {
                    logger.LogWarning("Marca C no soportada aún — frame descartado");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error procesando frame TCP");
            }
        }
    }
}
