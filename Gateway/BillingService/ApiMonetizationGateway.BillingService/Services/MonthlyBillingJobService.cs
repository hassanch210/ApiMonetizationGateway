namespace ApiMonetizationGateway.BillingService.Services;

public class MonthlyBillingJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonthlyBillingJobService> _logger;

    public MonthlyBillingJobService(IServiceProvider serviceProvider, ILogger<MonthlyBillingJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monthly Billing Job Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run billing job at the beginning of each month
                var now = DateTime.UtcNow;
                var nextRunTime = GetNextRunTime(now);

                _logger.LogInformation("Next billing job scheduled for: {NextRunTime}", nextRunTime);

                var delayUntilNextRun = nextRunTime - now;
                if (delayUntilNextRun > TimeSpan.Zero)
                {
                    await Task.Delay(delayUntilNextRun, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                await RunBillingJob();

                // If we somehow get here before the next month, wait a day to prevent rapid re-execution
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Monthly Billing Job Service");
                // Wait 1 hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Monthly Billing Job Service stopped");
    }

    private async Task RunBillingJob()
    {
        try
        {
            _logger.LogInformation("Starting monthly billing job");

            using var scope = _serviceProvider.CreateScope();
            var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();

            await billingService.ProcessAllPendingBillingAsync();

            _logger.LogInformation("Monthly billing job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running monthly billing job");
        }
    }

    private static DateTime GetNextRunTime(DateTime now)
    {
        // Run on the 1st of each month at 2:00 AM UTC
        var nextMonth = now.Month == 12 ? new DateTime(now.Year + 1, 1, 1) : new DateTime(now.Year, now.Month + 1, 1);
        var nextRunTime = nextMonth.AddHours(2);

        // If we're already past this month's run time, schedule for next month
        if (now >= nextRunTime)
        {
            nextMonth = nextRunTime.Month == 12 ? new DateTime(nextRunTime.Year + 1, 1, 1) : new DateTime(nextRunTime.Year, nextRunTime.Month + 1, 1);
            nextRunTime = nextMonth.AddHours(2);
        }

        return nextRunTime;
    }
}