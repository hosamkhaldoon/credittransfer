#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("setup", "test", "status", "help")]
    [string]$Action,
    
    [string]$DockerUsername = "",
    [string]$DockerPassword = "",
    [string]$SonarToken = "",
    [string]$SlackWebhook = ""
)

function Show-Help {
    Write-Host "===============================================================================" -ForegroundColor Cyan
    Write-Host "GitHub Actions CI/CD Setup Help" -ForegroundColor Cyan  
    Write-Host "===============================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage Examples:" -ForegroundColor Yellow
    Write-Host "1. Setup with Docker Hub credentials:" -ForegroundColor Green
    Write-Host "   ./setup-github-actions.ps1 -Action setup -DockerUsername 'dockerhosam' -DockerPassword 'your-token'"
    Write-Host ""
    Write-Host "2. Test workflow:" -ForegroundColor Green
    Write-Host "   ./setup-github-actions.ps1 -Action test"
    Write-Host ""
    Write-Host "3. Check status:" -ForegroundColor Green
    Write-Host "   ./setup-github-actions.ps1 -Action status"
    Write-Host ""
    Write-Host "Prerequisites:" -ForegroundColor Yellow
    Write-Host "- GitHub CLI installed (gh)"
    Write-Host "- Git repository with GitHub remote"
    Write-Host "- GitHub repository access"
    Write-Host "- Docker Hub account (for deployment)"
    Write-Host ""
    Write-Host "Setup GitHub Secrets:" -ForegroundColor Yellow
    Write-Host "Go to GitHub repo -> Settings -> Secrets and variables -> Actions"
    Write-Host "Add these secrets:"
    Write-Host "- DOCKER_USERNAME: your Docker Hub username"
    Write-Host "- DOCKER_PASSWORD: your Docker Hub password/token"
    Write-Host "- SONAR_TOKEN (optional): SonarQube token"
    Write-Host "- SLACK_WEBHOOK_URL (optional): Slack webhook for notifications"
}

function Test-Prerequisites {
    $allGood = $true
    
    # Test GitHub CLI
    try {
        $null = gh --version
        Write-Host "✅ GitHub CLI found" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ GitHub CLI not found. Install from: https://cli.github.com/" -ForegroundColor Red
        $allGood = $false
    }
    
    # Test Git repository
    try {
        $gitRemote = git remote get-url origin 2>$null
        if ($gitRemote) {
            Write-Host "✅ Git repository found" -ForegroundColor Green
        }
        else {
            Write-Host "❌ No Git remote found" -ForegroundColor Red
            $allGood = $false
        }
    }
    catch {
        Write-Host "❌ Not in a Git repository" -ForegroundColor Red
        $allGood = $false
    }
    
    return $allGood
}

function Set-Secrets {
    Write-Host "Setting up GitHub repository secrets..." -ForegroundColor Yellow
    
    if ($DockerUsername -and $DockerPassword) {
        try {
            gh secret set DOCKER_USERNAME --body $DockerUsername
            gh secret set DOCKER_PASSWORD --body $DockerPassword
            Write-Host "✅ Docker Hub secrets configured" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Failed to set Docker secrets" -ForegroundColor Red
            return $false
        }
    }
    else {
        Write-Host "⚠️ Docker credentials not provided" -ForegroundColor Yellow
    }
    
    if ($SonarToken) {
        try {
            gh secret set SONAR_TOKEN --body $SonarToken
            Write-Host "✅ SonarQube token configured" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Failed to set SonarQube token" -ForegroundColor Red
        }
    }
    
    if ($SlackWebhook) {
        try {
            gh secret set SLACK_WEBHOOK_URL --body $SlackWebhook
            Write-Host "✅ Slack webhook configured" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Failed to set Slack webhook" -ForegroundColor Red
        }
    }
    
    return $true
}

function Test-WorkflowTrigger {
    Write-Host "Testing GitHub Actions workflow..." -ForegroundColor Yellow
    
    # Check if workflow file exists
    if (!(Test-Path ".github/workflows/ci-build-test.yml")) {
        Write-Host "❌ Workflow file not found: .github/workflows/ci-build-test.yml" -ForegroundColor Red
        return $false
    }
    
    Write-Host "✅ Workflow file found" -ForegroundColor Green
    
    # Trigger workflow
    try {
        Write-Host "Triggering workflow manually..." -ForegroundColor Blue
        gh workflow run "ci-build-test.yml" --field skip_sonarqube=true
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Workflow triggered successfully!" -ForegroundColor Green
            Write-Host "Check progress in GitHub Actions tab" -ForegroundColor Blue
            return $true
        }
        else {
            Write-Host "❌ Failed to trigger workflow" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ Error triggering workflow" -ForegroundColor Red
        return $false
    }
}

function Get-Status {
    Write-Host "Checking workflow status..." -ForegroundColor Yellow
    
    try {
        Write-Host "Recent workflow runs:" -ForegroundColor Cyan
        gh run list --limit=5
        
        Write-Host "`nConfigured secrets:" -ForegroundColor Cyan
        gh secret list
    }
    catch {
        Write-Host "❌ Error getting status" -ForegroundColor Red
    }
}

# Main execution
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "GitHub Actions CI/CD Workflow Setup" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Cyan

switch ($Action) {
    "setup" {
        if (!(Test-Prerequisites)) {
            Write-Host "❌ Prerequisites not met" -ForegroundColor Red
            exit 1
        }
        
        if (Set-Secrets) {
            Write-Host "`n🎉 Setup completed!" -ForegroundColor Green
            Write-Host "Next steps:" -ForegroundColor Yellow
            Write-Host "1. Run: ./setup-github-actions.ps1 -Action test"
            Write-Host "2. Push to main branch to trigger automatic workflow"
        }
        else {
            Write-Host "❌ Setup failed" -ForegroundColor Red
            exit 1
        }
    }
    
    "test" {
        if (!(Test-Prerequisites)) {
            Write-Host "❌ Prerequisites not met" -ForegroundColor Red
            exit 1
        }
        
        if (Test-WorkflowTrigger) {
            Write-Host "`n🎉 Workflow test completed!" -ForegroundColor Green
            Write-Host "Monitor progress with: ./setup-github-actions.ps1 -Action status" -ForegroundColor Blue
        }
        else {
            Write-Host "❌ Workflow test failed" -ForegroundColor Red
            exit 1
        }
    }
    
    "status" {
        Get-Status
    }
    
    "help" {
        Show-Help
    }
}

Write-Host "`nDone!" -ForegroundColor Green 