﻿using Microsoft.Extensions.Caching.Distributed;

namespace DPoPApi;

// original source: https://github.com/DuendeSoftware/Samples/tree/main/IdentityServer/v6/DPoP
public class DefaultReplayCache : IReplayCache
{
    private const string Prefix = nameof(DefaultReplayCache) + "-";

    private readonly IDistributedCache _cache;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="cache"></param>
    public DefaultReplayCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task AddAsync(string purpose, string handle, DateTimeOffset expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiration
        };

        await _cache.SetAsync(Prefix + purpose + handle, new byte[] { }, options);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string purpose, string handle)
    {
        return (await _cache.GetAsync(Prefix + purpose + handle, default)) != null;
    }
}