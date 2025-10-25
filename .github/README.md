# GitHub Configuration & CI/CD Pipeline

Welcome to the Spamma CI/CD infrastructure. This directory contains complete automation for building, testing, and deploying the application.

---

## 📖 Documentation Guide

### 🚀 **Start Here**
1. **[DELIVERY_SUMMARY.md](./DELIVERY_SUMMARY.md)** - Overview of what's been implemented ⭐
2. **[CICD_QUICK_REFERENCE.md](./CICD_QUICK_REFERENCE.md)** - Quick commands and common tasks (5 min read)

### 📋 **For Setup & Configuration**
3. **[IMPLEMENTATION_CHECKLIST.md](./IMPLEMENTATION_CHECKLIST.md)** - Step-by-step setup guide (30-60 min)
   - Phase 1: Basic setup
   - Phase 2: Docker registry
   - Phase 3: Server setup
   - Phase 4: GitHub secrets
   - Phase 5: Docker compose configuration
   - Phase 6: Testing workflows
   - Phase 7: Coverage verification
   - Phase 8: Production deployment

### 📚 **Complete Reference**
4. **[CI_CD_DOCUMENTATION.md](./CI_CD_DOCUMENTATION.md)** - Comprehensive documentation
   - Workflow details
   - Secrets configuration
   - Server setup
   - Troubleshooting
   - FAQ

---

## 🔄 Workflow Files

| File | Purpose | Status |
|------|---------|--------|
| **workflows/ci-build-test.yml** | Build frontend, build .NET, run tests, coverage | ✅ Active |
| **workflows/ci-pr-checks.yml** | Code quality, linting, dependency audit | ✅ Active |
| **workflows/ci-docker.yml** | Build Docker image, push to registry | ✅ Active |
| **workflows/cd-deploy.yml** | Deploy to staging/production | ⏳ Awaiting secrets |
| **workflows/build.yml** | Original Docker build workflow | ✅ Preserved |

---

## ⚡ Quick Start

### View Workflows
```
GitHub → Actions → [Select Workflow] → [View Run Details]
```

### Check Coverage Report
```
GitHub → Actions → CI - Build & Test → [Latest Run] → Artifacts → coverage-report
```

### Manually Deploy to Staging
```
GitHub → Actions → CD - Deploy to Environment → Run workflow
  - Environment: staging
  - Image tag: sha-abc123 (from Docker Build summary)
```

---

## 🔐 Required Setup (for cd-deploy.yml)

Before using deployment workflow, configure:

1. **GitHub Secrets** (5 required)
   - `DEPLOY_KEY` - SSH private key
   - `STAGING_DEPLOY_HOST` - Staging server
   - `STAGING_DEPLOY_USER` - SSH user
   - `PRODUCTION_DEPLOY_HOST` - Production server
   - `PRODUCTION_DEPLOY_USER` - SSH user

2. **Server SSH Keys**
   - Add GitHub Actions SSH public key to servers
   - Ensure Docker is installed
   - Create `/opt/spamma` directory

3. **docker-deployment/ Directory** (in repo root)
   - `docker-compose.yml` - Base configuration
   - `docker-compose.staging.yml` - Staging overrides
   - `docker-compose.production.yml` - Production overrides
   - `.env.staging` - Staging environment variables
   - `.env.production` - Production environment variables

**See [IMPLEMENTATION_CHECKLIST.md](./IMPLEMENTATION_CHECKLIST.md) phases 3-5 for detailed setup**

---

## 📊 What Each Workflow Does

