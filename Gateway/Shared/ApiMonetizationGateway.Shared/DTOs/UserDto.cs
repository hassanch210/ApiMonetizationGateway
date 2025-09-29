namespace ApiMonetizationGateway.Shared.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Deprecated
    public string? ApiKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class TierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long MonthlyQuota { get; set; }
    public int RateLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long MonthlyQuota { get; set; }
    public int RateLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
}

public class ApiUsageDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime RequestTimestamp { get; set; }
}

public class UsageTrackingRequest
{
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
}