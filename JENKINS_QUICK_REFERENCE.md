# 🚀 **Jenkins Quick Reference Guide**

## 🔑 **Access Information**

### **🌐 Web Interface**
- **URL**: http://localhost:8080
- **Container**: `jenkins-with-docker`
- **Security**: Currently unsecured (no login required)

### **📋 Essential Commands**

#### **Container Management**
```bash
# Start Jenkins container
docker compose -f docker-compose-jenkins.yml up -d

# Stop Jenkins container
docker compose -f docker-compose-jenkins.yml down

# Access Jenkins shell
docker exec -it jenkins-with-docker bash

# View Jenkins logs
docker logs jenkins-with-docker -f

# Restart Jenkins
docker restart jenkins-with-docker
```

#### **Docker Integration**
```bash
# Check Docker in Jenkins
docker exec jenkins-with-docker docker --version
# Output: Docker version 28.3.2, build 578ccf6

# List Docker images built by Jenkins
docker exec jenkins-with-docker docker images | grep dockerhosam
```

#### **Configuration Access**
```bash
# Main Jenkins config
docker exec jenkins-with-docker cat /var/jenkins_home/config.xml

# View credentials (encrypted)
docker exec jenkins-with-docker cat /var/jenkins_home/credentials.xml

# List all plugins
docker exec jenkins-with-docker ls /var/jenkins_home/plugins/
```

## 🔧 **Key Configuration Files**

| File | Purpose | Location |
|------|---------|----------|
| `config.xml` | Main Jenkins configuration | `/var/jenkins_home/` |
| `credentials.xml` | Stored credentials | `/var/jenkins_home/` |
| `CreditTransfer-Pipeline/config.xml` | Pipeline job config | `/var/jenkins_home/jobs/` |

## 🔑 **Configured Credentials**

| ID | Type | Username | Purpose |
|----|------|----------|---------|
| `docker-hub-credentials` | Username/Password | `dockerhosam` | Docker Hub push |
| `github-token` | String | - | GitHub API access |
| `sonartokenV2` | String | - | SonarQube integration |

## 🚀 **Active Pipelines**

### **CreditTransfer-Pipeline**
- **Repository**: https://github.com/hosamkhaldoon/credittransfer.git
- **Branch**: `main`
- **Script**: `Jenkinsfile`
- **Status**: ✅ Working with Docker builds

### **Manual Trigger**
```bash
# From Jenkins Web UI:
# 1. Go to http://localhost:8080
# 2. Click "CreditTransfer-Pipeline"
# 3. Click "Build Now"
```

## 🐳 **Docker Images Built**

### **Successfully Built Images**
```bash
dockerhosam/wcf-service:2        264MB
dockerhosam/wcf-service:latest   264MB  
dockerhosam/rest-api:2           258MB
dockerhosam/rest-api:latest      258MB
```

### **Deploy Built Images**
```bash
# Run WCF Service
docker run -d -p 8080:80 dockerhosam/wcf-service:2

# Run REST API
docker run -d -p 8090:80 dockerhosam/rest-api:2
```

## 📊 **System Status**

### **✅ What's Working**
- Jenkins 2.504.3 running
- Docker 28.3.2 integrated
- 119 plugins installed
- .NET 8 builds successful
- Docker image creation working
- GitHub integration active

### **⚠️ Configuration Needed**
- Security/Authentication setup
- SMTP email configuration
- Admin user account creation

## 🛠️ **Maintenance Commands**

### **Workspace Cleanup**
```bash
# Clean all workspaces
docker exec jenkins-with-docker rm -rf /var/jenkins_home/workspace/*

# Clean specific pipeline workspace  
docker exec jenkins-with-docker rm -rf /var/jenkins_home/workspace/CreditTransfer-Pipeline*
```

### **Plugin Management**
```bash
# List installed plugins
docker exec jenkins-with-docker find /var/jenkins_home/plugins -name "*.jpi" | wc -l

# Check plugin directory
docker exec jenkins-with-docker ls /var/jenkins_home/plugins/ | grep -E "(docker|dotnet|git)"
```

### **Build History**
```bash
# List all builds for CreditTransfer-Pipeline
docker exec jenkins-with-docker ls /var/jenkins_home/jobs/CreditTransfer-Pipeline/builds/

# View latest build log
docker exec jenkins-with-docker cat /var/jenkins_home/jobs/CreditTransfer-Pipeline/builds/*/log
```

## 🔒 **Security Recommendations**

### **Immediate Actions**
1. **Enable Security**: Go to "Manage Jenkins" > "Security"
2. **Create Admin User**: Set up proper authentication
3. **Review Credentials**: Ensure proper scoping
4. **Enable Matrix Security**: Configure role-based access

### **Security Commands**
```bash
# Check current security status
docker exec jenkins-with-docker grep -A 5 -B 5 "useSecurity" /var/jenkins_home/config.xml

# View current authorization strategy
docker exec jenkins-with-docker grep "authorizationStrategy" /var/jenkins_home/config.xml
```

## 📈 **Monitoring & Troubleshooting**

### **Health Checks**
```bash
# Check Jenkins is responding
curl -I http://localhost:8080

# Check Docker integration
docker exec jenkins-with-docker docker ps

# View system info
docker exec jenkins-with-docker df -h
docker exec jenkins-with-docker free -m
```

### **Log Access**
```bash
# Jenkins application logs
docker logs jenkins-with-docker --tail 100

# Build logs for specific job
docker exec jenkins-with-docker ls /var/jenkins_home/jobs/CreditTransfer-Pipeline/builds/*/log
```

## 🎯 **Quick Troubleshooting**

| Issue | Command | Solution |
|-------|---------|----------|
| Pipeline fails | Check workspace | `rm -rf workspace/*` |
| Docker not working | Verify Docker | `docker --version` in container |
| Credentials missing | Check credentials.xml | Reconfigure in UI |
| Build artifacts missing | Check artifacts dir | Verify publish paths |

## 🚀 **Next Steps**

1. **Security Setup**: Configure authentication and authorization
2. **Email Configuration**: Set up SMTP for notifications  
3. **Backup Strategy**: Implement regular backups
4. **Monitoring**: Add health checks and alerting
5. **Scaling**: Consider distributed builds if needed

---

**📞 Support**: For issues, check the comprehensive audit in `JENKINS_CONFIGURATION_AUDIT.md` 