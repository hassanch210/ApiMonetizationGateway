using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ApiMonetizationGateway.Gateway.Middleware;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ApiMonetizationGateway.Tests;

public class RateLimitingMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext(int userId, UserTierInfoDto? userTier = null, string path = "/api/users")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        if (userTier != null)
        {
            context.Items["UserTier"] = userTier;
        }
        return context;
    }

    [Fact]
    public async Task WithinLimits_AllowsRequest()
    {
        // Arrange
        var userId = 123;
        var tier = new UserTierInfoDto { UserId = userId, TierId = 1, RateLimit = 5, MonthlyQuota = 100, TierName = "Free", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var context = CreateHttpContext(userId, tier);

        var redis = new Mock<IRedisService>();
        // Monthly increment within quota
        redis.Setup(r => r.IncrementAsync(It.Is<string>(k => k.StartsWith($"monthly_usage:{userId}:")), 1, It.IsAny<TimeSpan?>()))
             .ReturnsAsync(1);
        // Per-second increment within rate
        redis.Setup(r => r.IncrementAsync(It.Is<string>(k => k.StartsWith($"rate_limit:{userId}:")), 1, It.IsAny<TimeSpan?>()))
             .ReturnsAsync(1);

        var mq = new Mock<IMessageQueueService>();
        var scopeFactory = new Mock<IServiceScopeFactory>(); // not used because userTier provided

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; ctx.Response.StatusCode = StatusCodes.Status200OK; return Task.CompletedTask; };

        var sut = new RateLimitingMiddleware(next, scopeFactory.Object, redis.Object, mq.Object);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ExceedMonthlyQuota_Returns429_AndDecrementsCounter()
    {
        // Arrange
        var userId = 456;
        var tier = new UserTierInfoDto { UserId = userId, TierId = 1, RateLimit = 10, MonthlyQuota = 2, TierName = "Free", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var context = CreateHttpContext(userId, tier);

        var redis = new Mock<IRedisService>();
        // Monthly count exceeds quota
        redis.Setup(r => r.IncrementAsync(It.Is<string>(k => k.StartsWith($"monthly_usage:{userId}:")), 1, It.IsAny<TimeSpan?>()))
             .ReturnsAsync(3);
        var monthKeyCaptured = string.Empty;
        redis.Setup(r => r.DecrementAsync(It.IsAny<string>(), 1))
             .Callback<string, long>((k, v) => monthKeyCaptured = k)
             .ReturnsAsync(2);
        // Rate limit shouldn't be checked once monthly exceeded, but safe to setup
        redis.Setup(r => r.IncrementAsync(It.Is<string>(k => k.StartsWith($"rate_limit:{userId}:")), 1, It.IsAny<TimeSpan?>()))
             .ReturnsAsync(1);

        var mq = new Mock<IMessageQueueService>();
        var scopeFactory = new Mock<IServiceScopeFactory>();

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        var sut = new RateLimitingMiddleware(next, scopeFactory.Object, redis.Object, mq.Object);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("Retry-After"));
Assert.StartsWith($"monthly_usage:{userId}:", monthKeyCaptured);
    }

    [Fact]
    public async Task ExceedPerSecondRate_Returns429_AndDecrementsSecondCounter()
    {
        // Arrange
        var userId = 789;
        var tier = new UserTierInfoDto { UserId = userId, TierId = 1, RateLimit = 2, MonthlyQuota = 100, TierName = "Free", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var context = CreateHttpContext(userId, tier);

        var redis = new Mock<IRedisService>();
        // Monthly increment under quota
        redis.Setup(r => r.IncrementAsync(It.Is<string>(k => k.StartsWith($"monthly_usage:{userId}:")), 1, It.IsAny<TimeSpan?>()))
             .ReturnsAsync(1);
        // Per-second exceeds rate
        var decrementedKey = string.Empty;
        redis.Setup(r => r.IncrementAsync(It.Is<string>(k => k.StartsWith($"rate_limit:{userId}:")), 1, It.IsAny<TimeSpan?>()))
             .ReturnsAsync(3);
        redis.Setup(r => r.DecrementAsync(It.IsAny<string>(), 1))
             .Callback<string, long>((k, v) => decrementedKey = k)
             .ReturnsAsync(2);

        var mq = new Mock<IMessageQueueService>();
        var scopeFactory = new Mock<IServiceScopeFactory>();

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        var sut = new RateLimitingMiddleware(next, scopeFactory.Object, redis.Object, mq.Object);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Contains("rate_limit:", decrementedKey);
        Assert.True(context.Response.Headers.ContainsKey("Retry-After"));
    }
}
