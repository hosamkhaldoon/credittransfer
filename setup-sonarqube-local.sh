#!/bin/bash

# 🔍 SonarQube Local Setup Script
# This script sets up SonarQube locally and allows you to view analysis results

set -e

echo "🔍 SonarQube Local Setup Script"
echo "================================"

# Configuration
SONAR_PROJECT_KEY="credit-transfer-modern"
SONAR_PROJECT_NAME="Credit Transfer Modern"
SONAR_HOST_URL="http://localhost:9000"
SOLUTION_FILE="Migrated/CreditTransfer.Modern.sln"

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        echo "❌ Docker is not running. Please start Docker Desktop first."
        exit 1
    fi
    echo "✅ Docker is running"
}

# Function to stop existing SonarQube container
stop_existing_sonarqube() {
    echo "🛑 Stopping any existing SonarQube containers..."
    docker stop sonarqube 2>/dev/null || true
    docker rm sonarqube 2>/dev/null || true
    echo "✅ Cleaned up existing containers"
}

# Function to start SonarQube
start_sonarqube() {
    echo "🚀 Starting SonarQube container..."
    docker run -d \
        --name sonarqube \
        -p 9000:9000 \
        -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true \
        sonarqube:10-community
    
    echo "⏳ Waiting for SonarQube to start up (this may take 2-3 minutes)..."
    
    # Wait for SonarQube to be ready
    for i in {1..60}; do
        if curl -s -f http://localhost:9000/api/system/status | grep -q "UP"; then
            echo "✅ SonarQube is ready!"
            break
        fi
        echo "⏳ Still starting up... (${i}/60)"
        sleep 5
    done
    
    if ! curl -s -f http://localhost:9000/api/system/status | grep -q "UP"; then
        echo "❌ SonarQube failed to start. Check Docker logs: docker logs sonarqube"
        exit 1
    fi
}

# Function to install SonarQube scanner
install_scanner() {
    echo "🔧 Checking SonarQube Scanner for .NET..."
    
    if ! command -v dotnet-sonarscanner &> /dev/null; then
        echo "📦 Installing SonarQube Scanner for .NET..."
        dotnet tool install --global dotnet-sonarscanner
    else
        echo "✅ SonarQube Scanner already installed"
    fi
}

# Function to run analysis
run_analysis() {
    echo "🔍 Running SonarQube analysis..."
    
    if [ ! -f "$SOLUTION_FILE" ]; then
        echo "❌ Solution file not found: $SOLUTION_FILE"
        echo "💡 Make sure you're running this script from the project root directory"
        exit 1
    fi
    
    # Get version from git or use default
    VERSION=$(git rev-parse --short HEAD 2>/dev/null || echo "local-$(date +%Y%m%d)")
    
    cd Migrated
    
    # Begin analysis
    dotnet sonarscanner begin \
        /k:"$SONAR_PROJECT_KEY" \
        /n:"$SONAR_PROJECT_NAME" \
        /v:"$VERSION" \
        /d:sonar.host.url="$SONAR_HOST_URL" \
        /d:sonar.login=admin \
        /d:sonar.password=admin
    
    # Build solution
    echo "🏗️ Building solution..."
    dotnet build CreditTransfer.Modern.sln \
        --configuration Release \
        --verbosity minimal
    
    # End analysis
    dotnet sonarscanner end \
        /d:sonar.login=admin \
        /d:sonar.password=admin
    
    cd ..
    
    echo "✅ Analysis completed!"
}

# Function to show access information
show_access_info() {
    echo ""
    echo "🎉 Setup Complete!"
    echo "=================="
    echo ""
    echo "📊 Access SonarQube:"
    echo "   URL: http://localhost:9000"
    echo "   Username: admin"
    echo "   Password: admin"
    echo ""
    echo "🔍 Your project:"
    echo "   Project: $SONAR_PROJECT_NAME"
    echo "   Key: $SONAR_PROJECT_KEY"
    echo ""
    echo "🌐 Direct link to dashboard:"
    echo "   http://localhost:9000/dashboard?id=$SONAR_PROJECT_KEY"
    echo ""
    echo "🛑 To stop SonarQube later:"
    echo "   docker stop sonarqube"
    echo ""
}

# Main execution
main() {
    echo "Starting SonarQube local setup..."
    echo ""
    
    check_docker
    stop_existing_sonarqube
    start_sonarqube
    install_scanner
    run_analysis
    show_access_info
}

# Parse command line arguments
case "${1:-setup}" in
    "setup")
        main
        ;;
    "start")
        check_docker
        stop_existing_sonarqube
        start_sonarqube
        echo "✅ SonarQube started. Access: http://localhost:9000 (admin/admin)"
        ;;
    "stop")
        echo "🛑 Stopping SonarQube..."
        docker stop sonarqube 2>/dev/null || true
        docker rm sonarqube 2>/dev/null || true
        echo "✅ SonarQube stopped"
        ;;
    "analyze")
        install_scanner
        run_analysis
        echo "✅ Analysis completed. View results at: http://localhost:9000"
        ;;
    "help"|"--help"|"-h")
        echo "SonarQube Local Setup Script"
        echo ""
        echo "Usage: $0 [command]"
        echo ""
        echo "Commands:"
        echo "  setup    (default) - Complete setup: start SonarQube + run analysis"
        echo "  start    - Only start SonarQube container"
        echo "  stop     - Stop SonarQube container"
        echo "  analyze  - Only run analysis (assumes SonarQube is running)"
        echo "  help     - Show this help message"
        echo ""
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo "Use '$0 help' for usage information"
        exit 1
        ;;
esac 