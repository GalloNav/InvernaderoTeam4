# Sistema de Sensores en Invernadero

> Prueba de Concepto — Unidad 2 — Sistemas Distribuidos
> Arquitectura de microservicios con comunicación asíncrona, REST y tiempo real.

## Equipo 4

| ID | Nombre |
|-----------|--------|
| 94300 | Ana Lucia Aguilar Morales |
| 252538 | Dulce Maria Hernandez Ibarra |
| 252626 | Miguel Ernesto Leyva Duran |

**Materia:** Sistemas Distribuidos
**Profesor:** Ramón René Palacio Cinco

---

## Descripción del caso

Sistema de monitoreo para invernaderos donde se cultivan flores y hortalizas. Sensores de humedad y temperatura envían sus mediciones por radiofrecuencia (Zigbee) a un Gateway físico, el cual las reenvía vía TCP en formato binario al servidor.

El sistema permite:

- Registrar sensores y consultar sus mediciones históricas.
- Generar reportes y promedios por invernadero.
- Disparar alarmas cuando una medición supera el umbral configurado.
- Notificar las alarmas por correo electrónico (real, vía Mailtrap), SMS y push (mocks).
- Soportar múltiples marcas de sensores con formatos binarios distintos.
- Exponer los datos vía REST y empujarlos en tiempo real a un cliente web.

## Captura del Dashboard funcionando

<img width="1568" height="736" alt="preview" src="https://github.com/user-attachments/assets/c8e76609-af72-47b8-9ab2-5b2326a9da3d" />


> Dashboard de Blazor Server recibiendo mediciones y alarmas en tiempo real vía SignalR sobre WebSocket.

## Arquitectura general

```
Sensor (simulado) ─[TCP binario por marca]─> MS1 Ingesta
                                                 │
                                                 │ Adapter Pattern
                                                 │ Serialización Protobuf
                                                 ▼
                                       RabbitMQ — invernadero.mediciones
                                                 │
                       ┌─────────────────────────┼─────────────────────────┐
                       ▼                         ▼                         ▼
                 MS2 Mediciones            MS3 Alarmas              APIGateway (suscriptor)
                 ↓ persiste                ↓ evalúa umbrales        │
                 SQL Server                ↓ si dispara             │
                                           ▼                         │
                                 RabbitMQ — invernadero.alarmas      │
                                           │                         │
                                           ▼                         │
                                    MS4 Notificaciones               │
                                    │ Strategy Pattern               │
                                    ├─ Email → Mailtrap (real)       │
                                    ├─ SMS mock                      │
                                    └─ Push mock                     │
                                                                     │
                                                                     ▼
                                                         APIGateway (SignalR Hub)
                                                                     │
                                                                     ▼
                                                            BlazorClient (Dashboard)

                       MS5 SensoresUsuarios — emite JWT, expone catálogo
                                  ↑
                                  │ login
                                  └── BlazorClient (vía APIGateway)
```

## Stack tecnológico

