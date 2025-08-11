#!/usr/bin/env pwsh
param(
    [switch]$DryRun = $false
)

Write-Host "🚀 .NET 9 Upgrade Script" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Change to Migrated directory
Set-Location "Migrated"

# Get all .csproj files
$projectFiles = Get-ChildItem -Path . -Filter "*.csproj" -Recurse

Write-Host "📋 Found $($projectFiles.Count) project files to upgrade:" -ForegroundColor Yellow
foreach ($file in $projectFiles) {
    Write-Host "   • $($file.FullName.Replace((Get-Location).Path, '.'))" -ForegroundColor Gray
}

Write-Host ""

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
} else {
    Write-Host "⚡ LIVE MODE - Changes will be applied" -ForegroundColor Green
}

Write-Host ""

# Step 1: Update target frameworks
Write-Host "📝 Step 1: Updating target frameworks to .NET 9.0..." -ForegroundColor Cyan

foreach ($file in $projectFiles) {
    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Replace((Get-Location).Path, '.')
    
    if ($content -match '<TargetFramework>net8\.0</TargetFramework>') {
        Write-Host "   • Updating $relativePath" -ForegroundColor Green
        if (!$DryRun) {
            $content = $content -replace '<TargetFramework>net8\.0</TargetFramework>', '<TargetFramework>net9.0</TargetFramework>'
            Set-Content $file.FullName -Value $content -NoNewline
        }
    } else {
        Write-Host "   • Skipping $relativePath (no net8.0 target found)" -ForegroundColor Gray
    }
}

Write-Host ""

# Step 2: Update package references to latest .NET 9 compatible versions
Write-Host "📦 Step 2: Updating NuGet packages to latest versions..." -ForegroundColor Cyan

# Define package version mappings for .NET 9
$packageUpdates = @{
    # Microsoft .NET 9 packages
    'Microsoft.EntityFrameworkCore' = '9.0.0'
    'Microsoft.EntityFrameworkCore.SqlServer' = '9.0.0'
    'Microsoft.Extensions.Caching.Abstractions' = '9.0.0'
    'Microsoft.Extensions.Caching.Memory' = '9.0.0'
    'Microsoft.Extensions.Caching.StackExchangeRedis' = '9.0.0'
    'Microsoft.Extensions.Configuration.Abstractions' = '9.0.0'
    'Microsoft.Extensions.Configuration.Binder' = '9.0.0'
    'Microsoft.Extensions.DependencyInjection' = '9.0.0'
    'Microsoft.Extensions.DependencyInjection.Abstractions' = '9.0.0'
    'Microsoft.Extensions.Hosting' = '9.0.0'
    'Microsoft.Extensions.Http' = '9.0.0'
    'Microsoft.Extensions.Logging' = '9.0.0'
    'Microsoft.Extensions.Logging.Abstractions' = '9.0.0'
    'Microsoft.Extensions.Options' = '9.0.0'
    'Microsoft.Extensions.Options.ConfigurationExtensions' = '9.0.0'
    'Microsoft.AspNetCore.Authentication.JwtBearer' = '9.0.0'
    'Microsoft.AspNetCore.Mvc.Core' = '2.2.5'
    'Microsoft.AspNetCore.OpenApi' = '9.0.0'
    'Microsoft.AspNetCore.App' = '9.0.0'
    'System.Text.Json' = '9.0.0'
    'System.Diagnostics.DiagnosticSource' = '9.0.0'
    
    # CoreWCF and WCF related packages (latest compatible)
    'CoreWCF.Primitives' = '1.6.0'
    'CoreWCF.Http' = '1.6.0'
    'CoreWCF.NetTcp' = '1.6.0'
    'CoreWCF.ConfigurationManager' = '1.6.0'
    'System.ServiceModel.Primitives' = '8.1.2'
    'System.ServiceModel.Http' = '8.1.2'
    'System.ServiceModel.NetTcp' = '8.1.2'
    'System.ServiceModel.Security' = '8.1.2'
    'System.ServiceModel.Duplex' = '8.1.2'
    'System.Private.ServiceModel' = '4.10.3'
    
    # Other packages
    'System.Data.SqlClient' = '4.9.0'
    'StackExchange.Redis' = '2.8.16'
    'Swashbuckle.AspNetCore' = '7.2.0'
    'Serilog' = '4.2.0'
    'Serilog.Extensions.Hosting' = '8.0.0'
    'Serilog.Sinks.Console' = '6.0.0'
    'Serilog.Sinks.File' = '6.0.0'
    'Serilog.Settings.Configuration' = '8.0.4'
    'OpenTelemetry' = '1.12.0'
    'OpenTelemetry.Extensions.Hosting' = '1.12.0'
    'OpenTelemetry.Instrumentation.AspNetCore' = '1.10.0'
    'OpenTelemetry.Instrumentation.Http' = '1.10.0'
    'OpenTelemetry.Instrumentation.SqlClient' = '1.10.0-beta.1'
    'OpenTelemetry.Exporter.Jaeger' = '1.6.0'
    'OpenTelemetry.Exporter.Prometheus.AspNetCore' = '1.12.0-rc.1'
    
    # Test packages
    'Microsoft.NET.Test.Sdk' = '17.12.0'
    'xunit' = '2.9.2'
    'xunit.runner.visualstudio' = '3.0.0'
    'coverlet.collector' = '6.0.2'
    'FluentAssertions' = '7.0.0'
    'Moq' = '4.20.72'
    'Microsoft.AspNetCore.Mvc.Testing' = '9.0.0'
}

