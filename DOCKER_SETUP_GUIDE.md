# 🐳 Docker Setup Guide for Jenkins Pipeline

This guide explains how to enable Docker image building in your Jenkins pipeline.

## 📋 **Current Status**
- ✅ **.NET build and publish**: Working perfectly
- ⚠️ **Docker image building**: Skipped due to Docker unavailability
- ⚠️ **Docker image pushing**: Skipped due to missing images

## 🔧 **Solution Options**

### **Option 1: Install Docker on Jenkins Agent (Recommended)**

#### **For Ubuntu/Debian Jenkins Agent:**
```bash
# SSH into your Jenkins agent
ssh jenkins-user@your-jenkins-agent

# Install Docker
sudo apt-get update
sudo apt-get install -y docker.io

# Add Jenkins user to docker group
sudo usermod -aG docker jenkins

# Start Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Test Docker access
sudo -u jenkins docker --version
```

#### **For CentOS/RHEL Jenkins Agent:**
```bash
# Install Docker
sudo yum install -y docker

# Start and enable Docker
sudo systemctl start docker
sudo systemctl enable docker

# Add Jenkins user to docker group
sudo usermod -aG docker jenkins

# Test Docker access
sudo -u jenkins docker --version
```

### **Option 2: Use Docker-in-Docker Agent**

Modify your Jenkinsfile to use a Docker agent:

```groovy
pipeline {
    agent {
        docker {
            image 'docker:dind'
            args '-v /var/run/docker.sock:/var/run/docker.sock'
        }
    }
    // ... rest of pipeline
}
```

### **Option 3: Use Jenkins Docker Plugin**

1. **Install Docker Pipeline Plugin** in Jenkins
2. **Configure Docker** in Jenkins Global Tools:
   - Go to: Manage Jenkins → Global Tool Configuration
   - Add Docker installation
   - Set path to Docker executable

### **Option 4: Use External Docker Host**

Configure Docker to use a remote Docker daemon:

```groovy
environment {
    DOCKER_HOST = 'tcp://your-docker-host:2376'
    DOCKER_TLS_VERIFY = '1'
    DOCKER_CERT_PATH = '/path/to/docker/certs'
}
```

## 🐋 **Docker Hub Setup**

### **1. Create Docker Hub Account**
- Go to https://hub.docker.com
- Create account with username: `dockerhosam`

### **2. Create Jenkins Credentials**
- Go to: Jenkins → Manage Jenkins → Credentials
- Click "Add Credentials"
- Type: Username with password
- ID: `docker-hub-credentials`
- Username: `dockerhosam`
- Password: Your Docker Hub password

### **3. Test Docker Hub Access**
```bash
docker login
# Enter: dockerhosam
# Enter: your-password
```

## 🎯 **Expected Results After Setup**

Once Docker is properly configured, your pipeline will build and push these images:

```
dockerhosam/wcf-service:latest
dockerhosam/wcf-service:123  # Build number
dockerhosam/rest-api:latest
dockerhosam/rest-api:123     # Build number
dockerhosam/worker-service:latest
dockerhosam/worker-service:123  # Build number
```

## 🚀 **Manual Docker Build (Alternative)**

If you can't set up Docker in Jenkins, you can manually build and push images:

```bash
# Clone the repository
git clone https://github.com/hosamkhaldoon/credittransfer.git
cd credittransfer/Migrated

# Build images manually
docker build -t dockerhosam/wcf-service:latest \
  -f src/Services/WebServices/CreditTransferService/Dockerfile .

docker build -t dockerhosam/rest-api:latest \
  -f src/Services/ApiServices/CreditTransferApi/Dockerfile .

docker build -t dockerhosam/worker-service:latest \
  -f src/Services/WorkerServices/CreditTransferWorker/Dockerfile .

# Push to Docker Hub
docker login
docker push dockerhosam/wcf-service:latest
docker push dockerhosam/rest-api:latest
docker push dockerhosam/worker-service:latest
```

## 🔍 **Troubleshooting**

### **Permission Denied Errors**
```bash
# Fix Docker socket permissions
sudo chmod 666 /var/run/docker.sock

# Or add user to docker group
sudo usermod -aG docker $USER
# Then logout and login again
```

### **Docker Daemon Not Running**
```bash
# Start Docker service
sudo systemctl start docker

# Check Docker status
sudo systemctl status docker
```

### **Jenkins Can't Access Docker**
```bash
# Test as Jenkins user
sudo -u jenkins docker ps

# If fails, check group membership
groups jenkins

# Should include 'docker' group
```

## ✅ **Verification**

After setup, run the Jenkins pipeline again. You should see:

```
🐳 Building Docker images
✅ Docker is available: Docker version 20.10.x
Building WCF Service Docker image...
✅ WCF Service image built successfully
Building REST API Docker image...
✅ REST API image built successfully
Building Worker Service Docker image...
✅ Worker Service image built successfully
```

## 📞 **Need Help?**

- Check Jenkins console logs for specific errors
- Verify Docker installation: `docker --version`
- Test Docker Hub access: `docker login`
- Check Jenkins user permissions: `sudo -u jenkins docker ps` 