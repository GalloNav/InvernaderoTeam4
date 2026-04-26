using Microsoft.AspNetCore.SignalR;

namespace ApiGateway.Hubs;

// Solo push servidor → cliente en esta PoC.
// Métodos que recibe el cliente: NuevaMedicion(MedicionDto), NuevaAlarma(AlarmaDto)
// En prod: [Authorize] aquí + validar JWT desde query string (ya wired en Program.cs)
public sealed class EventosHub : Hub { }
