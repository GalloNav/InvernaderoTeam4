using Microsoft.EntityFrameworkCore;

namespace MS3.Alarmas.Datos;

public class AlarmasDbContext(DbContextOptions<AlarmasDbContext> options) : DbContext(options)
{
    public DbSet<UmbralInvernadero> UmbralesInvernadero => Set<UmbralInvernadero>();
    public DbSet<UmbralSensor>      UmbralesSensor      => Set<UmbralSensor>();
    public DbSet<AlarmaHistorica>   AlarmasHistoricas   => Set<AlarmaHistorica>();
}
