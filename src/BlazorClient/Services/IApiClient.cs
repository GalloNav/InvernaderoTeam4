using BlazorClient.Models;

namespace BlazorClient.Services;

public interface IApiClient
{
    Task<LoginResponse?> LoginAsync(string username, string password);
    Task<List<MedicionDto>> GetMedicionesAsync(string token);
    Task<List<AlarmaDto>> GetAlarmasAsync(string token);
}
