namespace BlazorClient.Models;

public record MedicionDto(
    string SensorId,
    string InvernaderoId,
    float Temperatura,
    float Humedad,
    string Marca,
    DateTime Timestamp);
