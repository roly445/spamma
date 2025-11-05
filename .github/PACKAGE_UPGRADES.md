# NuGet Package Upgrade Tracking

This document tracks packages that are currently pre-release but should be added back when stable versions become available.

## Pending Stable Release

### OpenTelemetry.Instrumentation.EntityFrameworkCore
- **Current Status**: Pre-release only (1.13.0-beta.1 as of Oct 31, 2025)
- **Why Removed**: No stable GA release available yet
- **Semantic Conventions**: Experimental (subject to breaking changes)
- **Action**: Re-add to `src/Spamma.App/Spamma.App/Spamma.App.csproj` when stable version released
- **Installation Command**: `dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version [STABLE_VERSION]`
- **Configuration**: Manually add to Program.cs OpenTelemetry configuration:
  ```csharp
  .WithTracing(tracing =>
      tracing
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
          .AddEntityFrameworkCoreInstrumentation()  // Add this line
          .AddOtlpExporter())
  ```
- **Reference**: https://www.nuget.org/packages/OpenTelemetry.Instrumentation.EntityFrameworkCore/

### OpenTelemetry.Instrumentation.StackExchangeRedis
- **Current Status**: Pre-release only (1.13.0-beta.1 as of Oct 31, 2025)
- **Why Removed**: No stable GA release available yet
- **Previous Version Used**: 1.0.0-rc9.14 (release candidate)
- **Semantic Conventions**: Experimental (subject to breaking changes)
- **Action**: Re-add to `src/Spamma.App/Spamma.App/Spamma.App.csproj` when stable version released
- **Installation Command**: `dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis --version [STABLE_VERSION]`
- **Configuration**: Can be auto-discovered via `IConnectionMultiplexer` in DI container (no manual configuration needed)
- **Reference**: https://www.nuget.org/packages/OpenTelemetry.Instrumentation.StackExchangeRedis/

## Strategy

These packages provide valuable observability for:
- **EntityFrameworkCore**: Database query tracing (helpful for debugging slow queries)
- **StackExchangeRedis**: Redis operation tracing (helpful for cache/session performance)

Once the OpenTelemetry team releases stable versions with GA semantic conventions, add them back to enable full end-to-end observability. This will provide traces for:
1. HTTP requests (already instrumented)
2. Database queries (via EF Core instrumentation)
3. Redis operations (via StackExchangeRedis instrumentation)

## Monitoring

Check periodically:
- https://www.nuget.org/packages/OpenTelemetry.Instrumentation.EntityFrameworkCore/
- https://www.nuget.org/packages/OpenTelemetry.Instrumentation.StackExchangeRedis/

Look for versions without `-beta`, `-rc`, `-alpha` suffixes (i.e., stable GA releases).

## Last Updated
October 31, 2025 - All packages except these two are at stable versions
