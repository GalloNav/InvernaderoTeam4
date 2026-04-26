using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MS5.SensoresUsuarios.Datos;

namespace MS5.SensoresUsuarios.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController(SensoresUsuariosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Usuarios.ToListAsync());

    [HttpGet("{id}/preferencias")]
    public async Task<IActionResult> GetPreferencias(long id)
    {
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        var pref = await db.PreferenciasNotificacion
            .FirstOrDefaultAsync(p => p.UsuarioId == id);

        var canales = new List<string>();
        if (pref?.EmailActivo == true) canales.Add("Email");
        if (pref?.SmsActivo   == true) canales.Add("SMS");
        if (pref?.PushActivo  == true) canales.Add("Push");

        return Ok(new
        {
            usuario.Email,
            usuario.Telefono,
            usuario.PushToken,
            canalesActivos = canales
        });
    }
}
