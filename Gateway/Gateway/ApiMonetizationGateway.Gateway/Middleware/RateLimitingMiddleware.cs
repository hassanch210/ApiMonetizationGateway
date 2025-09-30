using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Services;
using System.Text.Json;
using System.Net;

namespace ApiMonetizationGateway.Gateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRedisService _redisService;

    public RateLimitingMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory, IRedisService redisService)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
        _redisService = redisService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            await _next(context);
            return;
        }

        var rateLimitInfo = await GetRateLimitInfo(userId);
        if (rateLimitInfo == null)
        {
            await _next(context);
            return;            
        }

        var rateLimitResult = await CheckRateLimits(rateLimitInfo);
        if (!rateLimitResult.IsWithinLimits)
        {
            await WriteRateLimitResponse(context, rateLimitResult);
            return;
        }

        context.Items["UserId"] = rateLimitInfo.UserId;
        context.Items["TierId"] = rateLimitInfo.TierId;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();
        _ = Task.Run(() => TrackUsage(context, rateLimitInfo.UserId, stopwatch.ElapsedMilliseconds));
    }

    private async Task<RateLimitInfo?> GetRateLimitInfo(int userId)
    {
        var cacheKey = $"rate_limit_info_user:{userId}";
        var cachedInfo = await _redisService.GetAsync<RateLimitInfo>(cacheKey);
        if (cachedInfo != null)
        {
            return cachedInfo;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null) return null;

        var activeTier = await db.UserTiers
            .Where(ut => ut.UserId == userId && ut.IsActive)
            .OrderByDescending(ut => ut.AssignedAt)
            .Select(ut => ut.TierId)
            .FirstOrDefaultAsync();
        var tier = await db.Tiers.FirstOrDefaultAsync(t => t.Id == activeTier && t.IsActive);

        var currentMonth = DateTime.UtcNow;
        var monthlyUsage = await db.ApiUsages
            .Where(a => a.UserId == user.Id && 
                       a.RequestTimestamp.Year == currentMonth.Year &&
                       a.RequestTimestamp.Month == currentMonth.Month)
            .CountAsync();

        var info = new RateLimitInfo
        {
            UserId = user.Id,
            ApiKey = string.Empty,
            TierId = activeTier,
            RateLimit = tier?.RateLimit ?? 0,
            MonthlyQuota = tier?.MonthlyQuota ?? 0,
            CurrentMonthUsage = monthlyUsage,
            IsWithinLimits = true
        };

        await _redisService.SetAsync(cacheKey, info, TimeSpan.FromDays(1));
        return info;
    }

    private async Task<RateLimitInfo> CheckRateLimits(RateLimitInfo info)
    {
        // Check monthly quota first
        var monthKey = $"monthly_usage_user:{info.UserId}:{DateTime.UtcNow:yyyyMM}";
        var monthCount = await _redisService.IncrementAsync(monthKey, 1, TimeSpan.FromDays(35));
        
        // If monthly quota is exceeded, return immediately
        if (info.MonthlyQuota > 0 && monthCount > info.MonthlyQuota)
        {
            info.IsWithinLimits = false;
            info.LimitExceededReason = $"Monthly quota exceeded. Limit: {info.MonthlyQuota}, Current: {monthCount}";
            return info;
        }

        // Check per-second rate limit using sliding window
        var currentSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rateLimitKey = $"rate_limit_counter_user:{info.UserId}:{currentSecond}";
        
        // Increment the counter for the current second and set expiry to 5 seconds
        var currentRequests = await _redisService.IncrementAsync(rateLimitKey, 1, TimeSpan.FromSeconds(5));

        // If rate limit is exceeded, return error
        if (info.RateLimit > 0 && currentRequests > info.RateLimit)
        {
            info.IsWithinLimits = false;
            info.LimitExceededReason = $"Rate limit exceeded. Limit: {info.RateLimit} requests/second, Current: {currentRequests}";
            return info;
        }

        // Update the current month usage in the rate limit info
        info.CurrentMonthUsage = (int)monthCount;
        
        return info;
    }

    private async Task TrackUsage(HttpContext context, int userId, long responseTimeMs)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var messageQueue = scope.ServiceProvider.GetService<IMessageQueueService>();
            
            if (messageQueue != null)
            {
                // Get tier ID from context items
                var tierId = context.Items.TryGetValue("TierId", out var tierIdObj) && tierIdObj != null 
                    ? Convert.ToInt32(tierIdObj) 
                    : 0;
                
                // Create detailed usage tracking message
                var usageMessage = new UsageTrackingMessage
                {
                    UserId = userId,
                    Endpoint = context.Request.Path.Value ?? "",
                    HttpMethod = context.Request.Method,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseTimeMs = responseTimeMs,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    RequestTimestamp = DateTime.UtcNow
                };

                // Publish to RabbitMQ queue for processing by the UsageTrackingService
                await messageQueue.PublishAsync("api-usage-tracking", usageMessage);
                
                // Also update the monthly usage counter in Redis
                var monthKey = $"monthly_usage_user:{userId}:{DateTime.UtcNow:yyyyMM}";
                await _redisService.IncrementAsync(monthKey, 1, TimeSpan.FromDays(35));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error tracking usage: {ex.Message}");
        }
    }

    private static async Task WriteRateLimitResponse(HttpContext context, RateLimitInfo rateLimitInfo)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        // Determine if it's a rate limit or quota violation
        bool isRateLimitViolation = rateLimitInfo.LimitExceededReason?.Contains("Rate limit") ?? false;
        int retryAfterSeconds = isRateLimitViolation ? 5 : 86400; // 5 seconds for rate limit, 24 hours for quota

        // Set appropriate headers based on violation type
        context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.RateLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds().ToString();
        context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        
        // For monthly quota, add additional headers
        if (!isRateLimitViolation)
        {
            context.Response.Headers["X-Monthly-Quota-Limit"] = rateLimitInfo.MonthlyQuota.ToString();
            context.Response.Headers["X-Monthly-Quota-Used"] = rateLimitInfo.CurrentMonthUsage.ToString();
            context.Response.Headers["X-Monthly-Quota-Reset-Date"] = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1).ToUniversalTime().ToString("R");
        }

        // Create simplified response with guidance on how to resolve the issue
        var response = new 
        {
            Error = "Rate limit exceeded",
            Message = (rateLimitInfo.LimitExceededReason ?? "Rate limit exceeded") + (isRateLimitViolation 
                ? " Please reduce your request rate or upgrade to a higher tier for increased limits."
                : " You have exceeded your monthly quota. Please upgrade your subscription tier for additional requests."),
            RetryAfterSeconds = retryAfterSeconds,
            Headers = new 
            {
                Limit = isRateLimitViolation ? rateLimitInfo.RateLimit : rateLimitInfo.MonthlyQuota,
                Remaining = 0,
                ResetTime = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds(),
                RetryAfter = retryAfterSeconds
            }
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(json);
    }
}