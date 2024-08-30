using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;

namespace MvcHybridBackChannel.Controllers;

public class HomeController : Controller
{
    private readonly AuthConfiguration _optionsAuthConfiguration;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;

    public HomeController(
        IOptions<AuthConfiguration> optionsAuthConfiguration,
        IConfiguration configuration,
        IHttpClientFactory clientFactory)
    {
        _configuration = configuration;
        _optionsAuthConfiguration = optionsAuthConfiguration.Value;
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
            _optionsAuthConfiguration.StsServerIdentityUrl);

        if (disco.IsError)
        {
            throw new ApplicationException($"Status code: {disco.IsError}, Error: {disco.Error}");
        }

        var rt = await HttpContext.GetTokenAsync("refresh_token");

        var tokenResult = await HttpClientTokenRequestExtensions.RequestRefreshTokenAsync(tokenclient, new RefreshTokenRequest
        {
            ClientSecret = "secret",
            Address = disco.TokenEndpoint,
            ClientId = "mvc.hybrid.backchannel",
            RefreshToken = rt
        });

        if (!tokenResult.IsError)
        {
            var old_id_token = await HttpContext.GetTokenAsync("id_token");
            var new_access_token = tokenResult.AccessToken;
            var new_refresh_token = tokenResult.RefreshToken;

            var tokens = new List<AuthenticationToken>
            {
                new AuthenticationToken { Name = OpenIdConnectParameterNames.IdToken, Value = old_id_token },
                new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = new_access_token },
                new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = new_refresh_token }
            };

            var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
            tokens.Add(new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) });

            var info = await HttpContext.AuthenticateAsync("Cookies");
            info.Properties!.StoreTokens(tokens);
            await HttpContext.SignInAsync("Cookies", info.Principal!, info.Properties);

            return Redirect("~/Home/Secure");
        }

        ViewData["Error"] = tokenResult.Error;
        return View("Error");
    }

    public IActionResult Logout()
    {
        return new SignOutResult(["Cookies", "oidc"]);
    }

    public IActionResult Error()
    {
        return View();
    }
}
