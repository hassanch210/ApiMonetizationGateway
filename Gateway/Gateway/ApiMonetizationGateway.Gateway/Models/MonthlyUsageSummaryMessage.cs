using System;

namespace ApiMonetizationGateway.Gateway.Models
{
    public class MonthlyUsageSummaryMessage
    {
        public int UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string EndpointPath { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public long TotalResponseTimeMs { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}