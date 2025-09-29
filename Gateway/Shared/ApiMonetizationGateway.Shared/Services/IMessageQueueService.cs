namespace ApiMonetizationGateway.Shared.Services;

public interface IMessageQueueService
{
    Task PublishAsync<T>(string queueName, T message) where T : class;
    Task<T?> ConsumeAsync<T>(string queueName) where T : class;
    void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class;
    Task DeclareQueueAsync(string queueName, bool durable = true);
}

public class UsageTrackingMessage
{
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }
}

public class BillingProcessingMessage
{
    public int UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}