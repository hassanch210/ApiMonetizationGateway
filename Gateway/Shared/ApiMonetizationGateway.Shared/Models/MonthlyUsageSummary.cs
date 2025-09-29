using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Shared.Models;

public class MonthlyUsageSummary
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public int Year { get; set; }
    public int Month { get; set; }
    
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    
    public decimal CalculatedCost { get; set; }
    public decimal TierPrice { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public bool IsBilled { get; set; } = false;
    
    // Summary by endpoint
    public string? EndpointUsageJson { get; set; }
}