using Microsoft.AspNetCore.Mvc;
using MS3.Alarmas.Datos;
using MS3.Alarmas.Servicios;

namespace MS3.Alarmas.Controllers;

[ApiController]
[Route("api/umbrales")]
public class UmbralesController(
    AlarmasDbContext db,
    ICacheUmbrales   cache) : ControllerBase
{
    [HttpGet("invernadero/{id}")]
    public async Task<IActionResult> GetUmbralInvernadero(string id)
    {
        var umbral = await db.UmbralesInvernadero.FindAsync(id);
        return umbral is null ? NotFound() : Ok(umbral);
    }

    [HttpPut("invernadero/{id}")]
    public async Task<IActionResult> UpsertUmbralInvernadero(string id, [FromBody] UmbralInvernadero body)
    {
        var existing = await db.UmbralesInvernadero.FindAsync(id);
        if (existing is null)
        {
            body.InvernaderoId = id;
            db.UmbralesInvernadero.Add(body);
        }
        else
        {
            existing.TempMin = body.TempMin;
            existing.TempMax = body.TempMax;
            existing.HumMin  = body.HumMin;
            existing.HumMax  = body.HumMax;
        }

        await db.SaveChangesAsync();
        await cache.RefreshAsync();
        return NoContent();
    }

    [HttpGet("sensor/{id}")]
    public async Task<IActionResult> GetUmbralSensor(string id)
    {
        var umbral = await db.UmbralesSensor.FindAsync(id);
        return umbral is null ? NotFound() : Ok(umbral);
    }

    [HttpPut("sensor/{id}")]
    public async Task<IActionResult> UpsertUmbralSensor(string id, [FromBody] UmbralSensor body)
    {
        var existing = await db.UmbralesSensor.FindAsync(id);
        if (existing is null)
        {
            body.SensorId = id;
            db.UmbralesSensor.Add(body);
        }
        else
        {
            existing.InvernaderoId = body.InvernaderoId;
            existing.TempMin       = body.TempMin;
            existing.TempMax       = body.TempMax;
            existing.HumMin        = body.HumMin;
            existing.HumMax        = body.HumMax;
        }

        await db.SaveChangesAsync();
        await cache.RefreshAsync();
        return NoContent();
    }

    [HttpDelete("sensor/{id}")]
    public async Task<IActionResult> DeleteUmbralSensor(string id)
    {
        var umbral = await db.UmbralesSensor.FindAsync(id);
        if (umbral is null) return NotFound();

        db.UmbralesSensor.Remove(umbral);
        await db.SaveChangesAsync();
        await cache.RefreshAsync();
        return NoContent();
    }
}
