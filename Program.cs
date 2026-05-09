using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using RedisCache.API.Cache;
using RedisCache.API.Data;
using RedisCache.API.Repositories;
using RedisCache.API.Services;

var builder = WebApplication.CreateBuilder(args);

// SQL Server via EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis — singleton because IConnectionMultiplexer is thread-safe and expensive to create.
// abortConnect=false means the app starts even if Redis is not yet available.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") + ",abortConnect=false"));

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "Redis Cache-Aside API",
        Version     = "v1",
        Description = "Demonstrates the cache-aside pattern using Redis in front of SQL Server. " +
                      "GET /api/products/{id} shows CACHE HIT / MISS in the console logs. " +
                      "PUT /api/products/{id} invalidates the cache on write."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Apply any pending EF Core migrations automatically on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
