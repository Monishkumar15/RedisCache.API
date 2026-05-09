using Microsoft.EntityFrameworkCore;
using RedisCache.API.Models;

namespace RedisCache.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}
