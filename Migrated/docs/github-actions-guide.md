# GitHub Actions CI/CD Workflow Guide

## 🚀 Overview
This guide explains how to use the GitHub Actions CI/CD workflow for the CreditTransfer.Modern project.

## 📋 Workflow Features

### 🏗️ Jobs Included:
1. **Build & Analyze** - Build solution, run tests, SonarQube analysis
2. **Integration Tests** - Database integration tests with PostgreSQL/SQL Server
3. **Docker Build & Push** - Build and push Docker images to Docker Hub
4. **Security Analysis** - CodeQL security scanning
5. **Performance Tests** - Optional performance test execution
6. **Deployment Summary** - Complete pipeline results summary

## 🎯 How to Run the Workflow

### Method 1: Automatic Trigger (Push to Main)
```bash
git checkout main
git add .
git commit -m "[Cursor] Add new feature - automatic CI trigger"
git push origin main
```

### Method 2: Manual Trigger via GitHub UI
1. Go to your repository on GitHub
2. Click **Actions** tab
3. Select **"CI/CD - Build, Test & Deploy"** workflow
4. Click **"Run workflow"** button
5. Configure options:
   - ✅ **Run performance tests**: Check to include performance tests
   - ✅ **Force Docker push**: Push Docker images even from non-main branches
   - ✅ **Skip SonarQube**: Skip SonarQube analysis if not configured

### Method 3: Manual Trigger via GitHub CLI
```bash
# Basic manual run
gh workflow run "CI/CD - Build, Test & Deploy"

# Run with performance tests
gh workflow run "CI/CD - Build, Test & Deploy" --field run_performance_tests=true

# Force Docker push from feature branch
gh workflow run "CI/CD - Build, Test & Deploy" --field force_docker_push=true

# Skip SonarQube analysis
gh workflow run "CI/CD - Build, Test & Deploy" --field skip_sonarqube=true
```

## ⚙️ Required Setup & Secrets

### 1. GitHub Repository Secrets
Go to **Settings → Secrets and variables → Actions** and add:

```yaml
# Docker Hub Integration
DOCKER_USERNAME: your-dockerhub-username
DOCKER_PASSWORD: your-dockerhub-password-or-token

# SonarQube Integration (Optional)
SONAR_TOKEN: your-sonarqube-token

# Repository Token for SonarQube
REPO_TOKEN: ${{ secrets.GITHUB_TOKEN }}  # Usually automatic

# Slack Notifications (Optional)
SLACK_WEBHOOK_URL: your-slack-webhook-url
```

### 2. Docker Hub Setup
```bash
# Create Docker Hub account and get credentials
echo "DOCKER_USERNAME=dockerhosam" >> .env
echo "DOCKER_PASSWORD=your-docker-token" >> .env

# Test Docker login locally
docker login -u dockerhosam
```

### 3. SonarQube Setup (Optional)
If you want code quality analysis:
1. Create account on [SonarCloud.io](https://sonarcloud.io)
2. Create project with key: `credit-transfer-modern`
3. Get authentication token
4. Add `SONAR_TOKEN` secret to GitHub

## 📊 Workflow Execution Steps

### Phase 1: Build & Analysis (5-10 minutes)
- ✅ Checkout code
- ✅ Setup .NET 8
- ✅ Generate version numbers
- ✅ Restore NuGet packages
- ✅ Build solution
- ✅ Run unit tests
- ✅ Generate code coverage
- ✅ SonarQube analysis (if enabled)

### Phase 2: Integration Tests (10-15 minutes)
- ✅ Setup PostgreSQL database
- ✅ Setup SQL Server database
- ✅ Run integration tests
- ✅ Database connectivity validation

### Phase 3: Docker Build & Push (10-15 minutes)
- ✅ Build WCF Service Docker image
- ✅ Build REST API Docker image
- ✅ Build Worker Service Docker image
- ✅ Push to Docker Hub (main branch only)

### Phase 4: Security Analysis (5-10 minutes)
- ✅ CodeQL security scanning
- ✅ Vulnerability detection
- ✅ Security report generation

### Phase 5: Performance Tests (15-30 minutes)
- ✅ Load testing (optional)
- ✅ Performance benchmarks
- ✅ Response time validation

### Phase 6: Summary & Notifications
- ✅ Generate deployment summary
- ✅ Send Slack notifications
- ✅ Update GitHub status

## 📈 Monitoring Workflow Results

### View Live Progress
```bash
# Watch workflow status
gh run list --workflow="CI/CD - Build, Test & Deploy"

# View specific run details
gh run view <run-id>

# Download artifacts
gh run download <run-id>
```

### Key Artifacts Generated
- **Test Results**: XML reports with test outcomes
- **Code Coverage**: HTML and XML coverage reports
- **Published Applications**: Ready-to-deploy binaries
- **Docker Images**: Tagged and pushed to Docker Hub

## 🚨 Troubleshooting Common Issues

### Build Failures
```bash
# Check build logs in GitHub Actions
# Common issues:
# - Missing NuGet packages
# - Compilation errors
# - Test failures
```

### Docker Push Failures
```bash
# Verify Docker credentials
# Check Docker Hub repository exists
# Ensure main branch or force_docker_push=true
```

### Database Connection Issues
```bash
# Integration tests require:
# - PostgreSQL: postgres/postgres
# - SQL Server: sa/Test123!@#
```

### SonarQube Issues
```bash
# Skip SonarQube if not configured:
# Set skip_sonarqube=true in manual run
```

## 🎯 Success Criteria

### ✅ Successful Pipeline Indicators:
- All tests pass (Unit + Integration)
- Code coverage > 80%
- No security vulnerabilities found
- Docker images built and pushed
- No SonarQube quality gate failures

### 📊 Expected Outputs:
- **Docker Images**: Available at `dockerhosam/wcf-service:latest`, `dockerhosam/rest-api:latest`, `dockerhosam/worker-service:latest`
- **Test Reports**: Comprehensive test results with coverage
- **Security Reports**: CodeQL analysis results
- **Build Artifacts**: Published application binaries

## 🔄 Development Workflow

### Feature Development
```bash
# 1. Create feature branch
git checkout -b feature/new-functionality

# 2. Make changes and test locally
dotnet test

# 3. Push feature branch (no deployment)
git push origin feature/new-functionality

# 4. Create pull request
gh pr create --title "Add new functionality" --body "Description"

# 5. After review, merge to main (triggers full deployment)
gh pr merge --squash
```

### Hotfix Workflow
```bash
# 1. Create hotfix branch
git checkout -b hotfix/critical-fix

# 2. Make fix and test
dotnet test

# 3. Force Docker build for testing
gh workflow run "CI/CD - Build, Test & Deploy" --field force_docker_push=true

# 4. After validation, merge to main
git checkout main
git merge hotfix/critical-fix
git push origin main
```

## 📝 Next Steps

1. **First Run**: Push to main branch to trigger initial workflow
2. **Monitor Results**: Check GitHub Actions tab for progress
3. **Configure Secrets**: Add Docker Hub and SonarQube credentials
4. **Test Docker Images**: Verify images are available on Docker Hub
5. **Setup Monitoring**: Configure Slack notifications for team updates 