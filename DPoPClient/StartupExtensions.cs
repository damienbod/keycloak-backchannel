using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace DPoPClient;

internal static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;

        var authConfiguration = configuration.GetSection("AuthConfiguration");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnSigningOut = async e =>
            {
                await e.HttpContext.RevokeRefreshTokenAsync();
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authConfiguration["IdentityProviderUrl"];
            options.ClientId = authConfiguration["ClientId"];
            options.ClientSecret = authConfiguration["ClientSecret"];
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.ResponseMode = "query";
            options.UsePkce = true;

            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("scope-dpop");
            options.Scope.Add("offline_access");
            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role,
            };
        });

        var privatePem = File.ReadAllText(Path.Combine(env.ContentRootPath, "ecdsa384-private.pem"));
        var publicPem = File.ReadAllText(Path.Combine(env.ContentRootPath, "ecdsa384-public.pem"));
        var ecdsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
        var ecdsaCertificateKey = new ECDsaSecurityKey(ecdsaCertificate.GetECDsaPrivateKey());

        //var privatePem = File.ReadAllText(Path.Combine(_environment.ContentRootPath, "rsa256-private.pem"));
        //var publicPem = File.ReadAllText(Path.Combine(_environment.ContentRootPath, "rsa256-public.pem"));
        //var rsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
        //var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());

        // add automatic token management
        services.AddOpenIdConnectAccessTokenManagement(options =>
        {
            // create and configure a DPoP JWK
            //var rsaKey = new RsaSecurityKey(RSA.Create(2048));
            //var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaKey);
            //jwk.Alg = "PS256";
            //options.DPoPJsonWebKey = JsonSerializer.Serialize(jwk);

            //var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaCertificateKey);
            //jwk.Alg = "PS256";
            //options.DPoPJsonWebKey = JsonSerializer.Serialize(jwk);

            var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(ecdsaCertificateKey);
            jwk.Alg = "ES384";
            options.DPoPJsonWebKey = JsonSerializer.Serialize(jwk);
        });

        services.AddUserAccessTokenHttpClient(authConfiguration["ApiClientId"]!, configureClient: client =>
        {
            client.BaseAddress = new Uri(authConfiguration["ApiUrl"]!);
        });

        services.AddRazorPages();

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