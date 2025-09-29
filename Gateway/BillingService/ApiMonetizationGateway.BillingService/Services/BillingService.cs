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
            var existingSummary = await _context.MonthlyUsageSummaries
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == year && s.Month == month);

            if (existingSummary != null)
            {
                return ApiResponse<MonthlyUsageSummary>.CreateError("Monthly summary already exists for this period");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<MonthlyUsageSummary>.CreateError("User not found");
            }

            var activeTierId = await _context.UserTiers
                .Where(ut => ut.UserId == userId && ut.IsActive)
                .OrderByDescending(ut => ut.AssignedAt)
                .Select(ut => ut.TierId)
                .FirstOrDefaultAsync();
            var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Id == activeTierId && t.IsActive);
            if (tier == null)
            {
                return ApiResponse<MonthlyUsageSummary>.CreateError("Active tier not found for user");
            }

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

            var endpointUsage = usages
                .GroupBy(u => u.Endpoint)
                .ToDictionary(g => g.Key, g => g.Count());

            var endpointUsageJson = JsonSerializer.Serialize(endpointUsage);

            var tierPrice = tier.MonthlyPrice;
            decimal calculatedCost = tierPrice;

            if (totalRequests > tier.MonthlyQuota)
            {
                var overageRequests = totalRequests - (long)tier.MonthlyQuota;
                var overageRate = 0.01m;
                calculatedCost += overageRequests * overageRate;
            }

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
            var activeTierId = await _context.UserTiers
                .Where(ut => ut.UserId == userId && ut.IsActive)
                .OrderByDescending(ut => ut.AssignedAt)
                .Select(ut => ut.TierId)
                .FirstOrDefaultAsync();
            var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Id == activeTierId && t.IsActive);
            if (tier == null)
            {
                return ApiResponse<decimal>.CreateError("Active tier not found for user");
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var totalRequests = await _context.ApiUsages
                .Where(u => u.UserId == userId && 
                           u.RequestTimestamp >= startDate && 
                           u.RequestTimestamp <= endDate)
                .CountAsync();

            decimal calculatedCost = tier.MonthlyPrice;

            if (totalRequests > tier.MonthlyQuota)
            {
                var overageRequests = totalRequests - (long)tier.MonthlyQuota;
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

            var users = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            var processedCount = 0;
            var errorCount = 0;

            foreach (var user in users)
            {
                try
                {
                    var exists = await _context.MonthlyUsageSummaries
                        .AnyAsync(s => s.UserId == user.Id && s.Year == lastMonth.Year && s.Month == lastMonth.Month);

                    if (!exists)
                    {
                        var result = await ProcessMonthlyBillingAsync(user.Id, lastMonth.Year, lastMonth.Month);
                        if (result.Success) processedCount++; else errorCount++;
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