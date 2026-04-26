using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/mediciones")]
[Authorize]
public class MedicionesProxyController(IMedicionesClient client) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Content(await client.GetAllAsync(), "application/json");

    [HttpGet("sensor/{sensorId}")]
    public async Task<IActionResult> GetBySensor(string sensorId)
        => Content(await client.GetBySensorAsync(sensorId), "application/json");

    [HttpGet("promedio/{invernaderoId}")]
    public async Task<IActionResult> GetPromedio(string invernaderoId)
        => Content(await client.GetPromedioAsync(invernaderoId), "application/json");
}
