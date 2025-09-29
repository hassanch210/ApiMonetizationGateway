using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;
using System.Security.Cryptography;
using System.Text;

namespace ApiMonetizationGateway.UserService.Services;

public class UserService : IUserService
{
    private readonly ApiMonetizationContext _context;

    public UserService(ApiMonetizationContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return ApiResponse<UserDto>.CreateError("User with this email already exists");
            }

            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return ApiResponse<UserDto>.CreateSuccess(MapToDto(user), "User created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.CreateError($"Error creating user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto?>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            return ApiResponse<UserDto?>.CreateSuccess(user != null ? MapToDto(user) : null);
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto?>.CreateError($"Error retrieving user: {ex.Message}");
        }
    }

    // Removed API key lookup

    public async Task<ApiResponse<UserDto?>> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            return ApiResponse<UserDto?>.CreateSuccess(user != null ? MapToDto(user) : null);
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto?>.CreateError($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return ApiResponse<IEnumerable<UserDto>>.CreateSuccess(users);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<UserDto>>.CreateError($"Error retrieving users: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, CreateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return ApiResponse<UserDto>.CreateError("User not found");
            }

            // Check if email already exists for another user
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
            {
                return ApiResponse<UserDto>.CreateError("Email already in use by another user");
            }

            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<UserDto>.CreateSuccess(MapToDto(user), "User updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.CreateError($"Error updating user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeactivateUserAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ApiResponse<bool>.CreateError("User not found");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "User deactivated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateError($"Error deactivating user: {ex.Message}");
        }
    }

    // Removed API key regeneration

    // Removed API key validation

    // Removed API key generator

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ApiKey = null,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}