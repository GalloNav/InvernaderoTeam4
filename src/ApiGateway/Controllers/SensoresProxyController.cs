using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/sensores")]
[Authorize]
public class SensoresProxyController(ISensoresUsuariosClient client) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Content(await client.GetSensoresAsync(), "application/json");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement body)
        => Content(await client.CreateSensorAsync(body.GetRawText()), "application/json");

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await client.DeleteSensorAsync(id);
        return NoContent();
    }
}
