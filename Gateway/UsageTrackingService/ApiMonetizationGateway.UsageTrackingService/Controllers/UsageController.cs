using Microsoft.AspNetCore.Mvc;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.UsageTrackingService.Services;

namespace ApiMonetizationGateway.UsageTrackingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsageController : ControllerBase
{
    private readonly IUsageTrackingService _usageTrackingService;

    public UsageController(IUsageTrackingService usageTrackingService)
    {
        _usageTrackingService = usageTrackingService;
    }

    [HttpPost("track")]
    public async Task<ActionResult<ApiResponse<bool>>> TrackUsage([FromBody] UsageTrackingRequest request)
    {
        var result = await _usageTrackingService.TrackUsageAsync(request);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ApiUsageDto>>>> GetUsageByUserId(
        int userId, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _usageTrackingService.GetUsageByUserIdAsync(userId, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("endpoint/{endpoint}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ApiUsageDto>>>> GetUsageByEndpoint(
        string endpoint, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _usageTrackingService.GetUsageByEndpointAsync(endpoint, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("stats/{userId}/{year}/{month}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, long>>>> GetUsageStats(int userId, int year, int month)
    {
        var result = await _usageTrackingService.GetUsageStatsAsync(userId, year, month);
        return Ok(result);
    }

    [HttpGet("monthly-count/{userId}/{year}/{month}")]
    public async Task<ActionResult<ApiResponse<long>>> GetMonthlyUsageCount(int userId, int year, int month)
    {
        var result = await _usageTrackingService.GetMonthlyUsageCountAsync(userId, year, month);
        return Ok(result);
    }
}