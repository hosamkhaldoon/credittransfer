pipeline {
    agent any
    
    environment {
        // Project Configuration
        SOLUTION_FILE = 'Migrated/CreditTransfer.Modern.sln'
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
                        cd Migrated
                        ${DOTNET_ROOT}/dotnet clean ${SOLUTION_FILE}
                        ${DOTNET_ROOT}/dotnet restore ${SOLUTION_FILE} --verbosity normal
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
                        cd Migrated
                        ${DOTNET_ROOT}/dotnet build ${SOLUTION_FILE} \
                            --configuration Release \
                            --no-restore \
                            --verbosity normal \
                            /p:Version=${VERSION}
                        
                        mkdir -p publish/wcf publish/api publish/worker
                        
                        ${DOTNET_ROOT}/dotnet publish src/Services/WebServices/CreditTransferService/CreditTransfer.Services.WcfService.csproj \
                            --configuration Release \
                            --output ./publish/wcf \
                            --no-build \
                            /p:Version=${VERSION}
                        
                        ${DOTNET_ROOT}/dotnet publish src/Services/ApiServices/CreditTransferApi/CreditTransfer.Services.RestApi.csproj \
                            --configuration Release \
                            --output ./publish/api \
                            --no-build \
                            /p:Version=${VERSION}
                        
                        ${DOTNET_ROOT}/dotnet publish src/Services/WorkerServices/CreditTransferWorker/CreditTransfer.Services.WorkerService.csproj \
                            --configuration Release \
                            --output ./publish/worker \
                            --no-build \
                            /p:Version=${VERSION}
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
                        cd Migrated
                        
                        # Run unit tests
                        mkdir -p ${TEST_RESULTS_DIR}/unit
                        ${DOTNET_ROOT}/dotnet test ${SOLUTION_FILE} \
                            --configuration Release \
                            --no-build \
                            --filter "Category=Unit" \
                            --results-directory ${TEST_RESULTS_DIR}/unit \
                            --logger trx \
                            --verbosity normal \
                            --collect:"XPlat Code Coverage"
                        
                        # Run integration tests
                        mkdir -p ${TEST_RESULTS_DIR}/integration
                        ${DOTNET_ROOT}/dotnet test tests/Integration/CreditTransfer.Integration.Tests/CreditTransfer.Integration.Tests.csproj \
                            --configuration Release \
                            --no-build \
                            --filter "Category=Integration" \
                            --results-directory ${TEST_RESULTS_DIR}/integration \
                            --logger trx \
                            --verbosity normal
                    '''
                }
            }
            post {
                always {
                    publishTestResults testResultsPattern: '**/Migrated/**/unit/*.trx'
                    publishTestResults testResultsPattern: '**/Migrated/**/integration/*.trx'
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