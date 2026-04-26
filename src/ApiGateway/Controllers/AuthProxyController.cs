using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthProxyController(ISensoresUsuariosClient client) : ControllerBase
{
    // Sin [Authorize] — es la puerta de entrada
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] JsonElement body)
        => Content(await client.LoginAsync(body.GetRawText()), "application/json");
}
