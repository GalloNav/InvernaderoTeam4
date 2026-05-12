using BlazorClient.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace BlazorClient.Services;

public sealed class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly TokenCache _tokenCache;
    private readonly AuthenticationStateProvider _authProvider;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient http, TokenCache tokenCache, AuthenticationStateProvider authProvider)
    {
        _http = http;
        _tokenCache = tokenCache;
        _authProvider = authProvider;
    }

    private async Task SetAuthHeaderAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        var sub = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? authState.User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(sub))
        {
            var token = _tokenCache.Get(sub);
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    public async Task<List<MedicionDto>> GetMedicionesAsync()
    {
        await SetAuthHeaderAsync();
        var resp = await _http.GetAsync("/api/mediciones");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<MedicionDto>>(JsonOpts) ?? [];
    }


}
