using System.ComponentModel.DataAnnotations;

namespace MS3.Alarmas.Datos;

public class UmbralInvernadero
{
    [Key]
    [MaxLength(50)]
    public string InvernaderoId { get; set; } = default!;

    public float TempMin { get; set; }
    public float TempMax { get; set; }
    public float HumMin  { get; set; }
    public float HumMax  { get; set; }
}
