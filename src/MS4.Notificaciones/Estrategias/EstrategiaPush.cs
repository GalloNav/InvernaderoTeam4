using Invernadero.Contracts.Alarmas;

namespace MS4.Notificaciones.Estrategias;

public sealed class EstrategiaPush(ILogger<EstrategiaPush> logger) : INotificacionStrategy
{
    public string Canal => "Push";

    public async Task<ResultadoNotificacion> EnviarAsync(AlarmaEvent alarma, string destinatario)
    {
        await Task.Delay(100);
        logger.LogInformation(
            "[MOCK PUSH] Token={Token}, mensaje: ALARMA {Tipo} sensor {SensorId}",
            destinatario, alarma.Tipo, alarma.SensorId);
        return new ResultadoNotificacion(true);
    }
}
