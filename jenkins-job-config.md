# Jenkins Pipeline Job Configuration

## 🚀 Create New Pipeline Job

### Step 1: Create New Job
1. Go to Jenkins: http://localhost:8080
2. Click "New Item"
3. Enter name: `CreditTransfer-Pipeline`
4. Select "Pipeline"
5. Click "OK"

### Step 2: Configure Source Control
**Pipeline Section:**
- **Definition**: Pipeline script from SCM
- **SCM**: Git
- **Repository URL**: `https://github.com/hosamkhaldoon/credittransfer.git`
- **Branch**: `*/main`
- **Script Path**: `Jenkinsfile`

### Step 3: Build Triggers (Optional)
- ☑️ GitHub hook trigger for GITScm polling
- ☑️ Poll SCM: `H/5 * * * *` (every 5 minutes)

### Step 4: Pipeline Configuration
```groovy
// Pipeline script from SCM is recommended
// Uses Jenkinsfile from your repository
```

### Step 5: Save and Build
1. Click "Save"
2. Click "Build Now"
3. Monitor build progress

## 🔍 Troubleshooting

### Git Authentication
If repository is private:
1. Go to "Manage Jenkins" > "Credentials"
2. Add GitHub credentials
3. Select credentials in job configuration

### Webhook Setup
For automatic builds on push:
1. Go to GitHub repository settings
2. Add webhook: `http://your-jenkins:8080/github-webhook/`
3. Content type: `application/json`
4. Events: `push`, `pull_request`

## ✅ Verification

After setup, verify:
- ✅ Git checkout works
- ✅ .NET build succeeds  
- ✅ Tests pass
- ✅ Docker images build (if Docker enabled)
- ✅ Artifacts published 