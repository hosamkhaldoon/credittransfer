#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Credit Transfer System Health Check Script
.DESCRIPTION
    Performs comprehensive health checks including:
    - Keycloak authentication and JWT token retrieval
    - API validation endpoint testing  
    - Log analysis for SQL Server connection issues
.EXAMPLE
    .\health-check.ps1
    .\health-check.ps1 -Verbose
#>

param(
    [switch]$Verbose
)

# Configuration
$KeycloakUrl = "http://localhost:6002/realms/credittransfer/protocol/openid-connect/token"
$ApiUrl = "http://localhost:6000/api/credittransfer/validate"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "=" * 60 -Color "Magenta"
    Write-ColorOutput "  $Title" -Color "Magenta"
    Write-ColorOutput "=" * 60 -Color "Magenta"
    Write-Host ""
}

function Test-KeycloakAuth {
    Write-ColorOutput "🔐 Testing Keycloak Authentication..." -Color "Cyan"
    
    try {
        $body = @{
            grant_type = "password"
            client_id = "credittransfer-api"
            username = "admin"
            password = "admin123"
            scope = "openid profile email"
        }
        
        $response = Invoke-RestMethod -Uri $KeycloakUrl -Method POST -ContentType "application/x-www-form-urlencoded" -Body $body -TimeoutSec 30
        
        if ($response.access_token) {
            Write-ColorOutput "✅ Keycloak Authentication: SUCCESS" -Color "Green"
            Write-ColorOutput "   Token expires in: $($response.expires_in) seconds" -Color "Gray"
            return $response.access_token
        } else {
            Write-ColorOutput "❌ Keycloak Authentication: FAILED - No token received" -Color "Red"
            return $null
        }
    }
    catch {
        Write-ColorOutput "❌ Keycloak Authentication: FAILED - $($_.Exception.Message)" -Color "Red"
        return $null
    }
}

function Test-ApiValidation {
    param([string]$Token)
    
    Write-ColorOutput "🌐 Testing API Validation..." -Color "Cyan"
    
    if (-not $Token) {
        Write-ColorOutput "❌ API Test: SKIPPED - No authentication token" -Color "Red"
        return $false
    }
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
            "Content-Type" = "application/json"
        }
        
        $testData = @{
            sourceMsisdn = "96898455550"
            destinationMsisdn = "96878323523"
            amount = 1.0
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri $ApiUrl -Method POST -Headers $headers -Body $testData -TimeoutSec 30
        
        Write-ColorOutput "✅ API Validation: SUCCESS" -Color "Green"
        Write-ColorOutput "   Status Code: $($response.statusCode)" -Color "Gray"
        Write-ColorOutput "   Status Message: $($response.statusMessage)" -Color "Gray"
        Write-ColorOutput "   Timestamp: $($response.timestamp)" -Color "Gray"
        
        return $true
    }
    catch {
        Write-ColorOutput "❌ API Validation: FAILED - $($_.Exception.Message)" -Color "Red"
        return $false
    }
}

function Analyze-Logs {
    Write-ColorOutput "📋 Analyzing System Logs..." -Color "Cyan"
    
    try {
        $logs = kubectl logs -n credittransfer deployment/credittransfer-api --tail=200 2>$null
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "❌ Log Retrieval: FAILED - Cannot access logs" -Color "Red"
            return $false
        }
        
        Write-ColorOutput "✅ Log Retrieval: SUCCESS - Retrieved 200 recent entries" -Color "Green"
        
        # Check for SQL Server connection errors
        $sqlErrors = $logs | Where-Object { $_ -match "A network-related or instance-specific error occurred while establishing a connection to SQL Server" }
        
        if ($sqlErrors.Count -gt 0) {
            Write-ColorOutput "⚠️  SQL Server Connectivity: ISSUES FOUND" -Color "Yellow"
            Write-ColorOutput "   Found $($sqlErrors.Count) SQL connection errors" -Color "Gray"
            Write-ColorOutput "   Server: 10.1.133.31 (External SQL Server)" -Color "Gray"
            
            if ($Verbose) {
                Write-ColorOutput "   Recent SQL errors:" -Color "Gray"
                $sqlErrors | Select-Object -First 3 | ForEach-Object {
                    Write-ColorOutput "   $_" -Color "DarkGray"
                }
            }
        } else {
            Write-ColorOutput "✅ SQL Server Connectivity: NO ERRORS in recent logs" -Color "Green"
        }
        
        # Check for authentication errors
        $authErrors = $logs | Where-Object { $_ -match "authentication|unauthorized" -and $_ -match "error|fail" }
        
        if ($authErrors.Count -gt 0) {
            Write-ColorOutput "⚠️  Authentication Issues: $($authErrors.Count) found" -Color "Yellow"
        } else {
            Write-ColorOutput "✅ Authentication: NO ERRORS in recent logs" -Color "Green"
        }
        
        # Check for critical errors
        $criticalErrors = $logs | Where-Object { $_ -match "\[Error\]|\[Fatal\]|Exception:" }
        
        if ($criticalErrors.Count -gt 0) {
            Write-ColorOutput "⚠️  Critical Errors: $($criticalErrors.Count) found" -Color "Yellow"
            if ($Verbose) {
                Write-ColorOutput "   Recent critical errors:" -Color "Gray"
                $criticalErrors | Select-Object -First 5 | ForEach-Object {
                    Write-ColorOutput "   $_" -Color "DarkGray"
                }
            }
        } else {
            Write-ColorOutput "✅ Critical Errors: NONE found" -Color "Green"
        }
        
        return $true
    }
    catch {
        Write-ColorOutput "❌ Log Analysis: FAILED - $($_.Exception.Message)" -Color "Red"
        return $false
    }
}

