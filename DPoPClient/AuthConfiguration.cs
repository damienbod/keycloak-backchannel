namespace DPoPClient;

public class AuthConfiguration
{
    public string IdentityProviderUrl { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public string ApiClientId { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}
