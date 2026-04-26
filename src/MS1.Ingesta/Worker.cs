using MS1.Ingesta.Tcp;

namespace MS1.Ingesta;

public class Worker(ILogger<Worker> logger, TcpServer tcpServer) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MS1.Ingesta iniciando...");
        return tcpServer.StartAsync(stoppingToken);
    }
}
