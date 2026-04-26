using Microsoft.EntityFrameworkCore;
using MS3.Alarmas.Datos;
using MS3.Alarmas.Mensajeria;
using MS3.Alarmas.Servicios;
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
        .MinimumLevel.Override("Microsoft.AspNetCore",           Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore",  Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddDbContext<AlarmasDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("Alarmas")));

    builder.Services.AddSingleton<AlarmasPublisher>();
    builder.Services.AddSingleton<ICacheUmbrales, CacheUmbrales>();
    builder.Services.AddSingleton<EvaluadorUmbrales>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "MS3 — Alarmas API", Version = "v1" });
    });

    builder.Services.AddHostedService<MedicionesSubscriber>();

    var app = builder.Build();

    // Crear BD y seed inicial
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AlarmasDbContext>();
        db.Database.EnsureCreated();

        if (!db.UmbralesInvernadero.Any())
        {
            db.UmbralesInvernadero.Add(new UmbralInvernadero
            {
                InvernaderoId = "INV-001",
                TempMin = 15f,
                TempMax = 32f,
                HumMin  = 40f,
                HumMax  = 88f
            });
            db.SaveChanges();
            Log.Information("Seed: umbral default INV-001 insertado");
        }
    }

    // Calentar caché en memoria antes de aceptar mensajes
    await app.Services.GetRequiredService<ICacheUmbrales>().RefreshAsync();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS3 Alarmas v1"));
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MS3.Alarmas terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
