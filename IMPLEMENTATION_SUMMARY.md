# API Monetization Gateway - Enhanced JWT Authentication & Rate Limiting

## Implementation Summary

This document summarizes the comprehensive implementation of JWT authentication, rate limiting, quota enforcement, and usage tracking for the API Monetization Gateway microservices architecture.

## Features Implemented

### 1. JWT Authentication
- ✅ **Token Generation**: JWT tokens generated during user login
- ✅ **Token Validation**: Tokens validated against SQL Server database (UserTokens table)
- ✅ **Token Storage**: Tokens stored in database with expiration tracking
- ✅ **Redis Caching**: Valid tokens cached in Redis for performance

### 2. Rate Limiting & Quota Enforcement
- ✅ **Per-Second Rate Limiting**: Based on user tier (e.g., Free: 2 req/sec, Pro: 10 req/sec)
- ✅ **Monthly Quota Limits**: Based on user tier (e.g., Free: 100 req/month, Pro: 100,000 req/month)
- ✅ **HTTP 429 Responses**: Proper rate limit exceeded responses with retry-after headers
- ✅ **Dynamic Configuration**: Tier-based limits stored in database and cached in Redis

### 3. User Tier Management
- ✅ **Tier Caching**: User tier information cached in Redis during login
- ✅ **Monthly Usage Tracking**: Current month usage cached and tracked in Redis
- ✅ **Database Integration**: User-tier relationships managed via UserTiers table

### 4. API Usage Tracking
- ✅ **RabbitMQ Integration**: Usage events published to message queues
- ✅ **Real-time Tracking**: Individual API calls logged to ApiUsages table
- ✅ **Monthly Summaries**: Aggregated usage data in MonthlyUsageSummaries table
- ✅ **Background Processing**: Consumer services process usage data asynchronously

## Architecture Components

### Database Schema
```sql
-- Core Tables
Users (Id, Email, FirstName, LastName, PasswordHash, CreatedAt, UpdatedAt, IsActive)
Tiers (Id, Name, Description, MonthlyQuota, RateLimit, MonthlyPrice, CreatedAt, UpdatedAt, IsActive)
UserTiers (Id, UserId, TierId, AssignedAt, UpdatedOn, UpdatedBy, IsActive, Notes)
UserTokens (Id, UserId, Token, IssuedAt, ExpiresAt, RevokedAt, IsActive)
ApiUsages (Id, UserId, Endpoint, HttpMethod, ResponseStatusCode, ResponseTimeMs, IpAddress, UserAgent, RequestTimestamp, Metadata)
MonthlyUsageSummaries (Id, UserId, Year, Month, TotalRequests, SuccessfulRequests, FailedRequests, CalculatedCost, TierPrice, ProcessedAt, IsBilled, EndpointUsageJson)
```

### Redis Cache Keys
```
user_tier:<userId>                    - User tier information
monthly_usage:<userId>:<YYYYMM>       - Monthly usage counter
rate_limit:<userId>:<UnixTimestamp>   - Per-second rate limiting
valid_token:<userId>                  - Valid JWT token cache
```

### RabbitMQ Queues
```
usage-tracking          - Individual API usage events
monthly-usage-summary   - Monthly usage summary updates
```

## Services Overview

### 1. Gateway Service (Port 5000)
- **JWT Authentication**: Validates tokens against database
- **Rate Limiting**: Enforces per-second and monthly limits
- **Request Routing**: Routes requests using Ocelot gateway
- **Usage Tracking**: Publishes usage events to RabbitMQ

### 2. User Service (Port 5001)
- **Authentication**: Login/Register endpoints
- **JWT Token Management**: Issues and validates tokens
- **User Management**: CRUD operations for users
- **Tier Caching**: Caches user tier info during login

### 3. Usage Tracking Service (Port 5003)
- **Event Processing**: Consumes usage events from RabbitMQ
- **Data Persistence**: Stores usage data in SQL Server
- **Monthly Summaries**: Aggregates usage data monthly

### 4. Tier Service (Port 5002)
- **Tier Management**: CRUD operations for pricing tiers
- **Configuration**: Dynamic tier configuration

### 5. Sample API Service (Port 5010)
- **Sample Endpoints**: For testing the gateway functionality
- **Product/Weather APIs**: Example business logic

## Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SHAHEER-SALEEM\\SQLEXPRESS;Database=MonetizationGatewayDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### JWT Configuration
```json
{
  "Jwt": {
    "Key": "ThisIsMySecretKeyForJWTTokenGenerationAndShouldBeAtLeast256Bits",
    "Issuer": "ApiMonetizationGateway",
    "Audience": "ApiMonetizationGatewayUsers",
    "ExpiryMinutes": 60
  }
}
```

## Rate Limiting Logic

### Monthly Quota Check
1. Get current month usage from Redis: `monthly_usage:<userId>:<YYYYMM>`
2. Increment counter atomically
3. Compare with user's monthly quota
4. If exceeded: Decrement counter and return HTTP 429
5. If within limits: Continue processing

### Per-Second Rate Limiting
1. Create time-based key: `rate_limit:<userId>:<UnixSeconds>`
2. Increment counter with 1-second TTL
3. Compare with user's rate limit
4. If exceeded: Decrement counter and return HTTP 429
5. If within limits: Continue processing

## Usage Tracking Flow

