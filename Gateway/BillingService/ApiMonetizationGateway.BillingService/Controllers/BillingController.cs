using Microsoft.AspNetCore.Mvc;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;
using ApiMonetizationGateway.BillingService.Services;

namespace ApiMonetizationGateway.BillingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpPost("process/{userId}/{year}/{month}")]
    public async Task<ActionResult<ApiResponse<MonthlyUsageSummary>>> ProcessMonthlyBilling(int userId, int year, int month)
    {
        var result = await _billingService.ProcessMonthlyBillingAsync(userId, year, month);
        
        if (!result.Success)
            return BadRequest(result);
        
        return Ok(result);
    }

    [HttpGet("summaries/{userId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MonthlyUsageSummary>>>> GetBillingSummaries(int userId)
    {
        var result = await _billingService.GetBillingSummariesAsync(userId);
        return Ok(result);
    }

    [HttpGet("summary/{userId}/{year}/{month}")]
    public async Task<ActionResult<ApiResponse<MonthlyUsageSummary?>>> GetBillingSummary(int userId, int year, int month)
    {
        var result = await _billingService.GetBillingSummaryAsync(userId, year, month);
        return Ok(result);
    }

    [HttpGet("calculate/{userId}/{year}/{month}")]
    public async Task<ActionResult<ApiResponse<decimal>>> CalculateMonthlyBill(int userId, int year, int month)
    {
        var result = await _billingService.CalculateMonthlyBillAsync(userId, year, month);
        return Ok(result);
    }

    [HttpPost("mark-paid/{summaryId}")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkBillAsPaid(int summaryId)
    {
        var result = await _billingService.MarkBillAsPaidAsync(summaryId);
        
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.Contains("not found")) == true)
                return NotFound(result);
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    [HttpPost("process-all-pending")]
    public async Task<ActionResult<ApiResponse<string>>> ProcessAllPendingBilling()
    {
        await _billingService.ProcessAllPendingBillingAsync();
        return Ok(ApiResponse<string>.CreateSuccess("Processing initiated", "All pending billing processing has been initiated"));
    }
}