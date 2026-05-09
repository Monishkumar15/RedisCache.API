using StackExchange.Redis;

namespace RedisCache.API.Cache;

// IConnectionMultiplexer is registered as a singleton — one shared connection for the app lifetime.
public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Redis unavailable on GET '{Key}': {Message}", key, ex.Message);
            return null;
        }
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl)
    {
        try
        {
            await _db.StringSetAsync(key, value, ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Redis unavailable on SET '{Key}': {Message}", key, ex.Message);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Redis unavailable on DELETE '{Key}': {Message}", key, ex.Message);
        }
    }
}
