namespace DPoPApi;

// original source: https://github.com/DuendeSoftware/Samples/tree/main/IdentityServer/v6/DPoP
public enum DPoPMode
{
    /// <summary>
    /// Only DPoP tokens will be accepted
    /// </summary>
    DPoPOnly,
    /// <summary>
    /// Both DPoP and Bearer tokens will be accepted
    /// </summary>
    DPoPAndBearer
}
