using System.Text;

namespace ApiGateway.Servicios.HttpClientes;

public sealed class SensoresUsuariosClient(
    HttpClient           http,
    IHttpContextAccessor ctx,
    ILogger<SensoresUsuariosClient> logger) : ISensoresUsuariosClient
{
    public async Task<string> LoginAsync(string jsonBody)
    {
        logger.LogInformation("🔀 GW → MS5: POST /auth/login");
        // MS5 usa /auth/login (sin /api/)
        using var req = Build(HttpMethod.Post, "/auth/login", jsonBody);
        return await ReadAsync(req);
    }

    public async Task<string> GetSensoresAsync()
    {
        logger.LogInformation("🔀 GW → MS5: GET /api/sensores");
        using var req = Build(HttpMethod.Get, "/api/sensores");
        return await ReadAsync(req);
    }

    public async Task<string> CreateSensorAsync(string jsonBody)
    {
        logger.LogInformation("🔀 GW → MS5: POST /api/sensores");
        using var req = Build(HttpMethod.Post, "/api/sensores", jsonBody);
        return await ReadAsync(req);
    }

    public async Task DeleteSensorAsync(string id)
    {
        logger.LogInformation("🔀 GW → MS5: DELETE /api/sensores/{Id}", id);
        using var req = Build(HttpMethod.Delete, $"/api/sensores/{id}");
        await http.SendAsync(req);
    }

    public async Task<string> GetUsuariosAsync()
    {
        logger.LogInformation("🔀 GW → MS5: GET /api/usuarios");
        using var req = Build(HttpMethod.Get, "/api/usuarios");
        return await ReadAsync(req);
    }

    public async Task<string> GetPreferenciasUsuarioAsync(long id)
    {
        logger.LogInformation("🔀 GW → MS5: GET /api/usuarios/{Id}/preferencias", id);
        using var req = Build(HttpMethod.Get, $"/api/usuarios/{id}/preferencias");
        return await ReadAsync(req);
    }

    public async Task<string> GetInvernaderosAsync()
    {
        logger.LogInformation("🔀 GW → MS5: GET /api/invernaderos");
        using var req = Build(HttpMethod.Get, "/api/invernaderos");
        return await ReadAsync(req);
    }

    public async Task<string> GetInvernaderoAsync(string id)
    {
        logger.LogInformation("🔀 GW → MS5: GET /api/invernaderos/{Id}", id);
        using var req = Build(HttpMethod.Get, $"/api/invernaderos/{id}");
        return await ReadAsync(req);
    }

    public async Task<string> CreateInvernaderoAsync(string jsonBody)
    {
        logger.LogInformation("🔀 GW → MS5: POST /api/invernaderos");
        using var req = Build(HttpMethod.Post, "/api/invernaderos", jsonBody);
        return await ReadAsync(req);
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
