namespace ApiMonetizationGateway.Shared.DTOs;

public class UserTierInfoDto
{
    public int UserId { get; set; }
    public int TierId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int RateLimit { get; set; }
    public int MonthlyQuota { get; set; }
    public DateTime ExpiresAt { get; set; }
}