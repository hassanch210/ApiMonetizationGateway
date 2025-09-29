# API Monetization Gateway - Swagger Documentation Guide

## Overview

All services in the API Monetization Gateway now have comprehensive Swagger documentation with JWT Bearer authentication support. Each service provides an interactive API documentation interface that allows you to test endpoints directly from the browser.

## Swagger Endpoints

### 🌐 API Gateway (Main Entry Point)
- **URL**: http://localhost:5000/swagger
- **Description**: Main API Gateway with route aggregation
- **Features**: JWT authentication, rate limiting, request routing
- **Routes**: All routes are proxied through this gateway

### 👤 User Service 
- **URL**: http://localhost:5001/swagger
- **Description**: User management and authentication service
- **Features**: User registration, login, JWT token generation
- **Port**: 5001

### 🏷️ Tier Service
- **URL**: http://localhost:5002/swagger  
- **Description**: Tier management and pricing configuration
- **Features**: Tier CRUD operations, pricing management
- **Port**: 5002

### 📊 Usage Tracking Service
- **URL**: http://localhost:5003/swagger
- **Description**: API usage tracking and analytics
- **Features**: Usage data collection, analytics, reporting
- **Port**: 5003

### 💰 Billing Service
- **URL**: http://localhost:5004/swagger
- **Description**: Billing and payment processing
- **Features**: Invoice generation, payment processing, billing calculations
- **Port**: 5004

### 🧪 Sample API Service
- **URL**: http://localhost:5010/swagger
- **Description**: Sample API for testing gateway functionality
- **Features**: Protected endpoints for testing JWT auth and rate limiting
- **Port**: 5010

## JWT Authentication in Swagger

All services are configured with JWT Bearer authentication support in Swagger UI:

### 🔐 How to Use JWT Authentication

1. **Get JWT Token**:
   - Go to User Service Swagger: http://localhost:5001/swagger
   - Use `/api/auth/register` or `/api/auth/login` endpoints
   - Copy the `token` from the response

2. **Authorize in Swagger**:
   - Click the **🔒 Authorize** button at the top of any Swagger UI
   - Enter your token in the format: `Bearer <your-token>`
   - Click **Authorize**

3. **Test Protected Endpoints**:
   - All protected endpoints will now include your JWT token
   - The token is automatically added to the Authorization header

### 📝 Example Authentication Flow

```bash
# 1. Register a new user
POST http://localhost:5001/api/auth/register
{
  "email": "test@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "password123",
  "tierId": 1
}

# 2. Copy the token from response
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-09-29T16:16:30Z",
  "user": { ... }
}

# 3. Use token in Swagger Authorization: Bearer <token>
```

## Service-Specific Features

### 🔑 User Service Endpoints
```
POST /api/auth/register     - User registration (Public)
POST /api/auth/login        - User login (Public)
GET  /api/users/{id}        - Get user by ID (Protected)
PUT  /api/users/{id}        - Update user (Protected)
DELETE /api/users/{id}      - Deactivate user (Protected)
GET  /api/users/by-api-key/{apiKey} - Get user by API key (Protected)
```

### 🏷️ Tier Service Endpoints
```
GET    /api/tiers          - Get all tiers (Protected)
GET    /api/tiers/{id}     - Get tier by ID (Protected)
POST   /api/tiers          - Create new tier (Protected)
PUT    /api/tiers/{id}     - Update tier (Protected)
DELETE /api/tiers/{id}     - Delete tier (Protected)
```

### 📊 Usage Tracking Service Endpoints
```
GET  /api/usage/user/{userId}           - Get user usage (Protected)
GET  /api/usage/user/{userId}/monthly   - Get monthly usage (Protected)
POST /api/usage/track                   - Track usage (Protected)
GET  /api/usage/analytics               - Get usage analytics (Protected)
```

### 💰 Billing Service Endpoints
```
GET  /api/billing/user/{userId}         - Get user bills (Protected)
GET  /api/billing/invoice/{invoiceId}   - Get invoice (Protected)
POST /api/billing/generate-monthly      - Generate monthly bills (Protected)
GET  /api/billing/summary/{userId}      - Get billing summary (Protected)
```

### 🧪 Sample API Service Endpoints
```
GET  /api/products          - Get products (Protected via Gateway)
GET  /api/products/{id}     - Get product by ID (Protected via Gateway)
GET  /WeatherForecast       - Get weather data (Protected via Gateway)
```

## Gateway Routes (Ocelot)

The main gateway at http://localhost:5000 provides these routes:

### 🔓 Public Routes (No JWT Required)
```
POST /api/auth/register     -> UserService:5001
POST /api/auth/login        -> UserService:5001
```

### 🔒 Protected Routes (JWT Required)
```
GET/POST/PUT/DELETE /api/sample/*    -> SampleApiService:5010
GET/POST/PUT/DELETE /admin/users/*   -> UserService:5001
GET/POST/PUT/DELETE /admin/tiers/*   -> TierService:5002
```

## Swagger Configuration Features

### 📋 Enhanced Documentation
- **API Titles**: Descriptive titles for each service
- **Version Information**: API versioning support
- **Contact Information**: Support contact details
- **Descriptions**: Detailed service descriptions

### 🔐 Security Configuration
- **JWT Bearer Authentication**: Automatic token handling
- **Authorization Headers**: Proper header configuration
- **Security Requirements**: Applied to protected endpoints

### 🎨 UI Enhancements
- **Custom Routing**: Clean URLs for each service
- **Response Time Display**: Shows API response times
- **Model Expansion**: Better model display in UI
- **Syntax Highlighting**: Enhanced code display

## Development Workflow

### 🚀 Starting Services with Swagger

1. **Start UserService**:
   ```bash
   cd Gateway/UserService/ApiMonetizationGateway.UserService
   dotnet run
   # Swagger: http://localhost:5001/swagger
   ```

2. **Start Other Services**:
   ```bash
   cd Gateway/TierService/ApiMonetizationGateway.TierService
   dotnet run
   # Swagger: http://localhost:5002/swagger
   ```

3. **Start API Gateway**:
   ```bash
   cd Gateway/Gateway/ApiMonetizationGateway.Gateway  
   dotnet run
   # Swagger: http://localhost:5000/swagger
   ```

### 🧪 Testing Flow

1. **Register/Login** via UserService Swagger
2. **Copy JWT token** from response
3. **Authorize** in any Swagger UI with the token
4. **Test protected endpoints** across all services
5. **Monitor rate limiting** via Gateway middleware

## Troubleshooting

### ❌ Common Issues

1. **401 Unauthorized**:
   - Check if JWT token is valid and not expired
   - Ensure token is properly formatted: `Bearer <token>`

2. **403 Forbidden**:
   - Verify API key rate limiting hasn't been exceeded
   - Check tier limits in database

3. **Service Unavailable**:
   - Ensure all required services are running
   - Check port conflicts

### 📊 Monitoring

- **Rate Limiting**: Headers show remaining requests
- **Response Times**: Displayed in Swagger UI
- **Usage Tracking**: Monitored via RabbitMQ
- **Error Logs**: Check console output of each service

## Security Notes

- JWT tokens expire after 60 minutes by default
- API keys are still used for rate limiting (dual authentication)
- All services support CORS for development
- Swagger UI is only enabled in Development environment
- Production deployments should disable Swagger for security

## Next Steps

1. Test all endpoints using Swagger UI
2. Implement API versioning if needed
3. Add more detailed API documentation
4. Set up automated API testing
5. Configure production-ready security settings