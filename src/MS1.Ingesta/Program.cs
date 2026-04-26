using MS1.Ingesta;
using MS1.Ingesta.Mensajeria;
using MS1.Ingesta.Tcp;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(dispose: true);

    builder.Services.AddSingleton<RabbitPublisher>();
    builder.Services.AddSingleton<TcpServer>();
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MS1.Ingesta terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
