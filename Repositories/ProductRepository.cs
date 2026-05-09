using Microsoft.EntityFrameworkCore;
using RedisCache.API.Data;
using RedisCache.API.Models;

namespace RedisCache.API.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(int id)
        => await db.Products.FindAsync(id);

    public async Task<IEnumerable<Product>> GetAllAsync()
        => await db.Products.ToListAsync();

    public async Task<Product> CreateAsync(Product product)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var existing = await db.Products.FindAsync(id);
        if (existing is null) return null;

        existing.Name = updated.Name;
        existing.Category = updated.Category;
        existing.Price = updated.Price;
        existing.Stock = updated.Stock;

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await db.Products.FindAsync(id);
        if (existing is null) return false;

        db.Products.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }
}
