using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/umbrales")]
[Authorize]
public class AlarmasProxyController(IAlarmasClient client) : ControllerBase
{
    [HttpGet("invernadero/{id}")]
    public async Task<IActionResult> GetInvernadero(string id)
        => Content(await client.GetUmbralInvernaderoAsync(id), "application/json");

    [HttpPut("invernadero/{id}")]
    public async Task<IActionResult> UpdateInvernadero(string id, [FromBody] JsonElement body)
    {
        await client.UpdateUmbralInvernaderoAsync(id, body.GetRawText());
        return NoContent();
    }

    [HttpGet("sensor/{id}")]
    public async Task<IActionResult> GetSensor(string id)
        => Content(await client.GetUmbralSensorAsync(id), "application/json");

    [HttpPut("sensor/{id}")]
    public async Task<IActionResult> UpdateSensor(string id, [FromBody] JsonElement body)
    {
        await client.UpdateUmbralSensorAsync(id, body.GetRawText());
        return NoContent();
    }

    [HttpDelete("sensor/{id}")]
    public async Task<IActionResult> DeleteSensor(string id)
    {
        await client.DeleteUmbralSensorAsync(id);
        return NoContent();
    }
}
