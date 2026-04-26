namespace MS3.Alarmas.Servicios;

public record UmbralEfectivo(
    string InvernaderoId,
    float  TempMin,
    float  TempMax,
    float  HumMin,
    float  HumMax
);

public interface ICacheUmbrales
{
    UmbralEfectivo? GetUmbralEfectivo(string sensorId, string invernaderoId);
    Task RefreshAsync();
}
