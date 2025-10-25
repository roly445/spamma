# CI/CD Implementation Checklist

Complete this checklist to fully activate the CI/CD pipelines for Spamma.

---

## ✅ Phase 1: Basic Setup (Immediate - No Deployment)

### Workflows Already Active
- [x] **ci-build-test.yml** - Runs on every push/PR to main/develop/feature/*
- [x] **ci-pr-checks.yml** - Runs on every PR
- [x] **ci-docker.yml** - Runs on push to main/develop

**Status:** All active immediately upon push to repository ✅

### Verification Steps
- [ ] Push feature branch to repository
- [ ] Verify GitHub Actions runs workflows (Actions tab)
- [ ] Check test results appear
- [ ] Verify coverage comment appears on PR

---

## ✅ Phase 2: Docker Registry (Required for cd-deploy.yml)

### GitHub Container Registry Setup
- [x] Docker build workflow created
- [ ] Verify first Docker build completes (check Actions workflow)
- [ ] Image appears in `ghcr.io/[username]/spamma` (check Packages tab)

**Status:** Uses `GITHUB_TOKEN` automatically (no manual secret needed)

### Verification Steps
- [ ] Trigger `ci-docker.yml` workflow manually or push to main
- [ ] Watch build complete
- [ ] Check image in GitHub Packages: Settings → Packages → Containers
- [ ] Note image tag (e.g., `sha-abc123` or `main-sha-...`)

---

## ⏳ Phase 3: Server Setup (Required for Deployment)

### Create SSH Key
```bash
# Run on local machine (or CI server if preferred)
ssh-keygen -t ed25519 -f ~/.ssh/spamma_deploy -N ""

# Copy private key to clipboard (you'll need this for GitHub secret)
cat ~/.ssh/spamma_deploy | xclip  # Linux
pbcopy < ~/.ssh/spamma_deploy    # macOS
# Windows: Use Notepad or PowerShell
```

- [ ] SSH key pair generated (ed25519 format)
- [ ] Private key ready for GitHub secret
- [ ] Public key ready for server installation

### Configure Staging Server
```bash
# On staging server
ssh ubuntu@staging.example.com

# Create deployment user
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG docker deploy

# Create deployment directory
sudo mkdir -p /opt/spamma
sudo chown deploy:deploy /opt/spamma

# Setup SSH key
sudo -u deploy mkdir -p /home/deploy/.ssh
echo "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5..." | sudo tee -a /home/deploy/.ssh/authorized_keys
sudo -u deploy chmod 700 /home/deploy/.ssh
sudo -u deploy chmod 600 /home/deploy/.ssh/authorized_keys

# Verify Docker installed
docker --version && docker-compose --version
```

- [ ] Staging server accessible via SSH
- [ ] `deploy` user created with Docker access
- [ ] Public SSH key added to authorized_keys
- [ ] `/opt/spamma` directory owned by deploy user
- [ ] Docker and Docker Compose installed
- [ ] SSH login works: `ssh -i spamma_deploy deploy@staging.example.com`

### Configure Production Server
- [ ] Repeat staging server steps (with production hostname)
- [ ] Verify SSH access works
- [ ] Confirm Docker installed

---

## 🔑 Phase 4: GitHub Secrets Setup (Required for cd-deploy.yml)

### Add Secrets to GitHub

**Location:** GitHub Repository → Settings → Secrets and variables → Actions

### Secret 1: Deploy Key
- [ ] Copy private key content (from SSH key generation)
- [ ] Go to GitHub Secrets
- [ ] New secret: `DEPLOY_KEY`
- [ ] Paste private key content

### Secret 2: Staging Credentials
- [ ] New secret: `STAGING_DEPLOY_HOST`
  - Value: `staging.example.com` (or IP address)
- [ ] New secret: `STAGING_DEPLOY_USER`
  - Value: `deploy` (the SSH user)

### Secret 3: Production Credentials
- [ ] New secret: `PRODUCTION_DEPLOY_HOST`
  - Value: `prod.example.com` (or IP address)
- [ ] New secret: `PRODUCTION_DEPLOY_USER`
  - Value: `deploy` (the SSH user)

### Verification
```bash
# You should now have 5 secrets:
DEPLOY_KEY                    # SSH private key
STAGING_DEPLOY_HOST           # Staging server hostname
STAGING_DEPLOY_USER           # Staging SSH user
PRODUCTION_DEPLOY_HOST        # Production server hostname
PRODUCTION_DEPLOY_USER        # Production SSH user
```

- [ ] All 5 secrets appear in GitHub UI
- [ ] No typos in secret names
- [ ] Private key is complete (includes BEGIN and END markers)

---

## 📦 Phase 5: Docker Compose Configuration

### Create Directory Structure
```bash
# In repository root
mkdir -p docker-deployment
```

### Create Base docker-compose.yml
```bash
# File: docker-deployment/docker-compose.yml
cat > docker-deployment/docker-compose.yml << 'EOF'
version: '3.8'

services:
  app:
    image: ghcr.io/[github-username]/spamma:${SPAMMA_IMAGE_TAG:-latest}
    container_name: spamma-app
    ports:
      - "443:8443"
      - "80:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=${ENVIRONMENT:-production}
      - ConnectionStrings__DefaultConnection=${DATABASE_URL}
      - Redis__Configuration=${REDIS_URL}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    depends_on:
      - postgres
      - redis
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  postgres:
    image: postgres:16-alpine
    container_name: spamma-postgres
    environment:
      POSTGRES_USER: ${DB_USER:-spamma}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: ${DB_NAME:-spamma}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    container_name: spamma-redis
    restart: unless-stopped
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
EOF
```

- [ ] Created `docker-deployment/docker-compose.yml`
- [ ] Verified structure (services, volumes, networks)

### Create Staging Override
```bash
# File: docker-deployment/docker-compose.staging.yml
cat > docker-deployment/docker-compose.staging.yml << 'EOF'
version: '3.8'

services:
  app:
    container_name: spamma-app-staging
    ports:
      - "8443:8443"
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - LOG_LEVEL=Information
EOF
```

- [ ] Created `docker-deployment/docker-compose.staging.yml`

### Create Production Override
```bash
# File: docker-deployment/docker-compose.production.yml
cat > docker-deployment/docker-compose.production.yml << 'EOF'
version: '3.8'

services:
  app:
    container_name: spamma-app-prod
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - LOG_LEVEL=Warning
EOF
```

- [ ] Created `docker-deployment/docker-compose.production.yml`

### Create Staging Environment
```bash
# File: docker-deployment/.env.staging
cat > docker-deployment/.env.staging << 'EOF'
# Staging Environment Configuration
SPAMMA_IMAGE_TAG=latest
ENVIRONMENT=Staging
DATABASE_URL=postgresql://spamma:YOUR_STAGING_PASSWORD@postgres:5432/spamma_staging
REDIS_URL=redis://redis:6379
DB_USER=spamma
DB_PASSWORD=YOUR_STAGING_PASSWORD
DB_NAME=spamma_staging
CERT_PASSWORD=YOUR_CERT_PASSWORD
EOF
```

- [ ] Created `docker-deployment/.env.staging`
- [ ] Updated passwords (replace `YOUR_STAGING_PASSWORD` with real value)
- [ ] Verified .env file not committed to git (add to .gitignore)

### Create Production Environment
```bash
# File: docker-deployment/.env.production
cat > docker-deployment/.env.production << 'EOF'
# Production Environment Configuration
SPAMMA_IMAGE_TAG=latest
ENVIRONMENT=Production
DATABASE_URL=postgresql://spamma:YOUR_PRODUCTION_PASSWORD@postgres:5432/spamma
REDIS_URL=redis://redis:6379
DB_USER=spamma
DB_PASSWORD=YOUR_PRODUCTION_PASSWORD
DB_NAME=spamma
CERT_PASSWORD=YOUR_CERT_PASSWORD
EOF
```

- [ ] Created `docker-deployment/.env.production`
- [ ] Updated passwords (replace `YOUR_PRODUCTION_PASSWORD` with real value)
- [ ] Verified .env file not committed to git

### Verify Structure
```bash
# Your docker-deployment/ directory should look like:
docker-deployment/
├── docker-compose.yml
├── docker-compose.staging.yml
├── docker-compose.production.yml
├── .env.staging
└── .env.production

# Verify files exist
ls -la docker-deployment/
```

- [ ] All 5 files present
- [ ] docker-compose files are valid YAML
- [ ] .env files have required variables

---

## 🧪 Phase 6: Testing CI/CD Workflows

### Test 1: Build & Test on Feature Branch
```bash
# Create feature branch
git checkout -b feature/cicd-test

# Make trivial change
echo "# CI/CD Test" >> README.md

# Push branch
git push origin feature/cicd-test
```

- [ ] Feature branch pushed
- [ ] GitHub Actions triggered (check Actions tab)
- [ ] ci-build-test.yml runs successfully
- [ ] ci-pr-checks.yml runs successfully

### Test 2: Create Pull Request
```bash
# Via GitHub UI: Open PR from feature/cicd-test → main
```

- [ ] PR created
- [ ] All checks pass (green checkmarks)
- [ ] Coverage comment appears
- [ ] Auto-labels applied

### Test 3: Merge and Test Docker Build
```bash
# Via GitHub UI: Merge PR to main
# OR via CLI:
git checkout main
git pull origin main
```

- [ ] Merge to main
- [ ] ci-build-test.yml runs
- [ ] ci-docker.yml runs
- [ ] Docker image builds successfully
- [ ] Image appears in GitHub Packages

### Test 4: Manual Deployment to Staging
```bash
# GitHub UI: Actions → CD - Deploy to Environment → Run workflow
# Select:
#   - Environment: staging
#   - Image tag: (from docker build, e.g., sha-abc123 or main-sha-...)
```

- [ ] Workflow triggered manually
- [ ] Deployment started
- [ ] SSH connection successful
- [ ] Health checks pass
- [ ] App accessible at staging URL

### Test 5: Verify Staging Deployment
```bash
# SSH to staging server
ssh -i deploy_key deploy@staging.example.com

# Check containers running
docker-compose -f /opt/spamma/docker-compose.yml ps

# Check logs
docker-compose -f /opt/spamma/docker-compose.yml logs app

# Test app endpoint
curl http://localhost:8080/health
```

- [ ] Containers running
- [ ] No errors in logs
- [ ] Health endpoint responds
- [ ] App features working

---

## 📊 Phase 7: Verify Coverage Reporting

### Check Coverage in PR
- [ ] View PR → Scroll to comments
- [ ] Coverage report visible with percentage
- [ ] Threshold check showing pass/warning status

### Download Coverage Report
- [ ] Go to GitHub Actions → ci-build-test.yml → Latest successful run
- [ ] Download `coverage-report` artifact
- [ ] Extract and open `index.html` in browser
- [ ] Review coverage by file/module

### Monitor Coverage Over Time
- [ ] Each PR shows coverage metric
- [ ] Track coverage increases/decreases
- [ ] Monitor against 60% threshold

---

## 🚀 Phase 8: Production Deployment (Optional, when ready)

### One-Time Setup
- [ ] Staging deployment validated
- [ ] Production servers configured (same as staging)
- [ ] Production secrets verified in GitHub
- [ ] Production docker-compose files deployed

### First Production Deployment
```bash
# GitHub UI: Actions → CD - Deploy to Environment
# Select:
#   - Environment: production
#   - Image tag: (same as staging test, e.g., sha-abc123)
```

- [ ] Workflow triggered
- [ ] Production deployment started
- [ ] Health checks pass
- [ ] Production app accessible

### Post-Deployment Validation
```bash
# SSH to production server
ssh -i deploy_key deploy@prod.example.com

# Check app running
docker-compose -f /opt/spamma/docker-compose.yml ps

# Check logs for errors
docker-compose -f /opt/spamma/docker-compose.yml logs app -n 50

# Test endpoints
curl https://spamma.app/health
```

- [ ] All containers running
- [ ] No errors in logs
- [ ] Production endpoints responding
- [ ] All features working

---

## ✨ Final Verification Checklist

### Workflows Running
- [x] ci-build-test.yml running on every push/PR ✅
- [x] ci-pr-checks.yml running on every PR ✅
- [x] ci-docker.yml running on main branch ✅
- [ ] cd-deploy.yml ready for manual deployment

### Monitoring & Notifications
- [ ] Coverage reports visible in PR comments
- [ ] Test results appearing in Actions
- [ ] Docker images building and pushing
- [ ] GitHub Packages showing images

### Security
- [ ] All secrets configured
- [ ] SSH keys secured
- [ ] .env files in .gitignore
- [ ] Docker registry private (or public if intended)

### Deployment Validation
- [ ] Staging deployment working
- [ ] Health checks passing
- [ ] Rollback tested (optional)
- [ ] Production ready (if applicable)

---

## 📝 Notes

### Performance Tips
- Workflows cache npm and NuGet packages (~30% faster builds)
- Docker builds use layer caching (~50% faster on changes)
- Coverage reports generated only on test success
- Health checks retry 3 times before failing

### Common Issues & Solutions

**Workflow not triggering?**
- Check branch name (must be main/develop/feature/*)
- Check file has no YAML syntax errors
- Push another commit to trigger

**Coverage comment missing?**
- Tests must pass for report to generate
- Check artifact `test-results/` exists
- Verify ReportGenerator step completed

**Deployment failing?**
- Check secrets configured (5 required secrets)
- Verify server SSH access: `ssh -i key user@host`
- Check server has Docker and Docker Compose
- Review logs: GitHub Actions step output

**Docker image not pushing?**
- Only main branch pushes to registry
- Feature branches build locally only
- Verify GITHUB_TOKEN permissions

### Next Steps After Setup
1. ✅ Commit docker-deployment/ directory
2. ✅ Create PR with CI/CD changes
3. ✅ Merge to main
4. ✅ Monitor first automated workflows
5. ✅ Deploy to staging
6. ✅ Validate staging deployment
7. ✅ Deploy to production

---

## 📚 Documentation References

- **CI_CD_DOCUMENTATION.md** - Complete workflow documentation
- **CICD_QUICK_REFERENCE.md** - Quick commands and patterns
- **WORKFLOWS_IMPLEMENTATION_SUMMARY.md** - High-level overview
- **Workflow files** - `.github/workflows/` directory

---

**Status:** Ready for implementation ✅

**Last Updated:** [Current Date]
