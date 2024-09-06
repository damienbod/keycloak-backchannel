using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;

// See Duende samples for original code
namespace MvcPar.Controllers;

public class HomeController : Controller
{
    private readonly AuthConfiguration _authConfiguration;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;

    public HomeController(
        IOptions<AuthConfiguration> optionsAuthConfiguration,
        IConfiguration configuration,
        IHttpClientFactory clientFactory)
    {
        _configuration = configuration;
        _authConfiguration = optionsAuthConfiguration.Value;
        _clientFactory = clientFactory;
    }

    public IActionResult Index()
    {
        var cs = _configuration["Test"];
        return View("Index", cs);
    }

    [Authorize]
    public IActionResult Secure()
    {
        return View();
    }

    public async Task<IActionResult> RenewTokens()
    {
        var tokenclient = _clientFactory.CreateClient();

        var disco = await HttpClientDiscoveryExtensions.GetDiscoveryDocumentAsync(
            tokenclient,
            _authConfiguration.IdentityProviderUrl);

        if (disco.IsError)
        {
            throw new ApplicationException($"Status code: {disco.IsError}, Error: {disco.Error}");
        }

        var refreshToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

        var tokenResult = await HttpClientTokenRequestExtensions.RequestRefreshTokenAsync(tokenclient, new RefreshTokenRequest
        {
            ClientSecret = _authConfiguration.ClientSecret,
            Address = disco.TokenEndpoint,
            ClientId = _authConfiguration.Audience,
            RefreshToken = refreshToken
        });

        if (!tokenResult.IsError)
        {
            var oldIdToken = await HttpContext.GetTokenAsync("id_token");
            var newAccessToken = tokenResult.AccessToken;
            var newRefreshToken = tokenResult.RefreshToken;

            var tokens = new List<AuthenticationToken>
            {
                new AuthenticationToken { Name = OpenIdConnectParameterNames.IdToken, Value = oldIdToken },
                new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = newAccessToken },
                new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = newRefreshToken }
            };

            var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
            tokens.Add(new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) });

            var info = await HttpContext.AuthenticateAsync("Cookies");
            info.Properties!.StoreTokens(tokens);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, info.Principal!, info.Properties);

            return Redirect("~/Home/Secure");
        }

        ViewData["Error"] = tokenResult.Error;
        return View("Error");
    }

    public IActionResult Logout()
    {
        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }

    public IActionResult Error()
    {
        return View();
    }
}
