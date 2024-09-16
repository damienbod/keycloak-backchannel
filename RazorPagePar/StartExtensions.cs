using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace RazorPagePar;

internal static class StartExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        var services = builder.Services;
        var configuration = builder.Configuration;

        services.Configure<AuthConfiguration>(configuration.GetSection("AuthConfiguration"));

        var authConfiguration = configuration.GetSection("AuthConfiguration");

        services.AddHttpClient();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
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

        services.AddRazorPages();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        IdentityModelEventSource.ShowPII = true;

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        return app;
    }
}