# SonarQube Local Setup Guide

## 🔧 Setting up Local SonarQube for GitHub Actions

### Step 1: Generate SonarQube Token

1. **Access your local SonarQube**: http://localhost:9000
2. **Login** with admin credentials (default: admin/admin)
3. **Navigate to**: User Menu → My Account → Security → Tokens
4. **Generate new token**:
   - Name: `GitHub Actions`
   - Type: `User Token`
   - Expires: Set appropriate expiration
5. **Copy the generated token** (you won't see it again!)

### Step 2: Add Token to GitHub Repository

1. **Go to your GitHub repository**
2. **Navigate to**: Settings → Secrets and variables → Actions
3. **Add new repository secret**:
   - Name: `SONAR_TOKEN`
   - Value: [Paste the token from Step 1]

### Step 3: Create Project in SonarQube (Optional)

1. **Go to**: http://localhost:9000/projects/create
2. **Create project manually**:
   - Project key: `credit-transfer-modern`
   - Display name: `Credit Transfer Modern`
3. **Set up analysis method**: Select "With GitHub Actions"

### Step 4: Configure Quality Gate (Optional)

1. **Go to**: Quality Gates in SonarQube
2. **Create or modify** quality gate rules
3. **Set project** to use your quality gate

## 🐳 Running SonarQube with Docker

If you need to start SonarQube locally:

```bash
# Run SonarQube Community Edition
docker run -d --name sonarqube \
  -p 9000:9000 \
  -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true \
  sonarqube:10-community

# Check if it's running
docker ps | grep sonarqube

# View logs
docker logs sonarqube
```

## ✅ Verification

Once configured, your GitHub Actions workflow will:

1. ✅ Start a SonarQube service container
2. ✅ Wait for SonarQube to be ready
3. ✅ Run code analysis with your local configuration
4. ✅ Upload results to your local SonarQube instance

## 🔍 Troubleshooting

**If SonarQube fails to start in GitHub Actions:**
- Check if the health check passes
- Increase timeout values if needed
- Verify the SonarQube image version

**If analysis fails:**
- Verify SONAR_TOKEN is correctly set
- Check project key matches in SonarQube
- Review SonarQube scanner logs in GitHub Actions

## 📊 Viewing Results

After successful analysis:
1. **Go to**: http://localhost:9000/projects
2. **Find your project**: `credit-transfer-modern`
3. **View analysis results**: Code coverage, quality gates, issues, etc. 