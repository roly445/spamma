# Debugging Traces in Jaeger

## Connection Status: ✅ ALL SYSTEMS GO

Your Jaeger setup is perfect:
- ✅ Jaeger container running
- ✅ OTLP endpoint listening on 4317
- ✅ UI accessible at http://localhost:16686
- ✅ 'spamma' service already registered (meaning traces have been sent!)

## Why You Might Not See Traces

### Issue 1: Browser Caching / Service Not Visible
**Solution:**
1. Open http://localhost:16686
2. In the dropdown at top-left, select "spamma" (not "jaeger-all-in-one")
3. Click "Find Traces"
4. If nothing shows, check the time range (top right - make sure it includes "now")

### Issue 2: Not Enough Requests to Generate Traces
**Solution:** Generate some traffic to the app:
```powershell
# Terminal 1: Start Jaeger (if not running)
docker-compose up -d jaeger

# Terminal 2: Start the app
dotnet run --project src/Spamma.App/Spamma.App

# Terminal 3: Generate some HTTP requests
# Wait a few seconds, then:
curl http://localhost:5000/dynamicsettings.json

# Repeat a few times to create multiple traces
for ($i=0; $i -lt 5; $i++) {
    curl http://localhost:5000/dynamicsettings.json
    Start-Sleep -Seconds 1
}
```

Then check Jaeger UI for traces.

### Issue 3: App Not Running or Not Sending Traces
**Solution:** Check app logs for OpenTelemetry startup:
```powershell
# When you run the app, look for this in startup logs:
# "Starting OpenTelemetry gRPC exporter"
# or similar OTLP export messages

# If you don't see any, add this to appsettings.json:
# "Logging": {
#   "LogLevel": {
#     "OpenTelemetry": "Debug"
#   }
# }
```

### Issue 4: Wrong Endpoint Configuration
**Solution:** Verify the app is using the right endpoint:
```powershell
# The app should send to http://localhost:4317
# Verify with:
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
dotnet run --project src/Spamma.App/Spamma.App
```

## Quick Test Command

Run this to make test requests and see traces:

```powershell
# Start in separate terminals
docker-compose up -d jaeger
dotnet run --project src/Spamma.App/Spamma.App

# In another terminal, run these 5 times:
for ($i=0; $i -lt 5; $i++) { 
    Write-Host "Request $($i+1)..."
    curl.exe http://localhost:5000/dynamicsettings.json
    Start-Sleep -Seconds 1
}

# Then go to http://localhost:16686
# Select "spamma" service
# Click "Find Traces"
```

## Trace Verification Checklist

- [ ] Jaeger UI shows "spamma" service in dropdown
- [ ] You've made at least one HTTP request to the app
- [ ] App is still running (check its logs)
- [ ] Time range on Jaeger UI includes the request time
- [ ] You selected "spamma" service (not "jaeger-all-in-one")
- [ ] You clicked "Find Traces" button

## Common Issues

| Problem | Solution |
|---------|----------|
| Empty trace list | Make an HTTP request first |
| No "spamma" service | App not running or OpenTelemetry not configured |
| Old traces only | Use time picker to look at recent time range |
| Port 4317 in use | Change endpoint: `$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4318"` |

## Additional Testing

If you want to see detailed what the app is sending:

```powershell
# Check app's OpenTelemetry configuration
$settings = Get-Content src/Spamma.App/Spamma.App/appsettings.json | ConvertFrom-Json
Write-Host $settings.OpenTelemetry.OtlpEndpoint

# Check Jaeger is receiving on port 4317
docker-compose logs jaeger | Select-String "4317" -Last 5
```

Got it working? Great! Now you have full distributed tracing of your requests through Spamma. 🎉
