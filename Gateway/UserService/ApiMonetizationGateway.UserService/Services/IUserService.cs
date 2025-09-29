using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.UserService.Services;

public interface IUserService
{
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto?>> GetUserByIdAsync(int id);
    Task<ApiResponse<UserDto?>> GetUserByApiKeyAsync(string apiKey);
    Task<ApiResponse<UserDto?>> GetUserByEmailAsync(string email);
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, CreateUserRequest request);
    Task<ApiResponse<bool>> DeactivateUserAsync(int id);
    Task<ApiResponse<string>> RegenerateApiKeyAsync(int id);
    Task<ApiResponse<bool>> ValidateApiKeyAsync(string apiKey);
}