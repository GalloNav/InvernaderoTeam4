using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MS5.SensoresUsuarios.Auth;
using MS5.SensoresUsuarios.Datos;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("MS5 iniciando...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, cfg) => cfg
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore",           Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore",  Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // EF Core
    builder.Services.AddDbContext<SensoresUsuariosDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("SensoresUsuarios")));

    // Auth config
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<UsuariosMockOptions>(builder.Configuration.GetSection("UsuariosMock"));
    builder.Services.AddSingleton<JwtTokenService>();

    // JWT Bearer
    var signingKey = builder.Configuration["Jwt:SigningKey"]!;
    var issuer     = builder.Configuration["Jwt:Issuer"]!;
    var audience   = builder.Configuration["Jwt:Audience"]!;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateIssuer           = true,
                ValidIssuer              = issuer,
                ValidateAudience         = true,
                ValidAudience            = audience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // Web API
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "MS5 — Sensores y Usuarios API", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Pega el JWT obtenido de POST /auth/login. Ejemplo: Bearer eyJhbGci..."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ── Startup: BD + seed ────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SensoresUsuariosDbContext>();
        db.Database.EnsureCreated();
        Log.Information("BD Invernadero_SensoresUsuarios lista");

        if (!db.Invernaderos.Any())
        {
            db.Invernaderos.Add(new Invernadero
            {
                Id            = "INV-001",
                Nombre        = "Invernadero Norte",
                Ubicacion     = "Navojoa, Sonora",
                FechaRegistro = DateTime.UtcNow
            });

            var marcas = new[] { "MarcaA", "MarcaB" };
            for (int i = 1; i <= 5; i++)
            {
                db.Sensores.Add(new Sensor
                {
                    Id            = i.ToString(),
                    InvernaderoId = "INV-001",
                    Marca         = marcas[(i - 1) % 2],
                    TipoSensor    = "ambos",
                    Activo        = true,
                    FechaRegistro = DateTime.UtcNow
                });
            }

            var admin = new Usuario
            {
                Username      = "admin",
                Email         = "admin@invernadero.com",
                Telefono      = "+5216441234567",
                PushToken     = "fcm_token_demo_123",
                Rol           = "Admin",
                FechaRegistro = DateTime.UtcNow
            };
            db.Usuarios.Add(admin);

            db.SaveChanges();   // admin.Id queda asignado aquí

            db.PreferenciasNotificacion.Add(new PreferenciaNotificacion
            {
                UsuarioId   = admin.Id,
                EmailActivo = true,
                SmsActivo   = true,
                PushActivo  = true
            });
            db.SaveChanges();

            Log.Information("Seed completado: 1 invernadero, 5 sensores, 1 usuario");
        }
    }

    // ── Pipeline ──────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS5 SensoresUsuarios v1"));
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MS5.SensoresUsuarios terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
