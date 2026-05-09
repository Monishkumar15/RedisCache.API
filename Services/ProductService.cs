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
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static string ProductKey(int id) => $"product:{id}";
    private const string AllProductsKey = "products:all";

    public async Task<Product?> GetByIdAsync(int id)
    {
        var key = ProductKey(id);

        var cached = await cache.GetAsync(key);
        if (cached is not null)
        {
            logger.LogInformation(">>> CACHE HIT  — key: {Key}", key);
            return JsonSerializer.Deserialize<Product>(cached);
        }

        logger.LogInformation(">>> CACHE MISS — key: {Key} | Fetching from DB...", key);
        var product = await repository.GetByIdAsync(id);

        if (product is not null)
        {
            await cache.SetAsync(key, JsonSerializer.Serialize(product), CacheTtl);
            logger.LogInformation(">>> CACHE SET  — key: {Key} | TTL: {Ttl} mins", key, CacheTtl.TotalMinutes);
        }

        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        var cached = await cache.GetAsync(AllProductsKey);
        if (cached is not null)
        {
            logger.LogInformation(">>> CACHE HIT  — key: {Key}", AllProductsKey);
            return JsonSerializer.Deserialize<IEnumerable<Product>>(cached)!;
        }

        logger.LogInformation(">>> CACHE MISS — key: {Key} | Fetching from DB...", AllProductsKey);
        var products = await repository.GetAllAsync();
        var list = products.ToList();

        if (list.Count > 0)
        {
            await cache.SetAsync(AllProductsKey, JsonSerializer.Serialize(list), CacheTtl);
            logger.LogInformation(">>> CACHE SET  — key: {Key} | {Count} products cached | TTL: {Ttl} mins",
                AllProductsKey, list.Count, CacheTtl.TotalMinutes);
        }

        return list;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        var created = await repository.CreateAsync(product);

        // Invalidate the all-products list so next GetAll reflects the new product
        await cache.RemoveAsync(AllProductsKey);
        logger.LogInformation(">>> CACHE INVALIDATED — key: {Key} | Reason: new product created (Id: {Id})",
            AllProductsKey, created.Id);

        return created;
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var result = await repository.UpdateAsync(id, updated);
        if (result is null) return null;

        // Invalidate both the single product key and the all-products list
        await cache.RemoveAsync(ProductKey(id));
        await cache.RemoveAsync(AllProductsKey);
        logger.LogInformation(">>> CACHE INVALIDATED — keys: [{Key1}, {Key2}] | Reason: product {Id} updated",
            ProductKey(id), AllProductsKey, id);

        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var deleted = await repository.DeleteAsync(id);
        if (!deleted) return false;

        await cache.RemoveAsync(ProductKey(id));
        await cache.RemoveAsync(AllProductsKey);
        logger.LogInformation(">>> CACHE INVALIDATED — keys: [{Key1}, {Key2}] | Reason: product {Id} deleted",
            ProductKey(id), AllProductsKey, id);

        return true;
    }
}
