using ApiMonetizationGateway.Shared.Services;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.Models;
using ApiMonetizationGateway.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiMonetizationGateway.UsageTrackingService.Services;

public class UsageTrackingConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UsageTrackingConsumerService> _logger;

    public UsageTrackingConsumerService(IServiceProvider serviceProvider, ILogger<UsageTrackingConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Usage Tracking Consumer Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                var usageTrackingService = scope.ServiceProvider.GetRequiredService<IUsageTrackingService>();

                // Subscribe to usage tracking messages
                messageQueue.Subscribe<UsageTrackingMessage>("usage-tracking", async (message) =>
                {
                    await ProcessUsageTrackingMessage(message);
                });

                // Subscribe to monthly usage summary messages
                messageQueue.Subscribe<MonthlyUsageSummaryMessage>("monthly-usage-summary", async (message) =>
                {
                    await ProcessMonthlyUsageSummaryMessage(message);
                });

                // Keep the service running
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Usage Tracking Consumer Service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Usage Tracking Consumer Service stopped");
    }

    private async Task ProcessUsageTrackingMessage(UsageTrackingMessage message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var usageTrackingService = scope.ServiceProvider.GetRequiredService<IUsageTrackingService>();
            
            _logger.LogDebug("Processing usage tracking message for User ID: {UserId}, Endpoint: {Endpoint}", 
                message.UserId, message.Endpoint);

            var request = new Shared.DTOs.UsageTrackingRequest
            {
                UserId = message.UserId,
                Endpoint = message.Endpoint,
                HttpMethod = message.HttpMethod,
                ResponseStatusCode = message.ResponseStatusCode,
                ResponseTimeMs = message.ResponseTimeMs,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent,
                Metadata = message.Metadata
            };

            var result = await usageTrackingService.TrackUsageAsync(request);
            if (!result.Success)
            {
                _logger.LogError("Failed to track usage: {Errors}", string.Join(", ", result.Errors ?? new List<string>()));
            }
            else
            {
                _logger.LogDebug("Successfully tracked usage for User ID: {UserId}", message.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing usage tracking message for User ID: {UserId}", message.UserId);
        }
    }

    private async Task ProcessMonthlyUsageSummaryMessage(MonthlyUsageSummaryMessage message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();
            
            _logger.LogDebug("Processing monthly usage summary message for User ID: {UserId}, Year: {Year}, Month: {Month}", 
                message.UserId, message.Year, message.Month);

            // Find or create monthly usage summary
            var summary = await context.MonthlyUsageSummaries
                .FirstOrDefaultAsync(s => s.UserId == message.UserId && s.Year == message.Year && s.Month == message.Month);

            if (summary == null)
            {
                // Get user's current tier for pricing
                var userTier = await context.UserTiers
                    .Include(ut => ut.Tier)
                    .Where(ut => ut.UserId == message.UserId && ut.IsActive)
                    .OrderByDescending(ut => ut.AssignedAt)
                    .FirstOrDefaultAsync();

                summary = new MonthlyUsageSummary
                {
                    UserId = message.UserId,
                    Year = message.Year,
                    Month = message.Month,
                    TotalRequests = 0,
                    SuccessfulRequests = 0,
                    FailedRequests = 0,
                    TierPrice = userTier?.Tier?.MonthlyPrice ?? 0m,
                    CalculatedCost = 0m,
                    ProcessedAt = DateTime.UtcNow,
                    EndpointUsageJson = "{}"
                };
                context.MonthlyUsageSummaries.Add(summary);
            }

            // Update totals
            summary.TotalRequests += message.RequestCount;
            if (message.EndpointPath.Contains("2")) // Check for 2xx success codes (simplified)
            {
                summary.SuccessfulRequests += message.RequestCount;
            }
            else
            {
                summary.FailedRequests += message.RequestCount;
            }
            
            // Update endpoint usage JSON
            var endpointUsage = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(summary.EndpointUsageJson))
            {
                try
                {
                    endpointUsage = JsonSerializer.Deserialize<Dictionary<string, object>>(summary.EndpointUsageJson) 
                                    ?? new Dictionary<string, object>();
                }
                catch
                {
                    endpointUsage = new Dictionary<string, object>();
                }
            }

            var endpointKey = message.EndpointPath ?? "unknown";
            if (endpointUsage.ContainsKey(endpointKey))
            {
                var currentCount = Convert.ToInt64(endpointUsage[endpointKey]);
                endpointUsage[endpointKey] = currentCount + message.RequestCount;
            }
            else
            {
                endpointUsage[endpointKey] = message.RequestCount;
            }

            summary.EndpointUsageJson = JsonSerializer.Serialize(endpointUsage);
            summary.ProcessedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            
            _logger.LogDebug("Successfully updated monthly usage summary for User ID: {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing monthly usage summary message for User ID: {UserId}", message.UserId);
        }
    }
}
