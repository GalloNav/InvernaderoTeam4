using System.ComponentModel.DataAnnotations;

namespace MS5.SensoresUsuarios.Datos;

public class Usuario
{
    public long     Id            { get; set; }

    [MaxLength(50)]
    public string   Username      { get; set; } = default!;

    [MaxLength(200)]
    public string   Email         { get; set; } = default!;

    [MaxLength(20)]
    public string   Telefono      { get; set; } = default!;

    [MaxLength(200)]
    public string   PushToken     { get; set; } = default!;

    [MaxLength(20)]
    public string   Rol           { get; set; } = default!;

    public DateTime FechaRegistro { get; set; }
}
