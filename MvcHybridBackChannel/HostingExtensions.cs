using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using MvcHybridBackChannel.BackChannelLogout;
using Serilog;

namespace MvcHybridBackChannel;

internal static class HostingExtensions
{
    private static IWebHostEnvironment? _env;

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        _env = builder.Environment;

        services.AddTransient<CookieEventHandler>();
        services.AddSingleton<LogoutSessionManager>();
        services.AddHttpClient();

        services.Configure<AuthConfiguration>(configuration.GetSection("AuthConfiguration"));

        var authConfiguration = configuration.GetSection("AuthConfiguration");
        var clientId_aud = authConfiguration["Audience"];

        var redisConnectionString = configuration.GetConnectionString("RedisCacheConnection");

        if (string.IsNullOrEmpty(redisConnectionString))
        {
            // remove this, if your use a proper development cache hich uses the same as the production
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("RedisCacheConnection");
                options.InstanceName = "MvcHybridBackChannelBackChannelInstance";
            });
        }

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.Cookie.Name = "MvcHybridBackChannel";

            options.EventsType = typeof(CookieEventHandler);
        })
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = authConfiguration["StsServerIdentityUrl"];
            options.RequireHttpsMetadata = false;
            options.ClientSecret = configuration["SecretMvcHybridBackChannelBackChannel"];
            options.ClientId = clientId_aud;
            options.ResponseType = "code";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");

            options.ClaimActions.Remove("amr");
            options.ClaimActions.MapJsonKey("website", "website");

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;

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

        if (_env!.IsDevelopment())
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
