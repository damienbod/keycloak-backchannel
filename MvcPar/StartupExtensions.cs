using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MvcPar.BackChannelLogout;
using Serilog;

namespace MvcPar;

internal static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddTransient<CookieEventHandler>();
        services.AddSingleton<LogoutSessionManager>();
        services.AddHttpClient();

        services.Configure<AuthConfiguration>(configuration.GetSection("AuthConfiguration"));

        var authConfiguration = configuration.GetSection("AuthConfiguration");

        builder.AddRedisDistributedCache("cache");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.Cookie.Name = "MvcPar";

            options.EventsType = typeof(CookieEventHandler);
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authConfiguration["IdentityProviderUrl"];
            options.ClientSecret = authConfiguration["ClientSecret"];
            options.ClientId = authConfiguration["Audience"];
            options.ResponseType = OpenIdConnectResponseType.Code;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");

            options.ClaimActions.Remove("amr");
            options.ClaimActions.MapJsonKey("website", "website");

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;

            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role,
            };
        });

        services.AddControllersWithViews();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        IdentityModelEventSource.ShowPII = true;
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
