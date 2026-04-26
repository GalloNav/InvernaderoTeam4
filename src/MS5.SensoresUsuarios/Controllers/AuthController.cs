using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MS5.SensoresUsuarios.Auth;

namespace MS5.SensoresUsuarios.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    IOptions<UsuariosMockOptions> mockOpts,
    JwtTokenService               tokenService,
    ILogger<AuthController>       logger) : ControllerBase
{
    public record LoginRequest(string Usuario, string Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        logger.LogInformation("POST /auth/login intentando '{Usuario}'...", req.Usuario);

        var usuario = mockOpts.Value.Usuarios
            .FirstOrDefault(u => u.Username == req.Usuario && u.Password == req.Password);

        if (usuario is null)
        {
            logger.LogWarning("✗ Credenciales inválidas para '{Usuario}'", req.Usuario);
            return Unauthorized(new { error = "Credenciales inválidas" });
        }

        var (token, expiraEn) = tokenService.GenerarToken(usuario.Username, usuario.Rol);

        logger.LogInformation("✓ Login exitoso, token emitido para '{Usuario}' [{Rol}]",
            usuario.Username, usuario.Rol);

        return Ok(new
        {
            token,
            expiraEn,
            rol = usuario.Rol
        });
    }
}
