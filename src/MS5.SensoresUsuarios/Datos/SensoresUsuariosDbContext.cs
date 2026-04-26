using Microsoft.EntityFrameworkCore;

namespace MS5.SensoresUsuarios.Datos;

public class SensoresUsuariosDbContext(DbContextOptions<SensoresUsuariosDbContext> options) : DbContext(options)
{
    public DbSet<Sensor>                  Sensores                  => Set<Sensor>();
    public DbSet<Invernadero>             Invernaderos              => Set<Invernadero>();
    public DbSet<Usuario>                 Usuarios                  => Set<Usuario>();
    public DbSet<PreferenciaNotificacion> PreferenciasNotificacion  => Set<PreferenciaNotificacion>();
}
