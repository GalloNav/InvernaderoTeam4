using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MS5.SensoresUsuarios.Datos;

namespace MS5.SensoresUsuarios.Controllers;

[ApiController]
[Route("api/invernaderos")]
public class InvernaderosController(SensoresUsuariosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Invernaderos.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var inv = await db.Invernaderos.FindAsync(id);
        return inv is null ? NotFound() : Ok(inv);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Invernadero invernadero)
    {
        invernadero.FechaRegistro = DateTime.UtcNow;
        db.Invernaderos.Add(invernadero);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = invernadero.Id }, invernadero);
    }
}
