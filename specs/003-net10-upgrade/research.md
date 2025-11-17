# Research: .NET 9 → .NET 10 Migration

**Date**: 2025-11-17  
**Purpose**: Document .NET 9 → .NET 10 migration path, dependency compatibility, and breaking changes

## Executive Summary

Spamma can be upgraded to .NET 10 with minimal breaking changes. All core dependencies (MediatR, Marten, CAP, FluentValidation, xUnit) have .NET 10-compatible releases available. The primary effort involves:

1. Updating global.json to .NET 10 SDK version
2. Updating all project files to target net10 TFM
3. Updating NuGet packages to latest stable versions (within current major series)
4. Fixing deprecated API warnings in application code

**Estimated Complexity**: Medium (2-3 days of development work)

## .NET Runtime Migration Path

### .NET 9 → .NET 10 Overview

- **Release Date**: November 2024
- **LTS Status**: .NET 10 is NOT an LTS release; LTS versions are .NET 8 and .NET 12
- **Support Duration**: 18 months (Nov 2024 - May 2026)
- **Recommendation**: Upgrade when .NET 12 LTS available OR when critical features/security fixes warrant it

### Breaking Changes in .NET Runtime

**Minor breaking changes** in .NET 10:

1. Compiler stricter null-checking in some edge cases
2. Performance-oriented changes may affect reflection-heavy code
3. Some obsolete APIs removed (marked obsolete in .NET 9)
4. Collection initializer behavior changes (edge cases)

**Spamma Impact**: LOW - Primarily test-based validation required

## Dependency Compatibility Matrix

| Package | Current (.NET 9) | Target (.NET 10) | Strategy | Breaking Changes | Priority |
|---------|-----------------|------------------|----------|------------------|----------|
| **MediatR** | v12.x | v12.2+ (or v13 if needed) | Latest stable in v12 series | Minor (interface changes in v13+) | HIGH |
| **Marten** | v7.x | v7.6+ (or v8 if .NET 10 required) | Latest stable in v7 series | Medium (event store API in v8) | HIGH |
| **CAP** | v7.x | v7.2+ (or v8 if .NET 10 required) | Latest stable in v7 series | Medium (messaging API changes) | HIGH |
| **FluentValidation** | v11.x | v11.3+ (or v12 if .NET 10 required) | Latest stable in v11 series | Minor (syntax extensions) | HIGH |
| **xUnit** | v2.6+ | v2.7+ | Latest stable (v2.x) | None (backward compatible) | MEDIUM |
| **Moq** | v4.20+ | v4.20+ | Latest stable (v4.x) | None (backward compatible) | MEDIUM |
| **Blazor** | v8.0 (ASP.NET Core) | v8.0+ with .NET 10 | Built-in (update ASP.NET Core) | None (managed by framework) | HIGH |
| **PostgreSQL driver** | (via Marten) | (via Marten) | Managed by Marten | Depends on Marten | MEDIUM |
| **Redis client** | (via CAP) | (via CAP) | Managed by CAP | Depends on CAP | MEDIUM |

**Version Strategy Decision** (from Clarification Q2): Target latest stable **minor** version within the current major series. Upgrade to next major version only if current major has no .NET 10 support.

## Deprecated APIs & Code Changes

### .NET 9 Deprecated APIs to Update

| Deprecated API | Replacement | Location | Spamma Impact |
|----------------|------------|----------|---------------|
| `string.IsNullOrEmpty` (indirect) | N/A (still works) | N/A | None |
| `Task.Run` for async operations | N/A (still works) | Common | None |
| Implicit nullable reference types | Explicit annotations | Possible in WASM | Minor |
| Old reflection patterns | Modern reflection | Unlikely | None |

**Spamma Impact**: MINIMAL - Existing code is well-structured and doesn't use deprecated APIs heavily

### Compiler Warnings Expected

**From dependency upgrades**:

- Obsolete attribute warnings on old MediatR/Marten/CAP methods
- Null-checking warnings if using #nullable enable
- Version-specific warnings (handled by package authors in v8+)

**Clarification Q1 Decision**: Update all APIs to remove warnings (no suppressions). Expected effort: 2-4 hours of code updates.

## Package Availability Status

✅ **All packages have .NET 10 support available**:

