#!/bin/bash
# Manual Docker Hub Push Test Script
# Run this to test Docker Hub access manually

echo "🐳 Testing Docker Hub Push Manually..."

# Check if images exist
echo "📋 Current dockerhosam images:"
docker images | grep dockerhosam

echo ""
echo "🔑 Testing Docker Hub login..."
echo "Please enter your Docker Hub password for 'dockerhosam':"

# Login (you'll need to enter password manually)
docker login --username dockerhosam

if [ $? -eq 0 ]; then
    echo "✅ Login successful!"
    
    echo "🚀 Pushing images to Docker Hub..."
    
    # Push WCF Service
    echo "Pushing WCF Service..."
    docker push dockerhosam/wcf-service:latest
    
    # Push REST API
    echo "Pushing REST API..."
    docker push dockerhosam/rest-api:latest
    
    # Push Worker Service
    echo "Pushing Worker Service..."
    docker push dockerhosam/worker-service:latest
    
    echo "✅ Manual push completed!"
    echo "📍 Check your repositories at: https://hub.docker.com/u/dockerhosam"
else
    echo "❌ Login failed - check username/password"
fi

# Logout
docker logout
echo "🔒 Logged out from Docker Hub" 