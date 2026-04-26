namespace MS4.Notificaciones.Configuracion;

public class MailtrapOptions
{
    public string Host        { get; set; } = default!;
    public int    Port        { get; set; }
    public string User        { get; set; } = default!;
    public string Password    { get; set; } = default!;
    public string FromAddress { get; set; } = default!;
    public string FromName    { get; set; } = default!;
}
