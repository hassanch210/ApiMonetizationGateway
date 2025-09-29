# Database Setup Guide

## ✅ **Database Successfully Configured**

The API Monetization Gateway has been configured to use your existing SQL Server instance:

- **Server**: `DESKTOP-KRJT078\SQLEXPRESS`
- **Database**: `MonetizationGatewayDB`
- **Connection**: Windows Authentication with `TrustServerCertificate=true`

## 🗄️ **Database Schema**

The following tables have been created:

### Core Tables

1. **`Tiers`** - API tier definitions (Free, Pro)
   - Rate limits, monthly quotas, pricing
   - Seeded with default Free and Pro tiers

2. **`Users`** - API users and their API keys
   - Email, names, generated API keys
   - Foreign key to Tiers table

3. **`ApiUsages`** - Individual API request logs
   - User, endpoint, response time, status codes
   - Indexed for performance

4. **`MonthlyUsageSummaries`** - Monthly billing summaries
   - Aggregated usage and cost calculations
   - Used for billing processes

### Pre-Seeded Data

```sql
-- Free Tier
Id: 1, Name: 'Free', MonthlyQuota: 100, RateLimit: 2/sec, Price: $0

-- Pro Tier  
Id: 2, Name: 'Pro', MonthlyQuota: 100000, RateLimit: 10/sec, Price: $50
```

## 🛠️ **Entity Framework Migrations**

### Migration Location
Migrations are stored in: `src\UserService\ApiMonetizationGateway.UserService\Migrations\`

### Common EF Commands

```bash
# Navigate to User Service directory
cd src\UserService\ApiMonetizationGateway.UserService

# Create new migration
dotnet ef migrations add MigrationName --context ApiMonetizationContext

# Update database
dotnet ef database update --context ApiMonetizationContext

# Remove last migration (if not applied)
dotnet ef migrations remove --context ApiMonetizationContext

# View migration history
dotnet ef migrations list --context ApiMonetizationContext
```

### Connection String Format
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-KRJT078\\SQLEXPRESS;Database=MonetizationGatewayDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

## 🔧 **Database Verification**

Run the verification script to test your database:

```powershell
.\verify-database.ps1
```

This script will:
- ✅ Test database connectivity
- ✅ Show created tables and column counts  
- ✅ Display seeded tier data
- ✅ Verify connection string format

## 📊 **Manual Database Queries**

### Check Tiers
```sql
SELECT Id, Name, MonthlyQuota, RateLimit, MonthlyPrice, IsActive 
FROM Tiers 
ORDER BY MonthlyPrice;
```

### View Users and Their Tiers
```sql
SELECT u.Id, u.Email, u.FirstName, u.LastName, u.ApiKey, t.Name as TierName
FROM Users u
INNER JOIN Tiers t ON u.TierId = t.Id
WHERE u.IsActive = 1;
```

### Check API Usage
```sql
SELECT TOP 10 
    u.Email,
    au.Endpoint,
    au.HttpMethod,
    au.ResponseStatusCode,
    au.RequestTimestamp
FROM ApiUsages au
INNER JOIN Users u ON au.UserId = u.Id
ORDER BY au.RequestTimestamp DESC;
```

### Monthly Usage Summary
```sql
SELECT 
    u.Email,
    mus.Year,
    mus.Month,
    mus.TotalRequests,
    mus.CalculatedCost,
    mus.IsBilled
FROM MonthlyUsageSummaries mus
INNER JOIN Users u ON mus.UserId = u.Id
ORDER BY mus.Year DESC, mus.Month DESC;
```

## 🚨 **Troubleshooting**

### Connection Issues

1. **SQL Server not accessible**
   ```bash
   # Test SQL Server service
   Get-Service -Name "*SQL*" | Where-Object {$_.Status -eq "Running"}
   ```

2. **Database doesn't exist**
   ```bash
   # Recreate database with migrations
   cd src\UserService\ApiMonetizationGateway.UserService
   dotnet ef database update --context ApiMonetizationContext
   ```

3. **Permission issues**
   - Ensure your Windows account has access to SQL Server
   - Check if Windows Authentication is enabled
   - Verify `TrustServerCertificate=true` is in connection string

### Migration Issues

1. **Migration already exists**
   ```bash
   dotnet ef migrations remove --context ApiMonetizationContext
   ```

2. **Database out of sync**
   ```bash
   # Reset to specific migration
   dotnet ef database update InitialCreate --context ApiMonetizationContext
   ```

3. **Clean slate**
   ```bash
   # Drop and recreate (⚠️ DELETES ALL DATA)
   dotnet ef database drop --context ApiMonetizationContext
   dotnet ef database update --context ApiMonetizationContext
   ```

## 🔄 **Backup & Restore**

### Backup
```sql
BACKUP DATABASE MonetizationGatewayDB 
TO DISK = 'C:\Temp\MonetizationGatewayDB_Backup.bak';
```

### Restore  
```sql
RESTORE DATABASE MonetizationGatewayDB 
FROM DISK = 'C:\Temp\MonetizationGatewayDB_Backup.bak'
WITH REPLACE;
```

## 🎯 **Performance Optimization**

The database includes optimized indexes:

- **Users**: Email (unique), ApiKey (unique), TierId
- **ApiUsages**: UserId + RequestTimestamp, RequestTimestamp  
- **MonthlyUsageSummaries**: UserId + Year + Month (unique)
- **Tiers**: Name (unique)

These indexes support:
- ✅ Fast API key lookups
- ✅ Efficient usage queries by user and date range
- ✅ Quick monthly summary retrievals
- ✅ Optimal billing report generation

---

**✅ Your database is ready!** You can now run the complete API monetization platform with:

```powershell
.\start-dev.ps1
```