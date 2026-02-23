using System.Text.Json;
using MovieSite.Domain;
using StackExchange.Redis;

namespace MovieSite.Infrastructure.Cache;

public sealed class RedisCache(IConnectionMultiplexer redis) : IRedisCache
{
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
    {
        var payload = JsonSerializer.Serialize(value);
        return _db.StringSetAsync(key, payload, ttl);
    }

    public Task DeleteAsync(string key)
    {
        return _db.KeyDeleteAsync(key);
    }

    public async Task DeleteByPatternAsync(string pattern)
    {
        var endpoints = _redis.GetEndPoints();
        if (endpoints.Length == 0)
        {
            return;
        }

        var keys = new List<RedisKey>();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            foreach (var key in server.Keys(pattern: pattern, pageSize: 100))
            {
                keys.Add(key);
            }
        }

        if (keys.Count > 0)
        {
            await _db.KeyDeleteAsync(keys.ToArray());
        }
    }

    public Task<bool> ExistsAsync(string key)
    {
        return _db.KeyExistsAsync(key);
    }
}
