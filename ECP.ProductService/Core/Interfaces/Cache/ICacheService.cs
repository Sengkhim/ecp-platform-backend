namespace ECP.ProductService.Core.Interfaces.Cache;

public interface ICacheService
{
    Task<T?>  GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
}

public static class CacheKeys
{
    public static string Product(string id) => $"product:{id}";
    public static string ProductBySlug(string slug) => $"product:slug:{slug}";
    public static string ProductSearch(string hash) => $"product:search:{hash}";
    public static string ProductCategory(string categoryId, int skip, int take)
        => $"product:category:{categoryId}:{skip}:{take}";
    public const string ProductPattern = "product:*";
}