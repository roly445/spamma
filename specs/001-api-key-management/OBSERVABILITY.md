# OpenTelemetry Tracing with Jaeger

Simple distributed tracing setup for the Spamma application using Jaeger.

## Components

### Jaeger (Port 16686)
- Distributed trace collection and visualization
- View end-to-end request flows through the system
- Performance analysis and debugging
- **URL**: http://localhost:16686

## Quick Start

### 1. Start Jaeger
```powershell
docker-compose up -d jaeger
```

### 2. Start the Spamma app
```powershell
dotnet run --project src/Spamma.App/Spamma.App
```

The app will automatically send traces to `http://localhost:4317` (OTLP endpoint).

### 3. View traces

Open http://localhost:16686 in your browser.

## Configuration

The Spamma app is pre-configured to send traces via OTLP to `localhost:4317`.

### Environment Variables
```powershell
# Change the OTLP endpoint (default: http://localhost:4317)
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://your-host:4317"

# Enable detailed logging (debug/info/warn/error)
$env:OTEL_LOG_LEVEL = "info"

# Set service name
$env:OTEL_SERVICE_NAME = "spamma"
```

## Viewing Traces

### In Jaeger
1. Open http://localhost:16686
2. Select service: `spamma`
3. Click "Find Traces"
4. Click a trace to see detailed timeline and span information

### Example Traces
- **Authentication**: `StartAuthenticationCommand` → email validation → cookie issuance
- **Email Reception**: SMTP → domain validation → database storage
- **API Requests**: HTTP → middleware → handlers → database

## Troubleshooting

### No traces appearing in Jaeger
1. Verify app started successfully
2. Check that app can reach `http://localhost:4317`
3. Make some requests to the app to generate traces
4. Check container logs:
   ```powershell
   docker-compose logs jaeger
   ```

### Container won't start
```powershell
# Check logs
docker-compose logs jaeger

# Rebuild and restart
docker-compose down
docker-compose up -d jaeger
```

## Storage

Jaeger stores data in memory by default. For local development, this is fine.

To reset all traces:
```powershell
docker-compose restart jaeger
```

## References

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Jaeger UI Guide](https://www.jaegertracing.io/docs/latest/frontend-ui/)
