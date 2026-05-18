# Sistema de Sensores en Invernadero

> Prueba de Concepto — Sistemas Distribuidos  
> Arquitectura de microservicios con SSO, autorización por roles, balanceo de carga, comunicación asíncrona, REST y tiempo real.

## Equipo 4

| ID | Nombre |
|----|--------|
| 94300 | Ana Lucía Aguilar Morales |
| 252538 | Dulce María Hernández Ibarra |
| 252626 | Miguel Ernesto Leyva Durán |

**Materia:** Sistemas Distribuidos  
**Profesor:** Dr. Ramón René Palacio Cinco

---

## Descripción del caso

Sistema de monitoreo distribuido para invernaderos donde se cultivan flores y hortalizas. Sensores de humedad y temperatura envían sus mediciones por radiofrecuencia (Zigbee) a un Gateway físico, el cual las reenvía vía TCP en formato binario al servidor.

El sistema permite:

- Registrar invernaderos, sensores y consultar mediciones históricas.
- Generar promedios por invernadero.
- Disparar alarmas cuando una medición sale del rango de umbrales configurados.
- Notificar las alarmas por correo electrónico (real, vía Mailtrap), SMS y push (mocks).
- Soportar múltiples marcas de sensores (MarcaA, MarcaB, MarcaC) con formatos binarios distintos.
- Autenticar usuarios mediante Single Sign-On con un Identity Provider externo (Keycloak).
- Autorizar operaciones según el rol del usuario (Administrador, Operador, Técnico).
- Empujar eventos en tiempo real a un cliente web vía WebSocket.

## Captura del Dashboard funcionando

