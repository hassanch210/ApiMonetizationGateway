using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Shared.Models;

public class ApiUsage
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string Endpoint { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;
    
    public int ResponseStatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(255)]
    public string? UserAgent { get; set; }
    
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    
    // Additional metadata as JSON
    public string? Metadata { get; set; }
}