using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.UserService.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    int? GetUserIdFromToken(string token);
}