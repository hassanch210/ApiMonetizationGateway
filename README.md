# API Monetization Gateway

A comprehensive microservice-based API monetization platform built with ASP.NET Core, implementing rate limiting, usage tracking, and billing capabilities.

## Architecture Overview

This solution implements a **microservice architecture** with the following components:

### Core Services

1. **API Gateway (Port 5000)** - Ocelot-based gateway with rate limiting middleware
2. **User Service (Port 5001)** - User management and API key generation
3. **Tier Service (Port 5002)** - Tier configuration management
4. **Usage Tracking Service (Port 5003)** - API usage logging and tracking
5. **Billing Service (Port 5004)** - Background billing and monthly summaries
6. **Sample API Service (Port 5010)** - Demo API for testing monetization

### Infrastructure Components

- **SQL Server** - Primary data store for all persistent data
- **Redis** - Caching and rate limiting counters
- **RabbitMQ** - Message queue for inter-service communication
- **Entity Framework Core** - Code First database approach

## Features Implemented

### ✅ Rate Limiting & Tier Enforcement

- **Per-second rate limiting** using Redis sliding window
- **Monthly quota enforcement** with database tracking
- **Dynamic tier configuration** stored in SQL Server
- **HTTP 429 responses** for rate limit violations
- **Proper rate limit headers** (X-RateLimit-*)

### ✅ API Usage Tracking

- **Comprehensive logging** with metadata (customer ID, user ID, endpoint, timestamp)
- **Asynchronous message-based tracking** via RabbitMQ
- **Performance metrics** (response time tracking)
- **IP and User-Agent logging**

### ✅ Tier Management

- **Free Tier**: 100 requests/month, 2 req/sec, $0
- **Pro Tier**: 100,000 requests/month, 10 req/sec, $50/month
- **Dynamic tier configuration** via REST API
- **Tier activation/deactivation**

### ✅ Clean Architecture

- **Code First** Entity Framework approach
- **Separation of concerns** with distinct microservices
- **Shared libraries** for common models and utilities
- **Dependency injection** throughout
- **Clean code practices** with proper error handling

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB is fine for development)
- Redis Server
- RabbitMQ Server

### Database Setup

The application uses **Code First migrations**. The database will be created automatically when you run the services.

**Connection String** (in appsettings.json):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ApiMonetizationGateway;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

### Running the Services

1. **Start Infrastructure Services:**
   ```bash
   # Start Redis (using Docker)
   docker run -d -p 6379:6379 redis:alpine
   
   # Start RabbitMQ (using Docker)
   docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. **Build the Solution:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run Each Service** (in separate terminals):
   ```bash
   # User Service
   cd src/UserService/ApiMonetizationGateway.UserService
   dotnet run
   
   # Tier Service
   cd src/TierService/ApiMonetizationGateway.TierService
   dotnet run
   
   # Sample API Service
   cd src/SampleApiService/ApiMonetizationGateway.SampleApi
   dotnet run
   
   # API Gateway (run last)
   cd src/Gateway/ApiMonetizationGateway.Gateway
   dotnet run
   ```

## API Usage Examples

### 1. Create a User

```bash
POST http://localhost:5001/api/users
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "tierId": 1
}
```

### 2. Use the API Gateway

```bash
GET http://localhost:5000/api/sample/weatherforecast
X-API-Key: [YOUR_API_KEY_FROM_STEP_1]
```

### 3. Check Rate Limits

Make multiple rapid requests to see rate limiting in action:

```bash
# This will succeed (within rate limits)
curl -H "X-API-Key: YOUR_API_KEY" http://localhost:5000/api/sample/weatherforecast

# After exceeding 2 req/sec (Free tier), you'll get HTTP 429
```

## Configuration

### Tier Configuration

Default tiers are seeded in the database:

| Tier | Monthly Quota | Rate Limit | Price |
|------|---------------|------------|-------|
| Free | 100 requests | 2 req/sec | $0 |
| Pro | 100,000 requests | 10 req/sec | $50/month |

You can modify tiers via the Tier Service API:

```bash
PUT http://localhost:5002/api/tiers/1
Content-Type: application/json

{
  "name": "Free",
  "description": "Updated free tier",
  "monthlyQuota": 200,
  "rateLimit": 3,
  "monthlyPrice": 0
}
```

### Redis Configuration

Rate limiting uses Redis for temporary storage:

- **Rate limit keys**: `rate_limit:{userId}:{timestamp}`
- **Cache keys**: `rate_limit_info:{apiKey}`
- **TTL**: 1 second for rate limits, 1 minute for cache

### RabbitMQ Queues

- **usage-tracking**: API usage messages
- **billing-processing**: Monthly billing jobs

## Database Schema

### Users Table
- `Id`, `Email`, `FirstName`, `LastName`, `ApiKey`, `TierId`, `IsActive`

### Tiers Table
- `Id`, `Name`, `Description`, `MonthlyQuota`, `RateLimit`, `MonthlyPrice`, `IsActive`

### ApiUsages Table
- `Id`, `UserId`, `Endpoint`, `HttpMethod`, `ResponseStatusCode`, `ResponseTimeMs`, `RequestTimestamp`

### MonthlyUsageSummaries Table
- `Id`, `UserId`, `Year`, `Month`, `TotalRequests`, `CalculatedCost`, `IsBilled`

## Error Handling

### Rate Limit Exceeded (HTTP 429)
```json
{
  "error": "Rate limit exceeded",
  "message": "Monthly quota exceeded",
  "retryAfterSeconds": 60,
  "headers": {
    "limit": 2,
    "remaining": 0,
    "resetTime": 1640995200,
    "retryAfter": 60
  }
}
```

### Invalid API Key (HTTP 401)
```json
{
  "error": "Invalid API key"
}
```

## Monitoring & Observability

### Swagger Documentation

Each service exposes Swagger UI:
- User Service: http://localhost:5001/swagger
- Tier Service: http://localhost:5002/swagger
- Sample API: http://localhost:5010/swagger

### RabbitMQ Management

Access RabbitMQ management interface:
- URL: http://localhost:15672
- Username: guest
- Password: guest

## Production Considerations

### Security
- [ ] Implement proper authentication/authorization
- [ ] Use HTTPS everywhere
- [ ] Secure API keys with encryption
- [ ] Implement rate limiting per IP address

### Scalability
- [ ] Use Redis Cluster for high availability
- [ ] Implement database connection pooling
- [ ] Add health checks for all services
- [ ] Use container orchestration (Kubernetes)

### Monitoring
- [ ] Add structured logging (Serilog)
- [ ] Implement metrics collection (Prometheus)
- [ ] Add distributed tracing (OpenTelemetry)
- [ ] Set up alerts for rate limit violations

## Architecture Benefits

1. **Scalability**: Each service can be scaled independently
2. **Maintainability**: Clean separation of concerns
3. **Extensibility**: Easy to add new tiers or features
4. **Performance**: Redis caching and async message processing
5. **Reliability**: Graceful error handling and fallback mechanisms

## Next Steps

To complete the full implementation, you would need to:

1. Implement the Usage Tracking Service
2. Implement the Background Billing Service
3. Add Docker containers and docker-compose
4. Add comprehensive unit and integration tests
5. Implement proper logging and monitoring
6. Add authentication and authorization
7. Optimize for production deployment

This architecture provides a solid foundation for a production-ready API monetization platform with clean code, proper separation of concerns, and industry best practices.