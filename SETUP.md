# API Monetization Gateway - Setup Guide

This guide will help you set up and run the complete API monetization platform.

## 🏗️ Architecture Overview

The system consists of 6 microservices + infrastructure:

### Core Services
- **API Gateway (Port 5000)** - Ocelot-based gateway with rate limiting
- **User Service (Port 5001)** - User management and API key generation
- **Tier Service (Port 5002)** - Tier configuration management
- **Usage Tracking Service (Port 5003)** - API usage logging with RabbitMQ consumer
- **Billing Service (Port 5004)** - Monthly billing and usage summarization
- **Sample API Service (Port 5010)** - Demo API for testing

### Infrastructure
- **SQL Server** - Database for all persistent data
- **Redis** - Rate limiting counters and caching
- **RabbitMQ** - Message queues for async communication

## 🚀 Quick Start (Recommended)

### Option 1: Automated Setup (Windows)

1. **Clone and navigate to the project**:
   ```powershell
   cd D:\ApiMonetizationGateway
   ```

2. **Run the automated setup script**:
   ```powershell
   .\start-dev.ps1
   ```

The script will:
- ✅ Check prerequisites (.NET SDK)
- ✅ Start Redis and RabbitMQ via Docker (if needed)
- ✅ Build the entire solution
- ✅ Start all microservices in the correct order
- ✅ Provide service URLs and testing instructions

### Option 2: Docker Compose (All Platforms)

1. **Start the entire platform**:
   ```bash
   docker-compose up -d
   ```

2. **Check service status**:
   ```bash
   docker-compose ps
   ```

3. **View logs**:
   ```bash
   docker-compose logs -f [service-name]
   ```

## 🔧 Manual Setup

### Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** - LocalDB or full instance
- **Redis** - Local installation or Docker
- **RabbitMQ** - Local installation or Docker

### Infrastructure Setup

#### SQL Server (LocalDB - Default)
The application is configured to use LocalDB by default:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ApiMonetizationGateway;Trusted_Connection=true;MultipleActiveResultSets=true"
```

#### Redis
```bash
# Using Docker
docker run -d --name redis -p 6379:6379 redis:alpine

# Or install locally
# Windows: Download from https://redis.io/download
# Ubuntu: sudo apt-get install redis-server
# macOS: brew install redis
```

#### RabbitMQ
```bash
# Using Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine

# Or install locally
# Windows: Download from https://www.rabbitmq.com/download.html
# Ubuntu: sudo apt-get install rabbitmq-server
# macOS: brew install rabbitmq
```

### Build and Run Services

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Start services in order** (each in a separate terminal):

   ```bash
   # Terminal 1: User Service
   cd src/UserService/ApiMonetizationGateway.UserService
   dotnet run

   # Terminal 2: Tier Service
   cd src/TierService/ApiMonetizationGateway.TierService
   dotnet run

   # Terminal 3: Usage Tracking Service
   cd src/UsageTrackingService/ApiMonetizationGateway.UsageTrackingService
   dotnet run

   # Terminal 4: Billing Service
   cd src/BillingService/ApiMonetizationGateway.BillingService
   dotnet run

   # Terminal 5: Sample API Service
   cd src/SampleApiService/ApiMonetizationGateway.SampleApi
   dotnet run

   # Terminal 6: API Gateway (start last)
   cd src/Gateway/ApiMonetizationGateway.Gateway
   dotnet run
   ```

## 🧪 Testing the System

### 1. Verify Services are Running

Access Swagger documentation for each service:
- User Service: http://localhost:5001/swagger
- Tier Service: http://localhost:5002/swagger
- Usage Tracking: http://localhost:5003/swagger
- Billing Service: http://localhost:5004/swagger
- Sample API: http://localhost:5010/swagger

### 2. Create a Test User

```bash
curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "tierId": 1
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "apiKey": "generated-api-key-here",
    "tier": {
      "id": 1,
      "name": "Free",
      "monthlyQuota": 100,
      "rateLimit": 2,
      "monthlyPrice": 0
    }
  }
}
```

### 3. Test API Gateway with Rate Limiting

```bash
# Test successful request
curl -H "X-API-Key: YOUR_API_KEY_HERE" \
  http://localhost:5000/api/sample/weatherforecast

