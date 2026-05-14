namespace BlazorClient.Models;

public record PreferenciasDto(
    string Email,
    string Telefono,
    string PushToken,
    List<string> CanalesActivos
);
