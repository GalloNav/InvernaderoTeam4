namespace MS5.SensoresUsuarios.Datos;

public class PreferenciaNotificacion
{
    public long Id          { get; set; }
    public long UsuarioId   { get; set; }
    public bool EmailActivo { get; set; }
    public bool SmsActivo   { get; set; }
    public bool PushActivo  { get; set; }
}
