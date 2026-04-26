using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MS4.Notificaciones.Datos;

namespace MS4.Notificaciones.Controllers;

[ApiController]
[Route("api/notificaciones")]
public class NotificacionesController(NotificacionesDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHistorial()
    {
        var historial = await db.NotificacionesEnviadas
            .OrderByDescending(n => n.TimestampEnvio)
            .ToListAsync();
        return Ok(historial);
    }

    [HttpGet("sensor/{sensorId}")]
    public async Task<IActionResult> GetPorSensor(string sensorId)
    {
        var historial = await db.NotificacionesEnviadas
            .Where(n => n.SensorId == sensorId)
            .OrderByDescending(n => n.TimestampEnvio)
            .ToListAsync();
        return Ok(historial);
    }
}
