using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Shared.Models;

public class Tier
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    public long MonthlyQuota { get; set; } // requests per month
    public int RateLimit { get; set; } // requests per second
    public decimal MonthlyPrice { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}