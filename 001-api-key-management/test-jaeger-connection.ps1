# Test Jaeger connection and connectivity

Write-Host "=== Testing Jaeger Connection ===" -ForegroundColor Cyan

# Test 1: Check if Jaeger container is running
Write-Host "`n[1] Checking if Jaeger container is running..."
$jaegerContainer = docker ps | Select-String "spamma-jaeger"
if ($jaegerContainer) {
    Write-Host "✓ Jaeger container is running" -ForegroundColor Green
} else {
    Write-Host "✗ Jaeger container is not running" -ForegroundColor Red
    Write-Host "  Run: docker-compose up -d jaeger"
    exit 1
}

# Test 2: Check if Jaeger UI is accessible
Write-Host "`n[2] Checking Jaeger UI on port 16686..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:16686/api/services" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✓ Jaeger UI is accessible (HTTP $($response.StatusCode))" -ForegroundColor Green
    $services = $response.Content | ConvertFrom-Json
    Write-Host "  Services found: $($services.data -join ', ')" -ForegroundColor Yellow
} catch {
    Write-Host "✗ Jaeger UI not accessible on port 16686" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)"
}

# Test 3: Check if OTLP gRPC endpoint is open
Write-Host "`n[3] Checking OTLP gRPC receiver on port 4317..."
$tcpClient = New-Object System.Net.Sockets.TcpClient
try {
    $tcpClient.Connect("localhost", 4317)
    if ($tcpClient.Connected) {
        Write-Host "✓ Port 4317 (OTLP gRPC) is open and listening" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Port 4317 not accessible: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    $tcpClient.Close()
}

# Test 4: Check if OTLP HTTP endpoint is open
Write-Host "`n[4] Checking OTLP HTTP receiver on port 4318..."
$tcpClient = New-Object System.Net.Sockets.TcpClient
try {
    $tcpClient.Connect("localhost", 4318)
    if ($tcpClient.Connected) {
        Write-Host "✓ Port 4318 (OTLP HTTP) is open and listening" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Port 4318 not accessible: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    $tcpClient.Close()
}

# Test 5: Check app configuration
Write-Host "`n[5] Checking Spamma app configuration..."
$appSettingsPath = "src/Spamma.App/Spamma.App/appsettings.json"
if (Test-Path $appSettingsPath) {
    $settings = Get-Content $appSettingsPath | ConvertFrom-Json
    if ($settings.OpenTelemetry -and $settings.OpenTelemetry.OtlpEndpoint) {
        Write-Host "✓ appsettings.json configured with: $($settings.OpenTelemetry.OtlpEndpoint)" -ForegroundColor Green
    } else {
        Write-Host "⚠ OpenTelemetry:OtlpEndpoint not found in appsettings" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ appsettings.json not found at $appSettingsPath" -ForegroundColor Red
}

# Test 6: Check environment variables
Write-Host "`n[6] Checking environment variables..."
$otelEnv = $env:OTEL_EXPORTER_OTLP_ENDPOINT
if ($otelEnv) {
    Write-Host "✓ OTEL_EXPORTER_OTLP_ENDPOINT = $otelEnv" -ForegroundColor Green
} else {
    Write-Host "ℹ OTEL_EXPORTER_OTLP_ENDPOINT not set (will use appsettings default)" -ForegroundColor Yellow
}

Write-Host "`n=== Testing Summary ===" -ForegroundColor Cyan
Write-Host "If all checks pass, try:"
Write-Host "  1. Run the app: dotnet run --project src/Spamma.App/Spamma.App"
Write-Host "  2. Make a request to the app (e.g., open http://localhost:5000)"
Write-Host "  3. Check Jaeger UI: http://localhost:16686"
Write-Host "  4. Select 'spamma' service from the dropdown"
Write-Host ""
Write-Host "If you still don't see traces, check:"
Write-Host "  - App logs for OpenTelemetry startup messages"
Write-Host "  - Jaeger logs: docker-compose logs jaeger"
Write-Host "  - OTEL endpoint configuration"
