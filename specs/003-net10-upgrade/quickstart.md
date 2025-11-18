# Quickstart: .NET 10 Upgrade Validation

**Purpose**: Validate the .NET 10 upgrade in local development environment before PR merge

**Estimated Time**: 15-20 minutes (after code changes are applied)

## Prerequisites

- .NET 10 SDK installed (verify: `dotnet --version`)
- Docker Compose running (PostgreSQL, Redis for local testing)
- Git repository at latest `003-net10-upgrade` branch
- Previous .NET 9 build artifacts cleaned

## Quick Validation Steps

### 1. Install .NET 10 SDK

Verify .NET 10 is available:

```powershell
dotnet --version
# Expected output: 10.x.x
```

If not installed, download from [Microsoft .NET Download](https://dotnet.microsoft.com/download/dotnet)

### 2. Start Infrastructure Services

Start Docker Compose for PostgreSQL and Redis:

```powershell
docker-compose up -d
# Verify: Services should be running
docker ps
```

### 3. Clean Previous Build Artifacts

Remove .NET 9 build outputs:

```powershell
dotnet clean Spamma.sln
# Removes bin/ and obj/ directories
```

### 4. Verify Project Configuration

Check that all project files target net10:

```powershell
# Quick check: grep for net10 in csproj files
Get-ChildItem -Path "src" -Filter "*.csproj" -Recurse | 
  ForEach-Object { Select-String -Path $_.FullName -Pattern "net10|net9" }
# Expected: All results should show "net10", NO "net9" entries
```

### 5. Build Solution

Build all projects targeting .NET 10:

```powershell
dotnet build Spamma.sln --no-restore
# Expected: BUILD SUCCESSFUL
# Note: Zero new compiler warnings (pre-existing suppressions OK)
```

If warnings appear:

```powershell
# Check warning details
dotnet build Spamma.sln --no-restore --verbosity:detailed 2>&1 | grep -i "warning"
```

### 6. Run All Tests

Execute the complete test suite:

```powershell
dotnet test tests/ --no-restore --verbosity=normal
# Expected: All tests pass with 100% success rate
```

**Monitor for**:
- Test failures (investigate dependency incompatibility)
- Timeout issues (infrastructure connectivity)
- Warning spikes (compiler or runtime)

### 7. Validate Application Startup

Start the main application:

```powershell
# Terminal 1: Start the app
dotnet run --project src/Spamma.App/Spamma.App

# Expected output (within ~10 seconds):
# - "Application started. Press Ctrl+C to shut down."
# - No exceptions or errors
# - Setup page accessible at http://localhost:5000
```

### 8. Test Setup Page

Verify the UI loads correctly:

```
Open browser: http://localhost:5000
Expected:
  ✅ Page loads without errors
  ✅ No JavaScript console errors (F12 → Console tab)
  ✅ CSS styling intact
  ✅ Blazor WASM initialized
```

### 9. Verify CI/CD Configuration

Check GitHub Actions workflow uses .NET 10:

```powershell
# Open .github/workflows/ci.yml
# Verify: dotnet-version: '10.0.x' or similar
```

### 10. Check Docker Build (Optional)

If Dockerfile exists, verify it builds with .NET 10:

```powershell
docker build -f Dockerfile -t spamma:net10-test .
# Expected: Build succeeds
# Verify image: docker images | grep spamma
```

## Validation Checklist

Use this checklist to confirm readiness for PR:

- [ ] .NET 10 SDK installed and verified
- [ ] `dotnet clean` executed successfully
- [ ] `dotnet build` completes with zero new warnings
- [ ] All project files show net10 target framework
- [ ] `dotnet test` passes with 100% success rate
- [ ] Application starts within 10 seconds
- [ ] Setup page loads without JavaScript errors
- [ ] No Docker/PostgreSQL/Redis connectivity errors
- [ ] `.github/workflows/ci.yml` updated for .NET 10
- [ ] No new security vulnerabilities detected (`dotnet list package --vulnerable`)

## Troubleshooting

### Build fails with "Project targets net9, not net10"

**Issue**: Some project files weren't updated to net10

**Fix**:
```powershell
# Find remaining net9 references
Get-ChildItem -Path "src" -Filter "*.csproj" -Recurse | 
  ForEach-Object { 
    $content = Get-Content $_.FullName
    if ($content -match "net9") { 
      Write-Host "Found net9 in: $($_.FullName)"
    }
  }
# Update manually in Visual Studio or text editor
```

### Tests fail with dependency errors

**Issue**: NuGet package version incompatibility

**Fix**:
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages (force redownload)
dotnet restore Spamma.sln --no-cache

# Try build again
dotnet build Spamma.sln --no-restore
```

### Application crashes on startup with "Assembly not found"

**Issue**: Stale binding redirects or cached assemblies

**Fix**:
```powershell
# Remove all bin/obj directories
Get-ChildItem -Path "." -Include "bin", "obj" -Recurse -Directory | 
  ForEach-Object { Remove-Item $_ -Recurse -Force }

# Full clean rebuild
dotnet clean Spamma.sln
dotnet build Spamma.sln --no-restore
```

### Blazor WASM components show JavaScript errors

**Issue**: WASM bundle not recompiled for .NET 10

**Fix**:
```powershell
# Check browser cache
# Press Ctrl+Shift+Delete in browser → Clear cache

# Rebuild WASM project
dotnet build src/Spamma.App/Spamma.App.Client/ --configuration Release

# Restart application
dotnet run --project src/Spamma.App/Spamma.App
```

### Docker build fails

**Issue**: Dockerfile references .NET 9 base image

**Fix**:
```dockerfile
# In Dockerfile, update base image from:
FROM mcr.microsoft.com/dotnet:9.0-aspnetcore AS runtime
# To:
FROM mcr.microsoft.com/dotnet:10.0-aspnetcore AS runtime
```

## Performance Verification

Optional: Compare build times with .NET 9 baseline

```powershell
# Measure build time
Measure-Command { dotnet build Spamma.sln --no-restore } | 
  Select-Object TotalSeconds

# Expected: ≤2 minutes (success criterion SC-001)

# Measure test execution time
Measure-Command { dotnet test tests/ --no-restore } | 
  Select-Object TotalSeconds

# Expected: Similar to .NET 9 baseline
```

## Success Criteria (from Spec)

| Criterion | Status | Notes |
|-----------|--------|-------|
| SC-001: Build ≤2 min | ✓ Check | Validate timing above |
| SC-002: 100% test pass | ✓ Check | Run full test suite |
| SC-003: App startup ≤10 sec | ✓ Check | Manual timing |
| SC-004: Zero runtime exceptions | ✓ Check | No errors in console |
| SC-005: CI/CD ≤10 min | ✓ Check | After PR, check Actions |
| SC-006: No new vulnerabilities | ✓ Check | `dotnet list package --vulnerable` |
| SC-007: Code coverage maintained | ✓ Check | Coverage reports |
| SC-008: Blazor bundle ≤5% growth | ✓ Check | Bundle size analysis |

## Ready to Commit?

If all checklist items are checked, the upgrade is ready for PR:

```powershell
# Stage changes
git add -A

# Commit
git commit -m "Upgrade to .NET 10 and update NuGet packages

- Update global.json to .NET 10 SDK
- Update all projects to target net10
- Update NuGet dependencies to .NET 10-compatible versions
  - MediatR: v12.x → [version]
  - Marten: v7.x → [version]
  - CAP: v7.x → [version]
  - FluentValidation: v11.x → [version]
  - xUnit: v2.x → [version]
- Fix deprecated API warnings
- Update CI/CD workflow for .NET 10
- Update Docker configuration

Closes #[issue-number] (if applicable)
See spec: specs/003-net10-upgrade/spec.md"

# Push to remote
git push origin 003-net10-upgrade
```

## Rollback (If Needed)

If critical issues emerge:

```powershell
# Switch back to main
git checkout main

# Clean artifacts
dotnet clean Spamma.sln

# Rebuild on .NET 9
dotnet build Spamma.sln
```

---

**Validation Complete!** ✅ Ready for PR review and CI/CD pipeline testing.
