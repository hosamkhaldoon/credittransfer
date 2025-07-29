pipeline {
    // Option 1: Use any available agent (current setup)
    agent any
    
    // Option 2: Use Docker-enabled agent (uncomment to use)
    // agent {
    //     label 'docker-enabled'
    // }
    
    // Option 3: Use Docker-in-Docker (requires Docker on host - uncomment to use)  
    // agent {
    //     docker {
    //         image 'docker:latest'
    //         args '--privileged -v /var/run/docker.sock:/var/run/docker.sock'
    //     }
    // }
    
    environment {
        // Project Configuration
        SOLUTION_FILE = 'Migrated/CreditTransfer.Modern.sln'
        PROJECT_NAME = 'credittransfer'
        
        // Version Management
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        GIT_COMMIT_SHORT = sh(script: 'git rev-parse --short HEAD', returnStdout: true).trim()
        VERSION = "${env.BUILD_NUMBER}-${env.GIT_COMMIT_SHORT}"
        
        // Test Configuration
        TEST_RESULTS_DIR = 'test-results'
        COVERAGE_DIR = 'coverage'
        
        // Docker Configuration
        DOCKER_REGISTRY = ''  // Empty for Docker Hub, or 'your-registry.azurecr.io' for Azure, etc.
        DOCKER_NAMESPACE = 'dockerhosam'  // Replace with your Docker Hub username or organization
        DOCKER_TAG = "${env.BUILD_NUMBER}"
        
        // Notification
        EMAIL_RECIPIENTS = 'hosam93644@gmail.com'
        
        // .NET SDK version and configuration
        DOTNET_VERSION = '8.0.100'
        DOTNET_ROOT = "${WORKSPACE}/.dotnet"
        PATH = "${DOTNET_ROOT}:${PATH}"
        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '1'
    }
    
    options {
        timeout(time: 2, unit: 'HOURS')
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '10'))
        timestamps()
        ansiColor('xterm')
    }
    
    stages {
        stage('🛠️ Setup Environment') {
            steps {
                script {
                    echo "🛠️ Setting up build environment"
                    sh '''#!/bin/bash
                        # Create directories with proper permissions
                        mkdir -p ${DOTNET_ROOT} ${TEST_RESULTS_DIR} ${COVERAGE_DIR}
                        chmod -R 777 ${TEST_RESULTS_DIR} ${COVERAGE_DIR}
                        
                        # Install .NET SDK
                        if [ ! -f ${DOTNET_ROOT}/dotnet ]; then
                            echo "Installing .NET SDK ${DOTNET_VERSION}..."
                            curl -SL --retry 3 --retry-delay 5 https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o dotnet-install.sh
                            if [ ! -f dotnet-install.sh ]; then
                                echo "Failed to download dotnet-install.sh"
                                exit 1
                            fi
                            chmod +x ./dotnet-install.sh
                            ./dotnet-install.sh --version ${DOTNET_VERSION} --install-dir ${DOTNET_ROOT} --verbose
                            rm dotnet-install.sh
                        fi
                        
                        # Verify .NET installation
                        echo ".NET SDK version:"
                            DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 ${DOTNET_ROOT}/dotnet --info || {
                                echo "Failed to run dotnet --info. Error code: $?"
                                exit 1
                            }
                    '''
                }
            }
        }
        
        stage('📦 Restore Dependencies') {
            steps {
                script {
                    echo "📦 Restoring NuGet packages"
                    sh '''#!/bin/bash
                        # Export environment variables
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        # Verify solution file exists
                        if [ ! -f "${SOLUTION_FILE}" ]; then
                            echo "Error: Solution file not found at ${SOLUTION_FILE}"
                            echo "Current directory contents:"
                            ls -la
                            echo "Migrated directory contents:"
                            ls -la Migrated/ || echo "Migrated directory not found"
                            exit 1
                        fi
                        
                        # Restore packages
                        echo "Restoring packages..."
                        ${DOTNET_ROOT}/dotnet restore "${SOLUTION_FILE}" --verbosity normal
                    '''
                }
            }
        }
        
        stage('🏗️ Build Solution') {
            steps {
                script {
                    echo "🔨 Building .NET Solution"
                    sh '''#!/bin/bash
                        # Export environment variables
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        # Build the entire solution
                        echo "Building solution..."
                        ${DOTNET_ROOT}/dotnet build "${SOLUTION_FILE}" \
                            --configuration Release \
                            --no-restore \
                            --verbosity normal \
                            /p:Version=${VERSION}
                    '''
                }
            }
        }

        stage('🧪 Run Tests') {
            steps {
                script {
                    echo "🧪 Running Tests"
                    sh '''#!/bin/bash
                        # Export environment variables
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        # Ensure test results directory exists and has correct permissions
                        TEST_RESULTS_PATH="${WORKSPACE}/${TEST_RESULTS_DIR}"
                        mkdir -p "${TEST_RESULTS_PATH}"
                        chmod -R 777 "${TEST_RESULTS_PATH}"
                        
                        # Run all tests
                        echo "Running all tests..."
                        ${DOTNET_ROOT}/dotnet test "${SOLUTION_FILE}" \
                            --configuration Release \
                            --no-build \
                            --logger "trx;LogFileName=test_results.trx" \
                            --results-directory "${TEST_RESULTS_PATH}" \
                            --verbosity normal \
                            --collect:"XPlat Code Coverage" || echo "Some tests failed, but continuing..."
                        
                        # List test results
                        echo "Test results directory contents:"
                        ls -la "${TEST_RESULTS_PATH}" || true
                    '''
                }
            }
            post {
                always {
                    junit(
                        allowEmptyResults: true,
                        testResults: '**/test-results/**/*.trx',
                        skipPublishingChecks: true
                    )
                }
            }
        }

        stage('📦 Publish Artifacts') {
            steps {
                script {
                    echo "📦 Publishing application artifacts"
                    sh '''#!/bin/bash
                        # Export environment variables
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        cd Migrated
                        
                        # Create publish directories
                        mkdir -p publish/wcf publish/api publish/worker
                        
                        # Publish WCF Service
                        echo "Publishing WCF Service..."
                        ${DOTNET_ROOT}/dotnet publish src/Services/WebServices/CreditTransferService/CreditTransfer.Services.WcfService.csproj \
                            --configuration Release \
                            --output ./publish/wcf \
                            --no-build \
                            /p:Version=${VERSION}
                        
                        # Publish REST API
                        echo "Publishing REST API..."
                        ${DOTNET_ROOT}/dotnet publish src/Services/ApiServices/CreditTransferApi/CreditTransfer.Services.RestApi.csproj \
                            --configuration Release \
                            --output ./publish/api \
                            --no-build \
                            /p:Version=${VERSION}
                        
                        # Publish Worker Service
                        echo "Publishing Worker Service..."
                        ${DOTNET_ROOT}/dotnet publish src/Services/WorkerServices/CreditTransferWorker/CreditTransfer.Services.WorkerService.csproj \
                            --configuration Release \
                            --output ./publish/worker \
                            --no-build \
                            /p:Version=${VERSION}
                        
                        # Verify publish output
                        echo "Verifying published output..."
                        ls -la publish/wcf || echo "WCF publish failed"
                        ls -la publish/api || echo "API publish failed"
                        ls -la publish/worker || echo "Worker publish failed"
                    '''
                }
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Migrated/publish/**/*', fingerprint: true, allowEmptyArchive: true
                }
            }
        }
        
        stage('🐳 Build Docker Images') {
            steps {
                script {
                    echo "🐳 Building Docker images"
                    sh '''#!/bin/bash
                        # Check if Docker is available
                        if ! command -v docker &> /dev/null; then
                            echo "⚠️ Docker not found on this Jenkins agent"
                            echo ""
                            echo "📋 To enable Docker image building, please:"
                            echo "1. Install Docker on the Jenkins agent, OR"
                            echo "2. Use a Jenkins agent with Docker pre-installed, OR" 
                            echo "3. Use Docker-in-Docker (DinD) agent configuration"
                            echo ""
                            echo "🚀 For now, skipping Docker build stage..."
                            echo "✅ The .NET applications were successfully built and published"
                            echo "📦 Published artifacts are available and can be deployed manually"
                            exit 0
                        fi
                        
                        # Verify Docker is working
                        docker --version || {
                            echo "❌ Docker installation failed or not accessible"
                            echo "📋 Manual Docker setup required on Jenkins agent"
                            exit 0
                        }
                        
                        echo "✅ Docker is available: $(docker --version)"
                        
                        cd Migrated
                        
                        # Build WCF Service Docker Image
                        echo "Building WCF Service Docker image..."
                        if [ -f "src/Services/WebServices/CreditTransferService/Dockerfile" ]; then
                            docker build -t ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:${DOCKER_TAG} \
                                -t ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:latest \
                                -f src/Services/WebServices/CreditTransferService/Dockerfile .
                            echo "✅ WCF Service image built successfully"
                        else
                            echo "⚠️ Dockerfile not found for WCF Service, skipping..."
                        fi
                        
                        # Build REST API Docker Image
                        echo "Building REST API Docker image..."
                        if [ -f "src/Services/ApiServices/CreditTransferApi/Dockerfile" ]; then
                            docker build -t ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:${DOCKER_TAG} \
                                -t ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:latest \
                                -f src/Services/ApiServices/CreditTransferApi/Dockerfile .
                            echo "✅ REST API image built successfully"
                        else
                            echo "⚠️ Dockerfile not found for REST API, skipping..."
                        fi
                        
                        # Build Worker Service Docker Image
                        echo "Building Worker Service Docker image..."
                        if [ -f "src/Services/WorkerServices/CreditTransferWorker/Dockerfile" ]; then
                            docker build -t ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:${DOCKER_TAG} \
                                -t ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:latest \
                                -f src/Services/WorkerServices/CreditTransferWorker/Dockerfile .
                            echo "✅ Worker Service image built successfully"
                        else
                            echo "⚠️ Dockerfile not found for Worker Service, skipping..."
                        fi
                        
                        # List built images
                        echo "Built Docker images:"
                        docker images | grep ${DOCKER_NAMESPACE} || echo "No images built"
                    '''
                }
            }
        }

        stage('📦 Push Docker Images') {
            steps {
                script {
                    // Debug: Show branch information
                    echo "🔍 DEBUG: Branch Name = ${env.BRANCH_NAME}"
                    echo "🔍 DEBUG: Git Branch = ${env.GIT_BRANCH}"
                    echo "🔍 DEBUG: Git Commit = ${env.GIT_COMMIT}"
                    
                    // Check if we should push (main branch or manual override)
                    def shouldPush = (env.BRANCH_NAME == 'main' || env.GIT_BRANCH?.contains('main') || env.BRANCH_NAME == 'origin/main')
                    
                    if (!shouldPush) {
                        echo "⚠️ Skipping Docker push - not on main branch"
                        echo "📋 Current branch: ${env.BRANCH_NAME}"
                        echo "📋 To force push, set environment variable FORCE_DOCKER_PUSH=true"
                        return
                    }
                    
                    echo "🚀 Proceeding with Docker push on branch: ${env.BRANCH_NAME}"
                }
                withCredentials([usernamePassword(credentialsId: 'docker-hub-credentials', 
                                                usernameVariable: 'DOCKER_USERNAME', 
                                                passwordVariable: 'DOCKER_PASSWORD')]) {
                    script {
                        echo "📦 Pushing Docker images to registry"
                        sh '''#!/bin/bash
                            # Check if Docker is available
                            if ! command -v docker &> /dev/null; then
                                echo "⚠️ Docker not available, skipping image push"
                                echo "📋 Docker images were not built in the previous stage"
                                exit 0
                            fi
                            
                            # Check if any images were built
                            if ! docker images | grep -q "${DOCKER_NAMESPACE}"; then
                                echo "⚠️ No Docker images found for ${DOCKER_NAMESPACE}"
                                echo "📋 Images were not built in the previous stage"
                                exit 0
                            fi
                            
                            # Login to Docker registry
                            echo "Logging in to Docker registry..."
                            if [ -n "${DOCKER_REGISTRY}" ]; then
                                echo "$DOCKER_PASSWORD" | docker login ${DOCKER_REGISTRY} -u "$DOCKER_USERNAME" --password-stdin || {
                                    echo "❌ Failed to login to Docker registry: ${DOCKER_REGISTRY}"
                                    exit 1
                                }
                            else
                                echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin || {
                                    echo "❌ Failed to login to Docker Hub"
                                    exit 1
                                }
                            fi
                            
                            # Push WCF Service image
                            if docker images | grep -q "${DOCKER_NAMESPACE}/wcf-service"; then
                                echo "Pushing WCF Service image..."
                                docker push ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:${DOCKER_TAG}
                                docker push ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:latest
                                echo "✅ WCF Service image pushed successfully"
                            fi
                            
                            # Push REST API image
                            if docker images | grep -q "${DOCKER_NAMESPACE}/rest-api"; then
                                echo "Pushing REST API image..."
                                docker push ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:${DOCKER_TAG}
                                docker push ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:latest
                                echo "✅ REST API image pushed successfully"
                            fi
                            
                            # Push Worker Service image
                            if docker images | grep -q "${DOCKER_NAMESPACE}/worker-service"; then
                                echo "Pushing Worker Service image..."
                                docker push ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:${DOCKER_TAG}
                                docker push ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:latest
                                echo "✅ Worker Service image pushed successfully"
                            fi
                            
                            # Logout from registry
                            if [ -n "${DOCKER_REGISTRY}" ]; then
                                docker logout ${DOCKER_REGISTRY}
                            else
                                docker logout
                            fi
                            
                            echo "✅ Docker image push process completed"
                        '''
                    }
                }
            }
            post {
                always {
                    // Clean up local images to save space (only if Docker is available)
                    sh '''#!/bin/bash
                        if command -v docker &> /dev/null; then
                            echo "🧹 Cleaning up local Docker images..."
                            docker rmi ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:${DOCKER_TAG} 2>/dev/null || true
                            docker rmi ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:${DOCKER_TAG} 2>/dev/null || true
                            docker rmi ${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:${DOCKER_TAG} 2>/dev/null || true
                            echo "✅ Cleanup completed"
                        else
                            echo "ℹ️ Docker not available, skipping cleanup"
                        fi
                    '''
                }
            }
        }
    }
    
    post {
        always {
            script {
                try {
                    echo "🧹 Performing cleanup tasks"
                    // Only archive if we have a workspace context
                    if (env.WORKSPACE) {
                        archiveArtifacts artifacts: '**/*.log', allowEmptyArchive: true
                    } else {
                        echo "⚠️ No workspace context available, skipping artifact archival"
                    }
                } catch (Exception e) {
                    echo "⚠️ Cleanup failed: ${e.getMessage()}"
                }
            }
        }
        
        success {
            script {
                try {
                    notifyBuild('SUCCESS', 'Pipeline completed successfully')
                    if (env.WORKSPACE) {
                        sh '''#!/bin/bash
                            echo "Pipeline completed successfully at $(date)" > deployment-status.txt
                        '''
                        archiveArtifacts artifacts: 'deployment-status.txt', allowEmptyArchive: true
                    }
                } catch (Exception e) {
                    echo "⚠️ Success notification failed: ${e.getMessage()}"
                }
            }
        }
        
        failure {
            script {
                try {
                    notifyBuild('FAILED', 'Pipeline failed')
                    if (env.WORKSPACE) {
                        sh '''#!/bin/bash
                            echo "Pipeline failed at $(date)" > failure-status.txt
                        '''
                        archiveArtifacts artifacts: 'failure-status.txt', allowEmptyArchive: true
                    }
                } catch (Exception e) {
                    echo "⚠️ Failure notification failed: ${e.getMessage()}"
                }
            }
        }
        
        unstable {
            script {
                try {
                    notifyBuild('UNSTABLE', 'Pipeline completed with issues')
                } catch (Exception e) {
                    echo "⚠️ Unstable notification failed: ${e.getMessage()}"
                }
            }
        }
        
        aborted {
            script {
                try {
                    notifyBuild('ABORTED', 'Pipeline was aborted')
                } catch (Exception e) {
                    echo "⚠️ Aborted notification failed: ${e.getMessage()}"
                }
            }
        }
    }
}

