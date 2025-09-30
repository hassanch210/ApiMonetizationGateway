using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.Models;
using ApiMonetizationGateway.UserService.Services;
using Microsoft.AspNetCore.Authorization;

namespace ApiMonetizationGateway.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApiMonetizationContext _context;
    private readonly IJwtService _jwtService;

private readonly ApiMonetizationGateway.Shared.Services.IRedisService _redis;

public AuthController(ApiMonetizationContext context, IJwtService jwtService, ApiMonetizationGateway.Shared.Services.IRedisService redis)
    {
        _context = context;
        _jwtService = jwtService;
    _redis = redis;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(AuthResponse.CreateError("Email and password are required"));
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(AuthResponse.CreateError("User with this email already exists"));
            }

            var tier = await _context.Tiers.FindAsync(request.TierId);
            if (tier == null)
            {
                return BadRequest(AuthResponse.CreateError("Invalid tier selected"));
            }

            var user = new User
            {
                Email = request.Email.ToLower(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);


            var userTier = new UserTier
            {
                User = user,
                TierId = request.TierId,
                AssignedAt = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                UpdatedBy = "System",
                IsActive = true,
                Notes = "Initial tier assignment during registration"
            };

            _context.UserTiers.Add(userTier);
          
            await _context.SaveChangesAsync();

            // Do not issue JWT on registration; instruct to login for token
            return Ok(new AuthResponse { Success = true, Message = "Registration successful. Please login to obtain a token." });
        }
        catch (Exception e)
        {
            return Ok();
        }
        
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(AuthResponse.CreateError("Email and password are required"));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized(AuthResponse.CreateError("Invalid email or password"));
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(AuthResponse.CreateError("Invalid email or password"));
        }

        
        var token = _jwtService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddDays(1);

        // Persist token
        _context.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Token = token,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ApiKey = null,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };

        // Cache user profile for rate limiting in Redis (1 day)
        var activeTierId = await _context.UserTiers
            .Where(ut => ut.UserId == user.Id && ut.IsActive)
            .OrderByDescending(ut => ut.AssignedAt)
            .Select(ut => ut.TierId)
            .FirstOrDefaultAsync();
        var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Id == activeTierId && t.IsActive);
        if (tier != null)
        {
            var cacheKey = $"rate_limit_info_user:{user.Id}";
            var info = new RateLimitInfo
            {
                UserId = user.Id,
                ApiKey = string.Empty,
                TierId = activeTierId,
                RateLimit = tier.RateLimit,
                MonthlyQuota = tier.MonthlyQuota,
                CurrentMonthUsage = 0,
                IsWithinLimits = true
            };
            await _redis.SetAsync(cacheKey, info, TimeSpan.FromDays(1));
        }

        return Ok(AuthResponse.CreateSuccess(token, expiresAt, userDto));
    }
}