![Dashboard](https://github.com/user-attachments/assets/c8e76609-af72-47b8-9ab2-5b2326a9da3d)

> Dashboard de Blazor Server recibiendo mediciones y alarmas en tiempo real vía SignalR sobre WebSocket. El navbar muestra dinámicamente las opciones disponibles según el rol del usuario autenticado.

---

## Arquitectura general

```
                                    ┌──────────────────────┐
                                    │      Keycloak        │
                                    │  Identity Provider   │
                                    │   (OIDC + RS256)     │
                                    └──────────┬───────────┘
                                               │ JWKS, OIDC
                                               │
Sensor (simulado) ──[TCP binario]──> MS1 Ingesta       │
                                        │            │
                                        │ Adapter    │
                                        │ Protobuf   │
                                        ▼            │
                                    RabbitMQ — mediciones    │
                                        │            │
                    ┌───────────────────┼────────────┼───────────────┐
                    ▼                   ▼            │               ▼
            MS2 Mediciones        MS3 Alarmas        │       APIGateway (suscriptor)
            (x2 instancias)       (x2 instancias)    │
                ↓ persiste            ↓ evalúa + cache│
            SQL Server                ↓ publica       │
                                      ▼               │
                                  RabbitMQ — alarmas  │
                                      │               │
                                      ▼               │
                                  MS4 Notificaciones  │
                                  │ Strategy Pattern  │
                                  ├─ Email Mailtrap   │
                                  ├─ SMS mock         │
                                  └─ Push mock        │
                                                      │
                              MS5 SensoresUsuarios    │
                              (x2 instancias)         │
                              catálogo y preferencias │
                                                      │
                                                      ▼
                                          APIGateway (REST + SignalR + Auth)
                                          │ Balanceador round-robin
                                          │ Validación JWT contra JWKS
                                          │ [Authorize(Roles=...)]
                                          │
                                          ▼
                                      BlazorClient (Dashboard)
                                      OIDC Authorization Code + PKCE
                                      5 páginas RBAC diferenciadas
```

---

## Stack tecnológico

| Categoría | Tecnología |
|-----------|------------|
| Lenguaje y runtime | .NET 9 (C# 13) |
| Web API | ASP.NET Core con controllers |
| Documentación de API | Swashbuckle.AspNetCore 7.2.0 (OpenAPI/Swagger) |
| Broker de mensajes | RabbitMQ 3 (AMQP) |
| Serialización binaria | Google.Protobuf con Grpc.Tools |
| Base de datos | SQL Server 2022 en Docker |
| ORM | Entity Framework Core 9 |
| Tiempo real | SignalR sobre WebSocket |
| Cliente web | Blazor Server con OIDC |
| SMTP | MailKit + Mailtrap (sandbox) |
| Identity Provider | Keycloak 24.0.0 (OIDC + OAuth 2.0) |
| Firma de tokens | RS256 asimétrica con JWKS |
| Logging | Serilog |
| Orquestación de infra | Docker Compose |

---

## Seguridad

El sistema implementa **Single Sign-On (SSO)** con propagación **Simple JWT (Valet Key)** sobre OpenID Connect:

- **Keycloak** actúa como Identity Provider centralizado del ecosistema.
- Tokens JWT firmados con **RS256** (clave asimétrica, JWKS publicado).
- BlazorClient autentica via **Authorization Code Flow + PKCE**.
- API Gateway y microservicios validan tokens localmente contra el JWKS de Keycloak (sin llamadas adicionales al IdP).
- Roles del realm (`Admin`, `Operador`, `Tecnico`) propagados a los claims estándar de ASP.NET Core para uso con `[Authorize(Roles = "Admin")]`.
- **Defensa en profundidad**: la UI esconde opciones que el rol no puede ejecutar, y el backend rechaza con 403 si se intenta saltar la UI.

---

## RBAC visual diferenciado

Cada rol ve una experiencia distinta en el cliente Blazor:

| Rol | Navbar muestra | Badge |
|-----|----------------|-------|
| Administrador | Dashboard + Invernaderos + Umbrales + Usuarios + Sensores | Rojo |
| Operador | Dashboard + Umbrales | Azul |
| Técnico | Dashboard + Sensores | Cyan |

Intentos de acceder a una ruta sin el rol adecuado muestran una página "Acceso denegado".

---

## Páginas del cliente web

| Página | Roles autorizados | Funcionalidad |
|--------|-------------------|---------------|
| `/dashboard` | Todos los autenticados | Tiempo real con SignalR (mediciones + alarmas) |
| `/invernaderos` | Admin | Listar y crear invernaderos |
| `/umbrales` | Admin + Operador | Ver y editar umbrales por invernadero |
| `/sensores` | Admin + Técnico | Catálogo de sensores con marca, tipo, estado |
| `/usuarios` | Admin | Ver usuarios y sus preferencias de notificación |

---

## Estructura del repositorio

```
Invernadero/
├── docker-compose.yml             # Keycloak + RabbitMQ + SQL Server
├── keycloak/
│   └── realm-export.json          # Realm "invernadero" pre-configurado
├── Invernadero.sln                # Solución .NET con todos los proyectos
├── run-all.ps1                    # Arranca todo el sistema (11 tabs)
├── stop-all.ps1                   # Detiene todo
├── start-ms2-5012.ps1             # Segunda instancia de MS2 (balanceador)
├── start-ms3-5013.ps1             # Segunda instancia de MS3 (balanceador)
├── start-ms5-5015.ps1             # Segunda instancia de MS5 (balanceador)
├── README.md
└── src/
    ├── Invernadero.Contracts/     # Esquemas Protobuf compartidos
    ├── Simulador.Gateway/         # Simula sensores + Gateway TCP
    ├── MS1.Ingesta/               # TCP listener + Adapter + publicador
    ├── MS2.Mediciones/            # API REST + suscriptor + persistencia
    ├── MS3.Alarmas/               # API REST + evaluador con caché + publicador
    ├── MS4.Notificaciones/        # Strategy: Email + SMS + Push
    ├── MS5.SensoresUsuarios/      # API REST de catálogo y preferencias
    ├── ApiGateway/                # Proxy + balanceador + Swagger + SignalR + Auth
    └── BlazorClient/              # Cliente web con OIDC y RBAC
```

---

## Requisitos previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) con WSL 2 habilitado
- [Git](https://git-scm.com/downloads)
- [Windows Terminal](https://aka.ms/terminal) (recomendado, viene preinstalado en Windows 11)
- PowerShell con política de ejecución habilitada:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

- (Opcional) [SQL Server Management Studio](https://aka.ms/ssmsfullsetup)
- (Opcional) [Cuenta gratuita en Mailtrap](https://mailtrap.io/register/signup) para envío de correos reales

---

## Levantamiento rápido

### 1. Clonar el repositorio

```powershell
git clone https://github.com/GalloNav/InvernaderoTeam4.git Invernadero
cd Invernadero
```

### 2. (Opcional) Configurar Mailtrap

Si quieres que las notificaciones por correo lleguen a un inbox real, edita `src/MS4.Notificaciones/appsettings.json`:

```json
"Mailtrap": {
  "User": "PEGAR_USER_DE_MAILTRAP_AQUI",
  "Password": "PEGAR_PASSWORD_DE_MAILTRAP_AQUI"
}
```

Sin Mailtrap, el sistema sigue funcionando pero el envío de email registra el error en logs y continúa con las demás estrategias.

### 3. Arrancar el sistema completo

```powershell
.\run-all.ps1
```

El script realiza automáticamente:

1. Detiene procesos `dotnet` zombi de ejecuciones previas.
2. Levanta Keycloak, RabbitMQ y SQL Server con `docker compose up -d`.
3. Espera 20 segundos a que la infraestructura esté lista.
4. Verifica que los scripts auxiliares estén presentes.
5. Abre 11 tabs en Windows Terminal con cada microservicio (incluyendo las instancias balanceadas).

Tras ~40-60 segundos adicionales (los servicios .NET compilan y arrancan), el sistema está completamente operativo.

### 4. Acceder al sistema

| Servicio | URL | Credenciales |
|----------|-----|--------------|
| BlazorClient (Dashboard) | http://localhost:5006 | Ver tabla "Usuarios de demo" |
| API Gateway (Swagger) | http://localhost:5000/swagger | — |
| Keycloak Admin Console | http://localhost:8080 | `admin` / `admin` |
| RabbitMQ Management | http://localhost:15672 | `guest` / `guest` |

### 5. Detener el sistema

```powershell
.\stop-all.ps1
```

Detiene todos los procesos `dotnet` y los contenedores Docker.

---

## Usuarios de demo

Los usuarios están configurados en Keycloak (no en el código). El realm completo se importa automáticamente desde `keycloak/realm-export.json` al levantar el contenedor.

| Usuario | Contraseña | Rol | Acceso |
|---------|------------|-----|--------|
| admin | admin123 | Administrador | Todas las páginas y operaciones |
| operador | operador123 | Operador | Dashboard + configuración de umbrales |
| tecnico | tecnico123 | Técnico | Dashboard + información de sensores |

---

## Puertos del sistema

| Componente | Puerto(s) |
|------------|-----------|
| BlazorClient | 5006 |
| ApiGateway | 5000 |
| MS2 Mediciones | 5002, 5012 (balanceadas) |
| MS3 Alarmas | 5003, 5013 (balanceadas) |
| MS5 SensoresUsuarios | 5005, 5015 (balanceadas) |
| MS1 Ingesta (TCP) | 6000 |
| Keycloak | 8080 |
| RabbitMQ AMQP | 5672 |
| RabbitMQ Management | 15672 |
| SQL Server | 1435 |

> El puerto de SQL Server se cambió de 1433 a 1435 para evitar conflicto con instancias locales que puedan tener los desarrolladores.

---

## Testing manual de seguridad

Obtener un token directamente de Keycloak:

```powershell
$tokenResponse = curl.exe -X POST http://localhost:8080/realms/invernadero/protocol/openid-connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=password&client_id=blazor-client&username=admin&password=admin123"

$token = ($tokenResponse | ConvertFrom-Json).access_token
```

Llamar al Gateway con el token:

```powershell
curl.exe -H "Authorization: Bearer $token" http://localhost:5000/api/mediciones
curl.exe -H "Authorization: Bearer $token" http://localhost:5000/api/sensores
curl.exe -H "Authorization: Bearer $token" http://localhost:5000/api/umbrales/invernadero/INV-001
```

Validar que un rol sin permisos recibe 403:

```powershell
# Token de operador (no debería poder crear invernaderos)
$tokenOp = (curl.exe -X POST http://localhost:8080/realms/invernadero/protocol/openid-connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=password&client_id=blazor-client&username=operador&password=operador123" | ConvertFrom-Json).access_token

# Esto debe responder 403 Forbidden
curl.exe -i -X POST -H "Authorization: Bearer $tokenOp" `
  -H "Content-Type: application/json" `
  -d '{"id":"INV-999","nombre":"Test","ubicacion":"Test"}' `
  http://localhost:5000/api/invernaderos
```

---

## Decisiones arquitectónicas principales

| # | Decisión | Resolución |
|---|----------|-----------|
| 1 | Identity Provider | Keycloak 24 self-hosted en Docker con realm pre-configurado |
| 2 | Protocolo de autenticación | OpenID Connect con Authorization Code Flow + PKCE |
| 3 | Firma de tokens | RS256 asimétrica con descubrimiento vía JWKS |
| 4 | Propagación de identidad | Simple JWT (Valet Key) — el token viaja del cliente a cada microservicio |
| 5 | Mapeo de roles | `realm_access.roles` del access_token → `ClaimTypes.Role` de ASP.NET Core |
| 6 | Balanceo de carga | Round-robin en el ApiGateway entre instancias de MS2, MS3, MS5 |
| 7 | Comunicación entre MS | Protobuf vía RabbitMQ (asíncrono) + REST con JSON (síncrono administrativo) |
| 8 | Persistencia | EF Core 9 con database-per-service (4 BDs lógicas en una instancia SQL Server) |
| 9 | Tiempo real | SignalR sobre WebSocket, autenticado vía query string |
| 10 | Cliente web | Blazor Server con render interactivo y RBAC visual diferenciado |
| 11 | Patrones aplicados | Adapter (MS1), Strategy (MS4), API Gateway, Cache en memoria (MS3) |

---

## Limitaciones conocidas y trabajo futuro

- **Balanceador round-robin sin health checks**: si una instancia de MS2, MS3 o MS5 está caída, ~50% de las peticiones fallarán hasta que sea reiniciada. Mitigación en producción: YARP, NGINX o Envoy con health probes.
- **TokenCache sin rotación automática**: si Keycloak rota sus claves de firma durante una sesión activa, el sistema requeriría re-login manual.
- **Comunicación HTTP sin cifrado**: todos los canales usan HTTP plano. En producción se migraría a HTTPS con TLS 1.3, AMQPS para RabbitMQ y WSS para SignalR.
- **Secrets en appsettings.json**: cadenas de conexión y configuración del cliente Keycloak viven en archivos del repositorio. En producción se moverían a un gestor de secretos.
- **Sin refresh tokens automáticos**: el usuario debe re-loguearse cada hora cuando expira el access_token.
- **Sin tests automatizados**: la PoC priorizó la demostración de conceptos sobre cobertura de tests.
- **Microservicios fuera de Docker**: los servicios .NET corren como procesos nativos para acelerar el desarrollo iterativo. En producción se contenerizarían y orquestarían con Kubernetes.
- **Cuota gratis de Mailtrap (50 emails/mes)**: una vez agotada, el envío de correos fallará pero el sistema continúa procesando otras estrategias.

---

## Documentación completa

El reporte completo de implementación con justificación de decisiones, explicación detallada de la arquitectura, comparación de patrones de seguridad (SAML vs OIDC, Simple JWT vs Token Exchange) y mapeo con los temas del curso está en:

`Pendiente el word` 

---

## Repositorio

https://github.com/GalloNav/InvernaderoTeam4
