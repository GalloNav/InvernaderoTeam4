using BlazorClient.Models;

namespace BlazorClient.Services;

public interface IApiClient
{
    Task<List<MedicionDto>> GetMedicionesAsync();
}
