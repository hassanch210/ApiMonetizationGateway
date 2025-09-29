using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Shared.Models;

public class UserTier
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public int TierId { get; set; }
    public virtual Tier Tier { get; set; } = null!;
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}