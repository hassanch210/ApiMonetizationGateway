using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Shared.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    // Password for JWT authentication
    public string? PasswordHash { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<ApiUsage> ApiUsages { get; set; } = new List<ApiUsage>();
    public virtual ICollection<MonthlyUsageSummary> MonthlyUsageSummaries { get; set; } = new List<MonthlyUsageSummary>();
    public virtual ICollection<UserTier> UserTiers { get; set; } = new List<UserTier>();
}