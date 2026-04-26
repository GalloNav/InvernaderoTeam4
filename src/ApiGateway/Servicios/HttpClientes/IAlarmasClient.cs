namespace ApiGateway.Servicios.HttpClientes;

public interface IAlarmasClient
{
    Task<string> GetUmbralInvernaderoAsync(string id);
    Task         UpdateUmbralInvernaderoAsync(string id, string jsonBody);
    Task<string> GetUmbralSensorAsync(string id);
    Task         UpdateUmbralSensorAsync(string id, string jsonBody);
    Task         DeleteUmbralSensorAsync(string id);
}
