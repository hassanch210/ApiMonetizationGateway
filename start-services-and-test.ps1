$ErrorActionPreference = 'Stop'
$logs = 'D:\my\Proj\logs'
if (-not (Test-Path $logs)) { New-Item -ItemType Directory -Path $logs | Out-Null }

function Start-Svc($projPath, $outFile, $errFile) {
  Start-Process -FilePath 'dotnet' -ArgumentList ("run --project `"$projPath`"") -RedirectStandardOutput $outFile -RedirectStandardError $errFile -PassThru
}

$procs = @()
$procs += Start-Svc 'D:\my\Proj\Gateway\UsageTrackingService\ApiMonetizationGateway.UsageTrackingService' "$logs\usage_out.txt" "$logs\usage_err.txt"
$procs += Start-Svc 'D:\my\Proj\Gateway\UserService\ApiMonetizationGateway.UserService' "$logs\user_out.txt" "$logs\user_err.txt"
$procs += Start-Svc 'D:\my\Proj\Gateway\TierService\ApiMonetizationGateway.TierService' "$logs\tier_out.txt" "$logs\tier_err.txt"
$procs += Start-Svc 'D:\my\Proj\Gateway\SampleApiService\ApiMonetizationGateway.SampleApi' "$logs\sample_out.txt" "$logs\sample_err.txt"
$procs += Start-Svc 'D:\my\Proj\Gateway\BillingService\ApiMonetizationGateway.BillingService' "$logs\billing_out.txt" "$logs\billing_err.txt"

Start-Sleep -Seconds 12
$procs += Start-Svc 'D:\my\Proj\Gateway\Gateway\ApiMonetizationGateway.Gateway' "$logs\gateway_out.txt" "$logs\gateway_err.txt"

Start-Sleep -Seconds 8
powershell -ExecutionPolicy Bypass -File 'D:\my\Proj\test-gateway-e2e.ps1'

"PIDS: " + ($procs | ForEach-Object { $_.Id } | Out-String)
