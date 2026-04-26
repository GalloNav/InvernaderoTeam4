namespace ApiGateway.Servicios.HttpClientes;

public interface IMedicionesClient
{
    Task<string> GetAllAsync();
    Task<string> GetBySensorAsync(string sensorId);
    Task<string> GetPromedioAsync(string invernaderoId);
}
