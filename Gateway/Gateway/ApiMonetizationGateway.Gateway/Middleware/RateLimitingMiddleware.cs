using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Services;
using ApiMonetizationGateway.Gateway.Models;
using System.Text.Json;
using System.Net;

namespace ApiMonetizationGateway.Gateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRedisService _redisService;
    private readonly IMessageQueueService _messageQueueService;

    public RateLimitingMiddleware(
        RequestDelegate next, 
        IServiceScopeFactory serviceScopeFactory, 
        IRedisService redisService,
        IMessageQueueService messageQueueService)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
        _redisService = redisService;
        _messageQueueService = messageQueueService;
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

        // Get user tier info from Redis (populated during JWT validation)
        var userTier = context.Items["UserTier"] as UserTierInfo;
        
        // If not available in context, try to get from Redis directly
        if (userTier == null)
        {
            var userTierKey = $"user_tier:{userId}";
            userTier = await _redisService.GetAsync<UserTierInfo>(userTierKey);
            
            // If still not available, fall back to database and cache in Redis
            if (userTier == null)
            {
                userTier = await GetUserTierFromDatabase(userId);
                if (userTier != null)
                {
                    await _redisService.SetAsync(userTierKey, userTier, TimeSpan.FromHours(24));
                }
            }
        }
        
        if (userTier == null)
        {
            await _next(context);
            return;
        }
        
        // Create rate limit info from user tier
        var rateLimitInfo = new RateLimitInfo
        {
            UserId = userId,
            ApiKey = string.Empty,
            TierId = userTier.TierId,
            RateLimit = userTier.RateLimit,
            MonthlyQuota = userTier.MonthlyQuota,
            CurrentMonthUsage = await GetMonthlyUsageFromRedis(userId),
            IsWithinLimits = true
        };

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
        
        // Track usage asynchronously via RabbitMQ
        _ = Task.Run(() => TrackUsage(context, rateLimitInfo.UserId, stopwatch.ElapsedMilliseconds));
    }
    
    private async Task<UserTierInfo?> GetUserTierFromDatabase(int userId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null) return null;

        var userTier = await db.UserTiers
            .Where(ut => ut.UserId == userId && ut.IsActive)
            .OrderByDescending(ut => ut.AssignedAt)
            .FirstOrDefaultAsync();
            
        if (userTier == null) return null;
        
        var tier = await db.Tiers.FirstOrDefaultAsync(t => t.Id == userTier.TierId && t.IsActive);
        if (tier == null) return null;
        
        return new UserTierInfo
        {
            UserId = userId,
            TierId = (int)tier.Id,
            TierName = tier.Name,
            RateLimit = (int)tier.RateLimit,
            MonthlyQuota = (int)tier.MonthlyQuota,
            ExpiresAt = DateTime.UtcNow.AddYears(1) // Default expiration
        };
    }
    
    private async Task<int> GetMonthlyUsageFromRedis(int userId)
    {
        var monthKey = $"monthly_usage:{userId}:{DateTime.UtcNow:yyyyMM}";
        var monthCount = await _redisService.GetAsync<string>(monthKey);
        return string.IsNullOrEmpty(monthCount) ? 0 : int.Parse(monthCount);
    }

    private async Task<RateLimitInfo> CheckRateLimits(RateLimitInfo info)
    {
        // Monthly quota via Redis counter per month
        var monthKey = $"monthly_usage:{info.UserId}:{DateTime.UtcNow:yyyyMM}";
        var monthCount = await _redisService.IncrementAsync(monthKey, 1, TimeSpan.FromDays(35));
        
        // Update the current month usage in the info object
        info.CurrentMonthUsage = monthCount;
        
        if (monthCount > info.MonthlyQuota)
        {
            info.IsWithinLimits = false;
            info.LimitExceededReason = "Monthly quota exceeded";
            return info;
        }

        // Per-second rate limiting using Redis
        var rateLimitKey = $"rate_limit:{info.UserId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var currentRequests = await _redisService.IncrementAsync(rateLimitKey, 1, TimeSpan.FromSeconds(1));

        if (currentRequests > info.RateLimit)
        {
            info.IsWithinLimits = false;
            info.LimitExceededReason = "Rate limit exceeded";
            return info;
        }

        return info;
    }

    private async Task TrackUsage(HttpContext context, int userId, long responseTimeMs)
    {
        try
        {
            // Create usage tracking message
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

            // Update Redis for real-time tracking
            var apiKey = $"api_hit:{userId}:{DateTime.UtcNow:yyyyMMdd}:{context.Request.Path.Value}";
            await _redisService.IncrementAsync(apiKey, 1, TimeSpan.FromDays(2));
            
            // Send message to RabbitMQ for persistent storage in MS SQL
            await _messageQueueService.PublishAsync("usage-tracking", usageMessage);
            
            // Also send a message for monthly usage summary update
            var monthlySummaryMessage = new MonthlyUsageSummaryMessage
            {
                UserId = userId,
                Year = DateTime.UtcNow.Year,
                Month = DateTime.UtcNow.Month,
                EndpointPath = context.Request.Path.Value ?? "",
                RequestCount = 1,
                TotalResponseTimeMs = responseTimeMs,
                LastUpdated = DateTime.UtcNow
            };
            
            await _messageQueueService.PublishAsync("monthly-usage-summary", monthlySummaryMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error tracking usage: {ex.Message}");
        }
    }

    private static async Task WriteRateLimitResponse(HttpContext context, RateLimitInfo rateLimitInfo)
    {
        context.Response.StatusCode = 429;
        context.Response.ContentType = "application/json";

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.RateLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds().ToString();
        context.Response.Headers["Retry-After"] = "60";

        var response = new RateLimitViolationResponse
        {
            Error = "Rate limit exceeded",
            Message = rateLimitInfo.LimitExceededReason ?? "Rate limit exceeded",
            RetryAfterSeconds = 60,
            Headers = new RateLimitHeaders
            {
                Limit = rateLimitInfo.RateLimit,
                Remaining = 0,
                ResetTime = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds(),
                RetryAfter = 60
            }
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}