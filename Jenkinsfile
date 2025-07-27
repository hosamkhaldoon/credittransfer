pipeline {
    agent any
    
    environment {
        // Project Configuration
        SOLUTION_FILE = 'Migrated/CreditTransfer.Modern.sln'  // Fixed path
        PROJECT_NAME = 'credittransfer'
        SONAR_HOST_URL = 'http://localhost:9000'
        NEXUS_URL = 'http://localhost:8081'
        
        // Version Management
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        GIT_COMMIT_SHORT = sh(script: 'git rev-parse --short HEAD', returnStdout: true).trim()
        VERSION = "${env.BUILD_NUMBER}-${env.GIT_COMMIT_SHORT}"
        
        // Test Configuration
        TEST_RESULTS_DIR = 'test-results'
        COVERAGE_DIR = 'coverage'
        SECURITY_SCAN_DIR = 'security-scans'
        
        // Security
        SONAR_TOKEN = credentials('sonartokenV2')
        
        // Deployment
        DEPLOY_ENVIRONMENT = 'staging'
        KUBERNETES_NAMESPACE = 'credittransfer'
        
        // Notification
        EMAIL_RECIPIENTS = 'hosam93644@gmail.com'
        
        // .NET SDK version and configuration
        DOTNET_VERSION = '8.0.100'
        DOTNET_ROOT = "${WORKSPACE}/.dotnet"
        PATH = "${DOTNET_ROOT}:${PATH}"
        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '1'  // Allow running without ICU
    }
    
    options {
        timeout(time: 3, unit: 'HOURS')
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '10'))
        timestamps()
        ansiColor('xterm')
        preserveStashes(buildCount: 5)
    }
    
    stages {
        stage('🛠️ Setup Environment') {
            steps {
                script {
                    echo "🛠️ Setting up build environment"
                    
                    // Create directories
                    sh '''#!/bin/bash
                        mkdir -p ${DOTNET_ROOT}
                        mkdir -p ${TEST_RESULTS_DIR}
                        mkdir -p ${COVERAGE_DIR}
                        mkdir -p ${SECURITY_SCAN_DIR}
                    '''
                    
                    // Install .NET SDK
                    sh '''#!/bin/bash
                        # Download and install .NET SDK
                        if [ ! -f ${DOTNET_ROOT}/dotnet ]; then
                            echo "Installing .NET SDK ${DOTNET_VERSION}..."
                            
                            # Download the script directly from Microsoft
                            curl -SL --retry 3 --retry-delay 5 https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o dotnet-install.sh
                            
                            # Verify the download
                            if [ ! -f dotnet-install.sh ]; then
                                echo "Failed to download dotnet-install.sh"
                                exit 1
                            fi
                            
                            # Make it executable and run
                            chmod +x ./dotnet-install.sh
                            ./dotnet-install.sh --version ${DOTNET_VERSION} --install-dir ${DOTNET_ROOT} --verbose
                            rm dotnet-install.sh
                            
                            # Add to PATH
                            export PATH="${DOTNET_ROOT}:${PATH}"
                            export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        fi
                        
                        # Verify .NET installation
                        if [ -f ${DOTNET_ROOT}/dotnet ]; then
                            echo ".NET SDK installed successfully"
                            DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 ${DOTNET_ROOT}/dotnet --info || {
                                echo "Failed to run dotnet --info. Error code: $?"
                                exit 1
                            }
                        else
                            echo ".NET SDK installation failed"
                            exit 1
                        fi
                    '''
                }
            }
        }
        
        stage('🚀 Pipeline Initialization') {
            steps {
                script {
                    echo "🚀 Starting CI/CD Pipeline for Credit Transfer Project"
                    echo "📋 Build Number: ${BUILD_NUMBER}"
                    echo "🔗 Git Commit: ${GIT_COMMIT_SHORT}"
                    echo "🏷️  Version: ${VERSION}"
                    echo "🖥️  Agent: ${env.NODE_NAME}"
                    echo "🌿 Branch: ${env.BRANCH_NAME}"
                    
                    // Checkout code
                    checkout scm
                    
                    // List workspace contents
                    sh '''#!/bin/bash
                        echo "Workspace contents:"
                        ls -la
                        
                        echo "Migrated directory contents:"
                        ls -la Migrated/
                        
                        # Find all .sln files
                        echo "Finding solution files:"
                        find . -name "*.sln" -type f
                    '''
                    
                    // Verify environment
                    sh '''#!/bin/bash
                        # Add to PATH and set globalization invariant
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        echo "Checking .NET SDK version:"
                        ${DOTNET_ROOT}/dotnet --info
                        
                        echo "Restoring .NET tools:"
                        ${DOTNET_ROOT}/dotnet tool restore || echo "Tool restore failed"
                        
                        echo "Listing global tools:"
                        ${DOTNET_ROOT}/dotnet tool list --global || echo "Tool list failed"
                        
                        echo "Checking Git version:"
                        git --version || echo "Git not available"
                    '''
                }
            }
            post {
                failure {
                    script {
                        currentBuild.result = 'FAILED'
                        notifyBuild('FAILED', 'Pipeline initialization failed')
                    }
                }
            }
        }
        
        stage('📦 Dependencies & Restore') {
            steps {
                script {
                    echo "📦 Restoring NuGet packages"
                    sh '''#!/bin/bash
                        # Verify solution file exists
                        if [ ! -f "${SOLUTION_FILE}" ]; then
                            echo "Error: Solution file not found at ${SOLUTION_FILE}"
                            echo "Current directory contents:"
                            ls -la
                            echo "Migrated directory contents:"
                            ls -la Migrated/
                            exit 1
                        fi
                        
                        # Export environment variables
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
                        
                        # Restore packages
                        echo "Cleaning solution..."
                        ${DOTNET_ROOT}/dotnet clean "${SOLUTION_FILE}" --verbosity normal
                        
                        echo "Restoring packages..."
                        ${DOTNET_ROOT}/dotnet restore "${SOLUTION_FILE}" --verbosity normal
                    '''
                }
            }
            post {
                failure {
                    script {
                        notifyBuild('FAILED', 'Dependencies restore failed')
                    }
                }
            }
        }
        
        stage('🏗️ Build & Test') {
            steps {
                script {
                    echo "🔨 Building .NET Solution"
                    sh '''#!/bin/bash
                        # Export environment variables
                        export PATH="${DOTNET_ROOT}:${PATH}"
                        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

                        cd Migrated

                        # First build the entire solution
                        echo "Building solution..."
                        ${DOTNET_ROOT}/dotnet build ${SOLUTION_FILE} \
                            --configuration Release \
                            --no-restore \
                            --verbosity normal \
                            /p:Version=${VERSION}

                        # Create publish directories
                        mkdir -p publish/wcf publish/api publish/worker

                        # Publish WCF Service
                        echo "Publishing WCF Service..."
                        ${DOTNET_ROOT}/dotnet publish src/Services/WebServices/CreditTransferService/CreditTransfer.Services.WcfService.csproj \
                            --configuration Release \
                            --output ./publish/wcf \
                            /p:Version=${VERSION}

                        # Publish REST API
                        echo "Publishing REST API..."
                        ${DOTNET_ROOT}/dotnet publish src/Services/ApiServices/CreditTransferApi/CreditTransfer.Services.RestApi.csproj \
                            --configuration Release \
                            --output ./publish/api \
                            /p:Version=${VERSION}

                        # Publish Worker Service
                        echo "Publishing Worker Service..."
                        ${DOTNET_ROOT}/dotnet publish src/Services/WorkerServices/CreditTransferWorker/CreditTransfer.Services.WorkerService.csproj \
                            --configuration Release \
                            --output ./publish/worker \
                            /p:Version=${VERSION}

                        # Verify publish output
                        echo "Verifying published output..."
                        ls -la publish/wcf
                        ls -la publish/api
                        ls -la publish/worker
                    '''
                }
            }
            post {
                success {
                    archiveArtifacts artifacts: 'Migrated/publish/**/*', fingerprint: true
                }
                failure {
                    script {
                        notifyBuild('FAILED', 'Build failed')
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
                        
                        # Verify workspace and solution
                        echo "Workspace contents:"
                        ls -la
                        
                        echo "Migrated directory contents:"
                        ls -la Migrated/
                        
                        echo "Finding solution files:"
                        find . -name "*.sln" -type f
                        
                        # Use absolute paths
                        WORKSPACE_PATH="$(pwd)"
                        SOLUTION_PATH="${WORKSPACE_PATH}/Migrated/CreditTransfer.Modern.sln"
                        
                        if [ ! -f "${SOLUTION_PATH}" ]; then
                            echo "Error: Solution file not found at ${SOLUTION_PATH}"
                            exit 1
                        fi
                        
                        cd Migrated
                        
                        # Build tests first
                        echo "Building tests..."
                        ${DOTNET_ROOT}/dotnet build "${SOLUTION_PATH}" \
                            --configuration Release \
                            --no-restore \
                            --verbosity normal \
                            /p:Version=${VERSION}
                        
                        # Run unit tests
                        echo "Running unit tests..."
                        mkdir -p "${WORKSPACE_PATH}/${TEST_RESULTS_DIR}/unit"
                        ${DOTNET_ROOT}/dotnet test "${SOLUTION_PATH}" \
                            --configuration Release \
                            --no-build \
                            --filter "Category=Unit" \
                            --logger "trx;LogFileName=unit_tests.trx" \
                            --results-directory "${WORKSPACE_PATH}/${TEST_RESULTS_DIR}/unit" \
                            --verbosity normal \
                            --collect:"XPlat Code Coverage"
                        
                        # Run integration tests if they exist
                        INTEGRATION_TEST_PROJECT="tests/Integration/CreditTransfer.Integration.Tests/CreditTransfer.Integration.Tests.csproj"
                        if [ -f "${INTEGRATION_TEST_PROJECT}" ]; then
                            echo "Running integration tests..."
                            mkdir -p "${WORKSPACE_PATH}/${TEST_RESULTS_DIR}/integration"
                            ${DOTNET_ROOT}/dotnet test "${INTEGRATION_TEST_PROJECT}" \
                                --configuration Release \
                                --no-build \
                                --filter "Category=Integration" \
                                --logger "trx;LogFileName=integration_tests.trx" \
                                --results-directory "${WORKSPACE_PATH}/${TEST_RESULTS_DIR}/integration" \
                                --verbosity normal
                        else
                            echo "Integration test project not found at: ${INTEGRATION_TEST_PROJECT}"
                        fi
                        
                        # List test results
                        echo "Test results directory contents:"
                        ls -la "${WORKSPACE_PATH}/${TEST_RESULTS_DIR}/unit" || true
                        ls -la "${WORKSPACE_PATH}/${TEST_RESULTS_DIR}/integration" || true
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
    }
    
    post {
        always {
            script {
                echo "🧹 Performing cleanup tasks"
                archiveArtifacts artifacts: '**/*.log', allowEmptyArchive: true
            }
        }
        
        success {
            script {
                notifyBuild('SUCCESS', 'Pipeline completed successfully')
                sh '''#!/bin/bash
                    echo "Pipeline completed successfully at $(date)" > deployment-status.txt
                '''
                archiveArtifacts artifacts: 'deployment-status.txt'
            }
        }
        
        failure {
            script {
                notifyBuild('FAILED', 'Pipeline failed')
                sh '''#!/bin/bash
                    echo "Pipeline failed at $(date)" > failure-status.txt
                '''
                archiveArtifacts artifacts: 'failure-status.txt'
            }
        }
        
        unstable {
            script {
                notifyBuild('UNSTABLE', 'Pipeline completed with issues')
            }
        }
        
        aborted {
            script {
                notifyBuild('ABORTED', 'Pipeline was aborted')
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
        emailext(
            subject: subject,
            body: details,
            to: env.EMAIL_RECIPIENTS,
            mimeType: 'text/plain'
        )
    } catch (Exception e) {
        echo "Email notification failed: ${e.getMessage()}"
    }
} 