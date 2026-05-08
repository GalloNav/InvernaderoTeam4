using ApiGateway.Hubs;
using ApiGateway.Mensajeria;
using ApiGateway.Servicios.Balanceador;
using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, cfg) => cfg
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore",          Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    var cfg = builder.Configuration;

    // ── JWT Bearer (Keycloak RS256/JWKS) ──────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var keycloakConfig = builder.Configuration.GetSection("Keycloak");
            options.Authority            = keycloakConfig["Authority"];
            options.Audience             = keycloakConfig["Audience"];
            options.RequireHttpsMetadata = bool.Parse(keycloakConfig["RequireHttpsMetadata"] ?? "false");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = keycloakConfig["Authority"],
                ValidateAudience         = false,  // Keycloak no incluye 'aud' por defecto
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                NameClaimType            = "preferred_username",
                RoleClaimType            = ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                // Soporte JWT desde query string para conexiones SignalR
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path        = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                },

                // Aplanar realm_access.roles → ClaimTypes.Role
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        var realmAccessClaim = identity.FindFirst("realm_access");
                        if (realmAccessClaim is not null)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                                if (doc.RootElement.TryGetProperty("roles", out var rolesElement)
                                    && rolesElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var role in rolesElement.EnumerateArray())
                                    {
                                        var roleName = role.GetString();
                                        if (!string.IsNullOrEmpty(roleName))
                                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                    }
                                }
                            }
                            catch (JsonException) { }
                        }
                    }
                    return Task.CompletedTask;
                },

                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("Auth fallida: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ── CORS (AllowCredentials requerido por SignalR) ─────────────────────────
    var allowedOrigins = cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(o =>
        o.AddPolicy("BlazorClient", p =>
            p.WithOrigins(allowedOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials()));

    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();

    // ── Balanceador round-robin (singleton: contador compartido entre llamadas) ─
    builder.Services.AddSingleton<IRoundRobinBalanceador, RoundRobinBalanceador>();

    // ── HttpClients tipados ───────────────────────────────────────────────────
    // Sin BaseAddress fijo: cada llamada resuelve la URL a través del balanceador.
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddHttpClient<IMedicionesClient, MedicionesClient>();
    builder.Services.AddHttpClient<IAlarmasClient, AlarmasClient>();
    builder.Services.AddHttpClient<ISensoresUsuariosClient, SensoresUsuariosClient>();

    // ── Suscriptor de eventos (Protobuf → SignalR) ────────────────────────────
    builder.Services.AddHostedService<EventosSubscriber>();

    // ── Controllers + Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Invernadero — API Gateway", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "1) POST /api/auth/login  2) Copia el token  3) Pégalo aquí"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── Pipeline ──────────────────────────────────────────────────────────────
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1"));
    }

    app.UseCors("BlazorClient");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<EventosHub>("/hubs/eventos");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "APIGateway terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
