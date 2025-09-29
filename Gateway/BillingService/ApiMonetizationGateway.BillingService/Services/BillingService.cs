using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;
using System.Text.Json;

namespace ApiMonetizationGateway.BillingService.Services;

public class BillingService : IBillingService
{
    private readonly ApiMonetizationContext _context;
    private readonly ILogger<BillingService> _logger;

    public BillingService(ApiMonetizationContext context, ILogger<BillingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<MonthlyUsageSummary>> ProcessMonthlyBillingAsync(int userId, int year, int month)
    {
        try
        {
            // Check if summary already exists
            var existingSummary = await _context.MonthlyUsageSummaries
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == year && s.Month == month);

            if (existingSummary != null)
            {
                return ApiResponse<MonthlyUsageSummary>.CreateError("Monthly summary already exists for this period");
            }

            // Get user and tier information
            var user = await _context.Users
                .Include(u => u.Tier)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<MonthlyUsageSummary>.CreateError("User not found");
            }

            // Calculate usage statistics
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var usages = await _context.ApiUsages
                .Where(u => u.UserId == userId && 
                           u.RequestTimestamp >= startDate && 
                           u.RequestTimestamp <= endDate)
                .ToListAsync();

            var totalRequests = usages.Count;
            var successfulRequests = usages.Count(u => u.ResponseStatusCode >= 200 && u.ResponseStatusCode < 400);
            var failedRequests = totalRequests - successfulRequests;

            // Calculate endpoint usage statistics
            var endpointUsage = usages
                .GroupBy(u => u.Endpoint)
                .ToDictionary(g => g.Key, g => g.Count());

            var endpointUsageJson = JsonSerializer.Serialize(endpointUsage);

            // Calculate costs
            var tierPrice = user.Tier.MonthlyPrice;
            decimal calculatedCost = tierPrice; // Base tier price

            // Could add additional costs based on usage if needed
            // For example: overage fees for exceeding quota
            if (totalRequests > user.Tier.MonthlyQuota)
            {
                var overageRequests = totalRequests - (long)user.Tier.MonthlyQuota;
                var overageRate = 0.01m; // $0.01 per overage request
                calculatedCost += overageRequests * overageRate;
            }

            // Create monthly summary
            var summary = new MonthlyUsageSummary
            {
                UserId = userId,
                Year = year,
                Month = month,
                TotalRequests = totalRequests,
                SuccessfulRequests = successfulRequests,
                FailedRequests = failedRequests,
                CalculatedCost = calculatedCost,
                TierPrice = tierPrice,
                ProcessedAt = DateTime.UtcNow,
                IsBilled = false,
                EndpointUsageJson = endpointUsageJson
            };

            _context.MonthlyUsageSummaries.Add(summary);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed monthly billing for User ID {UserId} for {Year}-{Month}. Total requests: {TotalRequests}, Cost: ${CalculatedCost:F2}",
                userId, year, month, totalRequests, calculatedCost);

            return ApiResponse<MonthlyUsageSummary>.CreateSuccess(summary, "Monthly billing processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing monthly billing for User ID {UserId} for {Year}-{Month}", userId, year, month);
            return ApiResponse<MonthlyUsageSummary>.CreateError($"Error processing monthly billing: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<MonthlyUsageSummary>>> GetBillingSummariesAsync(int userId)
    {
        try
        {
            var summaries = await _context.MonthlyUsageSummaries
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ToListAsync();

            return ApiResponse<IEnumerable<MonthlyUsageSummary>>.CreateSuccess(summaries);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<MonthlyUsageSummary>>.CreateError($"Error retrieving billing summaries: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MonthlyUsageSummary?>> GetBillingSummaryAsync(int userId, int year, int month)
    {
        try
        {
            var summary = await _context.MonthlyUsageSummaries
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == year && s.Month == month);

            return ApiResponse<MonthlyUsageSummary?>.CreateSuccess(summary);
        }
        catch (Exception ex)
        {
            return ApiResponse<MonthlyUsageSummary?>.CreateError($"Error retrieving billing summary: {ex.Message}");
        }
    }

    public async Task<ApiResponse<decimal>> CalculateMonthlyBillAsync(int userId, int year, int month)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Tier)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<decimal>.CreateError("User not found");
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var totalRequests = await _context.ApiUsages
                .Where(u => u.UserId == userId && 
                           u.RequestTimestamp >= startDate && 
                           u.RequestTimestamp <= endDate)
                .CountAsync();

            decimal calculatedCost = user.Tier.MonthlyPrice;

            // Add overage fees if applicable
            if (totalRequests > user.Tier.MonthlyQuota)
            {
                var overageRequests = totalRequests - (long)user.Tier.MonthlyQuota;
                var overageRate = 0.01m;
                calculatedCost += overageRequests * overageRate;
            }

            return ApiResponse<decimal>.CreateSuccess(calculatedCost);
        }
        catch (Exception ex)
        {
            return ApiResponse<decimal>.CreateError($"Error calculating monthly bill: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> MarkBillAsPaidAsync(int summaryId)
    {
        try
        {
            var summary = await _context.MonthlyUsageSummaries.FindAsync(summaryId);
            if (summary == null)
            {
                return ApiResponse<bool>.CreateError("Billing summary not found");
            }

            summary.IsBilled = true;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Bill marked as paid successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateError($"Error marking bill as paid: {ex.Message}");
        }
    }

    public async Task ProcessAllPendingBillingAsync()
    {
        try
        {
            _logger.LogInformation("Starting processing of all pending billing");

            var now = DateTime.UtcNow;
            var lastMonth = now.AddMonths(-1);

            // Get all active users
            var users = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            var processedCount = 0;
            var errorCount = 0;

            foreach (var user in users)
            {
                try
                {
                    // Check if billing for last month already exists
                    var existingSummary = await _context.MonthlyUsageSummaries
                        .AnyAsync(s => s.UserId == user.Id && s.Year == lastMonth.Year && s.Month == lastMonth.Month);

                    if (!existingSummary)
                    {
                        var result = await ProcessMonthlyBillingAsync(user.Id, lastMonth.Year, lastMonth.Month);
                        if (result.Success)
                        {
                            processedCount++;
                        }
                        else
                        {
                            errorCount++;
                            _logger.LogWarning("Failed to process billing for User ID {UserId}: {Errors}", 
                                user.Id, string.Join(", ", result.Errors ?? new List<string>()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Error processing billing for User ID {UserId}", user.Id);
                }
            }

            _logger.LogInformation("Completed processing pending billing. Processed: {ProcessedCount}, Errors: {ErrorCount}", 
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessAllPendingBillingAsync");
        }
    }
}