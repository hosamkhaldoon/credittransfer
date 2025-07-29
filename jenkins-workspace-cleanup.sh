#!/bin/bash
# Jenkins Workspace Cleanup Script
# Run this on your Jenkins server to fix git workspace issues

echo "🧹 Cleaning Jenkins workspace for CreditTransfer-Pipeline..."

# Navigate to Jenkins workspace directory
JENKINS_HOME="/var/jenkins_home"
WORKSPACE_DIR="$JENKINS_HOME/workspace/CreditTransfer-Pipeline"

# Stop Jenkins (optional, for thorough cleanup)
echo "🛑 Stopping Jenkins..."
sudo systemctl stop jenkins

# Remove corrupted workspace
if [ -d "$WORKSPACE_DIR" ]; then
    echo "🗑️ Removing corrupted workspace: $WORKSPACE_DIR"
    sudo rm -rf "$WORKSPACE_DIR"
else
    echo "ℹ️ Workspace directory not found: $WORKSPACE_DIR"
fi

# Remove workspace locks
echo "🔓 Removing workspace locks..."
sudo find $JENKINS_HOME -name "*.lock" -delete

# Fix permissions
echo "🔧 Fixing permissions..."
sudo chown -R jenkins:jenkins $JENKINS_HOME
sudo chmod -R 755 $JENKINS_HOME

# Start Jenkins
echo "🚀 Starting Jenkins..."
sudo systemctl start jenkins

echo "✅ Workspace cleanup completed!"
echo "🔄 Wait 30 seconds for Jenkins to start, then trigger your pipeline" 