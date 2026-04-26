using Microsoft.EntityFrameworkCore;

namespace MS4.Notificaciones.Datos;

public class NotificacionesDbContext(DbContextOptions<NotificacionesDbContext> options) : DbContext(options)
{
    public DbSet<NotificacionEnviada> NotificacionesEnviadas => Set<NotificacionEnviada>();
}
