# NuGet Dependency Audit - .NET 10 Upgrade

**Date**: 2025-11-17  
**Purpose**: Document current packages and .NET 10 compatibility

## Core Dependencies Assessment

| Package | Current Version | Target Version | .NET 10 Compatible | Breaking Changes | Priority | Action Required |
|---------|----------------|----------------|-------------------|------------------|----------|----------------|
| **Marten** | 8.13.3 | 8.13.3+ | ✅ Yes | None (already v8) | HIGH | Update to latest 8.x patch |
| **CAP** | 8.4.1 | 8.4.1+ | ✅ Yes | None (already v8) | HIGH | Update to latest 8.x patch |
| **FluentValidation** | 12.0.0 | 12.0.0+ | ✅ Yes | None (already v12) | HIGH | Update to latest 12.x patch |
| **Blazor (ASP.NET Core)** | 9.0.10 | 10.0.x | ✅ Yes | Framework updates | HIGH | Built-in framework update |
| **Grpc.AspNetCore** | 2.67.0 | 2.67.0+ | ✅ Yes | None | MEDIUM | Update to latest stable |
| **Fido2/Fido2.AspNet** | 4.0.0 | 4.0.0+ | ✅ Yes | None | MEDIUM | Check for .NET 10 support |
| **FluentEmail** | 3.0.2 | 3.0.2+ | ✅ Yes | None | MEDIUM | Update if available |
| **OpenTelemetry** | 1.13.x | 1.13.x+ | ✅ Yes | None | MEDIUM | Update to latest stable |
| **SmtpServer** | 11.0.0 | 11.0.0+ | ✅ Yes | None | MEDIUM | Check for .NET 10 support |

## Microsoft Packages (Framework-Specific)

| Package | Current Version | Target Version | Notes |
|---------|----------------|----------------|-------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.10 | 10.0.x | Auto-updated with framework |
| Microsoft.AspNetCore.Components.WebAssembly.Server | 9.0.10 | 10.0.x | Auto-updated with framework |
| Microsoft.Extensions.Caching.StackExchangeRedis | 9.0.10 | 10.0.x | Auto-updated with framework |

## Analysis Summary

### ✅ Good News
- **Already on v8+ for CAP and Marten**: These are the latest major versions and fully support .NET 10
- **Already on v12 for FluentValidation**: Latest major version, no upgrade needed
- **No major version bumps required**: All core dependencies are already on .NET 10-compatible versions

### 📋 Action Items
1. **Update project files**: Change all `<TargetFramework>net9</TargetFramework>` to `net10`
2. **Update Microsoft packages**: Framework packages will auto-resolve to 10.x versions
3. **Update patch versions**: Run `dotnet restore` to get latest compatible patches
4. **Test for warnings**: Run build and address any deprecated API warnings

### ⚠️ Low Risk Assessment
- **Risk Level**: LOW - All major dependencies already compatible
- **Breaking Changes**: Minimal - only framework-level changes expected
- **Estimated Effort**: 4-6 hours (mostly testing and validation)

## Upgrade Strategy

**Per Clarification Q2**: Target latest stable minor/patch within current major series.

Since we're already on:
- CAP v8 (latest major)
- Marten v8 (latest major)  
- FluentValidation v12 (latest major)

**Strategy**: Update to latest patch versions within current major series during the upgrade.

## Next Steps

1. ✅ Update global.json to 10.0.0 (DONE)
2. ⏭️ Update all .csproj files to target net10
3. ⏭️ Run `dotnet restore --no-cache` to fetch .NET 10 compatible versions
4. ⏭️ Run `dotnet build` and fix any compiler warnings
5. ⏭️ Run `dotnet test` to validate all tests pass
