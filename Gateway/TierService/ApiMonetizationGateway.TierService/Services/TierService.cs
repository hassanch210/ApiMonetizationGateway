using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.TierService.Services;

public class TierService : ITierService
{
    private readonly ApiMonetizationContext _context;

    public TierService(ApiMonetizationContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TierDto>> CreateTierAsync(CreateTierRequest request)
    {
        try
        {
            // Check if tier name already exists
            if (await _context.Tiers.AnyAsync(t => t.Name == request.Name))
            {
                return ApiResponse<TierDto>.CreateError("Tier with this name already exists");
            }

            // Validate business rules
            if (request.MonthlyQuota <= 0)
            {
                return ApiResponse<TierDto>.CreateError("Monthly quota must be greater than 0");
            }

            if (request.RateLimit <= 0)
            {
                return ApiResponse<TierDto>.CreateError("Rate limit must be greater than 0");
            }

            if (request.MonthlyPrice < 0)
            {
                return ApiResponse<TierDto>.CreateError("Monthly price cannot be negative");
            }

            var tier = new Tier
            {
                Name = request.Name,
                Description = request.Description,
                MonthlyQuota = request.MonthlyQuota,
                RateLimit = request.RateLimit,
                MonthlyPrice = request.MonthlyPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tiers.Add(tier);
            await _context.SaveChangesAsync();

            return ApiResponse<TierDto>.CreateSuccess(MapToDto(tier), "Tier created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<TierDto>.CreateError($"Error creating tier: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TierDto?>> GetTierByIdAsync(int id)
    {
        try
        {
            var tier = await _context.Tiers.FindAsync(id);
            return ApiResponse<TierDto?>.CreateSuccess(tier != null ? MapToDto(tier) : null);
        }
        catch (Exception ex)
        {
            return ApiResponse<TierDto?>.CreateError($"Error retrieving tier: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TierDto?>> GetTierByNameAsync(string name)
    {
        try
        {
            var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Name == name);
            return ApiResponse<TierDto?>.CreateSuccess(tier != null ? MapToDto(tier) : null);
        }
        catch (Exception ex)
        {
            return ApiResponse<TierDto?>.CreateError($"Error retrieving tier: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<TierDto>>> GetAllTiersAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.Tiers.AsQueryable();
            
            if (!includeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            var tiers = await query
                .OrderBy(t => t.MonthlyPrice)
                .Select(t => MapToDto(t))
                .ToListAsync();

            return ApiResponse<IEnumerable<TierDto>>.CreateSuccess(tiers);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<TierDto>>.CreateError($"Error retrieving tiers: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TierDto>> UpdateTierAsync(int id, CreateTierRequest request)
    {
        try
        {
            var tier = await _context.Tiers.FindAsync(id);
            if (tier == null)
            {
                return ApiResponse<TierDto>.CreateError("Tier not found");
            }

            // Check if name already exists for another tier
            if (await _context.Tiers.AnyAsync(t => t.Name == request.Name && t.Id != id))
            {
                return ApiResponse<TierDto>.CreateError("Tier name already in use by another tier");
            }

            // Validate business rules
            if (request.MonthlyQuota <= 0)
            {
                return ApiResponse<TierDto>.CreateError("Monthly quota must be greater than 0");
            }

            if (request.RateLimit <= 0)
            {
                return ApiResponse<TierDto>.CreateError("Rate limit must be greater than 0");
            }

            if (request.MonthlyPrice < 0)
            {
                return ApiResponse<TierDto>.CreateError("Monthly price cannot be negative");
            }

            tier.Name = request.Name;
            tier.Description = request.Description;
            tier.MonthlyQuota = request.MonthlyQuota;
            tier.RateLimit = request.RateLimit;
            tier.MonthlyPrice = request.MonthlyPrice;
            tier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<TierDto>.CreateSuccess(MapToDto(tier), "Tier updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<TierDto>.CreateError($"Error updating tier: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeactivateTierAsync(int id)
    {
        try
        {
            var tier = await _context.Tiers.FindAsync(id);
            if (tier == null)
            {
                return ApiResponse<bool>.CreateError("Tier not found");
            }

            // Check if any users are using this tier via active UserTier associations
            var usersCount = await _context.UserTiers.CountAsync(ut => ut.TierId == id && ut.IsActive);
            if (usersCount > 0)
            {
                return ApiResponse<bool>.CreateError($"Cannot deactivate tier. {usersCount} active users are using this tier.");
            }

            tier.IsActive = false;
            tier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Tier deactivated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateError($"Error deactivating tier: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ActivateTierAsync(int id)
    {
        try
        {
            var tier = await _context.Tiers.FindAsync(id);
            if (tier == null)
            {
                return ApiResponse<bool>.CreateError("Tier not found");
            }

            tier.IsActive = true;
            tier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Tier activated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateError($"Error activating tier: {ex.Message}");
        }
    }

    private static TierDto MapToDto(Tier tier)
    {
        return new TierDto
        {
            Id = tier.Id,
            Name = tier.Name,
            Description = tier.Description,
            MonthlyQuota = tier.MonthlyQuota,
            RateLimit = tier.RateLimit,
            MonthlyPrice = tier.MonthlyPrice,
            IsActive = tier.IsActive
        };
    }
}