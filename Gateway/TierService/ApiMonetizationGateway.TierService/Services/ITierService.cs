using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.TierService.Services;

public interface ITierService
{
    Task<ApiResponse<TierDto>> CreateTierAsync(CreateTierRequest request);
    Task<ApiResponse<TierDto?>> GetTierByIdAsync(int id);
    Task<ApiResponse<TierDto?>> GetTierByNameAsync(string name);
    Task<ApiResponse<IEnumerable<TierDto>>> GetAllTiersAsync(bool includeInactive = false);
    Task<ApiResponse<TierDto>> UpdateTierAsync(int id, CreateTierRequest request);
    Task<ApiResponse<bool>> DeactivateTierAsync(int id);
    Task<ApiResponse<bool>> ActivateTierAsync(int id);
}