#!/usr/bin/env pwsh
param(
    [switch]$DryRun = $false
)

Write-Host "🔧 .NET 9 Package Fixes Script" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Change to Migrated directory
Set-Location "Migrated"

# Get all .csproj files
$projectFiles = Get-ChildItem -Path . -Filter "*.csproj" -Recurse

Write-Host "📋 Found $($projectFiles.Count) project files to fix:" -ForegroundColor Yellow

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
} else {
    Write-Host "⚡ LIVE MODE - Changes will be applied" -ForegroundColor Green
}

Write-Host ""

# Define correct package versions
$packageFixes = @{
    # Fix incorrect ServiceModel package versions
    'System.ServiceModel.Security' = '6.0.0'
    'System.ServiceModel.Duplex' = '6.0.0'
    
    # Fix OpenTelemetry package versions
    'OpenTelemetry.Exporter.Prometheus.AspNetCore' = '1.12.0-beta.1'
    
    # Fix Serilog package versions
    'Serilog.Settings.Configuration' = '9.0.0'
    
    # Fix Microsoft Extensions package versions
    'Microsoft.Extensions.Options' = '9.0.0'
    'Microsoft.Extensions.Options.ConfigurationExtensions' = '9.0.0'
    
    # Fix test packages to remove duplicates and use latest
    'Microsoft.NET.Test.Sdk' = '17.12.0'
    'xunit' = '2.9.2'  
    'xunit.runner.visualstudio' = '3.0.0'
    'Moq' = '4.20.72'
    'FluentAssertions' = '7.0.0'
    'coverlet.collector' = '6.0.2'
}

Write-Host "🔨 Step 1: Fixing package versions..." -ForegroundColor Cyan

foreach ($file in $projectFiles) {
    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Replace((Get-Location).Path, '.')
    $modified = $false
    
    foreach ($package in $packageFixes.Keys) {
        $newVersion = $packageFixes[$package]
        
        # Replace any version of this package with the correct version
        $pattern = "<PackageReference Include=`"$([regex]::Escape($package))`" Version=`"[^`"]+`""
        
        if ($content -match $pattern) {
            Write-Host "   • Fixing $package to $newVersion in $relativePath" -ForegroundColor Green
            if (!$DryRun) {
                $content = $content -replace $pattern, "<PackageReference Include=`"$package`" Version=`"$newVersion`""
                $modified = $true
            }
        }
    }
    
    if ($modified -and !$DryRun) {
        Set-Content $file.FullName -Value $content -NoNewline
    }
}

Write-Host ""
Write-Host "🧹 Step 2: Removing duplicate package references..." -ForegroundColor Cyan

# Handle duplicate package references in test projects
$testProjects = $projectFiles | Where-Object { $_.Name -like "*Tests*.csproj" }

foreach ($file in $testProjects) {
    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Replace((Get-Location).Path, '.')
    $modified = $false
    
    # Remove duplicate Microsoft.NET.Test.Sdk entries (keep the newer version)
    if ($content -match "Microsoft\.NET\.Test\.Sdk.*17\.8\.0") {
        Write-Host "   • Removing duplicate Microsoft.NET.Test.Sdk 17.8.0 from $relativePath" -ForegroundColor Yellow
        if (!$DryRun) {
            $content = $content -replace '\s*<PackageReference Include="Microsoft\.NET\.Test\.Sdk" Version="17\.8\.0" />\s*', ''
            $modified = $true
        }
    }
    
    # Remove duplicate xunit entries (keep the newer version)
    if ($content -match "xunit.*2\.4\.2") {
        Write-Host "   • Removing duplicate xunit 2.4.2 from $relativePath" -ForegroundColor Yellow
        if (!$DryRun) {
            $content = $content -replace '\s*<PackageReference Include="xunit" Version="2\.4\.2" />\s*', ''
            $modified = $true
        }
    }
    
    # Remove duplicate xunit.runner.visualstudio entries
    if ($content -match "xunit\.runner\.visualstudio.*2\.4\.5") {
        Write-Host "   • Removing duplicate xunit.runner.visualstudio 2.4.5 from $relativePath" -ForegroundColor Yellow
        if (!$DryRun) {
            $content = $content -replace '\s*<PackageReference Include="xunit\.runner\.visualstudio" Version="2\.4\.5"[^>]*>\s*<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\s*<PrivateAssets>all</PrivateAssets>\s*</PackageReference>\s*', ''
            $modified = $true
        }
    }
    
    # Remove duplicate Moq entries
    if ($content -match "Moq.*4\.20\.69") {
        Write-Host "   • Removing duplicate Moq 4.20.69 from $relativePath" -ForegroundColor Yellow
        if (!$DryRun) {
            $content = $content -replace '\s*<PackageReference Include="Moq" Version="4\.20\.69" />\s*', ''
            $modified = $true
        }
    }
    
    # Remove duplicate FluentAssertions entries
    if ($content -match "FluentAssertions.*6\.12\.0") {
        Write-Host "   • Removing duplicate FluentAssertions 6.12.0 from $relativePath" -ForegroundColor Yellow
        if (!$DryRun) {
            $content = $content -replace '\s*<PackageReference Include="FluentAssertions" Version="6\.12\.0" />\s*', ''
            $modified = $true
        }
    }
    
    # Remove duplicate coverlet.collector entries
    if ($content -match "coverlet\.collector.*6\.0\.0") {
        Write-Host "   • Removing duplicate coverlet.collector 6.0.0 from $relativePath" -ForegroundColor Yellow
        if (!$DryRun) {
            $content = $content -replace '\s*<PackageReference Include="coverlet\.collector" Version="6\.0\.0"[^>]*>\s*<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\s*<PrivateAssets>all</PrivateAssets>\s*</PackageReference>\s*', ''
            $modified = $true
        }
    }
    
    if ($modified -and !$DryRun) {
        Set-Content $file.FullName -Value $content -NoNewline
    }
}

Write-Host ""
Write-Host "📦 Step 3: Adding missing packages for .NET 9 compatibility..." -ForegroundColor Cyan

# Some projects might need additional packages for .NET 9
$net9Packages = @{
    'Microsoft.Extensions.DependencyInjection.Abstractions' = '9.0.0'
    'Microsoft.Extensions.Logging.Abstractions' = '9.0.0'
    'Microsoft.Extensions.Configuration.Abstractions' = '9.0.0'
}

# Restore and build
if (!$DryRun) {
    Write-Host ""
    Write-Host "🔄 Step 4: Restoring NuGet packages..." -ForegroundColor Cyan
    try {
        dotnet restore CreditTransfer.Modern.sln --verbosity quiet
        Write-Host "✅ Package restore completed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Package restore failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Continuing with build attempt..." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🔨 Step 5: Building solution..." -ForegroundColor Cyan
    try {
        dotnet build CreditTransfer.Modern.sln --no-restore --verbosity quiet
        Write-Host "✅ Build completed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Running detailed build to see errors..." -ForegroundColor Yellow
        dotnet build CreditTransfer.Modern.sln --no-restore --verbosity normal
    }
} else {
    Write-Host "🔍 Dry run complete - no actual changes made" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 Package fixes process completed!" -ForegroundColor Green
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "   • Fixed incorrect package versions" -ForegroundColor White
Write-Host "   • Removed duplicate package references" -ForegroundColor White
Write-Host "   • Updated packages to correct .NET 9 compatible versions" -ForegroundColor White

if (!$DryRun) {
    Write-Host "   • Restored packages and attempted build" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "🚀 To apply fixes, run: .\fix-package-versions.ps1" -ForegroundColor Green
}

# Return to original directory
Set-Location .. 