#!/bin/bash
# Docker Installation Script for Jenkins Agent
# Run this on your Jenkins agent to enable Docker builds

set -e

echo "🐳 Installing Docker on Jenkins Agent..."

# Update package index
sudo apt-get update

# Install prerequisites
sudo apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Set up the stable repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Update package index again
sudo apt-get update

# Install Docker Engine
sudo apt-get install -y docker-ce docker-ce-cli containerd.io

# Add jenkins user to docker group
sudo usermod -aG docker jenkins

# Start and enable Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Restart Jenkins to apply group changes
sudo systemctl restart jenkins

echo "✅ Docker installation completed!"
echo "🔄 Jenkins has been restarted to apply Docker group permissions"
echo "🚀 Your Jenkins pipeline can now build Docker images!"

# Test Docker installation
echo "🧪 Testing Docker installation..."
sudo -u jenkins docker --version
echo "✅ Docker test successful!" 