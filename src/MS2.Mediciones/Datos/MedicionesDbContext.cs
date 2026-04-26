using Microsoft.EntityFrameworkCore;

namespace MS2.Mediciones.Datos;

public class MedicionesDbContext(DbContextOptions<MedicionesDbContext> options) : DbContext(options)
{
    public DbSet<Medicion> Mediciones => Set<Medicion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medicion>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.MedicionId).HasMaxLength(36);
            e.Property(m => m.SensorId).HasMaxLength(50);
            e.Property(m => m.InvernaderoId).HasMaxLength(50);
            e.Property(m => m.Marca).HasMaxLength(20);
            e.HasIndex(m => m.SensorId);
            e.HasIndex(m => m.InvernaderoId);
            e.HasIndex(m => m.Timestamp);
        });
    }
}
