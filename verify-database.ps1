#!/usr/bin/env pwsh

Write-Host "🗄️  API Monetization Gateway - Database Verification" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

$serverName = "DESKTOP-KRJT078\SQLEXPRESS"
$databaseName = "MonetizationGatewayDB"

Write-Host ""
Write-Host "Database Configuration:" -ForegroundColor Yellow
Write-Host "  📍 Server: $serverName" -ForegroundColor White
Write-Host "  📁 Database: $databaseName" -ForegroundColor White

Write-Host ""
Write-Host "Testing database connection..." -ForegroundColor Yellow

# Test connection using sqlcmd
$connectionTest = try {
    $result = sqlcmd -S $serverName -d $databaseName -Q "SELECT 1 as TestConnection" -h -1 2>$null
    if ($LASTEXITCODE -eq 0) { $true } else { $false }
} catch { $false }

if ($connectionTest) {
    Write-Host "✅ Database connection successful!" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "📋 Database Tables:" -ForegroundColor Yellow
    
    $tablesQuery = @"
SELECT 
    TABLE_NAME as [Table Name],
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) as [Column Count]
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE' 
    AND TABLE_NAME != '__EFMigrationsHistory'
ORDER BY TABLE_NAME
"@
    
    try {
        sqlcmd -S $serverName -d $databaseName -Q $tablesQuery -h -1
        
        Write-Host ""
        Write-Host "📊 Seeded Tiers:" -ForegroundColor Yellow
        
        $tiersQuery = @"
SELECT 
    Id,
    Name,
    MonthlyQuota as [Monthly Quota],
    RateLimit as [Rate Limit],
    MonthlyPrice as [Monthly Price],
    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END as Status
FROM Tiers
ORDER BY MonthlyPrice
"@
        
        sqlcmd -S $serverName -d $databaseName -Q $tiersQuery -h -1
        
        Write-Host ""
        Write-Host "🔗 Sample Entity Framework Connection Test:" -ForegroundColor Yellow
        Write-Host "Connection String: Server=$serverName;Database=$databaseName;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true" -ForegroundColor Gray
        
        Write-Host ""
        Write-Host "✅ Database verification completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "🚀 Ready to run the API Monetization Gateway!" -ForegroundColor Cyan
        Write-Host "   Use: .\start-dev.ps1 to start all services" -ForegroundColor White
        
    } catch {
        Write-Host "⚠️  Could not query table information, but connection works" -ForegroundColor Yellow
    }
    
} else {
    Write-Host "❌ Database connection failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Ensure SQL Server is running" -ForegroundColor White
    Write-Host "  2. Verify server name: $serverName" -ForegroundColor White
    Write-Host "  3. Check if database exists: $databaseName" -ForegroundColor White
    Write-Host "  4. Verify Windows Authentication is enabled" -ForegroundColor White
    Write-Host ""
    Write-Host "To recreate the database, run:" -ForegroundColor Yellow
    Write-Host "  cd src\UserService\ApiMonetizationGateway.UserService" -ForegroundColor Cyan
    Write-Host "  dotnet ef database update --context ApiMonetizationContext" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Press Enter to continue..." -ForegroundColor White
Read-Host