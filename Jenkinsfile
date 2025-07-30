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
        FORCE_DOCKER_PUSH = 'true'  // Force push regardless of branch (set to 'false' to disable)
        
        // SonarQube Configuration
        SONAR_HOST_URL = 'http://localhost:9000'  // SonarQube server URL
        SONAR_TOKEN = credentials('sonartokenV3')  // Will be set from Jenkins credentials
        SONAR_PROJECT_KEY = 'credit-transfer-modern'  // SonarQube project key
        SONAR_PROJECT_VERSION = "${VERSION}"  // SonarQube project version
        SONAR_PROJECT_NAME = "Credit Transfer Modern"  // SonarQube project name
        
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

        stage('🔍 SonarQube Analysis') {
            steps {
                script {
                    echo "🔍 Running SonarQube Code Quality Analysis"
                    
                    // Check if SonarQube token is available
                    if (!env.SONAR_TOKEN) {
                        echo "⚠️ SonarQube token not available. Skipping SonarQube analysis."
                        echo "💡 To enable SonarQube analysis:"
                        echo "   1. Create a token in SonarQube UI (My Account → Security → Tokens)"
                        echo "   2. Add it as 'Secret text' credential with ID 'sonartokenV3' in Jenkins"
                        return
                    }
                    
                    sh """
                        # Export environment variables
                        export PATH=\"\$PATH:/root/.dotnet/tools:${DOTNET_ROOT}:${PATH}\"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        cd Migrated
                        
                        # Install SonarQube Scanner for .NET if not present
                        if ! command -v dotnet-sonarscanner &> /dev/null; then
                            echo 'Installing SonarQube Scanner for .NET...'
                            ${DOTNET_ROOT}/dotnet tool install --global dotnet-sonarscanner
                        fi
                        
                        # Verify SonarQube server is accessible
                        echo 'Checking SonarQube server connectivity...'
                        if ! curl -f -s "${SONAR_HOST_URL}/api/system/status" > /dev/null; then
                            echo "⚠️ SonarQube server at ${SONAR_HOST_URL} is not accessible"
                            echo "💡 Make sure SonarQube is running: docker ps | grep sonarqube"
                            exit 1
                        fi
                        
                        export SONAR_HOST_URL=\"${SONAR_HOST_URL}\"
                        export SONAR_TOKEN=\"${SONAR_TOKEN}\"
                        export SONAR_PROJECT_KEY=\"${SONAR_PROJECT_KEY}\"
                        export SONAR_PROJECT_NAME=\"${SONAR_PROJECT_NAME}\"
                        export SONAR_PROJECT_VERSION=\"${SONAR_PROJECT_VERSION}\"
                        
                        echo \"SonarQube Host: \$SONAR_HOST_URL\"
                        echo \"Project Key: \$SONAR_PROJECT_KEY\"
                        echo \"Project Version: \$SONAR_PROJECT_VERSION\"
                        
                        # Begin SonarQube analysis
                        echo 'Starting SonarQube analysis...'
                        dotnet sonarscanner begin \\
                            /k:\"\$SONAR_PROJECT_KEY\" \\
                            /n:\"\$SONAR_PROJECT_NAME\" \\
                            /v:\"\$SONAR_PROJECT_VERSION\" \\
                            /d:sonar.host.url=\"\$SONAR_HOST_URL\" \\
                            /d:sonar.login=\"\$SONAR_TOKEN\" \\
                            /d:sonar.cs.opencover.reportsPaths=\"**/coverage.opencover.xml\" \\
                            /d:sonar.coverage.exclusions=\"**/*Test*,**/*Tests*,**/*test*,**/*tests*\" \\
                            /d:sonar.exclusions=\"**/bin/**/*,**/obj/**/*,**/node_modules/**/*\" \\
                            /d:sonar.sourceEncoding=UTF-8
                        
                        # Build with coverage
                        echo 'Building with code coverage...'
                        ${DOTNET_ROOT}/dotnet build CreditTransfer.Modern.sln \\
                            --configuration Release \\
                            --no-restore \\
                            /p:CollectCoverage=true \\
                            /p:CoverletOutputFormat=opencover \\
                            /p:Version=${VERSION}
                        
                        # Run tests with coverage
                        echo 'Running tests with coverage...'
                        ${DOTNET_ROOT}/dotnet test CreditTransfer.Modern.sln \\
                            --configuration Release \\
                            --no-build \\
                            --collect:\"XPlat Code Coverage\" \\
                            --results-directory \"${WORKSPACE}/${COVERAGE_DIR}\" \\
                            --logger \"trx;LogFileName=test_results.trx\" \\
                            --verbosity normal
                        
                        # End SonarQube analysis
                        echo 'Finalizing SonarQube analysis...'
                        dotnet sonarscanner end /d:sonar.login=\"\$SONAR_TOKEN\"
                        
                        echo '✅ SonarQube analysis completed'
                    """
                }
            }
            post {
                always {
                    script {
                        // Archive SonarQube reports
                        archiveArtifacts artifacts: '**/coverage/**/*', allowEmptyArchive: true
                        
                        // Add SonarQube quality gate check
                        if (env.SONAR_HOST_URL) {
                            try {
                                def qualityGate = httpRequest(
                                    url: "${env.SONAR_HOST_URL}/api/qualitygates/project_status?projectKey=${env.SONAR_PROJECT_KEY}",
                                    authentication: 'sonartokenV3',
                                    validResponseCodes: '200,404'
                                )
                                
                                if (qualityGate.status == 200) {
                                    def result = readJSON text: qualityGate.content
                                    echo "SonarQube Quality Gate: ${result.projectStatus.status}"
                                    
                                    if (result.projectStatus.status == 'ERROR') {
                                        error "SonarQube Quality Gate failed! Check: ${env.SONAR_HOST_URL}/dashboard?id=credit-transfer-modern"
                                    }
                                } else {
                                    echo "⚠️ SonarQube Quality Gate not available (status: ${qualityGate.status})"
                                }
                            } catch (Exception e) {
                                echo "⚠️ SonarQube Quality Gate check failed: ${e.getMessage()}"
                            }
                        }
                    }
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
                    echo "🔍 DEBUG: Force Push = ${env.FORCE_DOCKER_PUSH}"
                    
                    // Check if we should push (main branch, manual override, or force push)
                    def shouldPush = (env.BRANCH_NAME == 'main' || env.GIT_BRANCH?.contains('main') || env.BRANCH_NAME == 'origin/main' || env.FORCE_DOCKER_PUSH == 'true')
                    
                    if (!shouldPush) {
                        echo "⚠️ Skipping Docker push - conditions not met"
                        echo "📋 Current branch: ${env.BRANCH_NAME}"
                        echo "📋 To force push, set FORCE_DOCKER_PUSH=true in environment"
                        return
                    }
                    
                    if (env.FORCE_DOCKER_PUSH == 'true') {
                        echo "🚀 Force pushing Docker images (FORCE_DOCKER_PUSH=true)"
                    } else {
                        echo "🚀 Proceeding with Docker push on branch: ${env.BRANCH_NAME}"
                    }
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
                            
                            # Login to Docker registry with proper error handling
                            echo "🔑 Logging in to Docker registry..."
                            echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin
                            
                            if [ $? -ne 0 ]; then
                                echo "❌ Failed to login to Docker Hub"
                                echo "🔍 Debug: Username = $DOCKER_USERNAME"
                                echo "🔍 Debug: Registry = ${DOCKER_REGISTRY:-docker.io}"
                                exit 1
                            fi
                            
                            echo "✅ Docker login successful"
                            
                            # Function to push with retry logic
                            push_with_retry() {
                                local image_name=$1
                                local max_attempts=3
                                local attempt=1
                                
                                while [ $attempt -le $max_attempts ]; do
                                    echo "📤 Pushing $image_name (attempt $attempt/$max_attempts)"
                                    
                                    if docker push "$image_name"; then
                                        echo "✅ Successfully pushed $image_name"
                                        return 0
                                    else
                                        echo "⚠️ Push failed for $image_name (attempt $attempt/$max_attempts)"
                                        if [ $attempt -lt $max_attempts ]; then
                                            echo "🔄 Retrying in 10 seconds..."
                                            sleep 10
                                        fi
                                    fi
                                    
                                    attempt=$((attempt + 1))
                                done
                                
                                echo "❌ Failed to push $image_name after $max_attempts attempts"
                                return 1
                            }
                            
                            # Push WCF Service image with retry
                            if docker images | grep -q "${DOCKER_NAMESPACE}/wcf-service"; then
                                echo "📦 Pushing WCF Service image..."
                                if push_with_retry "${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:${DOCKER_TAG}"; then
                                    push_with_retry "${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/wcf-service:latest"
                                fi
                            fi
                            
                            # Push REST API image with retry
                            if docker images | grep -q "${DOCKER_NAMESPACE}/rest-api"; then
                                echo "📦 Pushing REST API image..."
                                if push_with_retry "${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:${DOCKER_TAG}"; then
                                    push_with_retry "${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/rest-api:latest"
                                fi
                            fi
                            
                            # Push Worker Service image with retry
                            if docker images | grep -q "${DOCKER_NAMESPACE}/worker-service"; then
                                echo "📦 Pushing Worker Service image..."
                                if push_with_retry "${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:${DOCKER_TAG}"; then
                                    push_with_retry "${DOCKER_REGISTRY}${DOCKER_REGISTRY:+/}${DOCKER_NAMESPACE}/worker-service:latest"
                                fi
                            fi
                            
                            # Logout from registry
                            echo "🔒 Logging out from Docker registry..."
                            docker logout
                            
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
                    notifySlack('SUCCESS', 'Pipeline completed successfully! 🎉')
                    echo "✅ Pipeline completed successfully! Slack notification sent."
                    
                    if (env.WORKSPACE) {
                        sh '''#!/bin/bash
                            echo "Pipeline completed successfully at $(date)" > deployment-status.txt
                            echo "Docker images pushed to: https://hub.docker.com/u/dockerhosam" >> deployment-status.txt
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
                    notifySlack('FAILURE', 'Pipeline failed! ❌')
                    echo "❌ Pipeline failed! Slack notification sent."
                    
                    if (env.WORKSPACE) {
                        sh '''#!/bin/bash
                            echo "Pipeline failed at $(date)" > failure-status.txt
                            echo "Check build logs for details" >> failure-status.txt
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
                    notifySlack('UNSTABLE', 'Pipeline completed with issues! ⚠️')
                    echo "⚠️ Pipeline unstable! Slack notification sent."
                } catch (Exception e) {
                    echo "⚠️ Unstable notification failed: ${e.getMessage()}"
                }
            }
        }
        
        aborted {
            script {
                try {
                    notifySlack('ABORTED', 'Pipeline was aborted! 🛑')
                    echo "🛑 Pipeline aborted! Slack notification sent."
                } catch (Exception e) {
                    echo "⚠️ Aborted notification failed: ${e.getMessage()}"
                }
            }
        }
    }
}

