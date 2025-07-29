# Manual Docker Hub Push Script
# This script manually pushes the built images from Jenkins to Docker Hub

Write-Host "🐳 Manual Docker Hub Push Starting..." -ForegroundColor Green
Write-Host ""

# Check if images exist in Jenkins container
Write-Host "📋 Checking available images in Jenkins container..." -ForegroundColor Yellow
docker exec jenkins-with-docker docker images | Select-String "dockerhosam"

Write-Host ""
Write-Host "🔑 Attempting to push images to Docker Hub..." -ForegroundColor Yellow
Write-Host "Note: Using credentials already configured in Jenkins" -ForegroundColor Gray
Write-Host ""

# Push WCF Service
Write-Host "📦 Pushing WCF Service..." -ForegroundColor Cyan
try {
    docker exec jenkins-with-docker docker push dockerhosam/wcf-service:latest
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ WCF Service pushed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ WCF Service push failed" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error pushing WCF Service: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Push REST API
Write-Host "📦 Pushing REST API..." -ForegroundColor Cyan
try {
    docker exec jenkins-with-docker docker push dockerhosam/rest-api:latest
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ REST API pushed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ REST API push failed" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error pushing REST API: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Push Worker Service
Write-Host "📦 Pushing Worker Service..." -ForegroundColor Cyan
try {
    docker exec jenkins-with-docker docker push dockerhosam/worker-service:latest
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Worker Service pushed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Worker Service push failed" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error pushing Worker Service: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 Push process completed!" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Check your repositories at:" -ForegroundColor Yellow
Write-Host "   - https://hub.docker.com/r/dockerhosam/wcf-service"
Write-Host "   - https://hub.docker.com/r/dockerhosam/rest-api"
Write-Host "   - https://hub.docker.com/r/dockerhosam/worker-service"
Write-Host ""
Write-Host "🔄 If push fails, we'll need to login manually..." -ForegroundColor Gray 