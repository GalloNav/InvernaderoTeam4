using BlazorClient.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
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
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddAuthorization();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme   = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.ExpireTimeSpan      = TimeSpan.FromMinutes(60);
        options.SlidingExpiration   = true;
        options.Cookie.HttpOnly     = true;
        options.Cookie.SameSite     = SameSiteMode.Lax;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        var kc = builder.Configuration.GetSection("Keycloak");
        options.Authority                = kc["Authority"];
        options.ClientId                 = kc["ClientId"];
        options.RequireHttpsMetadata     = false;
        options.ResponseType             = OpenIdConnectResponseType.Code;
        options.UsePkce                  = true;
        options.SaveTokens               = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.SignedOutRedirectUri = kc["PostLogoutRedirectUri"]!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType   = "preferred_username",
            RoleClaimType   = ClaimTypes.Role,
            ValidateAudience = false
        };

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var realmAccess = identity.FindFirst("realm_access");
                    if (realmAccess is not null)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(realmAccess.Value);
                            if (doc.RootElement.TryGetProperty("roles", out var rolesEl)
                                && rolesEl.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var role in rolesEl.EnumerateArray())
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
            }
        };
    });

    builder.Services.AddHttpClient<IApiClient, ApiClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["ApiGateway:BaseUrl"]!));

    var app = builder.Build();

    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapGet("/login", (string? returnUrl, HttpContext ctx) =>
        Results.Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
            new[] { OpenIdConnectDefaults.AuthenticationScheme }
        ));

    app.MapGet("/logout", async (HttpContext ctx) =>
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" });
    });

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
