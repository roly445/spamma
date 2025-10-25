# CI/CD Pipeline Documentation

This document outlines the complete CI/CD pipeline for Spamma, including build, test, security scanning, and deployment workflows.

## Workflows Overview

### 1. **ci-build-test.yml** - Main Build & Test Pipeline
**Trigger:** Push to `main`, `develop`, or `feature/*` branches; Pull Requests

**Purpose:** Primary continuous integration pipeline that validates code quality and functionality.

**Stages:**
1. **Checkout** - Clone repository
2. **Frontend Build**
   - Setup Node.js 20.x with npm caching
   - Install dependencies: `npm ci`
   - Build assets: `npm run build` (webpack)
   - Verify wwwroot/ directory created
3. **Backend Build**
   - Setup .NET 9
   - Restore NuGet packages: `dotnet restore`
   - Build solution: `dotnet build --configuration Release`
4. **Test Execution**
   - Run xUnit tests with XPlat Code Coverage
   - Collect coverage metrics
   - Generate HTML coverage report
5. **Reporting**
   - Publish test results via EnricoMi action
   - Comment on PR with coverage percentage
   - Check 60% coverage threshold (warning if below)
   - Upload artifacts (coverage report, test results)
   - Create GitHub step summary

**Key Features:**
- ✅ Frontend-first build sequence (npm before dotnet)
- ✅ Coverage tracking and PR feedback
- ✅ Quality gates (60% threshold)
- ✅ Artifact retention (30 days)
- ✅ Step summary for easy review

**Output Artifacts:**
- `coverage-report/` - HTML coverage report
- `test-results/` - XUnit test result logs

---

### 2. **ci-pr-checks.yml** - PR-Specific Code Quality Checks
**Trigger:** Pull Request creation/update to `main` or `develop`

**Purpose:** Provide detailed code quality analysis specifically for PR reviews.

**Jobs:**

#### 2a. Code Quality Checks
- **TypeScript Compilation:** `tsc --noEmit`
- **Code Style Analysis:** StyleCop enforcement
- **Optional SonarQube:** Integrated security scanning (disabled by default)
- **PR Comment:** Summary of all checks with results

#### 2b. Dependency Security Check
- **NuGet Audit:** `dotnet list package --outdated --include-transitive`
- **npm Audit:** `npm audit` (security vulnerabilities)
- **Reports:** Any outdated or vulnerable dependencies

#### 2c. Auto-Label PR
- **Frontend Changes** → `frontend` label
- **Backend Changes** → `backend` label
- **Test Changes** → `tests` label
- **Docker Changes** → `docker` label
- **CI Changes** → `ci` label

**Key Features:**
- ✅ Concurrency control (cancels in-progress runs for same PR)
- ✅ TypeScript and StyleCop linting
- ✅ Dependency vulnerability scanning
- ✅ Automatic PR labels for organization
- ✅ Optional SonarQube integration

---

### 3. **ci-docker.yml** - Docker Image Build & Push
**Trigger:** Push to `main`/`develop` (with source changes); Manual workflow dispatch

**Purpose:** Build and publish Docker images to container registry with security scanning.

**Stages:**
1. **Setup** - Configure Docker Buildx, authenticate to registry
2. **Metadata Extraction** - Generate tags based on branch/version/commit
3. **Build and Push**
   - Build multi-stage Docker image
   - Use GHA cache for faster builds
   - Push to GitHub Container Registry (GHCR) on `main` branch
   - Build metadata: BUILD_DATE, VCS_REF, VERSION
4. **Security Scan** (on main branch only)
   - Run Trivy vulnerability scanner
   - Report CRITICAL/HIGH vulnerabilities
   - Upload SARIF report to GitHub Security tab

**Image Tags Generated:**
- `latest` - Points to main branch
- `main-sha-abc123...` - Commit SHA on main
- Branch-based tags (e.g., `develop-sha-abc123...`)
- Semantic version tags if released (v1.0.0, v1.0, v1)

**Key Features:**
- ✅ Multi-stage Docker build
- ✅ Automated image tagging (branch, SHA, semver)
- ✅ GitHub Container Registry integration
- ✅ Build cache optimization (30% faster builds)
- ✅ Trivy security scanning
- ✅ SARIF upload to GitHub Security

