using StackExchange.Redis;
using System.Text.Json;

namespace ApiMonetizationGateway.Shared.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisService(IConnectionMultiplexer connection)
    {
        _database = connection.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _database.StringGetAsync(key);
        if (!value.HasValue)
            return null;

        // Special-case strings: values may be stored as raw strings (non-JSON)
        if (typeof(T) == typeof(string))
        {
            return value.ToString() as T;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch
        {
            // If deserialization fails (e.g., stale or non-JSON data), return null to avoid crashing middleware
            return null;
        }
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        await _database.StringSetAsync(key, value, expiry);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _database.StringSetAsync(key, json, expiry);
    }

    public async Task<bool> DeleteAsync(string key)
    {
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null)
    {
        var result = await _database.StringIncrementAsync(key, value);
        if (expiry.HasValue)
        {
            await _database.KeyExpireAsync(key, expiry);
        }
        return result;
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        return await _database.StringDecrementAsync(key, value);
    }

    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _database.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task<TimeSpan?> GetExpiryAsync(string key)
    {
        return await _database.KeyTimeToLiveAsync(key);
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiry)
    {
        return await _database.KeyExpireAsync(key, expiry);
    }
}
