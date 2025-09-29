using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.UsageTrackingService.Services;

public interface IUsageTrackingService
{
    Task<ApiResponse<bool>> TrackUsageAsync(UsageTrackingRequest request);
    Task<ApiResponse<IEnumerable<ApiUsageDto>>> GetUsageByUserIdAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<IEnumerable<ApiUsageDto>>> GetUsageByEndpointAsync(string endpoint, DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<Dictionary<string, long>>> GetUsageStatsAsync(int userId, int year, int month);
    Task<ApiResponse<long>> GetMonthlyUsageCountAsync(int userId, int year, int month);
}