**Output Artifacts:**
- Docker image in `ghcr.io/[user]/spamma:[tag]`
- Image digest for traceability

---

### 4. **cd-deploy.yml** - Deployment to Staging/Production
**Trigger:** Manual workflow dispatch with parameters

**Purpose:** Deploy validated Docker images to staging or production environments.

**Parameters:**
- **environment** (required): `staging` or `production`
- **image_tag** (required): Docker image tag to deploy (e.g., `sha-abc123` or `v1.0.0`)

**Stages:**

#### 4a. Pre-Deployment Checks
- Verify image exists in registry
- Validate deployment parameters

#### 4b. Deployment Job
1. Create GitHub Deployment record
2. Update docker-compose.yml with image tag
3. Deploy via SSH to target server:
   - Copy docker-compose files
   - Pull latest image: `docker pull ghcr.io/.../[tag]`
   - Stop old containers: `docker-compose down`
   - Start new containers: `docker-compose up -d`
4. Health checks (curl endpoint for 5 minutes)
5. Update GitHub Deployment status

#### 4c. Rollback (if deployment fails)
1. Query GitHub API for previous successful deployment
2. SSH to server and rollback image
3. Notify team via comment

**Required Secrets:**
- `DEPLOY_KEY` - SSH private key for deployment
- `STAGING_DEPLOY_HOST` - Staging server hostname
- `STAGING_DEPLOY_USER` - SSH user for staging
- `PRODUCTION_DEPLOY_HOST` - Production server hostname
- `PRODUCTION_DEPLOY_USER` - SSH user for production
- `DEPLOY_HOST_KEY` - SSH host key (optional, for known_hosts)

**Concurrency:**
- Only one deployment per environment at a time
- Previous deployments are canceled if new one starts

**Key Features:**
- ✅ Manual trigger with environment selection
- ✅ GitHub Deployments API integration
- ✅ Automated health checks
- ✅ Automatic rollback on failure
- ✅ SSH-based deployment
- ✅ Concurrency control (one per environment)
- ✅ Team notifications

---

## Setup Instructions

### Local Secrets Configuration

Add the following secrets to your GitHub repository settings:

**For Docker Build (ci-docker.yml):**
- Automatically uses `GITHUB_TOKEN` (no setup needed)

**For Deployment (cd-deploy.yml):**

```bash
# Generate SSH key (if not existing)
ssh-keygen -t ed25519 -f deploy_key -N ""

# Add public key to your servers:
# ~/.ssh/authorized_keys on staging and production servers

# Add secrets to GitHub:
# Settings → Secrets and variables → Actions

DEPLOY_KEY=<contents of deploy_key (private key)>
STAGING_DEPLOY_HOST=staging.example.com
STAGING_DEPLOY_USER=deploy
PRODUCTION_DEPLOY_HOST=prod.example.com
PRODUCTION_DEPLOY_USER=deploy
```

### Server-Side Setup

On your deployment servers (staging/production):

```bash
# Create deployment directory
sudo mkdir -p /opt/spamma
sudo chown deploy:deploy /opt/spamma

# Ensure Docker and Docker Compose are installed
docker --version
docker-compose --version

# Configure ssh for automated deployments
mkdir -p ~/.ssh
chmod 700 ~/.ssh
# Add DEPLOY_KEY public key to ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# Test SSH connection from GitHub Actions runner
# (This is verified in the workflow)
```

### docker-compose Structure

Deployment requires docker-compose files in `docker-deployment/` directory:

```
docker-deployment/
├── docker-compose.yml           # Base configuration
├── docker-compose.staging.yml   # Staging overrides
├── docker-compose.production.yml # Production overrides
├── .env.staging                 # Staging environment variables
└── .env.production              # Production environment variables
```

Example `.env.staging`:
```
SPAMMA_IMAGE_TAG=latest
DATABASE_URL=postgresql://user:pass@postgres:5432/spamma_staging
REDIS_URL=redis://redis:6379
ENVIRONMENT=staging
LOG_LEVEL=Information
```

---

## Workflow Status & Monitoring

### Viewing Workflow Runs

1. GitHub UI: **Actions** tab → Select workflow
2. Check specific run details:
   - Build logs (each step)
   - Artifacts
   - Deployment status
   - PR comments