def notifySlack(String buildStatus, String message) {
    // Color coding for different build statuses
    def color = 'good' // Default green for success
    def emoji = '✅'
    
    if (buildStatus == 'FAILURE') {
        color = 'danger'
        emoji = '❌'
    } else if (buildStatus == 'UNSTABLE') {
        color = 'warning' 
        emoji = '⚠️'
    } else if (buildStatus == 'ABORTED') {
        color = '#808080' // Gray
        emoji = '🛑'
    } else if (buildStatus == 'SUCCESS') {
        color = 'good'
        emoji = '✅'
    }
    
    // Create rich Slack message
    def slackMessage = [
        text: "${emoji} Jenkins Build ${buildStatus}",
        attachments: [[
            color: color,
            title: "${env.JOB_NAME} - Build #${env.BUILD_NUMBER}",
            title_link: "${env.BUILD_URL}",
            fields: [
                [title: "Project", value: env.JOB_NAME, short: true],
                [title: "Build", value: "#${env.BUILD_NUMBER}", short: true],
                [title: "Status", value: buildStatus, short: true],
                [title: "Branch", value: "${env.BRANCH_NAME ?: 'main'}", short: true],
                [title: "Duration", value: "${currentBuild.durationString}", short: true],
                [title: "Started By", value: "${currentBuild.getBuildCauses('hudson.model.Cause$UserIdCause')[0]?.userId ?: 'Timer/SCM'}", short: true]
            ],
            footer: "Jenkins CI/CD",
            footer_icon: "https://jenkins.io/images/logos/jenkins/jenkins.png",
            ts: System.currentTimeMillis() / 1000
        ]]
    ]
    
    // Add extra context for failures
    if (buildStatus == 'FAILURE') {
        slackMessage.attachments[0].fields << [
            title: "Build URL", 
            value: "<${env.BUILD_URL}console|View Console Output>", 
            short: false
        ]
    }
    
    // Add Docker Hub links for successful builds
    if (buildStatus == 'SUCCESS') {
        slackMessage.attachments[0].fields << [
            title: "Docker Images", 
            value: "<https://hub.docker.com/u/dockerhosam|View on Docker Hub>", 
            short: false
        ]
    }
    
    try {
        // Send to Slack using HTTP Request
        withCredentials([string(credentialsId: 'slack-webhook', variable: 'SLACK_WEBHOOK_URL')]) {
            def response = httpRequest(
                httpMode: 'POST',
                url: "${SLACK_WEBHOOK_URL}",
                contentType: 'APPLICATION_JSON',
                requestBody: groovy.json.JsonOutput.toJson(slackMessage),
                validResponseCodes: '200'
            )
            echo "✅ Slack notification sent successfully (${response.status})"
        }
    } catch (Exception e) {
        echo "⚠️ Failed to send Slack notification: ${e.getMessage()}"
        echo "💡 Make sure 'slack-webhook' credential is configured and HTTP Request plugin is installed"
        
        // Fallback: Create a simple notification file
        try {
            def simpleNotification = """
=== JENKINS BUILD NOTIFICATION ===
${emoji} Status: ${buildStatus}
📋 Project: ${env.JOB_NAME}
🔢 Build: #${env.BUILD_NUMBER}
🔗 URL: ${env.BUILD_URL}
🌿 Branch: ${env.BRANCH_NAME ?: 'main'}
⏱️ Duration: ${currentBuild.durationString}
📅 Time: ${new Date()}
================================
"""
            writeFile file: "slack-notification-${env.BUILD_NUMBER}.txt", text: simpleNotification
            archiveArtifacts artifacts: "slack-notification-${env.BUILD_NUMBER}.txt", allowEmptyArchive: true
            echo "📝 Notification saved to artifacts as fallback"
        } catch (Exception fe) {
            echo "⚠️ Fallback notification also failed: ${fe.getMessage()}"
        }
    }
} 