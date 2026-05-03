using ApiGateway.Hubs;
using ApiGateway.Mensajeria;
using ApiGateway.Servicios.Balanceador;
using ApiGateway.Servicios.HttpClientes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

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

    // ── JWT Bearer ────────────────────────────────────────────────────────────
    var jwtKey      = cfg["Jwt:Key"]!;
    var jwtIssuer   = cfg["Jwt:Issuer"]!;
    var jwtAudience = cfg["Jwt:Audience"]!;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer           = true,
                ValidIssuer              = jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = jwtAudience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };

            // Soporte JWT desde query string para conexiones SignalR
            opts.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path        = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
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
