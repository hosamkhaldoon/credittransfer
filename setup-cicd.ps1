# Credit Transfer CI/CD Infrastructure Setup Script
# This script sets up Jenkins, SonarQube, and Nexus for the Credit Transfer project

param(
    [ValidateSet("install", "start", "stop", "restart", "status", "logs", "clean")]
    [string]$Action = "install",
    
    [switch]$SkipWait,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Color functions for better output
function Write-Header($Message) {
    Write-Host "`n$('=' * 80)" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor White
    Write-Host "$('=' * 80)" -ForegroundColor Cyan
}

function Write-Status($Message) {
    Write-Host "🔄 $Message" -ForegroundColor Yellow
}

function Write-Success($Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error($Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info($Message) {
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Test-DockerRunning {
    try {
        docker version > $null 2>&1
        return $true
    }
    catch {
        return $false
    }
}

function Wait-ForService {
    param(
        [string]$ServiceName,
        [string]$Url,
        [int]$TimeoutSeconds = 300,
        [int]$RetryInterval = 10
    )
    
    if ($SkipWait) {
        Write-Info "Skipping wait for $ServiceName due to -SkipWait flag"
        return
    }
    
    Write-Status "Waiting for $ServiceName to be ready at $Url..."
    $elapsed = 0
    
    while ($elapsed -lt $TimeoutSeconds) {
        try {
            $response = Invoke-WebRequest -Uri $Url -TimeoutSec 10 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Success "$ServiceName is ready!"
                return
            }
        }
        catch {
            # Service not ready yet
        }
        
        Start-Sleep $RetryInterval
        $elapsed += $RetryInterval
        Write-Host "." -NoNewline
    }
    
    Write-Error "$ServiceName failed to start within $TimeoutSeconds seconds"
    throw "Service startup timeout: $ServiceName"
}

function Install-CICDInfrastructure {
    Write-Header "Setting up Credit Transfer CI/CD Infrastructure"
    
    # Check prerequisites
    Write-Status "Checking prerequisites..."
    
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop first."
        exit 1
    }
    
    if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
        Write-Error "docker-compose is not installed or not in PATH"
        exit 1
    }
    
    Write-Success "Prerequisites check passed"
    
    # Create required directories
    Write-Status "Creating required directories..."
    
    $directories = @(
        "jenkins/logs",
        "jenkins/workspace", 
        "monitoring/grafana/dashboards",
        "monitoring/grafana/datasources",
        "sonarqube/data",
        "sonarqube/logs",
        "nexus/data"
    )
    
    foreach ($dir in $directories) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "  Created: $dir" -ForegroundColor Gray
        }
    }
    
    # Create Grafana datasource configuration
    Write-Status "Creating Grafana configuration..."
    
    $prometheusDataSource = @"
apiVersion: 1

datasources:
  - name: Prometheus-Jenkins
    type: prometheus
    access: proxy
    url: http://prometheus-jenkins:9090
    isDefault: true
    editable: true
"@
    
    $prometheusDataSource | Out-File -FilePath "monitoring/grafana/datasources/prometheus.yml" -Encoding UTF8
    
    # Start services
    Write-Status "Starting CI/CD services with Docker Compose..."
    
    docker-compose -f ci-cd-compose.yml up -d
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to start services"
        exit 1
    }
    
    Write-Success "All services started successfully"
    
    # Wait for services to be ready
    Write-Status "Waiting for services to initialize..."
    
    try {
        Wait-ForService -ServiceName "Jenkins" -Url "http://localhost:8080/login"
        Wait-ForService -ServiceName "SonarQube" -Url "http://localhost:9000"
        Wait-ForService -ServiceName "Nexus" -Url "http://localhost:8081"
    }
    catch {
        Write-Error "One or more services failed to start properly"
        Write-Info "You can check the logs with: .\setup-cicd.ps1 -Action logs"
        exit 1
    }
    
    # Configure SonarQube
    Write-Status "Configuring SonarQube..."
    Configure-SonarQube
    
    # Display access information
    Show-AccessInformation
}

function Configure-SonarQube {
    Write-Status "Setting up SonarQube project and quality gate..."
    
    # Wait a bit more for SonarQube to be fully ready
    Start-Sleep 30
    
    # Default credentials: admin/admin
    $sonarUrl = "http://localhost:9000"
    $credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{
        "Authorization" = "Basic $credentials"
        "Content-Type" = "application/x-www-form-urlencoded"
    }
    
    try {
        # Create project
        $projectKey = "CreditTransfer-Modern"
        $projectName = "Credit Transfer Modern"
        
        $createProjectParams = @{
            Uri = "$sonarUrl/api/projects/create"
            Method = "POST"
            Headers = $headers
            Body = "project=$projectKey&name=$([uri]::EscapeDataString($projectName))"
            TimeoutSec = 30
        }
        
        try {
            Invoke-RestMethod @createProjectParams
            Write-Success "SonarQube project created: $projectName"
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 400) {
                Write-Info "SonarQube project already exists"
            }
            else {
                throw
            }
        }
        
        # Generate token for Jenkins
        $tokenName = "jenkins-token-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        $tokenParams = @{
            Uri = "$sonarUrl/api/user_tokens/generate"
            Method = "POST"
            Headers = $headers
            Body = "name=$tokenName"
            TimeoutSec = 30
        }
        
        $tokenResponse = Invoke-RestMethod @tokenParams
        $sonarToken = $tokenResponse.token
        
        Write-Success "SonarQube token generated: $tokenName"
        Write-Info "Token: $sonarToken"
        Write-Info "Please update the Jenkins credential 'sonartokenV2' with this token"
        
    }
    catch {
        Write-Error "Failed to configure SonarQube: $($_.Exception.Message)"
        Write-Info "You can configure SonarQube manually at http://localhost:9000"
        Write-Info "Default credentials: admin/admin"
    }
}

