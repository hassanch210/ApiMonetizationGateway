using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.BillingService.Services;

public interface IBillingService
{
    Task<ApiResponse<MonthlyUsageSummary>> ProcessMonthlyBillingAsync(int userId, int year, int month);
    Task<ApiResponse<IEnumerable<MonthlyUsageSummary>>> GetBillingSummariesAsync(int userId);
    Task<ApiResponse<MonthlyUsageSummary?>> GetBillingSummaryAsync(int userId, int year, int month);
    Task<ApiResponse<decimal>> CalculateMonthlyBillAsync(int userId, int year, int month);
    Task<ApiResponse<bool>> MarkBillAsPaidAsync(int summaryId);
    Task ProcessAllPendingBillingAsync();
}