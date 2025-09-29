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
        // Extract API key from headers
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues))
        {
            await WriteUnauthorizedResponse(context, "API key is required");
            return;
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteUnauthorizedResponse(context, "Invalid API key");
            return;
        }

        // Get rate limit info from cache or database
        var rateLimitInfo = await GetRateLimitInfo(apiKey);
        if (rateLimitInfo == null)
        {
            await WriteUnauthorizedResponse(context, "Invalid API key");
            return;
        }

        // Check rate limits
        var rateLimitResult = await CheckRateLimits(rateLimitInfo);
        if (!rateLimitResult.IsWithinLimits)
        {
            await WriteRateLimitResponse(context, rateLimitResult);
            return;
        }

        // Add user info to context for downstream services
        context.Items["UserId"] = rateLimitInfo.UserId;
        context.Items["TierId"] = rateLimitInfo.TierId;

        // Track request start time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Continue to next middleware
        await _next(context);

        // Track usage after request completes
        stopwatch.Stop();
        _ = Task.Run(() => TrackUsage(context, rateLimitInfo.UserId, stopwatch.ElapsedMilliseconds));
    }

    private async Task<RateLimitInfo?> GetRateLimitInfo(string apiKey)
    {
        // Try to get from Redis cache first
        var cacheKey = $"rate_limit_info:{apiKey}";
        var cachedInfo = await _redisService.GetAsync<RateLimitInfo>(cacheKey);
        
        if (cachedInfo != null)
        {
            return cachedInfo;
        }

        // Get from database and cache it
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();
        
        var user = await context.Users
            .Include(u => u.Tier)
            .FirstOrDefaultAsync(u => u.ApiKey == apiKey && u.IsActive);

        if (user == null) return null;

        var currentMonth = DateTime.UtcNow;
        var monthlyUsage = await context.ApiUsages
            .Where(a => a.UserId == user.Id && 
                       a.RequestTimestamp.Year == currentMonth.Year &&
                       a.RequestTimestamp.Month == currentMonth.Month)
            .CountAsync();

        var rateLimitInfo = new RateLimitInfo
        {
            UserId = user.Id,
            ApiKey = apiKey,
            TierId = user.TierId,
            RateLimit = user.Tier.RateLimit,
            MonthlyQuota = user.Tier.MonthlyQuota,
            CurrentMonthUsage = monthlyUsage,
            IsWithinLimits = true
        };

        // Cache for 1 minute
        await _redisService.SetAsync(cacheKey, rateLimitInfo, TimeSpan.FromMinutes(1));

        return rateLimitInfo;
    }

    private async Task<RateLimitInfo> CheckRateLimits(RateLimitInfo info)
    {
        // Check monthly quota
        if (info.CurrentMonthUsage >= info.MonthlyQuota)
        {
            info.IsWithinLimits = false;
            info.LimitExceededReason = "Monthly quota exceeded";
            return info;
        }

        // Check per-second rate limit using sliding window
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
            using var scope = _serviceScopeFactory.CreateScope();
            var messageQueue = scope.ServiceProvider.GetService<IMessageQueueService>();
            
            if (messageQueue != null)
            {
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

                await messageQueue.PublishAsync("usage-tracking", usageMessage);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            Console.WriteLine($"Error tracking usage: {ex.Message}");
        }
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new { Error = message };
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static async Task WriteRateLimitResponse(HttpContext context, RateLimitInfo rateLimitInfo)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", rateLimitInfo.RateLimit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", "0");
        context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds().ToString());
        context.Response.Headers.Add("Retry-After", "60");

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