# Spamma Observability & OpenTelemetry

Spamma includes built-in observability through **OpenTelemetry** with **OTLP (OpenTelemetry Protocol)** exporter for maximum flexibility.

## Overview

OpenTelemetry provides three pillars of observability:

- **Traces**: Request flow through the application (ASP.NET Core, HTTP clients)
- **Metrics**: Application health and performance (runtime, request rates, latencies)
- **Logs**: Structured logging with context correlation

All telemetry is exported via **OTLP/gRPC** to any compatible backend.

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://localhost:4317` | OTLP collector endpoint (gRPC) |
| `OTEL_EXPORTER_OTLP_HEADERS` | (empty) | Optional headers (e.g., API keys for DataDog, Honeycomb) |
| `OTEL_SERVICE_NAME` | `spamma` | Service identifier in observability backend |

### Example Docker Compose

Send telemetry to a local **Jaeger** instance for development:

```bash
# Set endpoint to Jaeger collector
export OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317

# Start Jaeger (in docker-compose.yml or standalone)
docker run -d \
  -p 4317:4317 \
  -p 16686:16686 \
  jaegertracing/all-in-one:latest
```

Then access Jaeger UI at `http://localhost:16686`

## What's Instrumented

### Tracing

- **ASP.NET Core**: Incoming HTTP requests, response times, error codes
- **HTTP Client**: Outgoing HTTP calls (external APIs, webhooks)
- **Custom Spans**: Domain logic, CQRS handlers (add `ITracer` injection if needed)

### Metrics

- **ASP.NET Core**: Active connections, request counts, latencies, errors
- **HTTP Client**: Request duration, active connections
- **Runtime**: CPU, memory, GC statistics, thread count
- **Redis**: Command count, latency, connection pool stats

### Logging

- **Structured Logs**: All application logs with context (TraceID, SpanID)
- **Log Levels**: Debug, Information, Warning, Error, Critical
- **Correlation**: Logs automatically linked to traces via TraceID

## Supported Backends

### Local Development

**Jaeger** (free, OSS)

```bash
docker run -p 4317:4317 -p 16686:16686 jaegertracing/all-in-one
# UI: http://localhost:16686
```

**Grafana Loki** (logs) + **Prometheus** (metrics) + **Grafana** (visualization)

```bash
docker-compose -f docker-compose.yml -f docker-compose.observability.yml up
```

### Cloud SaaS

| Backend | Endpoint | Setup |
|---------|----------|-------|
| **DataDog** | `https://otlp.datadoghq.com:443` | [Docs](https://docs.datadoghq.com/opentelemetry/) |
| **Honeycomb** | `https://api.honeycomb.io:443` | [Docs](https://docs.honeycomb.io/getting-data-in/otel-collector/) |
| **New Relic** | `https://otlp.nr-data.net:4317` | [Docs](https://docs.newrelic.com/docs/more-integrations/open-source-telemetry-integrations/opentelemetry/opentelemetry-intro/) |
| **Grafana Cloud** | `https://<tenant>.otlp-gateway-<region>.grafana.net/otlp` | [Docs](https://grafana.com/docs/grafana-cloud/send-data/otlp/send-otlp-data-to-grafana-cloud/) |
| **Lightstep** | `ingest.lightstep.com:443` | [Docs](https://docs.lightstep.com/otel/otel-quick-start) |
| **Jaeger** | `localhost:4317` | [Docs](https://www.jaegertracing.io/docs/latest/deployment/) |

### Example: DataDog

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.datadoghq.com:443
export OTEL_EXPORTER_OTLP_HEADERS="dd-api-key=YOUR_API_KEY"
```

## Development Setup

### Quick Start with Jaeger

1. **Start Jaeger**:

   ```bash
   docker run -d -p 4317:4317 -p 16686:16686 jaegertracing/all-in-one:latest
   ```

2. **Run Spamma**:

   ```bash
   export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
   dotnet run --project src/Spamma.App/Spamma.App
   ```

3. **Open UI** at `http://localhost:16686`
   - Select "spamma" service
   - View traces, spans, latencies, errors

### Custom Instrumentation

To add custom tracing to domain logic:

```csharp
using System.Diagnostics;

public class MyCommandHandler
{
    private static readonly ActivitySource ActivitySource = 
        new("Spamma.Modules.MyModule");

    public async Task Handle(MyCommand command)
    {
        using var activity = ActivitySource.StartActivity("ProcessCommand");
        activity?.SetTag("command.id", command.Id);
        
        // Business logic
        await _repository.SaveAsync(command);
        
        activity?.SetTag("status", "success");
    }
}
```

Register in Program.cs:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Spamma.Modules.*")
        .AddOtlpExporter());
```

## Performance Considerations

- **OTLP Batching**: Traces/metrics batched before export (default: 512 items or 5s timeout)
- **Sampling**: In production, consider sampling (e.g., 10% of traces) to reduce overhead
- **Export Timeout**: Default 10s; adjust via `OTEL_EXPORTER_OTLP_TIMEOUT_MILLIS`
- **Memory**: Typical overhead ~50MB; tune `OTEL_*_MEMORY_LIMIT` if constrained

## Troubleshooting

### No Data Appearing

1. Check endpoint is reachable:

   ```bash
   curl -i http://localhost:4317/
   ```

2. Verify environment variable:

   ```bash
   echo $OTEL_EXPORTER_OTLP_ENDPOINT
   ```

3. Check application logs for errors (usually silent if collector unavailable)

### Too Much Data

Enable sampling in production:

```bash
export OTEL_TRACES_SAMPLER=parentbased_traceidratio
export OTEL_TRACES_SAMPLER_ARG=0.1  # 10% sample rate
```

### High Memory Usage

Reduce batch size:

```bash
export OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE=delta
export OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf  # More compact
```

## References

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [OTLP Protocol Spec](https://opentelemetry.io/docs/specs/otlp/)
- [Jaeger Getting Started](https://www.jaegertracing.io/docs/latest/getting-started/)
- [Grafana Loki + Prometheus Stack](https://grafana.com/get-started/#intro-observability)
