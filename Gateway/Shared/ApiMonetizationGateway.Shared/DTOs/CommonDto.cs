namespace ApiMonetizationGateway.Shared.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> CreateSuccess(T data, string? message = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> CreateError(string error)
    {
        return new ApiResponse<T> { Success = false, Errors = new List<string> { error } };
    }

    public static ApiResponse<T> CreateError(List<string> errors)
    {
        return new ApiResponse<T> { Success = false, Errors = errors };
    }
}

public class RateLimitInfo
{
    public int UserId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public int TierId { get; set; }
    public int RateLimit { get; set; } // requests per second
    public long MonthlyQuota { get; set; } // requests per month
    public long CurrentMonthUsage { get; set; }
    public bool IsWithinLimits { get; set; }
    public string? LimitExceededReason { get; set; }
}

public class RateLimitViolationResponse
{
    public string Error { get; set; } = "Rate limit exceeded";
    public string Message { get; set; } = string.Empty;
    public int RetryAfterSeconds { get; set; }
    public RateLimitHeaders Headers { get; set; } = new();
}

public class RateLimitHeaders
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public long ResetTime { get; set; }
    public int RetryAfter { get; set; }
}