namespace BlazorClient.Models;

public record SensorDto(
    string Id,
    string InvernaderoId,
    string Marca,
    string TipoSensor,
    bool Activo,
    DateTime FechaRegistro
);
