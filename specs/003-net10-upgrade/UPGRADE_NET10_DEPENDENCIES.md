# .NET 10 Upgrade - Dependencies Analysis

**Date**: November 17, 2025  
**Upgrade**: .NET 9 → .NET 10

## Executive Summary

All critical dependencies were already compatible with .NET 10. The upgrade required minimal package changes:
- ✅ **Zero breaking dependency updates required**
- ✅ **1 prunable package removed** (Microsoft.AspNetCore.SignalR - now framework-included)
- ✅ **All core packages already on compatible versions**

## Core Framework Dependencies

| Package | Version | .NET 10 Status | Notes |
|---------|---------|----------------|-------|
| **Event Sourcing** |
| Marten | 8.13.3 | ✅ Compatible | Already on v8.x - full .NET 10 support |
| Marten.AspNetCore | 8.13.3 | ✅ Compatible | Matches Marten core version |
| **Messaging** |
| DotNetCore.CAP | 8.4.1 | ✅ Compatible | Already on v8.x - .NET 10 verified |
| DotNetCore.CAP.RedisStreams | 8.4.1 | ✅ Compatible | Matches CAP core version |
| **Validation** |
| FluentValidation | 12.0.0 | ✅ Compatible | Already on v12.x - .NET 10 ready |
| FluentValidation.AspNetCore | 11.3.0 | ✅ Compatible | ASP.NET Core integration stable |
| FluentValidation.DependencyInjectionExtensions | 11.11.1 | ✅ Compatible | DI extensions stable |

## Microsoft ASP.NET Core Packages

| Package | Version | .NET 10 Status | Action Taken |
|---------|---------|----------------|--------------|
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.0 | ✅ Updated | Framework package - auto-updated |
| Microsoft.AspNetCore.Components.WebAssembly.Server | 10.0.0 | ✅ Updated | Framework package - auto-updated |
| Microsoft.AspNetCore.SignalR | 1.2.0 | ⚠️ **REMOVED** | Now included in ASP.NET Core 10 framework |
| Microsoft.AspNetCore.OpenApi | 10.0.0 | ✅ Updated | Framework package - auto-updated |

## Observability & Telemetry

| Package | Version | .NET 10 Status | Notes |
|---------|---------|----------------|-------|
| OpenTelemetry | 1.10.0 | ✅ Compatible | Stable release with .NET 10 support |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.10.0 | ✅ Compatible | OTLP exporter compatible |
| OpenTelemetry.Extensions.Hosting | 1.10.0 | ✅ Compatible | Host integration compatible |
| OpenTelemetry.Instrumentation.AspNetCore | 1.10.0 | ✅ Compatible | ASP.NET Core instrumentation |
| OpenTelemetry.Instrumentation.Http | 1.10.0 | ✅ Compatible | HTTP client instrumentation |

## Testing Frameworks

| Package | Version | .NET 10 Status | Notes |
|---------|---------|----------------|-------|
| xunit | 2.7.0 | ✅ Compatible | Latest stable - .NET 10 verified |
| xunit.runner.visualstudio | 2.5.7 | ✅ Compatible | Visual Studio test runner |
| Microsoft.NET.Test.Sdk | 17.9.0 | ✅ Compatible | Test SDK compatible |
| Moq | 4.20.72 | ✅ Compatible | Mocking framework stable |
| FluentAssertions | 6.12.2 | ✅ Compatible | Assertion library stable |
| coverlet.collector | 6.0.1 | ✅ Compatible | Code coverage collector |

## Security & Authentication

| Package | Version | .NET 10 Status | Notes |
|---------|---------|----------------|-------|
| Fido2.NetFramework | 4.0.0 | ✅ Compatible | WebAuthn/Passkey support verified |
| Fido2.AspNet | 4.0.0 | ✅ Compatible | ASP.NET Core integration |

## Utilities & Extensions

