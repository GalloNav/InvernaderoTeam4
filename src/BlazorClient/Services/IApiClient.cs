using BlazorClient.Models;

namespace BlazorClient.Services;

public interface IApiClient
{
    Task<List<MedicionDto>> GetMedicionesAsync();
    Task<List<SensorDto>> GetSensoresAsync();
    Task<UmbralDto?> GetUmbralInvernaderoAsync(string invernaderoId);
    Task<bool> PutUmbralInvernaderoAsync(string invernaderoId, UmbralDto umbral);
    Task<List<InvernaderoDto>> GetInvernaderosAsync();
    Task<bool> PostInvernaderoAsync(InvernaderoDto invernadero);
    Task<List<UsuarioDto>> GetUsuariosAsync();
    Task<PreferenciasDto?> GetPreferenciasAsync(int userId);
}
