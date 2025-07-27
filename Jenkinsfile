pipeline {
    agent {
        docker {
            image 'jenkins/agent:latest-jdk11'
            args '''
                -v /var/run/docker.sock:/var/run/docker.sock
                -v ${WORKSPACE}:${WORKSPACE}
                --group-add $(getent group docker | cut -d: -f3)
            '''
        }
    }
    
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

        // Docker Configuration
        DOCKER_REGISTRY = 'docker.io'
        DOCKER_NAMESPACE = 'hosamkhaldoon'
        DOCKER_TAG = "${env.BUILD_NUMBER}-${env.GIT_COMMIT_SHORT}"
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
                    sh '''#!/bin/bash
                        # Create directories with proper permissions
                        mkdir -p ${DOTNET_ROOT} ${TEST_RESULTS_DIR} ${COVERAGE_DIR} ${SECURITY_SCAN_DIR}
                        chmod -R 777 ${TEST_RESULTS_DIR} ${COVERAGE_DIR} ${SECURITY_SCAN_DIR}
                        
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

                        # Verify Docker access
                        echo "Verifying Docker access..."
                        docker version || {
                            echo "Failed to access Docker. Error code: $?"
                            exit 1
                        }

                        # Test Docker functionality
                        echo "Testing Docker functionality..."
                        docker run --rm hello-world || {
                            echo "Failed to run Docker test container. Error code: $?"
                            exit 1
                        }

                        # Verify installations
                        echo ".NET SDK version:"
                        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 ${DOTNET_ROOT}/dotnet --info || {
                            echo "Failed to run dotnet --info. Error code: $?"
                            exit 1
                        }
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
                        
                        cd Migrated
                        
                        # Ensure test results directory exists and has correct permissions
                        TEST_RESULTS_PATH="${WORKSPACE}/${TEST_RESULTS_DIR}/tests"
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
                            --collect:"XPlat Code Coverage"
                        
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

        stage('🐳 Build Docker Images') {
            steps {
                script {
                    echo "🐳 Building Docker images"
                    sh '''#!/bin/bash
                        cd Migrated
                        
                        # Build WCF Service image
                        echo "Building WCF Service image..."
                        docker build \
                            -f src/Services/WebServices/CreditTransferService/Dockerfile \
                            -t ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-wcf:${DOCKER_TAG} \
                            -t ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-wcf:latest \
                            .
                        
                        # Build REST API image
                        echo "Building REST API image..."
                        docker build \
                            -f src/Services/ApiServices/CreditTransferApi/Dockerfile \
                            -t ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-api:${DOCKER_TAG} \
                            -t ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-api:latest \
                            .
                        
                        # Build Worker Service image
                        echo "Building Worker Service image..."
                        docker build \
                            -f src/Services/WorkerServices/CreditTransferWorker/Dockerfile \
                            -t ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-worker:${DOCKER_TAG} \
                            -t ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-worker:latest \
                            .
                    '''
                }
            }
        }

        stage('📦 Push Docker Images') {
            environment {
                DOCKER_CREDENTIALS = credentials('docker-hub-credentials')
            }
            steps {
                script {
                    echo "📦 Pushing Docker images to registry"
                    sh '''#!/bin/bash
                        # Login to Docker registry
                        echo "${DOCKER_CREDENTIALS_PSW}" | docker login ${DOCKER_REGISTRY} -u "${DOCKER_CREDENTIALS_USR}" --password-stdin
                        
                        # Push WCF Service images
                        docker push ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-wcf:${DOCKER_TAG}
                        docker push ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-wcf:latest
                        
                        # Push REST API images
                        docker push ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-api:${DOCKER_TAG}
                        docker push ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-api:latest
                        
                        # Push Worker Service images
                        docker push ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-worker:${DOCKER_TAG}
                        docker push ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-worker:latest
                        
                        # Logout from Docker registry
                        docker logout ${DOCKER_REGISTRY}
                    '''
                }
            }
            post {
                always {
                    sh '''#!/bin/bash
                        # Clean up local images to save space
                        docker rmi ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-wcf:${DOCKER_TAG} || true
                        docker rmi ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-wcf:latest || true
                        docker rmi ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-api:${DOCKER_TAG} || true
                        docker rmi ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-api:latest || true
                        docker rmi ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-worker:${DOCKER_TAG} || true
                        docker rmi ${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/credit-transfer-worker:latest || true
                    '''
                }
            }
        }
    }
    
    post {
        always {
            node('built-in') {
                script {
                    echo "🧹 Performing cleanup tasks"
                    archiveArtifacts artifacts: '**/*.log', allowEmptyArchive: true
                }
            }
        }
        
        success {
            node('built-in') {
                script {
                    notifyBuild('SUCCESS', 'Pipeline completed successfully')
                    sh '''#!/bin/bash
                        echo "Pipeline completed successfully at $(date)" > deployment-status.txt
                    '''
                    archiveArtifacts artifacts: 'deployment-status.txt'
                }
            }
        }
        
        failure {
            node('built-in') {
                script {
                    notifyBuild('FAILED', 'Pipeline failed')
                    sh '''#!/bin/bash
                        echo "Pipeline failed at $(date)" > failure-status.txt
                    '''
                    archiveArtifacts artifacts: 'failure-status.txt'
                }
            }
        }
        
        unstable {
            node('built-in') {
                script {
                    notifyBuild('UNSTABLE', 'Pipeline completed with issues')
                }
            }
        }
        
        aborted {
            node('built-in') {
                script {
                    notifyBuild('ABORTED', 'Pipeline was aborted')
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