using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MS3.Alarmas.Datos;

namespace MS3.Alarmas.Servicios;

public sealed class CacheUmbrales(
    IServiceScopeFactory   scopeFactory,
    ILogger<CacheUmbrales> logger) : ICacheUmbrales
{
    private ConcurrentDictionary<string, UmbralInvernadero> _invernaderos = new();
    private ConcurrentDictionary<string, UmbralSensor>      _sensores     = new();

    public UmbralEfectivo? GetUmbralEfectivo(string sensorId, string invernaderoId)
    {
        _sensores.TryGetValue(sensorId,       out var sensorOverride);
        _invernaderos.TryGetValue(invernaderoId, out var defecto);

        if (defecto is null) return null;

        return new UmbralEfectivo(
            InvernaderoId: invernaderoId,
            TempMin: sensorOverride?.TempMin ?? defecto.TempMin,
            TempMax: sensorOverride?.TempMax ?? defecto.TempMax,
            HumMin:  sensorOverride?.HumMin  ?? defecto.HumMin,
            HumMax:  sensorOverride?.HumMax  ?? defecto.HumMax
        );
    }

    public async Task RefreshAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlarmasDbContext>();

        var invernaderos = await db.UmbralesInvernadero.AsNoTracking().ToListAsync();
        var sensores     = await db.UmbralesSensor.AsNoTracking().ToListAsync();

        var newInv = new ConcurrentDictionary<string, UmbralInvernadero>();
        foreach (var u in invernaderos) newInv[u.InvernaderoId] = u;

        var newSen = new ConcurrentDictionary<string, UmbralSensor>();
        foreach (var s in sensores) newSen[s.SensorId] = s;

        // Swap atómico para no dejar ventana vacía
        _invernaderos = newInv;
        _sensores     = newSen;

        logger.LogInformation("Caché recargada — {Inv} invernaderos, {Sen} sensores con override",
            invernaderos.Count, sensores.Count);
    }
}
