using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosProxyController(ISensoresUsuariosClient client) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Content(await client.GetUsuariosAsync(), "application/json");

    [HttpGet("{id}/preferencias")]
    public async Task<IActionResult> GetPreferencias(long id)
        => Content(await client.GetPreferenciasUsuarioAsync(id), "application/json");
}
