using System.Text;

namespace ApiGateway.Servicios.HttpClientes;

public sealed class AlarmasClient(
    HttpClient           http,
    IHttpContextAccessor ctx,
    ILogger<AlarmasClient> logger) : IAlarmasClient
{
    public async Task<string> GetUmbralInvernaderoAsync(string id)
    {
        logger.LogInformation("🔀 GW → MS3: GET /api/umbrales/invernadero/{Id}", id);
        using var req = Build(HttpMethod.Get, $"/api/umbrales/invernadero/{id}");
        return await ReadAsync(req);
    }

    public async Task UpdateUmbralInvernaderoAsync(string id, string jsonBody)
    {
        logger.LogInformation("🔀 GW → MS3: PUT /api/umbrales/invernadero/{Id}", id);
        using var req = Build(HttpMethod.Put, $"/api/umbrales/invernadero/{id}", jsonBody);
        await http.SendAsync(req);
    }

    public async Task<string> GetUmbralSensorAsync(string id)
    {
        logger.LogInformation("🔀 GW → MS3: GET /api/umbrales/sensor/{Id}", id);
        using var req = Build(HttpMethod.Get, $"/api/umbrales/sensor/{id}");
        return await ReadAsync(req);
    }

    public async Task UpdateUmbralSensorAsync(string id, string jsonBody)
    {
        logger.LogInformation("🔀 GW → MS3: PUT /api/umbrales/sensor/{Id}", id);
        using var req = Build(HttpMethod.Put, $"/api/umbrales/sensor/{id}", jsonBody);
        await http.SendAsync(req);
    }

    public async Task DeleteUmbralSensorAsync(string id)
    {
        logger.LogInformation("🔀 GW → MS3: DELETE /api/umbrales/sensor/{Id}", id);
        using var req = Build(HttpMethod.Delete, $"/api/umbrales/sensor/{id}");
        await http.SendAsync(req);
    }

    private async Task<string> ReadAsync(HttpRequestMessage req)
    {
        var res = await http.SendAsync(req);
        return await res.Content.ReadAsStringAsync();
    }

    private HttpRequestMessage Build(HttpMethod method, string path, string? json = null)
    {
        var req = new HttpRequestMessage(method, path);
        PropagateAuth(req);
        if (json is not null)
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return req;
    }

    private void PropagateAuth(HttpRequestMessage req)
    {
        var auth = ctx.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (auth is not null) req.Headers.TryAddWithoutValidation("Authorization", auth);
    }
}
