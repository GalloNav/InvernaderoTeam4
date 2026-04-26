namespace ApiGateway.Dtos;

public record MedicionDto(
    string   SensorId,
    string   InvernaderoId,
    float    Temperatura,
    float    Humedad,
    string   Marca,
    DateTime Timestamp
);
