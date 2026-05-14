namespace BlazorClient.Models;

public record UsuarioDto(
    int Id,
    string Username,
    string Email,
    string Telefono,
    string PushToken,
    string Rol,
    DateTime FechaRegistro
);