| Categoría | Tecnología |
|-----------|------------|
| Lenguaje y runtime | .NET 9 (C# 13) |
| Web API | ASP.NET Core con controllers |
| Documentación de API | Swashbuckle.AspNetCore 7.2.0 (OpenAPI/Swagger) |
| Broker de mensajes | RabbitMQ 3 (AMQP) |
| Serialización binaria | Google.Protobuf con Grpc.Tools |
| Base de datos | SQL Server 2022 Express en Docker |
| ORM | Entity Framework Core 9 |
| Tiempo real | SignalR sobre WebSocket |
| Cliente web | Blazor Server |
| SMTP | MailKit + Mailtrap (sandbox) |
| Autenticación | JWT HS256 (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Logging | Serilog |
| Orquestación de infra | Docker Compose |

## Estructura del repositorio

```
Invernadero/
├── docker-compose.yml             # RabbitMQ + SQL Server
├── Invernadero.sln                # Solución .NET con todos los proyectos
├── README.md                      # Este archivo
└── src/
    ├── Invernadero.Contracts/     # Esquemas Protobuf compartidos (.proto)
    ├── Simulador.Gateway/         # Simula sensores + Gateway TCP
    ├── MS1.Ingesta/               # TCP listener + Adapter + publicador
    ├── MS2.Mediciones/            # Suscriptor + persistencia + REST
    ├── MS3.Alarmas/               # Evaluador con caché + publicador alarmas
    ├── MS4.Notificaciones/        # Strategy: Email + SMS + Push
    ├── MS5.SensoresUsuarios/      # Catálogo + JWT mock
    ├── ApiGateway/                # Proxy + Swagger consolidado + SignalR Hub
    └── BlazorClient/              # Dashboard en tiempo real
```

## Requisitos previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/downloads)
- (Opcional) [SQL Server Management Studio](https://aka.ms/ssmsfullsetup) o Azure Data Studio
- (Opcional) [Cuenta gratuita en Mailtrap](https://mailtrap.io/register/signup) — para envío de correos reales

## Cómo levantar la PoC

### 1. Clonar el repositorio

```powershell
git clone https://github.com/GalloNav/InvernaderoTeam4.git
cd InvernaderoTeam4
```

### 2. Configurar Mailtrap (solo si quieres emails reales)

Cada miembro del equipo debe crear su propia cuenta gratuita en Mailtrap. Después:

```powershell
copy src\MS4.Notificaciones\appsettings.example.json src\MS4.Notificaciones\appsettings.json
```

Edita `src/MS4.Notificaciones/appsettings.json` y reemplaza:

```json
"Mailtrap": {
  "User": "PEGAR_USER_DE_MAILTRAP_AQUI",
  "Password": "PEGAR_PASSWORD_DE_MAILTRAP_AQUI"
}
```

> Si no configuras Mailtrap, el sistema sigue funcionando pero el envío de email lanzará error y solo se ejecutarán las estrategias mock de SMS y Push.

### 3. Levantar la infraestructura

```powershell
docker compose up -d
docker compose ps
```

Ambos contenedores deben aparecer como `(healthy)`.

| Servicio | Puerto | URL |
|----------|--------|-----|
| RabbitMQ Management | 15672 | http://localhost:15672 (guest/guest) |
| RabbitMQ AMQP | 5672 | — |
| SQL Server | 1435 | localhost,1435 (sa / InvernaderoPwd2024) |

> Nota: el puerto de SQL Server se cambió a `1435` para evitar conflicto con instancias locales de SQL Server que tenga el desarrollador en `1433`.

### 4. Levantar los microservicios

Recomendamos usar **Windows Terminal** con tabs (8 tabs renombradas con el nombre del MS). Una pestaña por proyecto, ejecutados en este orden:

```powershell
# Tab 1 — MS5 (primero, emite los JWT)
cd src\MS5.SensoresUsuarios
dotnet run

# Tab 2 — MS2
cd src\MS2.Mediciones
dotnet run

# Tab 3 — MS3
cd src\MS3.Alarmas
dotnet run

# Tab 4 — MS4
cd src\MS4.Notificaciones
dotnet run

# Tab 5 — APIGateway
cd src\ApiGateway
dotnet run

# Tab 6 — BlazorClient
cd src\BlazorClient
dotnet run

# Tab 7 — MS1
cd src\MS1.Ingesta
dotnet run

# Tab 8 — Simulador (último)
cd src\Simulador.Gateway
dotnet run
```

| Microservicio | Puerto |
|---------------|--------|
| APIGateway | 5000 |
| MS2 Mediciones | 5002 |
| MS3 Alarmas | 5003 |
| MS4 Notificaciones | 5004 |
| MS5 SensoresUsuarios | 5005 |
| BlazorClient | 5006 |
| MS1 Ingesta (TCP) | 6000 |

### 5. Verificar que todo funciona

| Verificación | URL |
|--------------|-----|
| Dashboard del cliente | http://localhost:5006 (login: `admin` / `admin123`) |
| Swagger consolidado del Gateway | http://localhost:5000/swagger |
| Swagger de cada MS | http://localhost:500X/swagger (X = 2, 3, 4, 5) |
| RabbitMQ Management | http://localhost:15672 |
| Mailtrap inbox | https://mailtrap.io (tu sandbox) |

### 6. Usuarios de prueba

| Usuario | Password | Rol |
|---------|----------|-----|
| admin | admin123 | Admin |
| operador | op123 | Operador |
| bi_user | bi123 | BI |

## Decisiones arquitectónicas principales

| # | Decisión | Resolución |
|---|----------|-----------|
| 1 | Formato Gateway → MS1 | Cada marca con su formato binario propio (patrón Adapter) |
| 2 | Validación de sensor (MS1 → MS5) | REST con caché en memoria — diseñado, mock en PoC |
| 3 | Preferencias de usuario (MS4 → MS5) | REST con caché en memoria — diseñado, mock en PoC |
| 4 | Granularidad de umbrales | Por invernadero con override por sensor, caché refrescable por REST |
| 5 | Cliente web | Blazor Server + SignalR sobre WebSocket |
| 6 | Despliegue de BDs | 1 instancia SQL Server, 4 BDs lógicas separadas |
| 7 | Serialización entre MS | Protobuf con proyecto compartido `Invernadero.Contracts` |
| 8 | Notificaciones | Email real con Mailtrap, SMS y Push como mocks |
| 9 | Autenticación | JWT HS256 con usuarios hardcoded en `appsettings.json` |

## Limitaciones conocidas y puntos de extensión

- La cuota gratis de Mailtrap es 50 emails/mes. Una vez agotada, el envío fallará pero el sistema continúa procesando otras estrategias.
- La validación de sensor (MS1 → MS5) y la consulta de preferencias (MS4 → MS5) están diseñadas pero no activas en PoC.
- Las estrategias de SMS y Push son mocks que registran en log; la integración real con Twilio/FCM se implementaría reemplazando solo la implementación de la estrategia correspondiente.
- Los usuarios están definidos en `appsettings.json` con contraseñas en texto plano; en producción se reemplazaría por una tabla con hashing BCrypt.
- En PoC se usa `EnsureCreated()` de EF Core; en producción se reemplazaría por migrations.

## Documentación completa

El reporte completo de implementación con justificación de decisiones, explicación de diagramas y mapeo con los temas de la Unidad 2 está en:

`pendiente el word`

## Repositorio

https://github.com/GalloNav/InvernaderoTeam4
