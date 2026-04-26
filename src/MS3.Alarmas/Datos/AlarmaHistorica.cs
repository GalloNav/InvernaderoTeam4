using System.ComponentModel.DataAnnotations;

namespace MS3.Alarmas.Datos;

public class AlarmaHistorica
{
    public long     Id                  { get; set; }
    public Guid     AlarmaId            { get; set; }

    [MaxLength(50)]
    public string   SensorId            { get; set; } = default!;

    [MaxLength(50)]
    public string   InvernaderoId       { get; set; } = default!;

    [MaxLength(30)]
    public string   TipoAlarma          { get; set; } = default!;

    public float    ValorMedido         { get; set; }
    public float    UmbralConfigurado   { get; set; }
    public DateTime TimestampDisparada  { get; set; }
}
