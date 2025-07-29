#!/bin/bash
# Docker Installation in Jenkins Container - Complete Setup
# Run these commands to enable Docker builds in your Jenkins container

echo "🐳 Installing Docker inside Jenkins Container..."

docker exec -it jenkins-with-docker bash -c "curl -fsSL https://get.docker.com -o get-docker.sh && sh get-docker.sh"

# Step 1: Install Docker inside the Jenkins container
echo "📦 Installing Docker in Jenkins container..."
docker exec -u root jenkins-with-docker bash -c "
    # Update package list
    apt-get update
    
    # Install prerequisites
    apt-get install -y curl ca-certificates gnupg lsb-release
    
    # Add Docker's official GPG key
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
    
    # Add Docker repository
    echo \"deb [arch=\$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian \$(lsb_release -cs) stable\" | tee /etc/apt/sources.list.d/docker.list > /dev/null
    
    # Update package list again
    apt-get update
    
    # Install Docker
    apt-get install -y docker-ce docker-ce-cli containerd.io
    
    # Add jenkins user to docker group
    usermod -aG docker jenkins
"
# Step 2: Restart Jenkins container to apply changes
echo "🔄 Restarting Jenkins container..."
docker restart jenkins-with-docker

# Step 3: Wait for Jenkins to start
echo "⏳ Waiting for Jenkins to restart..."
sleep 30

# Step 4: Test Docker installation
echo "🧪 Testing Docker installation..."
docker exec jenkins-with-docker docker --version

# Step 5: Test Docker functionality
echo "🚀 Testing Docker functionality..."
docker exec jenkins-with-docker docker run hello-world

echo "✅ Docker installation completed successfully!"
echo "🎯 Your Jenkins can now build Docker images!"
echo "🔄 Trigger your pipeline again to see Docker builds in action!" 