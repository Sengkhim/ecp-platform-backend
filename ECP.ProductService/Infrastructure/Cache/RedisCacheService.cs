using System.Text.Json;
using System.Text.Json.Serialization;
using ECP.ProductService.Core.Interfaces.Cache;
using StackExchange.Redis;

namespace ECP.ProductService.Infrastructure.Cache;

public sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly IServer   _server = redis.GetServer(redis.GetEndPoints().First());

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);


    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _db.StringGetAsync(key);
        return !value.HasValue 
            ? null 
            : JsonSerializer.Deserialize<T>((ReadOnlySpan<byte>)value!, JsonOpts);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
        where T : class
    {
        var json = JsonSerializer.Serialize(value, JsonOpts);
        await _db.StringSetAsync(key, json, ttl ?? DefaultTtl);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _db.KeyDeleteAsync(key);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // SCAN-based deletion — safe in production (no KEYS command)
        var keys = _server
            .Keys(pattern: $"{prefix}*", pageSize: 1000)
            .ToArray();

        if (keys.Length > 0)
            await _db.KeyDeleteAsync(keys);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory(ct);

        await SetAsync(key, value, ttl, ct);

        return value;
    }
}
