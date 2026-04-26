namespace ApiGateway.Servicios.HttpClientes;

public interface ISensoresUsuariosClient
{
    Task<string> LoginAsync(string jsonBody);
    Task<string> GetSensoresAsync();
    Task<string> CreateSensorAsync(string jsonBody);
    Task         DeleteSensorAsync(string id);
    Task<string> GetUsuariosAsync();
    Task<string> GetPreferenciasUsuarioAsync(long id);
    Task<string> GetInvernaderosAsync();
    Task<string> GetInvernaderoAsync(string id);
    Task<string> CreateInvernaderoAsync(string jsonBody);
}
