using Invernadero.Contracts.Alarmas;

namespace MS4.Notificaciones.Estrategias;

public sealed class EstrategiaSMS(ILogger<EstrategiaSMS> logger) : INotificacionStrategy
{
    public string Canal => "SMS";

    public async Task<ResultadoNotificacion> EnviarAsync(AlarmaEvent alarma, string destinatario)
    {
        await Task.Delay(100);
        logger.LogInformation(
            "📱 [MOCK SMS] Para {Tel}, msg: ALARMA {Tipo} sensor {SensorId}",
            destinatario, alarma.Tipo, alarma.SensorId);
        return new ResultadoNotificacion(true);
    }
}
