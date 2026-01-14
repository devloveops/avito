using AvitoBackend.Data;
using AvitoBackend.Services.Cache;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AvitoBackend.GraphQL.Queries;

[ExtendObjectType("Query")]
public class AdvertisementQuery
{
    
public async Task<List<AdvertisementDto>> Advertisements(
    string? categoryName,
    decimal? maxPrice,
    [Service] AppDbContext context,
    [Service] RedisCacheService cache)
{
    try
    {
        var cacheKey = $"ads:cat:{categoryName ?? "all"}:price:{maxPrice?.ToString() ?? "any"}";

        var cached = await cache.GetAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<List<AdvertisementDto>>(cached)!;

        var query = context.Advertisements
            .AsNoTracking()
            .Include(a => a.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoryName))
            query = query.Where(a => a.Category!.Name == categoryName);

        if (maxPrice.HasValue)
            query = query.Where(a => a.Price <= maxPrice.Value);

        var result = await query
            .Select(a => new AdvertisementDto
            {
                Id = a.Id,
                Title = a.Title,
                Price = a.Price,
                CategoryName = a.Category!.Name
            })
            .ToListAsync();

        await cache.SetAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            TimeSpan.FromMinutes(5));

        return result;
    }
    catch (Exception ex)
    {
        throw new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(ex.ToString())
                .Build());
    }
}
}

public class AdvertisementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = null!;
}