| Package | Version | .NET 10 Status | Notes |
|---------|---------|----------------|-------|
| MimeKit | 4.9.0 | ✅ Compatible | Email MIME parsing - .NET 10 compatible |
| SmtpServer | 10.0.3 | ✅ Compatible | SMTP server library stable |
| MinVer | 6.0.0 | ✅ Compatible | Semantic versioning tool |
| CSharpFunctionalExtensions | 3.1.0 | ✅ Compatible | Result/Maybe monad library |

## Breaking Changes Summary

### API Changes Required

1. **JsonSerializer Ambiguity** (CS0121)
   - **Location**: `Spamma.Modules.UserManagement/Infrastructure/Services/UserStatusCache.cs`
   - **Issue**: .NET 10 added new `JsonSerializer.Deserialize<T>(ReadOnlySpan<byte>)` overload causing ambiguity
   - **Fix**: Added explicit string cast: `JsonSerializer.Deserialize<CachedUser>((string)value!)`

2. **ForwardedHeadersOptions.KnownNetworks Obsolete** (ASPDEPR005)
   - **Location**: `Spamma.App/Program.cs`
   - **Issue**: API renamed in .NET 10
   - **Fix**: Changed `options.KnownNetworks.Clear()` → `options.KnownIPNetworks.Clear()`

3. **Blazor Form Parameter Null-Safety** (BL0008)
   - **Locations**: 
     - `Login.razor.cs`
     - `VerifyLogin.razor.cs`
     - `Setup/Login.razor.cs`
     - `Setup/Complete.razor.cs`
   - **Issue**: .NET 10 Blazor enforces stricter null-safety for `[SupplyParameterFromForm]` properties
   - **Fix**: Made properties nullable (`LoginModel?`) and added null-forgiving operators at usage sites (`Model!.Email`)

### Package Removals

1. **Microsoft.AspNetCore.SignalR** (NU1510)
   - **Reason**: Now included in ASP.NET Core 10 framework
   - **Action**: Removed from `Spamma.App.csproj`

## Validation Results

### Build Verification
```
✅ All 19 projects build successfully
✅ Zero new compiler warnings (2 pre-existing warnings from Spamma.Analyzers targeting net7.0)
✅ Build time: ~11 seconds (well under 2-minute requirement)
```

### Test Validation
```
✅ 843 total tests
✅ 822 passed
✅ 21 skipped (E2E tests - requires infrastructure)
✅ 0 failed
✅ Test duration: 91.1 seconds
✅ 100% test success rate
```

### Runtime Verification
```
✅ Application starts successfully on .NET 10.0.0
✅ All services initialize correctly
✅ PostgreSQL connection established
✅ Redis connection established
✅ CAP framework started with .NET 10.0.0
✅ Startup time: <10 seconds
```

### Security Scan
```
✅ No vulnerable packages detected
✅ dotnet list package --vulnerable returned zero issues
```

## Recommendations

### Immediate Actions (Completed)
- ✅ Update all .csproj files to `<TargetFramework>net10</TargetFramework>`
- ✅ Update `global.json` to SDK version `10.0.0`
- ✅ Fix API deprecations (KnownNetworks → KnownIPNetworks)
- ✅ Resolve JsonSerializer ambiguity with explicit casts
- ✅ Update Blazor form properties for stricter null-safety
- ✅ Remove Microsoft.AspNetCore.SignalR package reference
- ✅ Update Dockerfile base images to .NET 10
- ✅ Update GitHub Actions workflows to .NET 10 SDK

### Future Considerations
- Monitor Marten 9.x release for potential event store improvements
- Watch CAP framework updates for .NET 10 performance optimizations
- Consider updating FluentValidation.AspNetCore to v12.x when available (currently v11.3.0 stable)
- Evaluate OpenTelemetry 2.x when released for enhanced observability features

## References

- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [ASP.NET Core 10 Breaking Changes](https://learn.microsoft.com/en-us/aspnet/core/migration/90-to-100)
- [Marten v8 Documentation](https://martendb.io/)
- [CAP .NET Compatibility Matrix](https://cap.dotnetcore.xyz/)
