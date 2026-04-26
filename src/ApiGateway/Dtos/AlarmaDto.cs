namespace ApiGateway.Dtos;

public record AlarmaDto(
    string   SensorId,
    string   InvernaderoId,
    string   Tipo,
    float    ValorMedido,
    float    UmbralConfigurado,
    DateTime Timestamp
);
