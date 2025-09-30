using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.Services;
using ApiMonetizationGateway.Shared.DTOs;

namespace ApiMonetizationGateway.UserService.Middleware;

public class EnforceRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnforceRateLimitMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public EnforceRateLimitMiddleware(RequestDelegate next, ILogger<EnforceRateLimitMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Ensure user is authenticated and we have a user id
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();
        var redis = scope.ServiceProvider.GetRequiredService<IRedisService>();

        // Validate token exists in DB (token revocation support)
        var authHeader = context.Request.Headers["Authorization"].ToString();
        var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring("Bearer ".Length) : authHeader;
        var tokenRow = await db.UserTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token && t.IsActive && t.ExpiresAt > DateTime.UtcNow);
        if (tokenRow == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token invalid or revoked");
            return;
        }

        // Get user tier
        var userTier = await redis.GetAsync<UserTierInfoDto>($"user_tier:{userId}");
        if (userTier == null)
        {
            var userTierDb = await db.UserTiers.Include(ut => ut.Tier)
                .Where(ut => ut.UserId == userId && ut.IsActive)
                .OrderByDescending(ut => ut.AssignedAt)
                .FirstOrDefaultAsync();
            if (userTierDb?.Tier == null)
            {
                await _next(context);
                return;
            }
            userTier = new UserTierInfoDto
            {
                UserId = userId,
                TierId = userTierDb.TierId,
                TierName = userTierDb.Tier.Name,
                RateLimit = userTierDb.Tier.RateLimit,
                MonthlyQuota = (int)userTierDb.Tier.MonthlyQuota,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            await redis.SetAsync($"user_tier:{userId}", userTier, TimeSpan.FromDays(1));
        }

        // Monthly quota
        var monthKey = $"monthly_usage:{userId}:{DateTime.UtcNow:yyyyMM}";
        var monthCount = await redis.IncrementAsync(monthKey, 1, TimeSpan.FromDays(35));
        if (monthCount > userTier.MonthlyQuota)
        {
            await redis.DecrementAsync(monthKey, 1);
            await Write429(context, "Monthly quota has been reached", userTier.RateLimit);
            return;
        }

        // Per-second rate limit
        var secondKey = $"rate_limit:{userId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var secondCount = await redis.IncrementAsync(secondKey, 1, TimeSpan.FromSeconds(1));
        if (secondCount > userTier.RateLimit)
        {
            await redis.DecrementAsync(secondKey, 1);
            await Write429(context, "API rate limit per second has been reached", userTier.RateLimit);
            return;
        }

        await _next(context);
    }

    private static async Task Write429(HttpContext ctx, string message, int limit)
    {
        ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        ctx.Response.Headers["X-RateLimit-Remaining"] = "0";
        ctx.Response.Headers["Retry-After"] = "1";
        var payload = $"{{\"error\":\"Rate limit exceeded\",\"message\":\"{message}\"}}";
        await ctx.Response.WriteAsync(payload);
    }
}
