# CI/CD Quick Reference

## Workflow Files Created

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| Main Build & Test | `ci-build-test.yml` | Push to main/develop/feature/*, PR | Build frontend, build .NET, run tests, collect coverage |
| PR Checks | `ci-pr-checks.yml` | PR to main/develop | Code quality, linting, dependency audit, auto-label |
| Docker Build | `ci-docker.yml` | Push to main/develop, manual | Build & push Docker image, security scan with Trivy |
| Deployment | `cd-deploy.yml` | Manual workflow dispatch | Deploy to staging/production with health checks & rollback |

---

## Quick Start

### 1. Push Code to Main
```bash
git push origin main
```
→ Automatically triggers `ci-build-test.yml` and `ci-docker.yml`
→ Check PR for coverage report comment
→ Docker image available in GHCR

### 2. Deploy to Staging
Go to **GitHub** → **Actions** → **CD - Deploy to Environment** → **Run workflow**

Select:
- **Environment:** `staging`
- **Image tag:** (copy from Docker Build workflow summary)

### 3. Deploy to Production
Same as staging, select **Environment:** `production`

---

## Workflow Details

### CI - Build & Test (`ci-build-test.yml`)

**When:** Every push to main/develop/feature/*, every PR

**What it does:**
1. Builds frontend (Node.js + webpack)
2. Builds backend (.NET 9)
3. Runs all unit tests
4. Collects code coverage
5. Comments on PR with coverage %
6. Uploads test results & coverage report

**Duration:** ~2-3 minutes

**Check results:** GitHub → PR → Comments section

---

### CI - PR Checks (`ci-pr-checks.yml`)

**When:** PR created/updated to main/develop

**What it does:**
1. TypeScript compilation check
2. StyleCop code analysis
3. NuGet package audit
4. npm security audit
5. Auto-labels PR based on files changed

**Duration:** ~1 minute

**Auto-labels:**
- `frontend` - Blazor component/TypeScript changes
- `backend` - Module/service changes
- `tests` - Test file changes
- `docker` - Docker/docker-compose changes
- `ci` - Workflow/GitHub Actions changes

---

### CI - Docker Build (`ci-docker.yml`)

**When:** Push to main/develop, or manual trigger

**What it does:**
1. Builds Docker image (multi-stage)
2. Generates smart tags (branch, SHA, semver)
3. Pushes to GitHub Container Registry (main only)
4. Runs Trivy security scan
5. Reports vulnerabilities to GitHub Security tab

**Image tags:**
- `latest` - Main branch only
- `main-sha-abc123def` - Main branch commit
- `develop-sha-xyz789...` - Develop branch commit

**Duration:** ~3-5 minutes

**Check results:** GitHub → Actions → Workflow run summary

---

### CD - Deploy to Environment (`cd-deploy.yml`)

**When:** Manual trigger via GitHub UI

**What it does:**
1. Verifies image exists in registry
2. Creates GitHub Deployment record
3. Deploys via SSH to target server
4. Runs health checks (5 min timeout)
5. Auto-rollback on failure
6. Notifies team

**Parameters:**
- `environment` - staging or production
- `image_tag` - Docker image tag (e.g., `sha-abc123` or `v1.0.0`)

**Duration:** ~2-5 minutes + health check time

**Check results:** GitHub → Deployments tab

---

## Troubleshooting

### Coverage report not showing in PR?
- Ensure tests run successfully: Check **Actions** → **Workflow run** → **Test Execution** step
- Coverage files created: Look for `test-results/` in workflow logs
- Try re-running workflow: **Actions** → **Workflow run** → **Re-run failed jobs**

### Docker image not pushed?
- Check branch: Only `main` branch pushes to registry
- Verify feature branch: Should build image locally (not push)
- Check logs: **Actions** → **Workflow run** → **Build and push Docker image** step

### Deployment fails with SSH error?
- Verify secrets: GitHub → **Settings** → **Secrets and variables** → **Actions**
- Required: `DEPLOY_KEY`, `STAGING_DEPLOY_HOST`, `STAGING_DEPLOY_USER`, etc.
- Test SSH: `ssh -i deploy_key user@host`

### Health check times out?
- Verify app is running: SSH to server, check `docker-compose ps`
- Check logs: `docker-compose logs app`
- Verify health endpoint: curl https://environment.spamma.app/health
- Increase timeout: Edit workflow, search for "health checks", change sleep/iteration values

---

## Secrets Required (for Deployment)

Set in GitHub → **Settings** → **Secrets and variables** → **Actions**

```
DEPLOY_KEY                    # SSH private key (ed25519 or RSA)
STAGING_DEPLOY_HOST           # Staging server hostname/IP
STAGING_DEPLOY_USER           # SSH user for staging
PRODUCTION_DEPLOY_HOST        # Production server hostname/IP
PRODUCTION_DEPLOY_USER        # SSH user for production
DEPLOY_HOST_KEY               # (Optional) SSH host key for known_hosts
```

---

## Viewing Artifacts

### Coverage Report
1. **Actions** → **CI - Build & Test** → Latest successful run
2. **Artifacts** section → Download `coverage-report`
3. Extract and open `index.html` in browser

### Test Results
1. **Actions** → **CI - Build & Test** → Latest run
2. **Artifacts** section → Download `test-results`
3. View XML test report or parse with tools

---

## Performance Tips

### Speed Up Frontend Build
- npm cache is automatic (5 GB limit per repo)
- Don't commit `node_modules/`
- Keep `package-lock.json` up to date

### Speed Up .NET Build
- NuGet cache automatic via actions/setup-dotnet
- Keep fewer project references when possible
- Enable incremental builds (default in VS)

### Speed Up Docker Build
- GHA Cache enabled by default
- ~30% faster on subsequent builds
- Cache invalidated on Dockerfile changes

---

## Monitoring & Notifications

### PR Feedback
- Coverage report: Automatic comment on every PR
- Code quality: Comment from PR checks
- Auto-labels: Based on files changed

### Deployment Notifications
- GitHub Deployments tab: Full history
- Status badges available (can add to README)
- Slack/Teams integration available (configure via Actions settings)

### Scheduled Tests
Coming soon: Add nightly test runs for full regression testing

---

## Common Patterns

### Review Merge Checklist
- [ ] PR checks pass (green checkmarks)
- [ ] Code coverage report shows threshold met
- [ ] Docker build successful (check workflow summary)
- [ ] At least 1 approval

### Pre-Production Validation
- [ ] Merge to develop
- [ ] Verify develop branch docker build
- [ ] Deploy to staging
- [ ] Run manual smoke tests
- [ ] Merge to main
- [ ] Deploy to production

### Rollback Procedure
1. **Automatic:** Health checks fail → auto-rollback (no action needed)
2. **Manual:** Re-run CD workflow with previous `image_tag`

---

## Accessing Workflows

**All workflows available at:**
```
GitHub → Actions → [Select Workflow Name] → [Select Run]
```

**Or directly in repo:**
```
.github/workflows/
├── ci-build-test.yml      # Main build & test
├── ci-pr-checks.yml       # PR quality checks
├── ci-docker.yml          # Docker build
└── cd-deploy.yml          # Deployment
```
