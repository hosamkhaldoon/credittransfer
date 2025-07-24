pipeline {
    agent any
    
    environment {
        // Project Configuration
        SOLUTION_FILE = 'Migrated/CreditTransfer.Modern.sln'
        DOCKER_REGISTRY = 'dockerhosam'
        PROJECT_NAME = 'credittransfer'
        SONAR_HOST_URL = 'http://localhost:9000'
        NEXUS_URL = 'http://localhost:8081'
        
        // Version Management
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        GIT_COMMIT_SHORT = sh(script: 'git rev-parse --short HEAD', returnStdout: true).trim()
        VERSION = "${env.BUILD_NUMBER}-${env.GIT_COMMIT_SHORT}"
        
        // Docker Images
        WCF_IMAGE = "${DOCKER_REGISTRY}/${PROJECT_NAME}-wcf:${VERSION}"
        API_IMAGE = "${DOCKER_REGISTRY}/${PROJECT_NAME}-api:${VERSION}"
        WORKER_IMAGE = "${DOCKER_REGISTRY}/${PROJECT_NAME}-worker:${VERSION}"
        
        // Test Configuration
        TEST_RESULTS_DIR = 'test-results'
        COVERAGE_DIR = 'coverage'
        SECURITY_SCAN_DIR = 'security-scans'
        
        // Security
        SONAR_TOKEN = credentials('sonartokenV2')
        DOCKER_HUB_CREDENTIALS = credentials('docker-hub-credentials')
        TRIVY_IGNORE_FILE = '.trivyignore'
        
        // Deployment
        DEPLOY_ENVIRONMENT = 'staging'
        KUBERNETES_NAMESPACE = 'credittransfer'
        
        // Notification
        SLACK_CHANNEL = '#ci-cd-notifications'
        EMAIL_RECIPIENTS = 'dev-team@company.com'
    }
    
    options {
        timeout(time: 3, unit: 'HOURS')
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '10'))
        timestamps()
        ansiColor('xterm')
        preserveStashes(buildCount: 5)
    }
    
    tools {
        dotnetsdk 'DotNet-8.0'
    }
    
    stages {
        stage('🚀 Pipeline Initialization') {
            steps {
                script {
                    echo "🚀 Starting CI/CD Pipeline for Credit Transfer Project"
                    echo "📋 Build Number: ${BUILD_NUMBER}"
                    echo "🔗 Git Commit: ${GIT_COMMIT_SHORT}"
                    echo "🏷️  Version: ${VERSION}"
                    echo "🖥️  Agent: ${env.NODE_NAME}"
                    echo "🌿 Branch: ${env.BRANCH_NAME}"
                    
                    // Set platform-specific commands
                    env.MKDIR_CMD = sh(script: 'if [ "$(uname)" = "Linux" ]; then echo "mkdir -p"; else echo "mkdir"; fi', returnStdout: true).trim()
                    env.CD_CMD = sh(script: 'if [ "$(uname)" = "Linux" ]; then echo "cd"; else echo "cd"; fi', returnStdout: true).trim()
                    env.PATH_SEPARATOR = sh(script: 'if [ "$(uname)" = "Linux" ]; then echo "/"; else echo "\\\\"; fi', returnStdout: true).trim()
                    
                    // Checkout code
                    checkout scm
                    
                    // Setup .NET tools
                    if (isUnix()) {
                        sh '''
                            dotnet --version
                            dotnet tool restore
                            dotnet tool list --global
                            docker --version
                            git --version
                        '''
                    } else {
                        bat '''
                            dotnet --version
                            dotnet tool restore
                            dotnet tool list --global
                            docker --version
                            git --version
                        '''
                    }
                }
            }
            post {
                failure {
                    script {
                        currentBuild.result = 'FAILED'
                        sendNotification('FAILED', 'Pipeline initialization failed')
                    }
                }
            }
        }
        
        stage('📦 Dependencies & Restore') {
            steps {
                script {
                    echo "📦 Restoring NuGet packages"
                    
                    if (isUnix()) {
                        sh '''
                            cd Migrated
                            dotnet clean $SOLUTION_FILE
                            dotnet restore $SOLUTION_FILE --verbosity normal
                        '''
                    } else {
                        bat '''
                            cd Migrated
                            dotnet clean %SOLUTION_FILE%
                            dotnet restore %SOLUTION_FILE% --verbosity normal
                        '''
                    }
                }
            }
            post {
                failure {
                    script {
                        sendNotification('FAILED', 'Dependencies restore failed')
                    }
                }
            }
        }
        
        stage('🔍 Code Quality & Security') {
            parallel {
                stage('SonarQube Analysis') {
                    when {
                        anyOf {
                            branch 'main'
                            branch 'develop'
                            branch 'migrated'
                            changeRequest()
                        }
                    }
                    steps {
                        script {
                            echo "🔍 Running SonarQube Analysis"
                            
                            withSonarQubeEnv('sonarserver') {
                                if (isUnix()) {
                                    sh '''
                                        cd Migrated
                                        dotnet sonarscanner begin \
                                            /k:"CreditTransfer-Modern" \
                                            /n:"Credit Transfer Modern" \
                                            /v:"$VERSION" \
                                            /d:sonar.host.url="$SONAR_HOST_URL" \
                                            /d:sonar.login="$SONAR_TOKEN" \
                                            /d:sonar.cs.opencover.reportsPaths="coverage/coverage.opencover.xml" \
                                            /d:sonar.coverage.exclusions="**/*Test.cs,**/*Tests.cs,**/Program.cs,**/Startup.cs" \
                                            /d:sonar.exclusions="**/bin/**,**/obj/**"
                                        
                                        dotnet build CreditTransfer.Modern.sln --configuration Release --no-restore
                                        
                                        dotnet test CreditTransfer.Modern.sln \
                                            --configuration Release \
                                            --no-build \
                                            --collect:"XPlat Code Coverage" \
                                            --results-directory $COVERAGE_DIR \
                                            --logger trx \
                                            --verbosity normal
                                        
                                        dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
                                    '''
                                } else {
                                    bat '''
                                        cd Migrated
                                        dotnet sonarscanner begin ^
                                            /k:"CreditTransfer-Modern" ^
                                            /n:"Credit Transfer Modern" ^
                                            /v:"%VERSION%" ^
                                            /d:sonar.host.url="%SONAR_HOST_URL%" ^
                                            /d:sonar.login="%SONAR_TOKEN%" ^
                                            /d:sonar.cs.opencover.reportsPaths="coverage\\coverage.opencover.xml" ^
                                            /d:sonar.coverage.exclusions="**/*Test.cs,**/*Tests.cs,**/Program.cs,**/Startup.cs" ^
                                            /d:sonar.exclusions="**/bin/**,**/obj/**"
                                        
                                        dotnet build CreditTransfer.Modern.sln --configuration Release --no-restore
                                        
                                        dotnet test CreditTransfer.Modern.sln ^
                                            --configuration Release ^
                                            --no-build ^
                                            --collect:"XPlat Code Coverage" ^
                                            --results-directory %COVERAGE_DIR% ^
                                            --logger trx ^
                                            --verbosity normal
                                        
                                        dotnet sonarscanner end /d:sonar.login="%SONAR_TOKEN%"
                                    '''
                                }
                            }
                        }
                    }
                    post {
                        always {
                            publishTestResults testResultsPattern: '**/Migrated/**/*.trx'
                            publishCoverage adapters: [opencoverAdapter('Migrated/coverage/coverage.opencover.xml')], 
                                           sourceFileResolver: sourceFiles('STORE_LAST_BUILD')
                        }
                        failure {
                            script {
                                sendNotification('FAILED', 'SonarQube analysis failed')
                            }
                        }
                    }
                }
                
                stage('SAST Security Scan') {
                    steps {
                        script {
                            echo "🔒 Running Static Application Security Testing"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    mkdir -p "$SECURITY_SCAN_DIR"
                                    
                                    dotnet tool install --global security-scan --version 5.6.7 || echo "Tool already installed"
                                    
                                    security-scan "CreditTransfer.Modern.sln" --export="$SECURITY_SCAN_DIR/security-report.json"
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    if not exist "%SECURITY_SCAN_DIR%" mkdir "%SECURITY_SCAN_DIR%"
                                    
                                    dotnet tool install --global security-scan --version 5.6.7 || echo "Tool already installed"
                                    
                                    security-scan "CreditTransfer.Modern.sln" --export="%SECURITY_SCAN_DIR%\\security-report.json"
                                '''
                            }
                        }
                    }
                    post {
                        always {
                            archiveArtifacts artifacts: 'Migrated/security-scans/**', allowEmptyArchive: true
                        }
                    }
                }
                
                stage('Dependency Check') {
                    steps {
                        script {
                            echo "📋 Checking for vulnerable dependencies"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    dotnet list CreditTransfer.Modern.sln package --vulnerable --include-transitive > "$SECURITY_SCAN_DIR/vulnerable-packages.txt" || echo "No vulnerabilities found"
                                    dotnet list CreditTransfer.Modern.sln package --outdated > "$SECURITY_SCAN_DIR/outdated-packages.txt" || echo "All packages up to date"
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    dotnet list CreditTransfer.Modern.sln package --vulnerable --include-transitive > "%SECURITY_SCAN_DIR%\\vulnerable-packages.txt" || echo "No vulnerabilities found"
                                    dotnet list CreditTransfer.Modern.sln package --outdated > "%SECURITY_SCAN_DIR%\\outdated-packages.txt" || echo "All packages up to date"
                                '''
                            }
                        }
                    }
                }
            }
        }
        
        stage('🏗️  Build & Test') {
            parallel {
                stage('Build Solution') {
                    steps {
                        script {
                            echo "🔨 Building .NET Solution"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    dotnet build CreditTransfer.Modern.sln \
                                        --configuration Release \
                                        --no-restore \
                                        --verbosity normal \
                                        /p:Version=$VERSION
                                    
                                    mkdir -p publish/wcf publish/api publish/worker
                                    
                                    dotnet publish src/Services/WebServices/CreditTransferService/CreditTransfer.Services.WcfService.csproj \
                                        --configuration Release \
                                        --output ./publish/wcf \
                                        --no-build \
                                        /p:Version=$VERSION
                                    
                                    dotnet publish src/Services/ApiServices/CreditTransferApi/CreditTransfer.Services.RestApi.csproj \
                                        --configuration Release \
                                        --output ./publish/api \
                                        --no-build \
                                        /p:Version=$VERSION
                                    
                                    dotnet publish src/Services/WorkerServices/CreditTransferWorker/CreditTransfer.Services.WorkerService.csproj \
                                        --configuration Release \
                                        --output ./publish/worker \
                                        --no-build \
                                        /p:Version=$VERSION
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    dotnet build CreditTransfer.Modern.sln ^
                                        --configuration Release ^
                                        --no-restore ^
                                        --verbosity normal ^
                                        /p:Version=%VERSION%
                                    
                                    if not exist "publish" mkdir "publish"
                                    if not exist "publish\\wcf" mkdir "publish\\wcf"
                                    if not exist "publish\\api" mkdir "publish\\api"
                                    if not exist "publish\\worker" mkdir "publish\\worker"
                                    
                                    dotnet publish src\\Services\\WebServices\\CreditTransferService\\CreditTransfer.Services.WcfService.csproj ^
                                        --configuration Release ^
                                        --output .\\publish\\wcf ^
                                        --no-build ^
                                        /p:Version=%VERSION%
                                    
                                    dotnet publish src\\Services\\ApiServices\\CreditTransferApi\\CreditTransfer.Services.RestApi.csproj ^
                                        --configuration Release ^
                                        --output .\\publish\\api ^
                                        --no-build ^
                                        /p:Version=%VERSION%
                                    
                                    dotnet publish src\\Services\\WorkerServices\\CreditTransferWorker\\CreditTransfer.Services.WorkerService.csproj ^
                                        --configuration Release ^
                                        --output .\\publish\\worker ^
                                        --no-build ^
                                        /p:Version=%VERSION%
                                '''
                            }
                        }
                    }
                    post {
                        success {
                            archiveArtifacts artifacts: 'Migrated/publish/**/*', fingerprint: true
                        }
                        failure {
                            script {
                                sendNotification('FAILED', 'Build failed')
                            }
                        }
                    }
                }
                
                stage('Unit Tests') {
                    steps {
                        script {
                            echo "🧪 Running Unit Tests"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    mkdir -p "$TEST_RESULTS_DIR/unit"
                                    
                                    dotnet test CreditTransfer.Modern.sln \
                                        --configuration Release \
                                        --no-build \
                                        --filter "Category=Unit" \
                                        --results-directory $TEST_RESULTS_DIR/unit \
                                        --logger trx \
                                        --verbosity normal \
                                        --collect:"XPlat Code Coverage"
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    if not exist "%TEST_RESULTS_DIR%\\unit" mkdir "%TEST_RESULTS_DIR%\\unit"
                                    
                                    dotnet test CreditTransfer.Modern.sln ^
                                        --configuration Release ^
                                        --no-build ^
                                        --filter "Category=Unit" ^
                                        --results-directory %TEST_RESULTS_DIR%\\unit ^
                                        --logger trx ^
                                        --verbosity normal ^
                                        --collect:"XPlat Code Coverage"
                                '''
                            }
                        }
                    }
                    post {
                        always {
                            publishTestResults testResultsPattern: '**/Migrated/**/unit/*.trx'
                        }
                    }
                }
                
                stage('Integration Tests') {
                    steps {
                        script {
                            echo "🔗 Running Integration Tests"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    mkdir -p "$TEST_RESULTS_DIR/integration"
                                    
                                    dotnet test tests/Integration/CreditTransfer.Integration.Tests/CreditTransfer.Integration.Tests.csproj \
                                        --configuration Release \
                                        --no-build \
                                        --filter "Category=Integration" \
                                        --results-directory $TEST_RESULTS_DIR/integration \
                                        --logger trx \
                                        --verbosity normal
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    if not exist "%TEST_RESULTS_DIR%\\integration" mkdir "%TEST_RESULTS_DIR%\\integration"
                                    
                                    dotnet test tests\\Integration\\CreditTransfer.Integration.Tests\\CreditTransfer.Integration.Tests.csproj ^
                                        --configuration Release ^
                                        --no-build ^
                                        --filter "Category=Integration" ^
                                        --results-directory %TEST_RESULTS_DIR%\\integration ^
                                        --logger trx ^
                                        --verbosity normal
                                '''
                            }
                        }
                    }
                    post {
                        always {
                            publishTestResults testResultsPattern: '**/Migrated/**/integration/*.trx'
                        }
                    }
                }
            }
        }
        
        stage('🐳 Docker Operations') {
            parallel {
                stage('Build Docker Images') {
                    steps {
                        script {
                            echo "🐳 Building Docker Images"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    docker build \
                                        --file src/Services/WebServices/CreditTransferService/Dockerfile \
                                        --tag $WCF_IMAGE \
                                        --tag $DOCKER_REGISTRY/$PROJECT_NAME-wcf:latest \
                                        --build-arg BUILD_CONFIGURATION=Release \
                                        --build-arg VERSION=$VERSION \
                                        .
                                    
                                    docker build \
                                        --file src/Services/ApiServices/CreditTransferApi/Dockerfile \
                                        --tag $API_IMAGE \
                                        --tag $DOCKER_REGISTRY/$PROJECT_NAME-api:latest \
                                        --build-arg BUILD_CONFIGURATION=Release \
                                        --build-arg VERSION=$VERSION \
                                        .
                                    
                                    docker build \
                                        --file src/Services/WorkerServices/CreditTransferWorker/Dockerfile \
                                        --tag $WORKER_IMAGE \
                                        --tag $DOCKER_REGISTRY/$PROJECT_NAME-worker:latest \
                                        --build-arg BUILD_CONFIGURATION=Release \
                                        --build-arg VERSION=$VERSION \
                                        .
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    docker build ^
                                        --file src\\Services\\WebServices\\CreditTransferService\\Dockerfile ^
                                        --tag %WCF_IMAGE% ^
                                        --tag %DOCKER_REGISTRY%/%PROJECT_NAME%-wcf:latest ^
                                        --build-arg BUILD_CONFIGURATION=Release ^
                                        --build-arg VERSION=%VERSION% ^
                                        .
                                    
                                    docker build ^
                                        --file src\\Services\\ApiServices\\CreditTransferApi\\Dockerfile ^
                                        --tag %API_IMAGE% ^
                                        --tag %DOCKER_REGISTRY%/%PROJECT_NAME%-api:latest ^
                                        --build-arg BUILD_CONFIGURATION=Release ^
                                        --build-arg VERSION=%VERSION% ^
                                        .
                                    
                                    docker build ^
                                        --file src\\Services\\WorkerServices\\CreditTransferWorker\\Dockerfile ^
                                        --tag %WORKER_IMAGE% ^
                                        --tag %DOCKER_REGISTRY%/%PROJECT_NAME%-worker:latest ^
                                        --build-arg BUILD_CONFIGURATION=Release ^
                                        --build-arg VERSION=%VERSION% ^
                                        .
                                '''
                            }
                        }
                    }
                    post {
                        failure {
                            script {
                                sendNotification('FAILED', 'Docker image build failed')
                            }
                        }
                    }
                }
                
                stage('Security Scan Images') {
                    steps {
                        script {
                            echo "🔒 Scanning Docker images for vulnerabilities"
                            
                            if (isUnix()) {
                                sh '''
                                    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
                                        -v $PWD:/root/.cache/ aquasec/trivy:latest image \
                                        --exit-code 0 --severity HIGH,CRITICAL \
                                        --format json --output trivy-wcf-report.json $WCF_IMAGE
                                    
                                    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
                                        -v $PWD:/root/.cache/ aquasec/trivy:latest image \
                                        --exit-code 0 --severity HIGH,CRITICAL \
                                        --format json --output trivy-api-report.json $API_IMAGE
                                    
                                    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
                                        -v $PWD:/root/.cache/ aquasec/trivy:latest image \
                                        --exit-code 0 --severity HIGH,CRITICAL \
                                        --format json --output trivy-worker-report.json $WORKER_IMAGE
                                '''
                            } else {
                                bat '''
                                    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock ^
                                        -v %cd%:/root/.cache/ aquasec/trivy:latest image ^
                                        --exit-code 0 --severity HIGH,CRITICAL ^
                                        --format json --output trivy-wcf-report.json %WCF_IMAGE%
                                    
                                    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock ^
                                        -v %cd%:/root/.cache/ aquasec/trivy:latest image ^
                                        --exit-code 0 --severity HIGH,CRITICAL ^
                                        --format json --output trivy-api-report.json %API_IMAGE%
                                    
                                    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock ^
                                        -v %cd%:/root/.cache/ aquasec/trivy:latest image ^
                                        --exit-code 0 --severity HIGH,CRITICAL ^
                                        --format json --output trivy-worker-report.json %WORKER_IMAGE%
                                '''
                            }
                        }
                    }
                    post {
                        always {
                            archiveArtifacts artifacts: 'trivy-*-report.json', allowEmptyArchive: true
                        }
                    }
                }
            }
        }
        
        stage('🔍 Quality Gate') {
            steps {
                script {
                    echo "🔍 Waiting for SonarQube Quality Gate"
                    
                    timeout(time: 10, unit: 'MINUTES') {
                        waitForQualityGate abortPipeline: true
                    }
                }
            }
            post {
                failure {
                    script {
                        sendNotification('FAILED', 'Quality gate failed')
                    }
                }
            }
        }
        
        stage('📤 Publish Artifacts') {
            when {
                anyOf {
                    branch 'main'
                    branch 'develop'
                    branch 'migrated'
                }
            }
            parallel {
                stage('Push Docker Images') {
                    steps {
                        script {
                            echo "📤 Pushing Images to Registry"
                            
                            withCredentials([usernamePassword(credentialsId: 'docker-hub-credentials', usernameVariable: 'DOCKER_USERNAME', passwordVariable: 'DOCKER_PASSWORD')]) {
                                if (isUnix()) {
                                    sh '''
                                        echo $DOCKER_PASSWORD | docker login -u $DOCKER_USERNAME --password-stdin
                                        
                                        docker push $WCF_IMAGE
                                        docker push $API_IMAGE
                                        docker push $WORKER_IMAGE
                                        
                                        docker push $DOCKER_REGISTRY/$PROJECT_NAME-wcf:latest
                                        docker push $DOCKER_REGISTRY/$PROJECT_NAME-api:latest
                                        docker push $DOCKER_REGISTRY/$PROJECT_NAME-worker:latest
                                    '''
                                } else {
                                    bat '''
                                        echo %DOCKER_PASSWORD% | docker login -u %DOCKER_USERNAME% --password-stdin
                                        
                                        docker push %WCF_IMAGE%
                                        docker push %API_IMAGE%
                                        docker push %WORKER_IMAGE%
                                        
                                        docker push %DOCKER_REGISTRY%/%PROJECT_NAME%-wcf:latest
                                        docker push %DOCKER_REGISTRY%/%PROJECT_NAME%-api:latest
                                        docker push %DOCKER_REGISTRY%/%PROJECT_NAME%-worker:latest
                                    '''
                                }
                            }
                        }
                    }
                }
                
                stage('Upload to Nexus') {
                    steps {
                        script {
                            echo "📦 Uploading artifacts to Nexus"
                            
                            if (isUnix()) {
                                sh '''
                                    cd Migrated
                                    zip -r CreditTransfer-$VERSION.zip publish/*
                                '''
                            } else {
                                bat '''
                                    cd Migrated
                                    powershell -Command "Compress-Archive -Path 'publish\\*' -DestinationPath 'CreditTransfer-%VERSION%.zip'"
                                '''
                            }
                            
                            nexusArtifactUploader(
                                nexusVersion: 'nexus3',
                                protocol: 'http',
                                nexusUrl: env.NEXUS_URL,
                                groupId: 'com.company.credittransfer',
                                version: env.VERSION,
                                repository: 'maven-releases',
                                credentialsId: 'nexus-credentials',
                                artifacts: [
                                    [artifactId: 'credittransfer-app',
                                     classifier: '',
                                     file: "Migrated/CreditTransfer-${env.VERSION}.zip",
                                     type: 'zip']
                                ]
                            )
                        }
                    }
                }
            }
            post {
                failure {
                    script {
                        sendNotification('FAILED', 'Artifact publishing failed')
                    }
                }
            }
        }
        
        stage('🚀 Deploy to Staging') {
            when {
                anyOf {
                    branch 'develop'
                    branch 'migrated'
                }
            }
            steps {
                script {
                    echo "🚀 Deploying to Staging Environment"
                    
                    if (isUnix()) {
                        sh '''
                            cd Migrated/deployment
                            docker-compose -f docker-compose.yml down || echo "No existing deployment"
                            docker-compose -f docker-compose.yml up -d
                            
                            sleep 30
                            
                            docker-compose -f docker-compose.yml ps
                        '''
                    } else {
                        bat '''
                            cd Migrated\\deployment
                            docker-compose -f docker-compose.yml down || echo "No existing deployment"
                            docker-compose -f docker-compose.yml up -d
                            
                            timeout /t 30
                            
                            docker-compose -f docker-compose.yml ps
                        '''
                    }
                }
            }
            post {
                success {
                    script {
                        sendNotification('SUCCESS', 'Successfully deployed to staging')
                    }
                }
                failure {
                    script {
                        sendNotification('FAILED', 'Staging deployment failed')
                    }
                }
            }
        }
        
        stage('🎯 Deploy to Production') {
            when {
                branch 'main'
            }
            steps {
                script {
                    def deployApproval = input(
                        id: 'deploy-to-prod',
                        message: 'Deploy to Production?',
                        parameters: [
                            choice(choices: ['Deploy', 'Abort'], description: 'Deploy to Production?', name: 'DEPLOY_CHOICE')
                        ]
                    )
                    
                    if (deployApproval == 'Deploy') {
                        echo "🎯 Deploying to Production Environment"
                        
                        if (isUnix()) {
                            sh '''
                                cd Migrated/deployment
                                docker-compose -f docker-compose.yml down || echo "No existing deployment"
                                
                                cp env.prod .env
                                docker-compose -f docker-compose.yml up -d
                                
                                sleep 30
                                
                                docker-compose -f docker-compose.yml ps
                            '''
                        } else {
                            bat '''
                                cd Migrated\\deployment
                                docker-compose -f docker-compose.yml down || echo "No existing deployment"
                                
                                copy env.prod .env
                                docker-compose -f docker-compose.yml up -d
                                
                                timeout /t 30
                                
                                docker-compose -f docker-compose.yml ps
                            '''
                        }
                    } else {
                        echo "❌ Production deployment aborted by user"
                        currentBuild.result = 'ABORTED'
                    }
                }
            }
            post {
                success {
                    script {
                        sendNotification('SUCCESS', 'Successfully deployed to production')
                    }
                }
                failure {
                    script {
                        sendNotification('FAILED', 'Production deployment failed')
                    }
                }
            }
        }
    }
    
    post {
        always {
            script {
                echo "🧹 Performing cleanup tasks"
                
                // Clean up Docker images
                if (isUnix()) {
                    sh '''
                        docker image prune -f || echo "No images to prune"
                        docker system df
                    '''
                } else {
                    bat '''
                        docker image prune -f || echo "No images to prune"
                        docker system df
                    '''
                }
                
                // Archive important logs
                archiveArtifacts artifacts: '**/*.log', allowEmptyArchive: true
            }
        }
        
        success {
            script {
                sendNotification('SUCCESS', 'Pipeline completed successfully')
                
                // Update deployment status
                if (isUnix()) {
                    sh '''
                        echo "Pipeline completed successfully at $(date)" > deployment-status.txt
                    '''
                } else {
                    bat '''
                        echo Pipeline completed successfully at %date% %time% > deployment-status.txt
                    '''
                }
                archiveArtifacts artifacts: 'deployment-status.txt'
            }
        }
        
        failure {
            script {
                sendNotification('FAILED', 'Pipeline failed')
                
                // Collect failure information
                if (isUnix()) {
                    sh '''
                        echo "Pipeline failed at $(date)" > failure-status.txt
                        docker ps -a >> failure-status.txt
                        docker images >> failure-status.txt
                    '''
                } else {
                    bat '''
                        echo Pipeline failed at %date% %time% > failure-status.txt
                        docker ps -a >> failure-status.txt
                        docker images >> failure-status.txt
                    '''
                }
                archiveArtifacts artifacts: 'failure-status.txt'
            }
        }
        
        unstable {
            script {
                sendNotification('UNSTABLE', 'Pipeline completed with issues')
            }
        }
        
        aborted {
            script {
                sendNotification('ABORTED', 'Pipeline was aborted')
            }
        }
    }
}

def sendNotification(String buildStatus, String message) {
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
    
    // Slack notification (only if plugin is available)
    try {
        if (env.SLACK_CHANNEL) {
            def slackResponse = slackSend(
                channel: env.SLACK_CHANNEL,
                color: colorCode,
                message: summary,
                teamDomain: 'your-team',
                token: 'slack-token',
                failOnError: false
            )
            if (!slackResponse) {
                echo "Slack notification skipped - plugin not available"
            }
        }
    } catch (Exception e) {
        echo "Slack notification failed: ${e.getMessage()}"
    }
    
    // Email notification
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