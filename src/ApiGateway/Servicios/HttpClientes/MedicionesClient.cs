using ApiGateway.Servicios.Balanceador;

namespace ApiGateway.Servicios.HttpClientes;

public sealed class MedicionesClient(
    HttpClient                http,
    IHttpContextAccessor      ctx,
    IRoundRobinBalanceador    balanceador,
    ILogger<MedicionesClient> logger) : IMedicionesClient
{
    public async Task<string> GetAllAsync()
    {
        logger.LogInformation("[PROXY] GW -> MS2 [balanceado]: GET /api/mediciones");
        using var req = Get("/api/mediciones");
        return await Send(req);
    }

    public async Task<string> GetBySensorAsync(string sensorId)
    {
        logger.LogInformation("[PROXY] GW -> MS2 [balanceado]: GET /api/mediciones/sensor/{Id}", sensorId);
        using var req = Get($"/api/mediciones/sensor/{sensorId}");
        return await Send(req);
    }

    public async Task<string> GetPromedioAsync(string invernaderoId)
    {
        logger.LogInformation("[PROXY] GW -> MS2 [balanceado]: GET /api/mediciones/promedio/{Id}", invernaderoId);
        using var req = Get($"/api/mediciones/promedio/{invernaderoId}");
        return await Send(req);
    }

    private async Task<string> Send(HttpRequestMessage req)
    {
        var res = await http.SendAsync(req);
        return await res.Content.ReadAsStringAsync();
    }

    private HttpRequestMessage Get(string path)
    {
        var baseUri = balanceador.ObtenerSiguiente("Mediciones");
        var req = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, path));
        PropagateAuth(req);
        return req;
    }

    private void PropagateAuth(HttpRequestMessage req)
    {
        var auth = ctx.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (auth is not null) req.Headers.TryAddWithoutValidation("Authorization", auth);
    }
}
