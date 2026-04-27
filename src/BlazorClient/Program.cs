using BlazorClient.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
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
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpClient<IApiClient, ApiClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["ApiGateway:BaseUrl"]!));

    var app = builder.Build();

    app.UseStaticFiles();
    app.UseAntiforgery();
    app.MapRazorComponents<BlazorClient.Components.App>()
       .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BlazorClient terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
