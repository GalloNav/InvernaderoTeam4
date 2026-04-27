namespace BlazorClient.Models;

public record AlarmaDto(
    Guid AlarmaId,
    string SensorId,
    string InvernaderoId,
    string Tipo,
    float Valor,
    float Umbral,
    DateTime Timestamp);
