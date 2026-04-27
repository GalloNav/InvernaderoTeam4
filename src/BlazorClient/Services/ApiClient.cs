using BlazorClient.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorClient.Services;

public sealed class ApiClient(HttpClient http) : IApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var resp = await http.PostAsJsonAsync("/api/auth/login", new { usuario = username, password });
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    public async Task<List<MedicionDto>> GetMedicionesAsync(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/mediciones");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<MedicionDto>>(JsonOpts) ?? [];
    }

    public async Task<List<AlarmaDto>> GetAlarmasAsync(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/alarmas");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<AlarmaDto>>(JsonOpts) ?? [];
    }
}
