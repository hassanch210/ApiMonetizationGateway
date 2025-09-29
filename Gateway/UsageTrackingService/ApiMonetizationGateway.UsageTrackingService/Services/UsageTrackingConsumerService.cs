using ApiMonetizationGateway.Shared.Services;

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
                    try
                    {
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
                        _logger.LogError(ex, "Error processing usage tracking message");
                    }
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
}