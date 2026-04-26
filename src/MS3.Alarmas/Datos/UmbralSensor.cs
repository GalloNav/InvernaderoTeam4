using System.ComponentModel.DataAnnotations;

namespace MS3.Alarmas.Datos;

public class UmbralSensor
{
    [Key]
    [MaxLength(50)]
    public string SensorId { get; set; } = default!;

    [MaxLength(50)]
    public string InvernaderoId { get; set; } = default!;

    // Nullable: solo se sobrescribe el campo que se desea
    public float? TempMin { get; set; }
    public float? TempMax { get; set; }
    public float? HumMin  { get; set; }
    public float? HumMax  { get; set; }
}
