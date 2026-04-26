using Microsoft.EntityFrameworkCore;
using MS2.Mediciones.Datos;
using MS2.Mediciones.Mensajeria;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, cfg) => cfg
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore",            Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore",   Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddDbContext<MedicionesDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("Mediciones")));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "MS2 — Mediciones API", Version = "v1" });
    });

    builder.Services.AddHostedService<MedicionesSubscriber>();

    var app = builder.Build();

    // Crear la BD si no existe (PoC: EnsureCreated en lugar de migrations)
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider
            .GetRequiredService<MedicionesDbContext>()
            .Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS2 Mediciones v1"));
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MS2.Mediciones terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
