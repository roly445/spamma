Write-Host "=== Testing Jaeger Connection ===" -ForegroundColor Cyan

# Test 1: Check if Jaeger container is running
Write-Host "`n[1] Checking if Jaeger container is running..."
$jaegerContainer = docker ps | Select-String "spamma-jaeger"
if ($jaegerContainer) {
    Write-Host "OK: Jaeger container is running" -ForegroundColor Green
} else {
    Write-Host "FAIL: Jaeger container is not running" -ForegroundColor Red
    exit 1
}

# Test 2: Check if Jaeger UI is accessible
Write-Host "`n[2] Checking Jaeger UI on port 16686..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:16686/api/services" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "OK: Jaeger UI accessible (HTTP $($response.StatusCode))" -ForegroundColor Green
    $services = $response.Content | ConvertFrom-Json
    if ($services.data) {
        Write-Host "    Services: $($services.data -join ', ')" -ForegroundColor Yellow
    } else {
        Write-Host "    No services yet (normal if app hasn't sent traces)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "FAIL: Jaeger UI not accessible on port 16686" -ForegroundColor Red
}

# Test 3: Check if OTLP gRPC endpoint is open
Write-Host "`n[3] Checking OTLP gRPC on port 4317..."
$tcpClient = New-Object System.Net.Sockets.TcpClient
try {
    $tcpClient.Connect("localhost", 4317)
    if ($tcpClient.Connected) {
        Write-Host "OK: Port 4317 listening" -ForegroundColor Green
    }
} catch {
    Write-Host "FAIL: Port 4317 not accessible" -ForegroundColor Red
} finally {
    $tcpClient.Close()
}

# Test 4: Check configuration
Write-Host "`n[4] Checking OpenTelemetry config..."
if (Test-Path "src/Spamma.App/Spamma.App/appsettings.json") {
    $settings = Get-Content "src/Spamma.App/Spamma.App/appsettings.json" | ConvertFrom-Json
    if ($settings.OpenTelemetry.OtlpEndpoint) {
        Write-Host "OK: Config endpoint = $($settings.OpenTelemetry.OtlpEndpoint)" -ForegroundColor Green
    }
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Start the app: dotnet run --project src/Spamma.App/Spamma.App"
Write-Host "2. Open browser: http://localhost:5000"
Write-Host "3. Jaeger UI: http://localhost:16686"
Write-Host "4. Select 'spamma' service"
