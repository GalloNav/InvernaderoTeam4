using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MS5.SensoresUsuarios.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opts = options.Value;

    public (string Token, DateTime ExpiraEn) GenerarToken(string username, string rol)
    {
        var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds     = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var emitidoEn = DateTime.UtcNow;
        var expiraEn  = emitidoEn.AddMinutes(_opts.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim("role", rol),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(emitidoEn).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer:             _opts.Issuer,
            audience:           _opts.Audience,
            claims:             claims,
            notBefore:          emitidoEn,
            expires:            expiraEn,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
