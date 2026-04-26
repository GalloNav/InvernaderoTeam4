using System.ComponentModel.DataAnnotations;

namespace MS5.SensoresUsuarios.Datos;

public class Invernadero
{
    [Key]
    [MaxLength(50)]
    public string   Id            { get; set; } = default!;

    [MaxLength(100)]
    public string   Nombre        { get; set; } = default!;

    [MaxLength(200)]
    public string   Ubicacion     { get; set; } = default!;

    public DateTime FechaRegistro { get; set; }
}
