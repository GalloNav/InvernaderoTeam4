using Serilog;

namespace ApiGateway.Servicios.Balanceador;

public sealed class RoundRobinBalanceador : IRoundRobinBalanceador
{
    private readonly Dictionary<string, List<Uri>> _endpoints;
    private readonly Dictionary<string, int[]>     _contadores;

    public RoundRobinBalanceador(IConfiguration config)
    {
        _endpoints  = new(StringComparer.OrdinalIgnoreCase);
        _contadores = new(StringComparer.OrdinalIgnoreCase);

        foreach (var child in config.GetSection("ServiciosInternos").GetChildren())
        {
            var urls = child.Get<string[]>() ?? [];
            _endpoints[child.Key]  = [.. urls.Select(u => new Uri(u))];
            _contadores[child.Key] = [0];
        }
    }

    public Uri ObtenerSiguiente(string servicio)
    {
        if (!_endpoints.TryGetValue(servicio, out var lista))
            throw new KeyNotFoundException(
                $"Servicio '{servicio}' no encontrado en ServiciosInternos.");

        if (lista.Count == 1)
        {
            Log.Information("[BALANCEADOR] {Servicio} -> instancia [0] {Url}", servicio, lista[0]);
            return lista[0];
        }

        // Interlocked.Increment sobre int[] es thread-safe sin lock.
        // Cast a uint antes del módulo para evitar índices negativos en caso de overflow.
        var val = Interlocked.Increment(ref _contadores[servicio][0]);
        var idx = (int)((uint)val % (uint)lista.Count);
        var url = lista[idx];

        Log.Information("[BALANCEADOR] {Servicio} -> instancia [{Indice}] {Url}",
            servicio, idx, url);

        return url;
    }
}
