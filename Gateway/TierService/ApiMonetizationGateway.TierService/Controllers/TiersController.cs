using Microsoft.AspNetCore.Mvc;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.TierService.Services;

namespace ApiMonetizationGateway.TierService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TiersController : ControllerBase
{
    private readonly ITierService _tierService;

    public TiersController(ITierService tierService)
    {
        _tierService = tierService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TierDto>>> CreateTier([FromBody] CreateTierRequest request)
    {
        var result = await _tierService.CreateTierAsync(request);
        
        if (result.Success)
            return CreatedAtAction(nameof(GetTier), new { id = result.Data!.Id }, result);
        
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TierDto?>>> GetTier(int id)
    {
        var result = await _tierService.GetTierByIdAsync(id);
        
        if (result.Success && result.Data == null)
            return NotFound();
        
        return Ok(result);
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<ApiResponse<TierDto?>>> GetTierByName(string name)
    {
        var result = await _tierService.GetTierByNameAsync(name);
        
        if (result.Success && result.Data == null)
            return NotFound();
        
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TierDto>>>> GetAllTiers([FromQuery] bool includeInactive = false)
    {
        var result = await _tierService.GetAllTiersAsync(includeInactive);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TierDto>>> UpdateTier(int id, [FromBody] CreateTierRequest request)
    {
        var result = await _tierService.UpdateTierAsync(id, request);
        
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.Contains("not found")) == true)
                return NotFound(result);
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateTier(int id)
    {
        var result = await _tierService.DeactivateTierAsync(id);
        
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.Contains("not found")) == true)
                return NotFound(result);
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> ActivateTier(int id)
    {
        var result = await _tierService.ActivateTierAsync(id);
        
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.Contains("not found")) == true)
                return NotFound(result);
            return BadRequest(result);
        }
        
        return Ok(result);
    }
}