using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MS5.SensoresUsuarios.Datos;

namespace MS5.SensoresUsuarios.Controllers;

[ApiController]
[Route("api/sensores")]
public class SensoresController(SensoresUsuariosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Sensores.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var sensor = await db.Sensores.FindAsync(id);
        return sensor is null ? NotFound() : Ok(sensor);
    }

    [HttpGet("{id}/existe")]
    public async Task<IActionResult> Existe(string id)
    {
        var existe = await db.Sensores.AnyAsync(s => s.Id == id && s.Activo);
        return Ok(new { existe });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Sensor sensor)
    {
        sensor.FechaRegistro = DateTime.UtcNow;
        db.Sensores.Add(sensor);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = sensor.Id }, sensor);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var sensor = await db.Sensores.FindAsync(id);
        if (sensor is null) return NotFound();
        db.Sensores.Remove(sensor);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
