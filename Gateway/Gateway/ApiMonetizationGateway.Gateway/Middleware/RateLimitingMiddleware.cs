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
        // Monthly quota via Redis counter per month
        var monthKey = $"monthly_usage:{info.UserId}:{DateTime.UtcNow:yyyyMM}";
        var monthCount = await _redisService.IncrementAsync(monthKey, 1, TimeSpan.FromDays(35));
        if (monthCount > info.MonthlyQuota)
        {
            info.IsWithinLimits = false;
            info.LimitExceededReason = "Monthly quota exceeded";
            return info;
        }

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
            Console.WriteLine($"Error tracking usage: {ex.Message}");
        }
    }

    private static async Task WriteRateLimitResponse(HttpContext context, RateLimitInfo rateLimitInfo)
    {
        context.Response.StatusCode = 429;
        context.Response.ContentType = "application/json";

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