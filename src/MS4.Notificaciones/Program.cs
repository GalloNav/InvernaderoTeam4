using Microsoft.EntityFrameworkCore;
using MS4.Notificaciones.Configuracion;
using MS4.Notificaciones.Datos;
using MS4.Notificaciones.Estrategias;
using MS4.Notificaciones.Mensajeria;
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

    builder.Services.AddDbContext<NotificacionesDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("Notificaciones")));

    builder.Services.Configure<MailtrapOptions>(builder.Configuration.GetSection("Mailtrap"));

    builder.Services.AddSingleton<INotificacionStrategy, EstrategiaEmail>();
    builder.Services.AddSingleton<INotificacionStrategy, EstrategiaSMS>();
    builder.Services.AddSingleton<INotificacionStrategy, EstrategiaPush>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "MS4 — Notificaciones API", Version = "v1" });
    });

    builder.Services.AddHostedService<AlarmasSubscriber>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider
            .GetRequiredService<NotificacionesDbContext>()
            .Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS4 Notificaciones v1"));
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MS4.Notificaciones terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
