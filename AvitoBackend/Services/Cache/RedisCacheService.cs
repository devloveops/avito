using StackExchange.Redis;

namespace AvitoBackend.Services.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    public RedisCacheService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public async Task<string?> GetAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }
    
    public Task SetAsync(string key, string value, TimeSpan? expiry = null) =>
        _db.StringSetAsync(key, value, expiry);
    public Task RemoveAsync(string key)
    {
        return _db.KeyDeleteAsync(key);
    }
}