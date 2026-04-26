namespace MS5.SensoresUsuarios.Auth;

public class JwtOptions
{
    public string SigningKey        { get; set; } = default!;
    public string Issuer            { get; set; } = default!;
    public string Audience          { get; set; } = default!;
    public int    ExpirationMinutes { get; set; } = 60;
}
