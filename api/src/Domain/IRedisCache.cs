namespace MovieSite.Domain;

public interface IRedisCache
{
    Task<T?> GetAsync<T>(string key) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class;

    Task DeleteAsync(string key);

    Task DeleteByPatternAsync(string pattern);

    Task<bool> ExistsAsync(string key);
}
