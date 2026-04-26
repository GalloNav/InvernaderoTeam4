using System.ComponentModel.DataAnnotations;

namespace MS5.SensoresUsuarios.Datos;

public class Sensor
{
    [Key]
    [MaxLength(50)]
    public string   Id            { get; set; } = default!;

    [MaxLength(50)]
    public string   InvernaderoId { get; set; } = default!;

    [MaxLength(20)]
    public string   Marca         { get; set; } = default!;

    [MaxLength(20)]
    public string   TipoSensor    { get; set; } = default!;   // "temperatura"|"humedad"|"ambos"

    public bool     Activo        { get; set; }
    public DateTime FechaRegistro { get; set; }
}