### CI - Build & Test (`ci-build-test.yml`)
**When:** Every push to main/develop/feature/*, every PR

**Steps:**
1. Build frontend (Node.js + webpack)
2. Build backend (.NET)
3. Run 134 unit tests
4. Collect code coverage
5. Comment on PR with coverage metrics
6. Upload artifacts

**Results:** Coverage report, test results, PR comment

### CI - PR Checks (`ci-pr-checks.yml`)
**When:** Every PR to main/develop

**Steps:**
1. TypeScript compilation
2. StyleCop enforcement
3. NuGet security audit
4. npm security audit
5. Auto-label PR

**Results:** Quality check comment, automatic labels

### CI - Docker Build (`ci-docker.yml`)
**When:** Push to main/develop, manual trigger

**Steps:**
1. Build Docker image (multi-stage)
2. Tag image smartly
3. Push to GitHub Container Registry
4. Run Trivy security scan
5. Report vulnerabilities

**Results:** Docker image in GHCR, security reports

### CD - Deploy (`cd-deploy.yml`)
**When:** Manual trigger (requires secrets setup)

**Steps:**
1. Verify image exists
2. Create GitHub Deployment
3. SSH to target server
4. Pull and run image
5. Health check
6. Automatic rollback if needed

**Results:** Deployed application, updated GitHub Deployments tab

---

## ✨ Key Features

✅ **Frontend-First Builds**
- Node.js setup before .NET build
- npm caching for faster builds

✅ **Comprehensive Testing**
- 134 unit tests
- Code coverage reporting
- PR feedback with metrics

✅ **Docker Containerization**
- Multi-stage build
- Security scanning (Trivy)
- Smart image tagging
- GitHub Container Registry

✅ **Automated Deployment**
- SSH-based to servers
- Health checks
- Automatic rollback
- GitHub integration

✅ **Code Quality**
- TypeScript compilation
- StyleCop enforcement
- Dependency auditing
- Auto-labeling

---

## 🚨 Troubleshooting

### Coverage report not in PR?
→ Check workflow passed, tests executed, artifacts generated

### Docker image not pushing?
→ Only main branch pushes. Feature branches build locally.

### Deployment failing with SSH error?
→ Verify secrets configured, SSH key authorized on server

**See [CI_CD_DOCUMENTATION.md](./CI_CD_DOCUMENTATION.md) for full troubleshooting guide**

---

## 📈 Monitoring

### GitHub Actions
- View all workflow runs: **Actions** tab
- Monitor builds: Real-time logs for each step
- Track deployments: **Deployments** tab
- View artifacts: Coverage reports, test results

### PR Feedback
- Coverage metrics automatically commented
- Code quality checks visible
- Auto-labels applied based on changes

### Deployment Status
- GitHub Deployments tab shows history
- SARIF security reports in Security tab
- Step summaries for quick review

---

## 📞 Quick Links

| Need | Resource |
|------|----------|
| Quick overview | [DELIVERY_SUMMARY.md](./DELIVERY_SUMMARY.md) |
| Common commands | [CICD_QUICK_REFERENCE.md](./CICD_QUICK_REFERENCE.md) |
| Step-by-step setup | [IMPLEMENTATION_CHECKLIST.md](./IMPLEMENTATION_CHECKLIST.md) |
| Complete reference | [CI_CD_DOCUMENTATION.md](./CI_CD_DOCUMENTATION.md) |
| Copilot instructions | [copilot-instructions.md](./copilot-instructions.md) |

---

## 🎯 Next Steps

1. **Immediate** (no setup): Push code, watch workflows run, view coverage in PR
2. **Short-term** (30 min): Follow [IMPLEMENTATION_CHECKLIST.md](./IMPLEMENTATION_CHECKLIST.md) phases 1-2
3. **Medium-term** (1-2 hours): Complete phases 3-7 for deployment capability
4. **Optional** (when ready): Deploy to production (phase 8)

---

## 📝 Status

**CI Workflows (Build & Test):** ✅ Active & Ready
- Automatically run on every push
- No configuration needed
- View in Actions tab

**Docker Build Workflow:** ✅ Active & Ready
- Automatically builds on main branch
- Images in GitHub Packages
- Security scanning enabled

**Deployment Workflow:** ⏳ Ready but Requires Setup
- Awaiting GitHub secrets
- Awaiting server configuration
- Awaiting docker-deployment directory
- Follow IMPLEMENTATION_CHECKLIST.md for setup

---

*Last Updated: [Current Session]*

**Questions?** See documentation files above or review workflow files in `workflows/` directory.
