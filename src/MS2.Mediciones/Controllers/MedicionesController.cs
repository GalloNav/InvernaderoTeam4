using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MS2.Mediciones.Datos;

namespace MS2.Mediciones.Controllers;

[ApiController]
[Route("api/mediciones")]
public class MedicionesController(MedicionesDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var mediciones = await db.Mediciones
            .OrderByDescending(m => m.Timestamp)
            .Take(200)
            .ToListAsync();
        return Ok(mediciones);
    }

    [HttpGet("sensor/{sensorId}")]
    public async Task<IActionResult> GetBySensor(string sensorId)
    {
        var mediciones = await db.Mediciones
            .Where(m => m.SensorId == sensorId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
        return Ok(mediciones);
    }

    [HttpGet("promedio/{invernaderoId}")]
    public async Task<IActionResult> GetPromedio(string invernaderoId)
    {
        var query = db.Mediciones.Where(m => m.InvernaderoId == invernaderoId);
        var total = await query.CountAsync();

        if (total == 0)
            return NotFound(new { mensaje = $"No hay mediciones para invernadero '{invernaderoId}'" });

        
        var avgTemp = await query.AverageAsync(m => (double)m.Temperatura);
        var avgHum  = await query.AverageAsync(m => (double)m.Humedad);

        return Ok(new
        {
            invernaderoId,
            totalMediciones      = total,
            promedioTemperatura  = Math.Round(avgTemp, 2),
            promedioHumedad      = Math.Round(avgHum,  2)
        });
    }
}
