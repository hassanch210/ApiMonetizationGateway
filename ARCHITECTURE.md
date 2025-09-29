# API Monetization Gateway - Architecture & Flow

## Project Overview

The API Monetization Gateway is a microservices-based system that provides JWT authentication, API key-based rate limiting, usage tracking, billing, and tier management for monetizing APIs.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                          Client Applications                     │
├─────────────────────────────────────────────────────────────────┤
│                         HTTP Requests                           │
│                    (JWT Token Required)                         │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                  API Gateway (Port 5000)                       │
│                     Ocelot Gateway                              │
├─────────────────────────────────────────────────────────────────┤
│  1. JWT Authentication Middleware                               │
│  2. Rate Limiting Middleware (API Key based)                   │
│  3. Request Routing                                             │
│  4. Usage Tracking (via RabbitMQ)                             │
└─────┬──────────┬──────────┬──────────┬──────────┬──────────────┘
      │          │          │          │          │
      ▼          ▼          ▼          ▼          ▼
┌──────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────────────┐
│UserService│ │TierSrv │ │UsageSrv │ │BillSrv │ │SampleApiSrv │
│(Port 5001)│ │(5002)  │ │(5003)  │ │(5004)  │ │(Port 5010)  │
└──────────┘ └─────────┘ └─────────┘ └─────────┘ └──────────────┘
      │          │          │          │          │
      └──────────┴──────────┴──────────┴──────────┘
                           │
                    ┌─────────────┐
                    │  Database   │
                    │(SQL Server) │
                    └─────────────┘
```

## Authentication Flow

### 1. User Registration/Login Flow
```
Client -> Gateway -> UserService
  │         │           │
  │         │           ├─ Validate credentials
  │         │           ├─ Hash password (BCrypt)
  │         │           ├─ Create user & UserTier
  │         │           ├─ Generate JWT token
  │         │           └─ Return token + user info
  │         │
  │         └─ Forward response
  │
  └─ Store JWT token for future requests
```

### 2. API Request Flow (Dual Authentication)
```
Client Request with JWT Token
  │
  ▼
┌─────────────────────────────────────┐
│          API Gateway                 │
├─────────────────────────────────────┤
│  Step 1: JWT Authentication         │
│  ├─ Validate JWT token              │
│  ├─ Extract user claims             │
│  └─ Continue if valid               │
│                                     │
│  Step 2: Rate Limiting              │
│  ├─ Extract API Key from user       │
│  ├─ Check tier limits               │
│  ├─ Apply rate limiting             │
│  └─ Track usage via RabbitMQ        │
│                                     │
│  Step 3: Forward to Service         │
└─────────────────────────────────────┘
```

## Database Schema

### Tables
1. **Users** - User accounts with JWT auth
   - Id, Email, FirstName, LastName
   - PasswordHash (for JWT auth)
   - ApiKey (for rate limiting - backward compatibility)
   - TierId (current tier)

2. **UserTiers** - User tier assignment history
   - Id, UserId, TierId
   - AssignedAt, UpdatedOn, UpdatedBy
   - IsActive, Notes

3. **Tiers** - Service tiers and limits
   - Id, Name, Description
   - MonthlyQuota, RateLimit, MonthlyPrice

4. **ApiUsage** - Request tracking
5. **MonthlyUsageSummary** - Billing summaries

## Service Breakdown

### 1. Gateway (Port 5000)
- **Technology**: Ocelot API Gateway
- **Authentication**: JWT Bearer + API Key Rate Limiting
- **Responsibilities**:
  - JWT token validation
  - Request routing
  - Rate limiting based on API keys
  - Usage tracking
  - CORS handling

### 2. UserService (Port 5001)
- **Responsibilities**:
  - JWT authentication (login/register)
  - User management
  - User tier assignments
- **Endpoints**:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `GET/POST/PUT/DELETE /api/users/*`

### 3. TierService (Port 5002)
- **Responsibilities**:
  - Tier management
  - Pricing configuration

### 4. UsageTrackingService (Port 5003)
- **Responsibilities**:
  - Process usage events from RabbitMQ
  - Store usage data
  - Generate usage reports

### 5. BillingService (Port 5004)
- **Responsibilities**:
  - Calculate monthly bills
  - Process payments
  - Generate invoices

### 6. SampleApiService (Port 5010)
- **Responsibilities**:
  - Example API for testing
  - Protected by JWT + rate limiting

## Configuration

### JWT Settings (Gateway & UserService)
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

### Ocelot Routes
- `/api/auth/*` - Public (UserService) - No JWT required
- `/api/sample/*` - Protected (SampleApiService) - JWT required
- `/admin/users/*` - Protected (UserService) - JWT required  
- `/admin/tiers/*` - Protected (TierService) - JWT required

## Usage Instructions

### 1. Start Services
```bash
# Start all services in order:
cd Gateway/UserService/ApiMonetizationGateway.UserService
dotnet run

cd Gateway/TierService/ApiMonetizationGateway.TierService  
dotnet run

cd Gateway/SampleApiService/ApiMonetizationGateway.SampleApi
dotnet run

cd Gateway/Gateway/ApiMonetizationGateway.Gateway
dotnet run
```

### 2. Register a User
```bash
POST http://localhost:5000/api/auth/register
{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe", 
  "password": "password123",
  "tierId": 1
}
```

### 3. Login
```bash
POST http://localhost:5000/api/auth/login
{
  "email": "user@example.com",
  "password": "password123"
}
```

### 4. Use Protected APIs
```bash
GET http://localhost:5000/api/sample/products
Authorization: Bearer <JWT_TOKEN>
```

## Security Features

1. **JWT Authentication**: Secure token-based authentication
2. **Password Hashing**: BCrypt for secure password storage
3. **API Key Rate Limiting**: Prevents abuse and manages quotas
4. **CORS Configuration**: Secure cross-origin requests
5. **Tier-based Access Control**: Different limits per user tier

## Monitoring & Tracking

1. **Usage Tracking**: Every request is tracked via RabbitMQ
2. **Rate Limiting**: Redis-based rate limiting with tier enforcement
3. **Billing Integration**: Automatic usage calculation for billing
4. **Audit Trail**: UserTier changes are tracked with timestamps and user info