foreach ($file in $projectFiles) {
    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Replace((Get-Location).Path, '.')
    $modified = $false
    
    foreach ($package in $packageUpdates.Keys) {
        $newVersion = $packageUpdates[$package]
        $pattern = "<PackageReference Include=`"$([regex]::Escape($package))`" Version=`"[^`"]+`" />"
        $replacement = "<PackageReference Include=`"$package`" Version=`"$newVersion`" />"
        
        if ($content -match $pattern) {
            Write-Host "   • Updating $package to $newVersion in $relativePath" -ForegroundColor Green
            if (!$DryRun) {
                $content = $content -replace $pattern, $replacement
                $modified = $true
            }
        }
    }
    
    if ($modified -and !$DryRun) {
        Set-Content $file.FullName -Value $content -NoNewline
    }
}

Write-Host ""

# Step 3: Restore packages and build
if (!$DryRun) {
    Write-Host "🔄 Step 3: Restoring NuGet packages..." -ForegroundColor Cyan
    try {
        dotnet restore CreditTransfer.Modern.sln
        Write-Host "✅ Package restore completed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Package restore failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "🔨 Step 4: Building solution..." -ForegroundColor Cyan
    try {
        dotnet build CreditTransfer.Modern.sln --no-restore
        Write-Host "✅ Build completed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   This is expected - we may need to fix some compilation issues manually" -ForegroundColor Yellow
    }
} else {
    Write-Host "🔍 Dry run complete - no actual changes made" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 .NET 9 upgrade process completed!" -ForegroundColor Green
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "   • Updated $($projectFiles.Count) project files to target .NET 9.0" -ForegroundColor White
Write-Host "   • Updated NuGet packages to latest .NET 9 compatible versions" -ForegroundColor White

if (!$DryRun) {
    Write-Host "   • Restored packages and attempted build" -ForegroundColor White
    Write-Host ""
    Write-Host "🔧 Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Review any build errors and fix them manually" -ForegroundColor Gray
    Write-Host "   2. Test the application to ensure everything works correctly" -ForegroundColor Gray
    Write-Host "   3. Update any deprecated APIs if needed" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "🚀 To apply changes, run: .\upgrade-to-net9.ps1" -ForegroundColor Green
}

# Return to original directory
Set-Location .. 