function Show-SystemStatus {
    Write-ColorOutput "🔍 Checking System Status..." -Color "Cyan"
    
    try {
        # Check pod status
        $pods = kubectl get pods -n credittransfer --no-headers 2>$null
        if ($LASTEXITCODE -eq 0) {
            $podLines = $pods -split "`n" | Where-Object { $_ -ne "" }
            $runningPods = $podLines | Where-Object { $_ -match "Running" }
            Write-ColorOutput "✅ Kubernetes Pods: $($runningPods.Count)/$($podLines.Count) running" -Color "Green"
            
            if ($Verbose) {
                $podLines | ForEach-Object {
                    Write-ColorOutput "   $_" -Color "Gray"
                }
            }
        }
        
        # Check services
        $services = kubectl get services -n credittransfer --no-headers 2>$null
        if ($LASTEXITCODE -eq 0) {
            $serviceLines = $services -split "`n" | Where-Object { $_ -ne "" }
            Write-ColorOutput "✅ Kubernetes Services: $($serviceLines.Count) configured" -Color "Green"
        }
        
    }
    catch {
        Write-ColorOutput "⚠️  System Status: Partial information available" -Color "Yellow"
    }
}

# Main execution
try {
    Write-Header "CREDIT TRANSFER SYSTEM HEALTH CHECK"
    
    # Show system status
    Show-SystemStatus
    
    # Test Keycloak authentication
    $token = Test-KeycloakAuth
    
    # Test API validation
    $apiSuccess = Test-ApiValidation -Token $token
    
    # Analyze logs
    $logSuccess = Analyze-Logs
    
    # Summary
    Write-Header "HEALTH CHECK SUMMARY"
    
    $keycloakStatus = if ($token) { "✅ PASS" } else { "❌ FAIL" }
    $apiStatus = if ($apiSuccess) { "✅ PASS" } else { "❌ FAIL" }
    $logStatus = if ($logSuccess) { "✅ PASS" } else { "❌ FAIL" }
    
    Write-ColorOutput "Keycloak Authentication: $keycloakStatus" -Color $(if ($token) { "Green" } else { "Red" })
    Write-ColorOutput "API Validation: $apiStatus" -Color $(if ($apiSuccess) { "Green" } else { "Red" })
    Write-ColorOutput "Log Analysis: $logStatus" -Color $(if ($logSuccess) { "Green" } else { "Red" })
    
    Write-Host ""
    Write-ColorOutput "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Color "Gray"
    
    # Recommendations
    if (-not $token) {
        Write-ColorOutput "💡 Check Keycloak service and port forwarding (port 6002)" -Color "Yellow"
    }
    if (-not $apiSuccess) {
        Write-ColorOutput "💡 Check API service and port forwarding (port 6000)" -Color "Yellow"
    }
    
    Write-Host ""
    
    # Exit with appropriate code
    $totalTests = 3
    $passedTests = @($token, $apiSuccess, $logSuccess) | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count
    
    if ($passedTests -eq $totalTests) {
        Write-ColorOutput "🎉 Overall Status: ALL TESTS PASSED ($passedTests/$totalTests)" -Color "Green"
        exit 0
    } elseif ($passedTests -ge 2) {
        Write-ColorOutput "⚠️  Overall Status: MOSTLY HEALTHY ($passedTests/$totalTests)" -Color "Yellow"
        exit 1
    } else {
        Write-ColorOutput "🚨 Overall Status: NEEDS ATTENTION ($passedTests/$totalTests)" -Color "Red"
        exit 2
    }
}
catch {
    Write-ColorOutput "❌ CRITICAL ERROR: $($_.Exception.Message)" -Color "Red"
    exit 3
}
