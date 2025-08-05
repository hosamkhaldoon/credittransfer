# 🚀 GitHub Actions CI/CD Workflow

This project uses GitHub Actions instead of Jenkins for automated CI/CD. The workflow builds, tests, and deploys the migrated .NET 8 CreditTransfer application.

## 🎯 Quick Start

### 1. Prerequisites
- GitHub CLI installed: `winget install GitHub.cli`
- Git repository pushed to GitHub
- Docker Hub account (for deployment)

### 2. Setup Secrets (One-time)
```powershell
# Run the setup script
./scripts/setup-github-actions.ps1 -Action setup -DockerUsername "your-dockerhub-username" -DockerPassword "your-dockerhub-token"
```

**Or manually via GitHub UI:**
1. Go to your GitHub repository
2. Settings → Secrets and variables → Actions
3. Add these secrets:
   - `DOCKER_USERNAME`: Your Docker Hub username
   - `DOCKER_PASSWORD`: Your Docker Hub password/token

### 3. Trigger the Workflow

**Option A: Automatic (Recommended)**
```bash
git add .
git commit -m "Trigger CI/CD workflow"
git push origin main
```

**Option B: Manual Trigger**
```powershell
# Using the helper script
./scripts/setup-github-actions.ps1 -Action test

# Or using GitHub CLI directly
gh workflow run "ci-build-test.yml"
```

**Option C: GitHub UI**
1. Go to your repository on GitHub
2. Click "Actions" tab
3. Select "CI/CD - Build, Test & Deploy"
4. Click "Run workflow"

## 📊 What the Workflow Does

### 🏗️ Build & Test (10-15 minutes)
- ✅ Builds .NET 8 solution
- ✅ Runs unit tests with coverage
- ✅ Performs code quality analysis
- ✅ Generates test reports

### 🧪 Integration Tests (10-15 minutes)
- ✅ Sets up PostgreSQL and SQL Server
- ✅ Runs database integration tests
- ✅ Validates API endpoints

### 🐳 Docker Build & Push (10-15 minutes)
- ✅ Builds Docker images for:
  - WCF Service (`dockerhosam/wcf-service:latest`)
  - REST API (`dockerhosam/rest-api:latest`) 
  - Worker Service (`dockerhosam/worker-service:latest`)
- ✅ Pushes to Docker Hub (main branch only)

### 🔒 Security Analysis (5-10 minutes)
- ✅ CodeQL security scanning
- ✅ Vulnerability detection
- ✅ SARIF security reports

## 📈 Monitor Progress

### Check Status
```powershell
# View recent runs and secrets
./scripts/setup-github-actions.ps1 -Action status

# Or use GitHub CLI
gh run list
gh run view <run-id>
```

### GitHub Actions Dashboard
- Visit: `https://github.com/YOUR-USERNAME/YOUR-REPO/actions`
- View live progress, logs, and artifacts
- Download test reports and build artifacts

## 🎛️ Workflow Configuration

### Triggers
- **Automatic**: Push to `main` branch (excludes `.md` files)
- **Manual**: GitHub UI or CLI with options:
  - `run_performance_tests`: Include performance tests
  - `force_docker_push`: Push Docker images from any branch
  - `skip_sonarqube`: Skip code quality analysis

### Environment Variables
Key settings in `.github/workflows/ci-build-test.yml`:
```yaml
DOTNET_VERSION: '8.0.x'
SOLUTION_FILE: 'Migrated/CreditTransfer.Modern.sln'
DOCKER_NAMESPACE: 'dockerhosam'
COVERAGE_THRESHOLD: 80
```

## 🚨 Troubleshooting

### Common Issues

**Build Failures**
- Check GitHub Actions logs for compilation errors
- Verify NuGet package restoration
- Ensure all project references are correct

**Docker Push Failures**
- Verify `DOCKER_USERNAME` and `DOCKER_PASSWORD` secrets
- Check Docker Hub repository exists
- Ensure pushing from `main` branch or use `force_docker_push=true`

**Test Failures**
- Review test output in Actions logs
- Check database connection strings
- Verify test data setup

### Get Help
```powershell
# Show detailed usage
./scripts/setup-github-actions.ps1 -Action help

# Check workflow file syntax
gh workflow view "ci-build-test.yml"
```

## 🔄 Development Workflow

### Feature Development
```bash
git checkout -b feature/new-feature
# Make changes
git push origin feature/new-feature
# Create PR → workflow runs without deployment
```

### Production Deployment  
```bash
git checkout main
git merge feature/new-feature
git push origin main
# Full workflow runs with Docker deployment
```

## 📦 Outputs

### Successful Run Produces:
- **Docker Images**: Available on Docker Hub
- **Test Reports**: Downloadable artifacts
- **Code Coverage**: Integrated reports
- **Security Reports**: CodeQL analysis
- **Build Artifacts**: Ready-to-deploy binaries

### Timeline
- **Total Duration**: 30-60 minutes (parallel execution)
- **Build**: ~10 minutes
- **Tests**: ~15 minutes  
- **Docker**: ~15 minutes
- **Security**: ~10 minutes

---

## 📚 Additional Resources

- **Workflow File**: `.github/workflows/ci-build-test.yml`
- **Setup Script**: `scripts/setup-github-actions.ps1` 
- **Detailed Guide**: `docs/github-actions-guide.md`
- **GitHub CLI**: https://cli.github.com/
- **Docker Hub**: https://hub.docker.com/u/dockerhosam 