### PR Feedback

- **Coverage Report:** Automatic comment on every PR with:
  - Overall coverage percentage
  - Coverage threshold status (✅ pass/⚠️ warning)
- **Code Quality:** Comment from PR checks workflow
- **Labels:** Automatic labels based on changed files

### Deployment Tracking

1. Check **Deployments** tab for deployment history
2. View deployment status for each environment
3. Rollback available from GitHub UI (via workflow re-run with previous tag)

---

## Common Tasks

### Deploy a Specific Version to Staging

1. Go to **Actions** → **CD - Deploy to Environment**
2. Click **Run workflow**
3. Select **Environment:** `staging`
4. Enter **Image tag:** (e.g., `sha-abc123def` or `v1.2.3`)
5. Click **Run workflow**

### Check Test Coverage

1. Go to **Actions** → **CI - Build & Test** → Latest run
2. Click **Summary** tab
3. Coverage % shown in GitHub step summary
4. Download `coverage-report` artifact for detailed HTML report

### Find Image Tag to Deploy

1. Go to **Actions** → **CI - Docker Build & Push** → Latest run
2. Check **Deployment summary** section
3. Image tag listed (e.g., `main-sha-abc123...`)

### Rollback Failed Deployment

Option 1 (Automatic):
- Rollback happens automatically if health checks fail

Option 2 (Manual):
1. Go to **Deployments** tab
2. Select previous successful deployment
3. Click **Activate deployment** (or re-run workflow with previous image tag)

---

## Troubleshooting

### Build Fails: "npm: command not found"
- Check Node.js setup step completed successfully
- Verify Node.js version is 20.x or higher

### Build Fails: ".NET SDK not found"
- Check .NET setup step: 9.x SDK required
- Verify dotnet is in PATH

### Tests Fail: "Connection string invalid"
- Tests use in-memory/test database
- Check appsettings.Test.json configuration
- Verify test fixtures properly configured

### Coverage Report Missing
- Check XPlat Code Coverage step output
- Verify test results directory created: `test-results/`
- Check ReportGenerator step completed

### Deployment Fails: "SSH connection refused"
- Verify `DEPLOY_KEY` secret added (private key)
- Check SSH key authorized on target server
- Verify server hostname in secrets is correct
- Test SSH manually: `ssh -i deploy_key user@host`

### Docker Push Failed: "Authentication failed"
- `GITHUB_TOKEN` used automatically (no manual secret needed)
- Verify workflow has `packages: write` permission
- Check workflow runs on correct branch (main for push)

---

## Performance Optimization

### Frontend Build Caching
- npm cache: `~/.npm` (restored from `package-lock.json`)
- Webpack cache: Enabled in webpack.config.js
- GHA Cache: Automatic, 5GB limit per repo

### Docker Build Caching
- GHA Cache type: `type=gha,mode=max`
- Caches all layers, ~30% faster subsequent builds
- Cache scope: Per branch

### Recommended: Scheduled Nightly Tests
Consider adding workflow to run full test suite nightly:

```yaml
name: Nightly Tests
on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM UTC daily

jobs:
  test:
    runs-on: ubuntu-latest
    # ... same as ci-build-test.yml
```

---

## Next Steps

1. ✅ Create GitHub secrets for deployment
2. ✅ Set up docker-deployment directory with compose files
3. ✅ Test workflow on feature branch
4. ✅ Monitor first successful builds
5. ✅ Configure staging deployment
6. ✅ Validate production readiness

---

## FAQ

**Q: Why does Docker build on main only?**
A: Reduces container registry storage and prevents image bloat. Feature branches build Docker images for testing but don't push.

**Q: Can I deploy from PR?**
A: Not directly. PRs trigger ci-build-test.yml for validation. Once merged to main, Docker image builds and can be deployed.

**Q: How long do artifacts retain?**
A: 30 days by default. Adjustable in workflow `retention-days` parameter.

**Q: Can coverage threshold be customized?**
A: Yes. Edit `ci-build-test.yml`, search for "60%" (line ~170), change to desired threshold.

**Q: Does every push trigger all workflows?**
A: No. Each workflow has specific triggers (push, PR, manual). Check `on:` section in workflow file.
