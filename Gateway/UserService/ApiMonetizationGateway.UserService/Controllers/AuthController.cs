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

    public AuthController(ApiMonetizationContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(AuthResponse.CreateError("Email and password are required"));
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return BadRequest(AuthResponse.CreateError("User with this email already exists"));
        }

        // Validate tier exists
        var tier = await _context.Tiers.FindAsync(request.TierId);
        if (tier == null)
        {
            return BadRequest(AuthResponse.CreateError("Invalid tier selected"));
        }

        // Create new user
        var user = new User
        {
            Email = request.Email.ToLower(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            ApiKey = Guid.NewGuid().ToString("N"), // Generate API key for backward compatibility
            TierId = request.TierId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);

        // Create user tier assignment
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

        // Load tier information for response
        await _context.Entry(user).Reference(u => u.Tier).LoadAsync();

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ApiKey = user.ApiKey,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Tier = new TierDto
            {
                Id = tier.Id,
                Name = tier.Name,
                Description = tier.Description,
                MonthlyQuota = tier.MonthlyQuota,
                RateLimit = tier.RateLimit,
                MonthlyPrice = tier.MonthlyPrice,
                IsActive = tier.IsActive
            }
        };

        return Ok(AuthResponse.CreateSuccess(token, expiresAt, userDto));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(AuthResponse.CreateError("Email and password are required"));
        }

        // Find user
        var user = await _context.Users
            .Include(u => u.Tier)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized(AuthResponse.CreateError("Invalid email or password"));
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(AuthResponse.CreateError("Invalid email or password"));
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ApiKey = user.ApiKey!,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Tier = new TierDto
            {
                Id = user.Tier.Id,
                Name = user.Tier.Name,
                Description = user.Tier.Description,
                MonthlyQuota = user.Tier.MonthlyQuota,
                RateLimit = user.Tier.RateLimit,
                MonthlyPrice = user.Tier.MonthlyPrice,
                IsActive = user.Tier.IsActive
            }
        };

        return Ok(AuthResponse.CreateSuccess(token, expiresAt, userDto));
    }
}