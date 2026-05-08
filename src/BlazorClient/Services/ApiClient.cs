using BlazorClient.Models;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorClient.Services;

public sealed class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await (_ctx.HttpContext?.GetTokenAsync("access_token")
            ?? Task.FromResult<string?>(null));
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<MedicionDto>> GetMedicionesAsync()
    {
        await SetAuthHeaderAsync();
        var resp = await _http.GetAsync("/api/mediciones");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<MedicionDto>>(JsonOpts) ?? [];
    }

    public async Task<List<AlarmaDto>> GetAlarmasAsync()
    {
        await SetAuthHeaderAsync();
        var resp = await _http.GetAsync("/api/alarmas");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<AlarmaDto>>(JsonOpts) ?? [];
    }
}
