namespace ApiMonetizationGateway.Shared.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TierId { get; set; } = 1; // Default to Free tier
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static AuthResponse CreateSuccess(string token, DateTime expiresAt, UserDto user)
    {
        return new AuthResponse 
        { 
            Success = true, 
            Token = token, 
            ExpiresAt = expiresAt,
            User = user
        };
    }

    public static AuthResponse CreateError(string error)
    {
        return new AuthResponse { Success = false, Errors = new List<string> { error } };
    }

    public static AuthResponse CreateError(List<string> errors)
    {
        return new AuthResponse { Success = false, Errors = errors };
    }
}

public class UserTierDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TierId { get; set; }
    public TierDto Tier { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public DateTime UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class AssignTierRequest
{
    public int UserId { get; set; }
    public int TierId { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Notes { get; set; }
}