using System.ComponentModel.DataAnnotations;

namespace MS4.Notificaciones.Datos;

public class NotificacionEnviada
{
    public long     Id            { get; set; }
    public Guid     AlarmaId      { get; set; }

    [MaxLength(50)]
    public string   SensorId      { get; set; } = default!;

    [MaxLength(50)]
    public string   InvernaderoId { get; set; } = default!;

    [MaxLength(30)]
    public string   TipoAlarma    { get; set; } = default!;

    [MaxLength(10)]
    public string   Canal         { get; set; } = default!;   // "Email" | "SMS" | "Push"

    [MaxLength(200)]
    public string   Destinatario  { get; set; } = default!;

    [MaxLength(10)]
    public string   Estado        { get; set; } = default!;   // "Enviado" | "Error"

    public string?  MensajeError  { get; set; }

    public DateTime TimestampEnvio { get; set; }
}