function Show-AccessInformation {
    Write-Header "CI/CD Infrastructure Access Information"
    
    Write-Host "🚀 Jenkins CI/CD Server" -ForegroundColor Green
    Write-Host "   URL: http://localhost:8080" -ForegroundColor White
    Write-Host "   Username: admin" -ForegroundColor Gray
    Write-Host "   Password: admin123" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "🔍 SonarQube Code Quality" -ForegroundColor Green
    Write-Host "   URL: http://localhost:9000" -ForegroundColor White
    Write-Host "   Username: admin" -ForegroundColor Gray
    Write-Host "   Password: admin" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "📦 Nexus Artifact Repository" -ForegroundColor Green
    Write-Host "   URL: http://localhost:8081" -ForegroundColor White
    Write-Host "   Username: admin" -ForegroundColor Gray
    Write-Host "   Password: Check container logs for initial password" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "📊 Prometheus Metrics (Jenkins)" -ForegroundColor Green
    Write-Host "   URL: http://localhost:9091" -ForegroundColor White
    Write-Host ""
    
    Write-Host "📈 Grafana Dashboards (Jenkins)" -ForegroundColor Green
    Write-Host "   URL: http://localhost:3001" -ForegroundColor White
    Write-Host "   Username: admin" -ForegroundColor Gray
    Write-Host "   Password: jenkins_admin_2024" -ForegroundColor Gray
    Write-Host ""
    
    Write-Header "Next Steps"
    Write-Host "1. 🔧 Access Jenkins and create a new pipeline job" -ForegroundColor Yellow
    Write-Host "2. 🔗 Point the pipeline to your Credit Transfer repository" -ForegroundColor Yellow
    Write-Host "3. 📋 Use the Jenkinsfile in the repository root" -ForegroundColor Yellow
    Write-Host "4. 🚀 Run your first build!" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "📚 Management Commands:" -ForegroundColor Cyan
    Write-Host "   Start services:    .\setup-cicd.ps1 -Action start" -ForegroundColor Gray
    Write-Host "   Stop services:     .\setup-cicd.ps1 -Action stop" -ForegroundColor Gray
    Write-Host "   View logs:         .\setup-cicd.ps1 -Action logs" -ForegroundColor Gray
    Write-Host "   Check status:      .\setup-cicd.ps1 -Action status" -ForegroundColor Gray
    Write-Host "   Clean everything:  .\setup-cicd.ps1 -Action clean" -ForegroundColor Gray
}

function Start-Services {
    Write-Header "Starting CI/CD Services"
    docker-compose -f ci-cd-compose.yml up -d
    Write-Success "Services started"
    Show-ServiceStatus
}

function Stop-Services {
    Write-Header "Stopping CI/CD Services"
    docker-compose -f ci-cd-compose.yml down
    Write-Success "Services stopped"
}

function Restart-Services {
    Write-Header "Restarting CI/CD Services"
    docker-compose -f ci-cd-compose.yml restart
    Write-Success "Services restarted"
    Show-ServiceStatus
}

function Show-ServiceStatus {
    Write-Header "CI/CD Service Status"
    docker-compose -f ci-cd-compose.yml ps
}

function Show-ServiceLogs {
    Write-Header "CI/CD Service Logs"
    Write-Info "Showing logs for all services (Ctrl+C to exit)"
    docker-compose -f ci-cd-compose.yml logs -f
}

function Clean-Everything {
    Write-Header "Cleaning CI/CD Infrastructure"
    Write-Host "⚠️  This will remove all containers, volumes, and data!" -ForegroundColor Red
    
    $confirmation = Read-Host "Are you sure? Type 'yes' to continue"
    if ($confirmation -eq "yes") {
        docker-compose -f ci-cd-compose.yml down --volumes --remove-orphans
        docker system prune -f
        Write-Success "Cleanup completed"
    }
    else {
        Write-Info "Cleanup cancelled"
    }
}

# Main execution
switch ($Action.ToLower()) {
    "install" { Install-CICDInfrastructure }
    "start" { Start-Services }
    "stop" { Stop-Services }
    "restart" { Restart-Services }
    "status" { Show-ServiceStatus }
    "logs" { Show-ServiceLogs }
    "clean" { Clean-Everything }
    default { 
        Write-Error "Invalid action: $Action"
        Write-Info "Valid actions: install, start, stop, restart, status, logs, clean"
        exit 1
    }
} 