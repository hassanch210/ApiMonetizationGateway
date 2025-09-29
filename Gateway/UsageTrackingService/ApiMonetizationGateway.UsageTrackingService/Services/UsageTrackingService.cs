using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.UsageTrackingService.Services;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly ApiMonetizationContext _context;

    public UsageTrackingService(ApiMonetizationContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> TrackUsageAsync(UsageTrackingRequest request)
    {
        try
        {
            var apiUsage = new ApiUsage
            {
                UserId = request.UserId,
                Endpoint = request.Endpoint,
                HttpMethod = request.HttpMethod,
                ResponseStatusCode = request.ResponseStatusCode,
                ResponseTimeMs = request.ResponseTimeMs,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                RequestTimestamp = DateTime.UtcNow,
                Metadata = request.Metadata
            };

            _context.ApiUsages.Add(apiUsage);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Usage tracked successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateError($"Error tracking usage: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<ApiUsageDto>>> GetUsageByUserIdAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.ApiUsages.Where(u => u.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(u => u.RequestTimestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(u => u.RequestTimestamp <= endDate.Value);

            var usages = await query
                .OrderByDescending(u => u.RequestTimestamp)
                .Take(1000) // Limit to prevent large responses
                .Select(u => MapToDto(u))
                .ToListAsync();

            return ApiResponse<IEnumerable<ApiUsageDto>>.CreateSuccess(usages);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<ApiUsageDto>>.CreateError($"Error retrieving usage data: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<ApiUsageDto>>> GetUsageByEndpointAsync(string endpoint, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.ApiUsages.Where(u => u.Endpoint.Contains(endpoint));

            if (startDate.HasValue)
                query = query.Where(u => u.RequestTimestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(u => u.RequestTimestamp <= endDate.Value);

            var usages = await query
                .OrderByDescending(u => u.RequestTimestamp)
                .Take(1000)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return ApiResponse<IEnumerable<ApiUsageDto>>.CreateSuccess(usages);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<ApiUsageDto>>.CreateError($"Error retrieving usage data: {ex.Message}");
        }
    }

    public async Task<ApiResponse<Dictionary<string, long>>> GetUsageStatsAsync(int userId, int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var stats = await _context.ApiUsages
                .Where(u => u.UserId == userId && 
                           u.RequestTimestamp >= startDate && 
                           u.RequestTimestamp <= endDate)
                .GroupBy(u => u.Endpoint)
                .Select(g => new { Endpoint = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.Endpoint, x => x.Count);

            return ApiResponse<Dictionary<string, long>>.CreateSuccess(stats);
        }
        catch (Exception ex)
        {
            return ApiResponse<Dictionary<string, long>>.CreateError($"Error retrieving usage statistics: {ex.Message}");
        }
    }

    public async Task<ApiResponse<long>> GetMonthlyUsageCountAsync(int userId, int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var count = await _context.ApiUsages
                .Where(u => u.UserId == userId && 
                           u.RequestTimestamp >= startDate && 
                           u.RequestTimestamp <= endDate)
                .LongCountAsync();

            return ApiResponse<long>.CreateSuccess(count);
        }
        catch (Exception ex)
        {
            return ApiResponse<long>.CreateError($"Error retrieving monthly usage count: {ex.Message}");
        }
    }

    private static ApiUsageDto MapToDto(ApiUsage usage)
    {
        return new ApiUsageDto
        {
            Id = usage.Id,
            UserId = usage.UserId,
            Endpoint = usage.Endpoint,
            HttpMethod = usage.HttpMethod,
            ResponseStatusCode = usage.ResponseStatusCode,
            ResponseTimeMs = usage.ResponseTimeMs,
            RequestTimestamp = usage.RequestTimestamp
        };
    }
}