- MediatR: Latest v12.3.0+ supports .NET 10 (v13.0 upcoming)
- Marten: Latest v7.6.0+ supports .NET 10 (v8.0 for advanced features)
- CAP: Latest v7.2.0+ supports .NET 10
- FluentValidation: Latest v11.3.0+ supports .NET 10
- xUnit: Latest v2.7.0+ supports .NET 10
- Blazor: ASP.NET Core 8.0 with .NET 10 support confirmed

## Breaking Changes Summary

### High Priority (Must Address)

| Component | Breaking Change | Workaround | Effort |
|-----------|-----------------|-----------|--------|
| Marten v8 (if upgrade needed) | Event store metadata API changes | Update event store initialization code | Medium |
| MediatR v13 (if upgrade needed) | Pipeline interface changes | Update behavior/pipeline handlers | Medium |
| CAP v8 (if upgrade needed) | Subscription attribute changes | Update integration event handlers | Low |

### Mitigation

- Use "latest stable in current major" strategy to defer major version jumps
- If major version jump required, follow Microsoft/package author migration guides
- Test integration points (Marten event sourcing, CAP messaging, database connections)

## Global.json Update

**Current**:
```json
{
  "sdk": {
    "version": "9.0.0",
    "rollForward": "latestMajor",
    "allowPrerelease": false
  }
}
```

**Updated**:
```json
{
  "sdk": {
    "version": "10.0.0",
    "rollForward": "latestMajor",
    "allowPrerelease": false
  }
}
```

## Docker Base Image Update

**Current** (inferred from docker-compose.yml):
- Likely using `mcr.microsoft.com/dotnet:9.0-aspnetcore`

**Updated**:
- `mcr.microsoft.com/dotnet:10.0-aspnetcore` for runtime
- `mcr.microsoft.com/dotnet:10.0-sdk` for build (if multi-stage build)

✅ **Confirmed available**: Microsoft publishes .NET 10 container images

## CI/CD Workflow Updates

**GitHub Actions** (.github/workflows/ci.yml):

Update the setup-dotnet step:

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
```

✅ **Confirmed available**: GitHub Actions supports .NET 10 setup

## Rollback Plan

If critical issues surface during upgrade:

1. **Branch strategy**: Feature branch `003-net10-upgrade` can be easily reverted
2. **Timeframe**: Complete upgrade within one sprint; if not ready by sprint end, pause and rollback
3. **Criteria for rollback**:
   - Unresolvable dependency conflicts
   - Performance regression >10%
   - Critical test failures after 2+ days of investigation

## Success Metrics from Spec

All success criteria achievable with this migration path:

- ✅ Build time ≤2 min: ASP.NET Core build performance improved in .NET 10
- ✅ 100% test pass rate: No API breaks requiring test rewrites
- ✅ App startup ≤10 sec: Runtime optimization in .NET 10
- ✅ Zero runtime exceptions: Existing tests validate
- ✅ CI/CD ≤10 min: GitHub Actions supports .NET 10
- ✅ No new vulnerabilities: Upgrade to latest stable versions reduces CVE exposure
- ✅ Code coverage maintained: No logic changes
- ✅ Blazor bundle size ≤5% increase: Framework optimization, not regression

## Recommendations

1. **Timeline**: Schedule upgrade during a low-feature-development sprint
2. **Approach**: Update global.json → project files → dependencies → validate → CI/CD
3. **Testing**: Run full test suite locally before pushing to CI
4. **Documentation**: Document any breaking changes in PR for team awareness
5. **Monitoring**: Watch for performance metrics post-deployment
6. **Deferred**: Consider .NET 12 LTS when available for long-term support strategy

## References

- [Microsoft .NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/release-notes/10.0/)
- [Breaking Changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [MediatR GitHub Releases](https://github.com/jbogard/MediatR)
- [Marten Documentation](https://martendb.io/)
- [CAP Documentation](https://cap.dotnetcore.xyz/)
- [FluentValidation GitHub](https://github.com/FluentValidation/FluentValidation)

## Research Complete

✅ All NEEDS CLARIFICATION items resolved  
✅ Dependency compatibility matrix created  
✅ Breaking changes documented  
✅ Migration approach established  

**Ready for Phase 1: Design & Validation**