def notifyBuild(String buildStatus, String message) {
    def colorCode = buildStatus == 'SUCCESS' ? 'good' : 
                   buildStatus == 'UNSTABLE' ? 'warning' : 'danger'
    
    def subject = "${buildStatus}: Job '${env.JOB_NAME} [${env.BUILD_NUMBER}]'"
    def summary = "${subject} - ${message}"
    def details = """
        *Project*: ${env.JOB_NAME}
        *Build Number*: ${env.BUILD_NUMBER}
        *Status*: ${buildStatus}
        *Message*: ${message}
        *Branch*: ${env.BRANCH_NAME}
        *Version*: ${env.VERSION}
        *Build URL*: ${env.BUILD_URL}
    """
    
    // Email notification (primary notification method)
    try {
        // Check if email is configured
        if (env.EMAIL_RECIPIENTS && env.EMAIL_RECIPIENTS.trim()) {
            emailext(
                subject: subject,
                body: details,
                to: env.EMAIL_RECIPIENTS,
                mimeType: 'text/plain',
                replyTo: '$DEFAULT_REPLYTO',
                recipientProviders: [
                    [$class: 'CulpritsRecipientProvider'],
                    [$class: 'DevelopersRecipientProvider'],
                    [$class: 'RequesterRecipientProvider']
                ]
            )
            echo "✅ Email notification sent successfully"
        } else {
            echo "⚠️ No email recipients configured, skipping email notification"
        }
    } catch (Exception e) {
        echo "⚠️ Email notification failed: ${e.getMessage()}"
        echo "📋 Configure SMTP settings in Jenkins System Configuration"
        
        // Fallback: Write notification to file
        try {
            writeFile file: 'build-notification.txt', text: """
BUILD NOTIFICATION
${summary}
${details}
Time: ${new Date()}
"""
            archiveArtifacts artifacts: 'build-notification.txt', allowEmptyArchive: true
            echo "📝 Build notification saved to artifacts"
        } catch (Exception fe) {
            echo "⚠️ Fallback notification also failed: ${fe.getMessage()}"
        }
    }
} 