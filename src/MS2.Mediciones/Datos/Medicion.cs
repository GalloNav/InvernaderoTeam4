namespace MS2.Mediciones.Datos;

public class Medicion
{
    public long     Id            { get; set; }
    public string   MedicionId    { get; set; } = string.Empty;
    public string   SensorId      { get; set; } = string.Empty;
    public string   InvernaderoId { get; set; } = string.Empty;
    public DateTime Timestamp     { get; set; }
    public float    Temperatura   { get; set; }
    public float    Humedad       { get; set; }
    public string   Marca         { get; set; } = string.Empty;
}
