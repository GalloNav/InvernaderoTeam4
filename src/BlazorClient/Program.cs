using BlazorClient.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
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
    builder.Services.AddSingleton<TokenCache>();
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

        options.SignedOutCallbackPath  = "/signout-callback-oidc";
        options.SignedOutRedirectUri   = "/";

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
                    var sub = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? identity.FindFirst("sub")?.Value;
                    var accessToken = context.TokenEndpointResponse?.AccessToken;

                    // Guardar token en cache singleton para uso desde componentes interactivos
                    if (!string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(accessToken))
                    {
                        var cache = context.HttpContext.RequestServices
                            .GetRequiredService<TokenCache>();
                        cache.Set(sub, accessToken);
                    }

                    // Decodificar el access_token y extraer realm_access.roles
                    // (los roles de Keycloak vienen en el ACCESS token, no en el ID token)
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        try
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var jwt = handler.ReadJwtToken(accessToken);
                            var realmAccessClaim = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access");

                            if (realmAccessClaim is not null)
                            {
                                using var doc = JsonDocument.Parse(realmAccessClaim.Value);
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
                        }
                        catch (Exception)
                        {
                            // Si la decodificación falla, simplemente no se agregan roles
                            // (el usuario seguirá autenticado pero sin permisos especiales)
                        }
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

    app.MapGet("/logout", async (HttpContext ctx, TokenCache cache) =>
    {
        var sub = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? ctx.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub))
            cache.Remove(sub);

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

public sealed class TokenCache
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _tokens = new();
    public void Set(string userId, string token) => _tokens[userId] = token;
    public string? Get(string userId) => _tokens.TryGetValue(userId, out var t) ? t : null;
    public void Remove(string userId) => _tokens.TryRemove(userId, out _);
}
