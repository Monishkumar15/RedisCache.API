using RedisCache.API.Models;

namespace RedisCache.API.Services;

public interface IProductService
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(int id, Product updated);
    Task<bool> DeleteAsync(int id);
}
