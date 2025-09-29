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
    [HttpGet("summary/{userId}/{year}/{month}")]
    public async Task<ActionResult<ApiResponse<MonthlyUsageSummary?>>> GetBillingSummary(int userId, int year, int month)
    {
        var result = await _billingService.GetBillingSummaryAsync(userId, year, month);
        return Ok(result);
    }

}