# Test rate limiting (make 3+ rapid requests)
for i in {1..5}; do
  curl -H "X-API-Key: YOUR_API_KEY_HERE" \
    http://localhost:5000/api/sample/weatherforecast
  echo "Request $i completed"
done
```

**Rate limit exceeded response (HTTP 429):**
```json
{
  "error": "Rate limit exceeded",
  "message": "Rate limit exceeded",
  "retryAfterSeconds": 60,
  "headers": {
    "limit": 2,
    "remaining": 0,
    "resetTime": 1640995200,
    "retryAfter": 60
  }
}
```

### 4. Check Usage Tracking

```bash
# View usage for a user
curl http://localhost:5003/api/usage/user/1
```

### 5. Process Monthly Billing

```bash
# Process billing for a specific user/month
curl -X POST http://localhost:5004/api/billing/process/1/2025/1

# View billing summaries for a user
curl http://localhost:5004/api/billing/summaries/1
```

## 📊 Monitoring and Management

### Service Health Checks

Each service provides:
- **Swagger UI**: `http://localhost:[port]/swagger`
- **Health endpoint**: `http://localhost:[port]/health` (if configured)

### Infrastructure Management

#### RabbitMQ Management
- **URL**: http://localhost:15672
- **Credentials**: guest/guest

#### Database Access
- **Server**: `(localdb)\mssqllocaldb` or `localhost,1433`
- **Database**: `ApiMonetizationGateway`

#### Redis CLI
```bash
# Connect to Redis
redis-cli -h localhost -p 6379

# View rate limiting keys
redis-cli KEYS "rate_limit:*"
```

## 🔧 Configuration

### Environment Variables

Each service supports these environment variables:

```bash
# Database
ConnectionStrings__DefaultConnection="Server=...;Database=..."

# Redis (Gateway only)
ConnectionStrings__Redis="localhost:6379"

# RabbitMQ (Gateway, Usage Tracking)
RabbitMQ__HostName="localhost"
RabbitMQ__Port=5672
RabbitMQ__UserName="guest"
RabbitMQ__Password="guest"
```

### Tier Configuration

Default tiers are automatically seeded:

| Tier | Monthly Quota | Rate Limit | Price |
|------|---------------|------------|-------|
| Free | 100 requests | 2 req/sec | $0 |
| Pro | 100,000 requests | 10 req/sec | $50/month |

Modify via Tier Service API:
```bash
curl -X PUT http://localhost:5002/api/tiers/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Free",
    "description": "Updated free tier",
    "monthlyQuota": 200,
    "rateLimit": 3,
    "monthlyPrice": 0
  }'
```

## 🐛 Troubleshooting

### Common Issues

1. **Port already in use**
   ```bash
   # Find process using port
   netstat -ano | findstr :5000
   # Kill process
   taskkill /PID [process-id] /F
   ```

2. **Database connection issues**
   - Ensure SQL Server/LocalDB is running
   - Check connection strings in appsettings.json
   - Run `dotnet ef database update` if needed

3. **Redis/RabbitMQ not accessible**
   - Verify services are running: `docker ps`
   - Check ports are not blocked by firewall
   - Restart containers if needed

4. **Build errors**
   ```bash
   # Clean and rebuild
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Logs and Debugging

- **Console logs**: Each service outputs to console
- **File logs**: Configure Serilog for file output
- **Application Insights**: Add for production monitoring

## 🚀 Production Deployment

For production deployment:

1. **Use Docker Compose with production configuration**
2. **Configure proper connection strings**
3. **Set up SSL certificates**
4. **Configure monitoring and alerting**
5. **Set up CI/CD pipeline**
6. **Configure backup strategies**

### Production Checklist

- [ ] Use secure connection strings with secrets management
- [ ] Configure HTTPS with valid certificates
- [ ] Set up load balancers for each service
- [ ] Configure monitoring (Application Insights, Prometheus)
- [ ] Set up centralized logging
- [ ] Configure database backups
- [ ] Set up health checks
- [ ] Configure auto-scaling policies
- [ ] Security scanning and vulnerability assessment

## 📞 Support

For issues or questions:

1. Check this documentation
2. Review service logs
3. Test individual components
4. Check infrastructure connectivity

The system is designed to be resilient with proper error handling and graceful degradation.