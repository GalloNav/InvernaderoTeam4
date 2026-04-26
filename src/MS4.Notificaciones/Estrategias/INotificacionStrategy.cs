using Invernadero.Contracts.Alarmas;

namespace MS4.Notificaciones.Estrategias;

public interface INotificacionStrategy
{
    string Canal { get; }
    Task<ResultadoNotificacion> EnviarAsync(AlarmaEvent alarma, string destinatario);
}
