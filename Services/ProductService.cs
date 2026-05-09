using System.Text.Json;
using RedisCache.API.Cache;
using RedisCache.API.Models;
using RedisCache.API.Repositories;

namespace RedisCache.API.Services;

public class ProductService(
    IProductRepository repository,
    ICacheService cache,
    ILogger<ProductService> logger) : IProductService
{
    // TTL of 5 minutes: product data is stable enough to cache but short
    // enough that stale data doesn't persist too long without an explicit invalidation.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static string CacheKey(int id) => $"product:{id}";

    public async Task<Product?> GetByIdAsync(int id)
    {
        var key = CacheKey(id);

        var cached = await cache.GetAsync(key);
        if (cached is not null)
        {
            logger.LogInformation("CACHE HIT  — key: {Key}", key);
            return JsonSerializer.Deserialize<Product>(cached);
        }

        logger.LogInformation("CACHE MISS — key: {Key}. Fetching from database.", key);
        var product = await repository.GetByIdAsync(id);

        if (product is not null)
            await cache.SetAsync(key, JsonSerializer.Serialize(product), CacheTtl);

        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        // GetAll always hits the DB — caching a list is more complex because
        // any single product update would need to invalidate the whole list.
        logger.LogInformation("GetAll — fetching all products from database.");
        return await repository.GetAllAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        var created = await repository.CreateAsync(product);
        logger.LogInformation("Product {Id} created.", created.Id);
        return created;
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var result = await repository.UpdateAsync(id, updated);
        if (result is null) return null;

        // Invalidate on write: remove stale cache entry so the next read
        // fetches fresh data from the DB and re-populates the cache.
        await cache.RemoveAsync(CacheKey(id));
        logger.LogInformation("CACHE INVALIDATED — key: {Key} after update.", CacheKey(id));

        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var deleted = await repository.DeleteAsync(id);
        if (!deleted) return false;

        await cache.RemoveAsync(CacheKey(id));
        logger.LogInformation("CACHE INVALIDATED — key: {Key} after delete.", CacheKey(id));

        return true;
    }
}
