using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/invernaderos")]
public class InvernaderosProxyController(ISensoresUsuariosClient client) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
        => Content(await client.GetInvernaderosAsync(), "application/json");

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(string id)
        => Content(await client.GetInvernaderoAsync(id), "application/json");

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] JsonElement body)
        => Content(await client.CreateInvernaderoAsync(body.GetRawText()), "application/json");
}