1. **Request Processing**: Gateway middleware tracks request start time
2. **Request Completion**: Calculate response time and status
3. **Event Publishing**: Publish usage event to RabbitMQ
4. **Background Processing**: Consumer service processes events
5. **Data Storage**: Store in ApiUsages table
6. **Monthly Aggregation**: Update MonthlyUsageSummaries table

## Testing

### Prerequisites
- SQL Server with database created and migrated
- Redis server running on localhost:6379
- RabbitMQ server running on localhost:5672

### Running Tests
```powershell
# Run the test script
.\test-api-functionality.ps1
```

### Manual Testing Steps
1. **Start Services** (in order):
   ```bash
   # Terminal 1: User Service
   cd Gateway/UserService/ApiMonetizationGateway.UserService
   dotnet run
   
   # Terminal 2: Usage Tracking Service
   cd Gateway/UsageTrackingService/ApiMonetizationGateway.UsageTrackingService
   dotnet run
   
   # Terminal 3: Gateway Service
   cd Gateway/Gateway/ApiMonetizationGateway.Gateway
   dotnet run
   
   # Terminal 4: Sample API Service
   cd Gateway/SampleApiService/ApiMonetizationGateway.SampleApi
   dotnet run
   ```

2. **Test Authentication**:
   ```bash
   # Login with demo user
   curl -X POST "http://localhost:5001/api/auth/login" \
   -H "Content-Type: application/json" \
   -d '{
     "email": "demo.user@example.com",
     "password": "Passw0rd!"
   }'
   ```

3. **Test Rate Limiting**:
   ```bash
   # Make multiple rapid requests
   for i in {1..5}; do
     curl -H "Authorization: Bearer <token>" \
     "http://localhost:5000/api/products"
   done
   ```

## Error Handling

### Rate Limit Exceeded (HTTP 429)
```json
{
  "error": "Rate limit exceeded",
  "message": "Monthly quota has been reached",
  "retryAfterSeconds": 2592000,
  "headers": {
    "limit": 2,
    "remaining": 0,
    "resetTime": 1696118400,
    "retryAfter": 2592000
  }
}
```

### Authentication Failed (HTTP 401)
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

## Monitoring & Observability

### Key Metrics to Monitor
- API request rates per user
- Rate limit violations
- Monthly quota usage
- JWT token validation failures
- RabbitMQ queue depths
- Redis cache hit/miss rates

### Database Queries for Monitoring
```sql
-- Check current month usage by user
SELECT u.Email, mus.TotalRequests, mus.SuccessfulRequests, mus.FailedRequests
FROM MonthlyUsageSummaries mus
JOIN Users u ON mus.UserId = u.Id
WHERE mus.Year = YEAR(GETDATE()) AND mus.Month = MONTH(GETDATE())

-- Check recent API usage
SELECT TOP 100 u.Email, au.Endpoint, au.HttpMethod, au.ResponseStatusCode, au.RequestTimestamp
FROM ApiUsages au
JOIN Users u ON au.UserId = u.Id
ORDER BY au.RequestTimestamp DESC

-- Check user tier assignments
SELECT u.Email, t.Name as TierName, t.RateLimit, t.MonthlyQuota
FROM Users u
JOIN UserTiers ut ON u.Id = ut.UserId AND ut.IsActive = 1
JOIN Tiers t ON ut.TierId = t.Id
WHERE u.IsActive = 1
```

## Security Considerations

1. **JWT Secret**: Use strong, environment-specific JWT signing keys
2. **Token Storage**: Tokens stored in database for revocation capability
3. **Rate Limiting**: Prevents abuse and DoS attacks
4. **Input Validation**: All API inputs validated and sanitized
5. **HTTPS**: Use HTTPS in production environments
6. **Redis Security**: Secure Redis instance with authentication
7. **Database Security**: Use parameterized queries, connection encryption

## Scalability Considerations

1. **Redis Clustering**: For high-availability caching
2. **Database Partitioning**: Partition usage tables by date
3. **Message Queue Clustering**: RabbitMQ clustering for reliability
4. **Horizontal Scaling**: Stateless services can be scaled horizontally
5. **Caching Strategy**: Optimize Redis key TTLs and memory usage

## Troubleshooting

### Common Issues
1. **Database Connection**: Ensure SQL Server is accessible
2. **Redis Connection**: Verify Redis is running and accessible
3. **RabbitMQ Connection**: Check RabbitMQ service status
4. **Token Expiration**: Verify JWT expiration times
5. **Rate Limit Cache**: Check Redis for rate limiting keys

### Debug Commands
```bash
# Check Redis keys
redis-cli KEYS "user_tier:*"
redis-cli KEYS "monthly_usage:*"
redis-cli KEYS "rate_limit:*"

# Check RabbitMQ queues
rabbitmqctl list_queues

# Check database migration status
dotnet ef migrations list --context ApiMonetizationContext
```

## Deployment Notes

1. Update connection strings for production environment
2. Configure appropriate JWT secrets
3. Set up Redis persistence if needed
4. Configure RabbitMQ clustering
5. Set up database backup schedules
6. Configure logging and monitoring
7. Set up SSL certificates for HTTPS

## Conclusion

The implementation successfully provides:
- ✅ JWT authentication with database validation
- ✅ Dynamic rate limiting based on user tiers  
- ✅ Monthly quota enforcement with proper error responses
- ✅ Comprehensive usage tracking via RabbitMQ
- ✅ Redis caching for performance optimization
- ✅ Scalable microservices architecture

The system is ready for production deployment with proper infrastructure setup and monitoring.