# Gateway E2E Test Script
# Uses Gateway only. Verifies JWT auth, rate limiting, and that endpoints route via gateway.

Write-Host "Starting Gateway E2E Tests..." -ForegroundColor Green

$GatewayUrl = "http://localhost:5000"
$LoginUrl = "$GatewayUrl/api/auth/login"

# Demo user seeded by the service
$loginBody = @{ email = "demo.user@example.com"; password = "Passw0rd!" } | ConvertTo-Json

try {
  $loginResp = Invoke-RestMethod -Uri $LoginUrl -Method POST -Body $loginBody -ContentType "application/json" -ErrorAction Stop
  $token = $loginResp.token
  if (-not $token) { throw "No token returned from login" }
  Write-Host "Login successful. Token acquired." -ForegroundColor Green
}
catch {
  Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}

$headers = @{ Authorization = "Bearer $token" }

function Hit-Gateway($path) {
  try {
    $url = "$GatewayUrl$path"
    $resp = Invoke-RestMethod -Uri $url -Method GET -Headers $headers -ErrorAction Stop
    Write-Host "200 OK: $path" -ForegroundColor Green
    return 200
  }
  catch {
    $status = try { $_.Exception.Response.StatusCode.Value__ } catch { -1 }
    Write-Host ("{0}: {1} -> {2}" -f $status, $path, $_.Exception.Message) -ForegroundColor Yellow
    return $status
  }
}

# 1) Authenticated calls to multiple services routed via gateway
$codes = @()
$codes += Hit-Gateway "/api/users"
$codes += Hit-Gateway "/api/products"
$codes += Hit-Gateway "/api/weatherforecast"
$codes += Hit-Gateway "/api/tiers"

# 2) Rate limit test on /api/users (Free tier: 2 rps)
Write-Host "Testing rate limit on /api/users (5 rapid calls)" -ForegroundColor Cyan
$rateCodes = @()
for ($i=1; $i -le 5; $i++) {
  $rateCodes += Hit-Gateway "/api/users"
  Start-Sleep -Milliseconds 200
}

# Summary
$ok = ($codes + $rateCodes) | Where-Object { $_ -eq 200 } | Measure-Object | Select-Object -ExpandProperty Count
$rl = ($codes + $rateCodes) | Where-Object { $_ -eq 429 } | Measure-Object | Select-Object -ExpandProperty Count
$unauth = ($codes + $rateCodes) | Where-Object { $_ -eq 401 } | Measure-Object | Select-Object -ExpandProperty Count

Write-Host "Summary: OK=$ok, 429=$rl, 401=$unauth" -ForegroundColor Magenta

if ($unauth -gt 0) { Write-Host "One or more requests were unauthorized. Ensure all calls go through the gateway and the Authorization header is set." -ForegroundColor Red }
if ($rl -eq 0) { Write-Host "Rate limit did not trigger; try increasing call rate or ensure Free tier (2 rps) is active for the user." -ForegroundColor Yellow }

Write-Host "Done." -ForegroundColor Green
