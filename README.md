# Invernadero — Sistema de Monitoreo (PoC Académica)

Sistema distribuido de monitoreo de invernaderos basado en microservicios.
Desarrollado como prueba de concepto para demostrar comunicación entre servicios
usando .NET 9, RabbitMQ y SQL Server.

## Arquitectura

| Servicio | Rol | Tecnología |
|---|---|---|
| MS1.Ingesta | Listener TCP, publica a RabbitMQ | Worker Service |
| MS2.Mediciones | Suscriptor, persiste lecturas | Web API + EF Core |
| MS3.Alarmas | Evalúa umbrales, dispara alertas | Web API + RabbitMQ |
| MS4.Notificaciones | Estrategias mock de notificación | Worker Service |
| MS5.SensoresUsuarios | CRUD sensores/usuarios, JWT | Web API + EF Core |
| ApiGateway | Punto de entrada, Swagger, SignalR | ASP.NET Core |
| BlazorClient | UI en tiempo real | Blazor Server |
| Simulador.Gateway | Genera lecturas de sensores | Consola |

## Prerrequisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Levantar la infraestructura

```bash
docker compose up -d
```

Espera ~30 segundos hasta que SQL Server pase el healthcheck.

### Verificar que todo esté sano

```bash
docker compose ps
```

Todos los servicios deben mostrar `healthy` o `running`.

## URLs útiles

| Servicio | URL | Credenciales |
|---|---|---|
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| SQL Server | localhost:1435 | sa / InvernaderoPwd2024 |

> Conectar SQL Server desde SSMS o Azure Data Studio con autenticación SQL Server.

## Tumbar la infraestructura

```bash
# Solo detener contenedores (conserva volúmenes)
docker compose down

# Detener y eliminar volúmenes (borra todos los datos)
docker compose down -v
```

## Bases de datos lógicas (pendiente de crear)

Se usará una sola instancia de SQL Server con 3 bases de datos lógicas:

- `DB_Mediciones` — lecturas de sensores
- `DB_Alarmas` — historial de alertas
- `DB_SensoresUsuarios` — catálogo de sensores y usuarios

## Equipo

PoC académica — 3 integrantes.
