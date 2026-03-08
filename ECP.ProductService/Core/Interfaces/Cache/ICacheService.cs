namespace ECP.ProductService.Core.Interfaces.Cache;

public interface ICacheService
{
    Task<T?>  GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task      SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
    Task      RemoveAsync(string key, CancellationToken ct = default);
    Task      RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<T>   GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
}

/// <summary>
/// Centralised key definitions — prevents key typos scattered across handlers.
/// Key format: {service}:{entity}:{discriminator}
/// </summary>
public static class CacheKey
{
    private const string Prefix = "product-svc";

    public static string ById(Guid id)           => $"{Prefix}:product:{id}";
    public static string BySlug(string slug)     => $"{Prefix}:product:slug:{slug}";
    public static string ByCategory(Guid catId, int skip, int take)
                                                  => $"{Prefix}:product:cat:{catId}:{skip}:{take}";
    public static string Search(string hash)     => $"{Prefix}:product:search:{hash}";

    // Prefix for bulk invalidation (all product caches)
    public const string ProductPrefix = $"{Prefix}:product";
}
