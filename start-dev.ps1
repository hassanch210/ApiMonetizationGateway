#!/usr/bin/env pwsh

Write-Host "🚀 Starting API Monetization Gateway Development Environment" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# Function to check if a port is in use
function Test-Port {
    param([int]$Port)
    try {
        $connection = Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue
        return $connection
    }
    catch {
        return $false
    }
}

# Function to start a service in background
function Start-Service {
    param(
        [string]$ServiceName,
        [string]$Path,
        [int]$Port
    )
    
    Write-Host "Starting $ServiceName on port $Port..." -ForegroundColor Yellow
    
    if (Test-Port -Port $Port) {
        Write-Host "Port $Port is already in use. Skipping $ServiceName." -ForegroundColor Red
        return
    }
    
    Push-Location $Path
    Start-Process -FilePath "dotnet" -ArgumentList "run" -WindowStyle Minimized
    Pop-Location
    
    # Wait a moment for the service to start
    Start-Sleep -Seconds 3
    
    if (Test-Port -Port $Port) {
        Write-Host "✅ $ServiceName started successfully on port $Port" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to start $ServiceName on port $Port" -ForegroundColor Red
    }
}

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# Check if Redis is available
Write-Host "Checking Redis..." -ForegroundColor Yellow
if (Test-Port -Port 6379) {
    Write-Host "✅ Redis is running on port 6379" -ForegroundColor Green
} else {
    Write-Host "⚠️  Redis not detected. Starting Redis via Docker..." -ForegroundColor Yellow
    try {
        docker run -d --name apimonetization-redis -p 6379:6379 redis:alpine
        Start-Sleep -Seconds 5
        if (Test-Port -Port 6379) {
            Write-Host "✅ Redis started successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to start Redis" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Docker not available. Please install Docker or Redis manually" -ForegroundColor Red
    }
}

# Check if RabbitMQ is available
Write-Host "Checking RabbitMQ..." -ForegroundColor Yellow
if (Test-Port -Port 5672) {
    Write-Host "✅ RabbitMQ is running on port 5672" -ForegroundColor Green
} else {
    Write-Host "⚠️  RabbitMQ not detected. Starting RabbitMQ via Docker..." -ForegroundColor Yellow
    try {
        docker run -d --name apimonetization-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
        Start-Sleep -Seconds 10
        if (Test-Port -Port 5672) {
            Write-Host "✅ RabbitMQ started successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to start RabbitMQ" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Docker not available. Please install Docker or RabbitMQ manually" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Starting microservices..." -ForegroundColor Cyan

# Build solution first
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Please fix build errors before continuing." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

Write-Host ""

# Start services in order (dependencies first)
Start-Service "User Service" "src\UserService\ApiMonetizationGateway.UserService" 5001
Start-Service "Tier Service" "src\TierService\ApiMonetizationGateway.TierService" 5002
Start-Service "Usage Tracking Service" "src\UsageTrackingService\ApiMonetizationGateway.UsageTrackingService" 5003
Start-Service "Billing Service" "src\BillingService\ApiMonetizationGateway.BillingService" 5004
Start-Service "Sample API Service" "src\SampleApiService\ApiMonetizationGateway.SampleApi" 5010

# Wait a bit for all services to be ready
Write-Host ""
Write-Host "Waiting for services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Start Gateway last (it depends on other services)
Start-Service "API Gateway" "src\Gateway\ApiMonetizationGateway.Gateway" 5000

Write-Host ""
Write-Host "🎉 API Monetization Gateway is now running!" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "📋 Service URLs:" -ForegroundColor White
Write-Host "  🌐 API Gateway:         http://localhost:5000" -ForegroundColor Cyan
Write-Host "  👥 User Service:        http://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "  🎯 Tier Service:        http://localhost:5002/swagger" -ForegroundColor Cyan
Write-Host "  📊 Usage Tracking:      http://localhost:5003/swagger" -ForegroundColor Cyan
Write-Host "  💰 Billing Service:     http://localhost:5004/swagger" -ForegroundColor Cyan
Write-Host "  🧪 Sample API:          http://localhost:5010/swagger" -ForegroundColor Cyan

Write-Host ""
Write-Host "📋 Infrastructure URLs:" -ForegroundColor White
Write-Host "  🐰 RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
Write-Host "  🗄️  SQL Server:          DESKTOP-KRJT078\SQLEXPRESS (MonetizationGatewayDB)" -ForegroundColor Cyan
Write-Host "  🚀 Redis:               localhost:6379" -ForegroundColor Cyan

Write-Host ""
Write-Host "🧪 Quick Test:" -ForegroundColor White
Write-Host "  1. Create a user: POST http://localhost:5001/api/users" -ForegroundColor Yellow
Write-Host "  2. Get API key from response" -ForegroundColor Yellow
Write-Host "  3. Test gateway: GET http://localhost:5000/api/sample/weatherforecast" -ForegroundColor Yellow
Write-Host "     Headers: X-API-Key: [your-api-key]" -ForegroundColor Yellow

Write-Host ""
Write-Host "⚠️  To stop all services, close the command windows or press Ctrl+C in each" -ForegroundColor Yellow
Write-Host "   To clean up Docker containers: docker stop apimonetization-redis apimonetization-rabbitmq" -ForegroundColor Yellow

Write-Host ""
Write-Host "Press Enter to continue monitoring or Ctrl+C to exit..." -ForegroundColor White
Read-Host