using Invernadero.Contracts.Alarmas;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MS4.Notificaciones.Configuracion;

namespace MS4.Notificaciones.Estrategias;

public sealed class EstrategiaEmail(
    IOptions<MailtrapOptions>  options,
    ILogger<EstrategiaEmail>   logger) : INotificacionStrategy
{
    public string Canal => "Email";

    public async Task<ResultadoNotificacion> EnviarAsync(AlarmaEvent alarma, string destinatario)
    {
        try
        {
            var cfg = options.Value;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.FromName, cfg.FromAddress));
            message.To.Add(new MailboxAddress(destinatario, destinatario));
            message.Subject = $"🚨 Alarma {alarma.Tipo} en sensor {alarma.SensorId}";

            message.Body = new TextPart("html")
            {
                Text = $"""
                        <h2>🚨 Alarma detectada en invernadero</h2>
                        <table style="border-collapse:collapse">
                          <tr><td style="padding:4px 12px"><b>Sensor</b></td><td>{alarma.SensorId}</td></tr>
                          <tr><td style="padding:4px 12px"><b>Invernadero</b></td><td>{alarma.InvernaderoId}</td></tr>
                          <tr><td style="padding:4px 12px"><b>Tipo</b></td><td>{alarma.Tipo}</td></tr>
                          <tr><td style="padding:4px 12px"><b>Valor medido</b></td><td>{alarma.ValorMedido:F2}</td></tr>
                          <tr><td style="padding:4px 12px"><b>Umbral configurado</b></td><td>{alarma.UmbralConfigurado:F2}</td></tr>
                          <tr><td style="padding:4px 12px"><b>Timestamp</b></td><td>{alarma.TimestampDisparada?.ToDateTime():O}</td></tr>
                        </table>
                        """
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(cfg.Host, cfg.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(cfg.User, cfg.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return new ResultadoNotificacion(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning("EstrategiaEmail falló: {Message}", ex.Message);
            return new ResultadoNotificacion(false, ex.Message);
        }
    }
}
