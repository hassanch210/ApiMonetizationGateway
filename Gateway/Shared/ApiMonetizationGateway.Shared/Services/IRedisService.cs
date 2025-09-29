namespace ApiMonetizationGateway.Shared.Services;

public interface IRedisService
{
    Task<string?> GetAsync(string key);
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync(string key, string value, TimeSpan? expiry = null);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null);
    Task<long> DecrementAsync(string key, long value = 1);
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null);
    Task<TimeSpan?> GetExpiryAsync(string key);
    Task<bool> ExpireAsync(string key, TimeSpan expiry);
}