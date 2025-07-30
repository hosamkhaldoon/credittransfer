# 🔍 SonarQube Integration Guide for Jenkins

## 🎯 **Overview**
SonarQube will analyze your .NET 8 code for:
- ✅ **Code Quality**: Bugs, vulnerabilities, code smells
- ✅ **Test Coverage**: Line and branch coverage metrics
- ✅ **Security**: Security hotspots and vulnerabilities
- ✅ **Maintainability**: Technical debt and complexity
- ✅ **Reliability**: Code reliability issues

## 🚀 **Step 1: Install SonarQube Server**

### **Option A: Docker (Recommended)**
```bash
# Create SonarQube container
docker run -d --name sonarqube \
  -p 9000:9000 \
  -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true \
  -v sonarqube_data:/opt/sonarqube/data \
  -v sonarqube_extensions:/opt/sonarqube/extensions \
  -v sonarqube_logs:/opt/sonarqube/logs \
  sonarqube:community

# Check if running
docker ps | grep sonarqube
```

### **Option B: Docker Compose**
```yaml
# docker-compose-sonarqube.yml
version: '3.8'
services:
  sonarqube:
    image: sonarqube:community
    ports:
      - "9000:9000"
    environment:
      - SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true
    volumes:
      - sonarqube_data:/opt/sonarqube/data
      - sonarqube_extensions:/opt/sonarqube/extensions
      - sonarqube_logs:/opt/sonarqube/logs

volumes:
  sonarqube_data:
  sonarqube_extensions:
  sonarqube_logs:
```

## 🔧 **Step 2: Configure SonarQube**

### **Access SonarQube**
1. **Open**: http://localhost:9000
2. **Default credentials**: `admin` / `admin`
3. **Change password** when prompted

### **Create Project**
1. **Click**: "Create new project"
2. **Project key**: `credit-transfer-modern`
3. **Display name**: `Credit Transfer Modern`
4. **Main branch**: `main`
5. **Click**: "Set Up"

### **Generate Token**
1. **Go to**: Administration → Security → Users
2. **Click**: "Tokens" tab
3. **Generate token**: `jenkins-sonar-token`
4. **Copy the token** (looks like: `sqa_1234567890abcdef`)

## 🔧 **Step 3: Configure Jenkins**

### **Install Required Plugins**
1. **Go to**: Manage Jenkins → Plugins → Available
2. **Install these plugins**:
   - ✅ **SonarQube Scanner for Jenkins**
   - ✅ **HTTP Request Plugin** (if not already installed)
   - ✅ **Pipeline Utility Steps** (for JSON parsing)

### **Configure SonarQube Server**
1. **Go to**: Manage Jenkins → System
2. **Find**: "SonarQube servers" section
3. **Add SonarQube**:
   - **Name**: `SonarQube`
   - **Server URL**: `http://localhost:9000`
   - **Server authentication token**: `[your-sonar-token]`
4. **Click**: "Save"

### **Add SonarQube Credentials**
1. **Go to**: Manage Jenkins → Credentials → Global
2. **Click**: "Add Credentials"
3. **Kind**: "Secret text"
4. **Secret**: `[your-sonar-token]`
5. **ID**: `sonar-token`
6. **Description**: `SonarQube Authentication Token`
7. **Click**: "Create"

## 📊 **Step 4: Quality Gate Configuration**

### **Default Quality Gate**
SonarQube comes with a default quality gate that checks:
- ✅ **Coverage on new code** > 80%
- ✅ **Duplicated lines on new code** < 3%
- ✅ **Maintainability rating** = A
- ✅ **Reliability rating** = A
- ✅ **Security rating** = A
- ✅ **Security hotspots reviewed** = 100%

### **Custom Quality Gate (Optional)**
1. **Go to**: Administration → Quality Gates
2. **Create new gate**: "Credit Transfer Standards"
3. **Add conditions**:
   - Coverage > 70%
   - Duplicated lines < 5%
   - Maintainability rating >= B
   - Reliability rating >= B
   - Security rating >= B

## 🚀 **Step 5: Test Integration**

### **Manual Test**
1. **Trigger Jenkins build**
2. **Check SonarQube stage** in pipeline
3. **Verify results** at: http://localhost:9000/dashboard?id=credit-transfer-modern

### **Expected Pipeline Output**
```
🔍 Running SonarQube Code Quality Analysis
Installing SonarQube Scanner for .NET...
Starting SonarQube analysis...
Building with code coverage...
Running tests with coverage...
Finalizing SonarQube analysis...
✅ SonarQube analysis completed
SonarQube Quality Gate: OK
```

## 📈 **Step 6: View Results**

### **SonarQube Dashboard**
- **URL**: http://localhost:9000/dashboard?id=credit-transfer-modern
- **Metrics**: Code coverage, bugs, vulnerabilities, code smells
- **History**: Track improvements over time
- **Issues**: Detailed code quality issues

### **Jenkins Integration**
- **Quality Gate**: Pass/fail based on SonarQube rules
- **Reports**: Archived coverage reports
- **Notifications**: Slack notifications include quality status

## 🎯 **Quality Metrics to Monitor**

### **Code Coverage**
- **Target**: > 80% line coverage
- **Current**: Check SonarQube dashboard
- **Improvement**: Add more unit tests

### **Code Quality**
- **Bugs**: Should be 0
- **Vulnerabilities**: Should be 0
- **Code Smells**: Minimize technical debt
- **Duplications**: < 3% of codebase

### **Security**
- **Security Hotspots**: Review and fix
- **Vulnerabilities**: Critical to fix immediately
- **Security Rating**: Maintain A or B

## 🔧 **Troubleshooting**

### **Common Issues**
1. **SonarQube not accessible**: Check Docker container status
2. **Authentication failed**: Verify token in Jenkins credentials
3. **Coverage not found**: Ensure tests are running with coverage
4. **Quality gate failed**: Check SonarQube dashboard for issues

### **Debug Commands**
```bash
# Check SonarQube container
docker logs sonarqube

# Check Jenkins SonarQube plugin
docker exec jenkins-with-docker curl -I http://localhost:9000

# Test SonarQube API
curl -u admin:admin http://localhost:9000/api/system/status
```

## 🎉 **Benefits**
- ✅ **Automated code quality checks**
- ✅ **Prevents poor code from reaching production**
- ✅ **Tracks code quality improvements over time**
- ✅ **Identifies security vulnerabilities early**
- ✅ **Enforces coding standards**
- ✅ **Provides detailed technical debt analysis**

**Your pipeline will now ensure high code quality before building Docker images!** 🚀 