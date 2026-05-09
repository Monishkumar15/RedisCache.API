using StackExchange.Redis;

namespace RedisCache.API.Cache;

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
            logger.LogWarning("!!! REDIS DOWN — GET '{Key}' failed. FALLING BACK TO DATABASE. Error: {Message}",
                key, ex.Message);
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
            logger.LogWarning("!!! REDIS DOWN — SET '{Key}' failed. Data will not be cached. Error: {Message}",
                key, ex.Message);
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
            logger.LogWarning("!!! REDIS DOWN — DELETE '{Key}' failed. Cache may be stale. Error: {Message}",
                key, ex.Message);
        }
    }
}
