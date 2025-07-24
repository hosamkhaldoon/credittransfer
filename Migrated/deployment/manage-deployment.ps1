# Credit Transfer System - Kubernetes Deployment Management Script
# Enhanced with Istio Service Mesh and Comprehensive Observability (Kiali, OpenTelemetry, Jaeger)

param(
    [string]$Action = "status",
    [string]$Service = "all",
    [switch]$SkipKubernetesCheck = $false,
    [switch]$ConvertToPublic = $false,
    [switch]$SkipIstio = $false,
    [switch]$ForcePullImages = $false
)

$Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Cyan"
    White = "White"
    Magenta = "Magenta"
}

function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $Colors.Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $Colors.Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $Colors.Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $Colors.Red
}

function Write-Progress {
    param([string]$Message)
    Write-Host "[PROGRESS] $Message" -ForegroundColor $Colors.Magenta
}

function Test-DockerDesktopRunning {
    try {
        $dockerInfo = docker info 2>$null
        if ($dockerInfo) {
            return $true
        }
    } catch {
        return $false
    }
    return $false
}

function Test-KubernetesRunning {
    try {
        # Test 1: Basic cluster info
        $clusterInfo = kubectl cluster-info *>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Kubernetes cluster is responsive"
            return $true
        }
        
        # Test 2: Try to get nodes
        $nodes = kubectl get nodes *>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Kubernetes nodes are accessible"
            return $true
        }
        
        # Test 3: Try to get namespaces
        $namespaces = kubectl get namespaces *>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Kubernetes API is responding"
            return $true
        }
        
    } catch {
        return $false
    }
    return $false
}

function Test-IstioInstalled {
    try {
        # Check if istioctl is available
        $istioctl = Get-Command istioctl -ErrorAction SilentlyContinue
        if (-not $istioctl) {
            Write-Warning "istioctl not found in PATH"
            # Check if it's in the local istio directory
            $localIstioctl = Join-Path $PSScriptRoot "istio-1.26.2\bin\istioctl.exe"
            if (Test-Path $localIstioctl) {
                Write-Status "Found local istioctl at: $localIstioctl"
            } else {
                return $false
            }
        }
        
        # Check if istio-system namespace exists
        $istioNamespace = kubectl get namespace istio-system --no-headers 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Status "istio-system namespace not found"
            return $false
        }
        
        # Check if Istio control plane pods are running
        $istiodPods = kubectl get pods -n istio-system -l app=istiod --no-headers 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $istiodPods) {
            Write-Status "Istio control plane pods not found"
            return $false
        }
        
        # Check if istiod is actually running
        $runningPods = kubectl get pods -n istio-system -l app=istiod --no-headers 2>$null | Where-Object { $_ -match "Running" }
        if (-not $runningPods) {
            Write-Status "Istio control plane pods not running"
            return $false
        }
        
        # Check if Istio CRDs are installed
        $gatewayCRD = kubectl get crd gateways.networking.istio.io --no-headers 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Status "Istio CRDs not found"
            return $false
        }
        
        Write-Status "Istio control plane is installed and running"
        return $true
    } catch {
        return $false
    }
}

function Update-ImagePullPolicy {
    param(
        [string]$Policy = "Always",
        [string]$ServiceName = "all"
    )
    
    Write-Status "Updating image pull policy to '$Policy'..."
    
    try {
        $success = $true
        
        if ($ServiceName -eq "all" -or $ServiceName -eq "api" -or $ServiceName -eq "credittransfer-api") {
            Write-Status "Updating credittransfer-api image pull policy..."
            
            # Use kubectl edit with a simple approach
            kubectl patch deployment credittransfer-api -n credittransfer --type='json' -p='[{"op": "replace", "path": "/spec/template/spec/containers/0/imagePullPolicy", "value": "'$Policy'"}]'
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "âœ… Updated credittransfer-api image pull policy to '$Policy'"
            } else {
                Write-Warning "âš ï¸ Failed to update credittransfer-api image pull policy (trying alternative method...)"
                # Alternative: Use environment variable substitution
                kubectl set env deployment/credittransfer-api -n credittransfer --overwrite FORCE_UPDATE="$(Get-Date -Format 'yyyyMMddHHmmss')"
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "âœ… Triggered credittransfer-api restart with timestamp update"
                } else {
                    $success = $false
                }
            }
        }
        
        if ($ServiceName -eq "all" -or $ServiceName -eq "wcf" -or $ServiceName -eq "credittransfer-wcf") {
            Write-Status "Updating credittransfer-wcf image pull policy..."
            
            # Use kubectl edit with a simple approach
            kubectl patch deployment credittransfer-wcf -n credittransfer --type='json' -p='[{"op": "replace", "path": "/spec/template/spec/containers/0/imagePullPolicy", "value": "'$Policy'"}]'
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "âœ… Updated credittransfer-wcf image pull policy to '$Policy'"
            } else {
                Write-Warning "âš ï¸ Failed to update credittransfer-wcf image pull policy (trying alternative method...)"
                # Alternative: Use environment variable substitution
                kubectl set env deployment/credittransfer-wcf -n credittransfer --overwrite FORCE_UPDATE="$(Get-Date -Format 'yyyyMMddHHmmss')"
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "âœ… Triggered credittransfer-wcf restart with timestamp update"
                } else {
                    $success = $false
                }
            }
        }
        
        return $success
    } catch {
        Write-Error "Failed to update image pull policy: $($_.Exception.Message)"
        return $false
    }
}

function Force-ImagePull {
    param([string]$ServiceName = "all")
    
    Write-Status "Forcing image pull and deployment restart for: $ServiceName"
    
    # Ultra-simple approach: Just use rollout restart with annotation
    # This forces Kubernetes to recreate pods and pull images
    if ($ServiceName -eq "all") {
        Write-Status "Force restarting all Credit Transfer services..."
        
        # Add annotation to force restart and trigger image pull
        $timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
        
        Write-Status "Adding force-pull annotation to API deployment..."
        kubectl annotate deployment credittransfer-api -n credittransfer deployment.kubernetes.io/force-pull="$timestamp" --overwrite
        kubectl rollout restart deployment credittransfer-api -n credittransfer
        
        Write-Status "Adding force-pull annotation to WCF deployment..."
        kubectl annotate deployment credittransfer-wcf -n credittransfer deployment.kubernetes.io/force-pull="$timestamp" --overwrite
        kubectl rollout restart deployment credittransfer-wcf -n credittransfer
        
        Write-Progress "Waiting for rollouts to complete..."
        kubectl rollout status deployment credittransfer-api -n credittransfer --timeout=300s
        $apiStatus = $LASTEXITCODE
        
        kubectl rollout status deployment credittransfer-wcf -n credittransfer --timeout=300s
        $wcfStatus = $LASTEXITCODE
        
        if ($apiStatus -eq 0 -and $wcfStatus -eq 0) {
            Write-Success "âœ… Successfully force-restarted all services (timestamp: $timestamp)"
            Write-Status "Note: If images are available in registry, they will be pulled during restart"
        } else {
            Write-Warning "âš ï¸ Some services may still be restarting (API: $apiStatus, WCF: $wcfStatus)"
        }
    } else {
        # Handle individual services
        $deploymentName = switch ($ServiceName.ToLower()) {
            "api" { "credittransfer-api" }
            "wcf" { "credittransfer-wcf" }
            "credittransfer-api" { "credittransfer-api" }
            "credittransfer-wcf" { "credittransfer-wcf" }
            default { 
                Write-Warning "Unknown service name: $ServiceName. Trying: $ServiceName"
                $ServiceName 
            }
        }
        
        $timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
        Write-Status "Force restarting $deploymentName with timestamp: $timestamp"
        
        kubectl annotate deployment $deploymentName -n credittransfer deployment.kubernetes.io/force-pull="$timestamp" --overwrite
        kubectl rollout restart deployment $deploymentName -n credittransfer
        
        Write-Progress "Waiting for rollout to complete..."
        kubectl rollout status deployment $deploymentName -n credittransfer --timeout=300s
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "âœ… Successfully force-restarted $deploymentName (timestamp: $timestamp)"
            Write-Status "Note: If images are available in registry, they will be pulled during restart"
        } else {
            Write-Warning "âš ï¸ $deploymentName may still be restarting (status code: $LASTEXITCODE)"
        }
    }
    
    # Show current pod status
    Write-Status "Current pod status:"
    kubectl get pods -n credittransfer -l app=credittransfer-api -o wide 2>/dev/null | Select-Object -First 5
    kubectl get pods -n credittransfer -l app=credittransfer-wcf -o wide 2>/dev/null | Select-Object -First 5
    
    return $true
}

function Install-Istio {
    Write-Status "Installing Istio service mesh..."
    
    # Use local istioctl from downloaded Istio
    $localIstioctl = Join-Path $PSScriptRoot "istio-1.26.2\bin\istioctl.exe"
    
    if (-not (Test-Path $localIstioctl)) {
        Write-Error "Local istioctl not found at: $localIstioctl"
        Write-Status "Please ensure Istio 1.26.2 is extracted in the istio-1.26.2 directory"
        return $false
    }
    
    Write-Status "Using local istioctl: $localIstioctl"
    
    # Install Istio with reduced resource requirements for Docker Desktop
    Write-Progress "Installing Istio control plane with Docker Desktop optimizations..."
    & $localIstioctl install --set values.pilot.resources.requests.memory=512Mi --set values.global.proxy.resources.requests.memory=128Mi --set values.global.proxy.resources.requests.cpu=100m --set values.pilot.traceSampling=100.0 -y
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install Istio control plane"
        return $false
    }
    
    # Wait for Istio control plane to be ready
    Write-Progress "Waiting for Istio control plane to be ready..."
    $timeout = 300
    $elapsed = 0
    $checkInterval = 10
    
    while ($elapsed -lt $timeout) {
        $istiodPods = kubectl get pods -n istio-system -l app=istiod --no-headers 2>$null
        if ($istiodPods -and $istiodPods -match "Running") {
            Write-Success "âœ… Istio control plane is ready!"
            break
        }
        
        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
        Write-Host "." -NoNewline -ForegroundColor $Colors.Yellow
        
        if ($elapsed % 60 -eq 0) {
            Write-Host ""
            Write-Status "[$elapsed/$timeout seconds] Still waiting for Istio control plane..."
        }
    }
    Write-Host ""
    
    if ($elapsed -ge $timeout) {
        Write-Error "Istio control plane failed to become ready within $timeout seconds"
        return $false
    }
    
    # Verify installation
    if (Test-IstioInstalled) {
        Write-Success "âœ… Istio service mesh installed successfully!"
        return $true
    } else {
        Write-Error "Istio installation verification failed"
        return $false
    }
}

function Install-IstioAddons {
    Write-Status "Installing Istio observability addons..."
    
    # Verify istio-system namespace exists
    $istioNamespace = kubectl get namespace istio-system --no-headers 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "istio-system namespace not found. Please install Istio first."
        return $false
    }
    
    $addonsPath = Join-Path $PSScriptRoot "istio-1.26.2\samples\addons"
    
    if (-not (Test-Path $addonsPath)) {
        Write-Error "Istio addons directory not found: $addonsPath"
        return $false
    }
    
    # Install Kiali for service mesh visualization (primary Istio addon)
    Write-Progress "Installing Kiali for service mesh visualization..."
    $kialiYaml = Join-Path $addonsPath "kiali.yaml"
    if (Test-Path $kialiYaml) {
        kubectl apply -f $kialiYaml
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install Kiali"
            return $false
        }
    } else {
        Write-Error "Kiali YAML not found at: $kialiYaml"
        return $false
    }
    
    # Note: We use our own Prometheus, Grafana, and Jaeger from k8s-manifests/monitoring.yaml
    # These provide better integration with our Credit Transfer system
    Write-Status "Note: Using custom Prometheus, Grafana, and Jaeger from monitoring.yaml"
    Write-Status "These provide better integration than default Istio addons"
    
    # Wait for Kiali to be ready
    Write-Progress "Waiting for Kiali to be ready..."
    $timeout = 300
    $elapsed = 0
    $checkInterval = 10
    
    while ($elapsed -lt $timeout) {
        $kialiPods = kubectl get pods -n istio-system -l app=kiali --no-headers 2>$null
        if ($kialiPods -and $kialiPods -match "Running") {
            Write-Success "âœ… Kiali is ready"
            break
        }
        
        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
        Write-Host "." -NoNewline -ForegroundColor $Colors.Yellow
        
        if ($elapsed % 60 -eq 0) {
            Write-Host ""
            Write-Status "[$elapsed/$timeout seconds] Still waiting for Kiali..."
        }
    }
    Write-Host ""
    
    if ($elapsed -ge $timeout) {
        Write-Warning "âš ï¸ Kiali may still be starting up"
    }
    
    Write-Success "âœ… Istio service mesh visualization (Kiali) installed successfully!"
    Write-Status "Custom observability stack (Prometheus, Grafana, Jaeger) will be deployed with main manifests"
    return $true
}

function Apply-IstioResources {
    Write-Status "Applying Istio service mesh resources..."
    
    # Apply Istio DestinationRules
    $destinationRulesFile = Join-Path $PSScriptRoot "k8s-manifests\istio-destination-rules.yaml"
    if (Test-Path $destinationRulesFile) {
        Write-Progress "Applying Istio DestinationRules..."
        kubectl apply -f $destinationRulesFile
        if ($LASTEXITCODE -eq 0) {
            Write-Success "âœ… Istio DestinationRules applied successfully"
        } else {
            Write-Warning "âš ï¸ Failed to apply some DestinationRules"
        }
    }
    
    # Apply Istio Gateway and VirtualService
    $gatewayFile = Join-Path $PSScriptRoot "k8s-manifests\istio-gateway.yaml"
    if (Test-Path $gatewayFile) {
        Write-Progress "Applying Istio Gateway and VirtualService..."
        kubectl apply -f $gatewayFile
        if ($LASTEXITCODE -eq 0) {
            Write-Success "âœ… Istio Gateway and VirtualService applied successfully"
        } else {
            Write-Warning "âš ï¸ Failed to apply Gateway and VirtualService"
        }
    }
    
    Write-Success "âœ… Istio resources applied successfully!"
}

function Enable-IstioSidecarInjection {
    Write-Status "Enabling Istio sidecar injection..."
    
    # Enable automatic sidecar injection for credittransfer namespace
    Write-Progress "Enabling automatic sidecar injection for credittransfer namespace..."
    kubectl label namespace credittransfer istio-injection=enabled --overwrite
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "âœ… Istio sidecar injection enabled for credittransfer namespace"
    } else {
        Write-Error "Failed to enable sidecar injection"
        return $false
    }
    
    # Restart deployments to get sidecars injected
    Write-Progress "Restarting deployments to inject Istio sidecars..."
    
    # Scale down to 1 replica to avoid port conflicts
    Write-Status "Scaling deployments to 1 replica to avoid port conflicts..."
    kubectl scale deployment credittransfer-api --replicas=1 -n credittransfer
    kubectl scale deployment credittransfer-wcf --replicas=1 -n credittransfer
    
    # Restart deployments
    Write-Status "Restarting deployments to inject sidecars..."
    kubectl rollout restart deployment credittransfer-api -n credittransfer
    kubectl rollout restart deployment credittransfer-wcf -n credittransfer
    
    # Wait for rollouts to complete
    Write-Progress "Waiting for deployments to restart with sidecars..."
    kubectl rollout status deployment credittransfer-api -n credittransfer --timeout=300s
    kubectl rollout status deployment credittransfer-wcf -n credittransfer --timeout=300s
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "âœ… Deployments restarted with Istio sidecars!"
    } else {
        Write-Warning "âš ï¸ Some deployments may still be restarting"
    }
    
    return $true
}

function Verify-IstioSidecarInjection {
    Write-Status "Verifying Istio sidecar injection..."
    
    # Check if pods have istio-proxy containers
    $apiPods = kubectl get pods -n credittransfer -l app=credittransfer-api -o jsonpath='{.items[*].spec.containers[*].name}' 2>$null
    $wcfPods = kubectl get pods -n credittransfer -l app=credittransfer-wcf -o jsonpath='{.items[*].spec.containers[*].name}' 2>$null
    
    $apiHasSidecar = $apiPods -and $apiPods -match "istio-proxy"
    $wcfHasSidecar = $wcfPods -and $wcfPods -match "istio-proxy"
    
    if ($apiHasSidecar -and $wcfHasSidecar) {
        Write-Success "âœ… Istio sidecars successfully injected into all Credit Transfer pods"
        return $true
    } elseif ($apiHasSidecar -or $wcfHasSidecar) {
        Write-Warning "âš ï¸ Istio sidecars partially injected (API: $apiHasSidecar, WCF: $wcfHasSidecar)"
        return $true
    } else {
        Write-Warning "âš ï¸ No Istio sidecars detected. Pods may still be restarting."
        return $false
    }
}

function Install-OpenTelemetryOperator {
    Write-Status "Installing OpenTelemetry Operator (optional)..."
    
    Write-Warning "OpenTelemetry Operator installation detected."
    Write-Host "The current configuration uses a simple OpenTelemetry Collector deployment."
    Write-Host "If you want to use the operator-based approach, you can install it manually:" -ForegroundColor $Colors.Yellow
    Write-Host ""
    Write-Host "  kubectl apply -f https://github.com/open-telemetry/opentelemetry-operator/releases/latest/download/opentelemetry-operator.yaml" -ForegroundColor $Colors.Blue
    Write-Host ""
    Write-Host "Then uncomment the OpenTelemetryCollector and Instrumentation resources in k8s-manifests/opentelemetry.yaml" -ForegroundColor $Colors.Yellow
    Write-Host ""
    
    $install = Read-Host "Do you want to install the OpenTelemetry Operator now? (y/N)"
    if ($install -eq "y" -or $install -eq "Y") {
        Write-Progress "Installing OpenTelemetry Operator..."
        kubectl apply -f "https://github.com/open-telemetry/opentelemetry-operator/releases/latest/download/opentelemetry-operator.yaml"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "âœ… OpenTelemetry Operator installed successfully!"
            Write-Status "You can now uncomment the CRDs in k8s-manifests/opentelemetry.yaml if desired"
            return $true
        } else {
            Write-Error "Failed to install OpenTelemetry Operator"
            return $false
        }
    } else {
        Write-Status "Skipping OpenTelemetry Operator installation"
        Write-Status "Using simple OpenTelemetry Collector deployment instead"
        return $true
    }
}

function Test-DockerDesktopKubernetesEnabled {
    try {
        # Method 1: Check if docker-desktop context exists
        $contexts = kubectl config get-contexts 2>$null
        if ($contexts -match "docker-desktop") {
            Write-Status "Found docker-desktop context"
            
            # Method 2: Try to switch to docker-desktop context and test
            $currentContext = kubectl config current-context 2>$null
            kubectl config use-context docker-desktop 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                # Method 3: Test if we can actually connect to Kubernetes
                $namespaces = kubectl get namespaces 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Status "Docker Desktop Kubernetes is fully functional"
                    return $true
                } else {
                    Write-Warning "docker-desktop context exists but Kubernetes is not responsive"
                }
            }
            
            # Restore original context if switch failed
            if ($currentContext) {
                kubectl config use-context $currentContext 2>$null
            }
        }
    } catch {
        return $false
    }
    return $false
}

function Start-DockerDesktop {
    Write-Status "Starting Docker Desktop..."
    
    # Try to find Docker Desktop executable
    $dockerDesktopPaths = @(
        "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe",
        "${env:ProgramFiles(x86)}\Docker\Docker\Docker Desktop.exe",
        "${env:LOCALAPPDATA}\Programs\Docker\Docker\Docker Desktop.exe"
    )
    
    $dockerDesktopPath = $null
    foreach ($path in $dockerDesktopPaths) {
        if (Test-Path $path) {
            $dockerDesktopPath = $path
            break
        }
    }
    
    if ($dockerDesktopPath) {
        Write-Status "Found Docker Desktop at: $dockerDesktopPath"
        Start-Process -FilePath $dockerDesktopPath -WindowStyle Hidden
        
        # Wait for Docker Desktop to start
        Write-Progress "Waiting for Docker Desktop to start..."
        $timeout = 120 # 2 minutes
        $elapsed = 0
        while (-not (Test-DockerDesktopRunning) -and $elapsed -lt $timeout) {
            Start-Sleep -Seconds 5
            $elapsed += 5
            Write-Host "." -NoNewline -ForegroundColor $Colors.Yellow
        }
        Write-Host ""
        
        if (Test-DockerDesktopRunning) {
            Write-Success "Docker Desktop is now running"
            return $true
        } else {
            Write-Error "Docker Desktop failed to start within $timeout seconds"
            return $false
        }
    } else {
        Write-Error "Docker Desktop executable not found. Please install Docker Desktop."
        Write-Host "Download from: https://www.docker.com/products/docker-desktop/" -ForegroundColor $Colors.Blue
        return $false
    }
}

function Enable-DockerDesktopKubernetes {
    Write-Warning "Docker Desktop Kubernetes is not enabled."
    
    # Show current status
    Show-KubernetesStatus
    
    Write-Host ""
    Write-Host "ðŸŽ¯ MANUAL ACTION REQUIRED:" -ForegroundColor $Colors.Yellow
    Write-Host "1. Open Docker Desktop (should be starting now...)" -ForegroundColor $Colors.White
    Write-Host "2. Click the Settings gear icon (âš™ï¸) in the top-right" -ForegroundColor $Colors.White
    Write-Host "3. Click 'Kubernetes' in the left sidebar" -ForegroundColor $Colors.White
    Write-Host "4. Check âœ… 'Enable Kubernetes'" -ForegroundColor $Colors.White
    Write-Host "5. Click 'Apply & Restart'" -ForegroundColor $Colors.White
    Write-Host "6. Wait 2-3 minutes for Kubernetes to initialize" -ForegroundColor $Colors.White
    Write-Host "7. You should see 'Kubernetes is running' with a green dot" -ForegroundColor $Colors.White
    Write-Host ""
    
    # Try to open Docker Desktop settings
    try {
        Start-Process -FilePath "docker-desktop://dashboard/settings/kubernetes" -ErrorAction SilentlyContinue
        Write-Status "Attempted to open Docker Desktop Kubernetes settings"
    } catch {
        # Fallback: just open Docker Desktop
        $dockerDesktopPaths = @(
            "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe",
            "${env:ProgramFiles(x86)}\Docker\Docker\Docker Desktop.exe",
            "${env:LOCALAPPDATA}\Programs\Docker\Docker\Docker Desktop.exe"
        )
        
        foreach ($path in $dockerDesktopPaths) {
            if (Test-Path $path) {
                Start-Process -FilePath $path -ErrorAction SilentlyContinue
                Write-Status "Opened Docker Desktop"
                break
            }
        }
    }
    
    # Wait for user to enable Kubernetes
    Write-Host "Press any key after you see 'Kubernetes is running' in Docker Desktop..." -ForegroundColor $Colors.Green
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    # Check if Kubernetes is now enabled with more detailed feedback
    Write-Status "Checking if Kubernetes is now enabled..."
    $timeout = 300 # 5 minutes instead of 3
    $elapsed = 0
    $checkInterval = 10
    
    while (-not (Test-DockerDesktopKubernetesEnabled) -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
        
        # Show progress every 30 seconds
        if ($elapsed % 30 -eq 0) {
            Write-Host ""
            Write-Host "[$elapsed/$timeout seconds] Still checking..." -ForegroundColor $Colors.Yellow
            Show-KubernetesStatus
        } else {
            Write-Host "." -NoNewline -ForegroundColor $Colors.Yellow
        }
    }
    Write-Host ""
    
    if (Test-DockerDesktopKubernetesEnabled) {
        Write-Success "Docker Desktop Kubernetes is now enabled!"
        Show-KubernetesStatus
        return $true
    } else {
        Write-Error "Docker Desktop Kubernetes is still not enabled after $timeout seconds."
        Write-Host ""
        Write-Host "ðŸ”§ Troubleshooting Tips:" -ForegroundColor $Colors.Yellow
        Write-Host "1. Check if Docker Desktop shows 'Kubernetes is running' with green dot" -ForegroundColor $Colors.White
        Write-Host "2. Try restarting Docker Desktop completely" -ForegroundColor $Colors.White
        Write-Host "3. Check Windows Event Viewer for Docker Desktop errors" -ForegroundColor $Colors.White
        Write-Host "4. Make sure Windows Subsystem for Linux (WSL2) is enabled" -ForegroundColor $Colors.White
        Write-Host "5. Run: kubectl config get-contexts" -ForegroundColor $Colors.White
        Write-Host ""
        Show-KubernetesStatus
        return $false
    }
}

function Switch-ToDockerDesktop {
    Write-Status "Switching kubectl context to docker-desktop..."
    
    try {
        kubectl config use-context docker-desktop 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Successfully switched to docker-desktop context"
            return $true
        }
    } catch {
        Write-Error "Failed to switch to docker-desktop context"
        return $false
    }
    return $false
}

function Wait-ForKubernetesReady {
    Write-Status "Waiting for Kubernetes to be ready..."
    
    $timeout = 180 # 3 minutes
    $elapsed = 0
    
    while (-not (Test-KubernetesRunning) -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 10
        $elapsed += 10
        Write-Host "." -NoNewline -ForegroundColor $Colors.Yellow
    }
    Write-Host ""
    
    if (Test-KubernetesRunning) {
        Write-Success "Kubernetes is ready!"
        return $true
    } else {
        Write-Error "Kubernetes is not ready after $timeout seconds"
        return $false
    }
}

function Ensure-KubernetesRunning {
    Write-Status "Checking Kubernetes status..."
    
    # Check if Kubernetes is already running
    if (Test-KubernetesRunning) {
        Write-Success "Kubernetes is already running"
        $currentContext = kubectl config current-context 2>$null
        if ($currentContext -ne "docker-desktop") {
            Write-Status "Switching to docker-desktop context..."
            kubectl config use-context docker-desktop 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Switched to docker-desktop context"
            }
        }
        return $true
    }
    
    Write-Warning "Kubernetes is not running. Setting up Docker Desktop Kubernetes..."
    
    # Check if Docker Desktop is running
    if (-not (Test-DockerDesktopRunning)) {
        Write-Status "Docker Desktop is not running"
        if (-not (Start-DockerDesktop)) {
            return $false
        }
    } else {
        Write-Success "Docker Desktop is running"
    }
    
    # Check if Docker Desktop Kubernetes is enabled
    if (-not (Test-DockerDesktopKubernetesEnabled)) {
        if (-not (Enable-DockerDesktopKubernetes)) {
            return $false
        }
    } else {
        Write-Success "Docker Desktop Kubernetes is enabled"
    }
    
    # Switch to docker-desktop context
    if (-not (Switch-ToDockerDesktop)) {
        return $false
    }
    
    # Wait for Kubernetes to be ready
    if (-not (Wait-ForKubernetesReady)) {
        Write-Warning "Kubernetes may still be starting up. Trying alternative check..."
        # Give it one more chance with a longer timeout
        Start-Sleep -Seconds 30
        if (-not (Test-KubernetesRunning)) {
            Write-Error "Kubernetes is not responding. Please check Docker Desktop status."
            Show-KubernetesStatus
            return $false
        }
    }
    
    Write-Success "âœ… Docker Desktop Kubernetes is now ready!"
    Show-KubernetesStatus
    return $true
}

function Show-Help {
    Write-Host ""
    Write-Host "ðŸš€ Credit Transfer System - Deployment Management" -ForegroundColor $Colors.Green
    Write-Host "Enhanced with Istio Service Mesh `& Comprehensive Observability" -ForegroundColor $Colors.Green
    Write-Host "=============================================================" -ForegroundColor $Colors.Green
    Write-Host ""
    Write-Host "Usage: .\manage-deployment.ps1 -Action `<action`> [-Service `<service`>] [-SkipKubernetesCheck] [-ConvertToPublic] [-SkipIstio] [-ForcePullImages]" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Actions:" -ForegroundColor $Colors.White
    Write-Host "  status         - Show deployment status (default)" -ForegroundColor $Colors.White
    Write-Host "  deploy         - Deploy all services with Istio and observability" -ForegroundColor $Colors.White
    Write-Host "  deploy-istio   - Install Istio service mesh and observability addons" -ForegroundColor $Colors.White
    Write-Host "  restart        - Restart services" -ForegroundColor $Colors.White
    Write-Host "  logs           - Show service logs" -ForegroundColor $Colors.White
    Write-Host "  port-forward   - Set up port forwarding (includes observability tools)" -ForegroundColor $Colors.White
    Write-Host "  test           - Run service tests" -ForegroundColor $Colors.White
    Write-Host "  cleanup        - Clean up deployment" -ForegroundColor $Colors.White
    Write-Host "  setup-keycloak - Run Keycloak setup (use -ConvertToPublic for public client)" -ForegroundColor $Colors.White
    Write-Host "  setup-k8s      - Setup Docker Desktop Kubernetes" -ForegroundColor $Colors.White
    Write-Host "  diagnose       - Show detailed Kubernetes and Istio diagnostics" -ForegroundColor $Colors.White
    Write-Host "  observability  - Show observability stack status and URLs" -ForegroundColor $Colors.White
    Write-Host "  clear-cache    - Clear Redis cache (app keys only, db, or all)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Services:" -ForegroundColor $Colors.White
    Write-Host "  all            - All services (default)" -ForegroundColor $Colors.White
    Write-Host "  api            - Credit Transfer API" -ForegroundColor $Colors.White
    Write-Host "  wcf            - Credit Transfer WCF" -ForegroundColor $Colors.White
    Write-Host "  keycloak       - Keycloak authentication" -ForegroundColor $Colors.White
    Write-Host "  prometheus     - Prometheus metrics" -ForegroundColor $Colors.White
    Write-Host "  grafana        - Grafana monitoring" -ForegroundColor $Colors.White
    Write-Host "  jaeger         - Jaeger tracing" -ForegroundColor $Colors.White
    Write-Host "  kiali          - Kiali service mesh visualization" -ForegroundColor $Colors.White
    Write-Host "  otel           - OpenTelemetry collector" -ForegroundColor $Colors.White
    Write-Host "  redis          - Redis cache server" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Switches:" -ForegroundColor $Colors.White
    Write-Host "  -SkipKubernetesCheck - Skip automatic Kubernetes setup" -ForegroundColor $Colors.White
    Write-Host "  -ConvertToPublic     - Convert Keycloak client to public (no client_secret)" -ForegroundColor $Colors.White
    Write-Host "  -SkipIstio          - Skip Istio installation (for quick operations)" -ForegroundColor $Colors.White
    Write-Host "  -ForcePullImages    - Force pull latest Docker images (changes imagePullPolicy to Always)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\manage-deployment.ps1 -Action status" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action deploy" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action deploy -ForcePullImages" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action restart -ForcePullImages" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action restart -Service api -ForcePullImages" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action deploy-istio" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action observability" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service kiali" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action setup-k8s" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action setup-keycloak" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action setup-keycloak -ConvertToPublic" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action diagnose" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action clear-cache" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action clear-cache -Service all" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "ðŸŽ¯ Observability Features:" -ForegroundColor $Colors.Green
    Write-Host "  â€¢ Istio Service Mesh with automatic sidecar injection" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Kiali for service mesh topology and health visualization" -ForegroundColor $Colors.White
    Write-Host "  â€¢ OpenTelemetry for distributed tracing and metrics collection" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Jaeger for end-to-end distributed tracing" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Prometheus for metrics collection and alerting" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Grafana for metrics visualization and dashboards" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Request tracing across all microservices" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Traffic management with circuit breakers and retries" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "ðŸ”§ Automatic Features:" -ForegroundColor $Colors.Green
    Write-Host "  â€¢ Automatically installs Istio service mesh" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Deploys comprehensive observability stack" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Enables automatic sidecar injection" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Sets up traffic management and fault injection" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Configures distributed tracing with 100% sampling" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Enhanced diagnostics for service mesh issues" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Smart image pulling with policy management" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "ðŸ–¼ï¸ Image Management:" -ForegroundColor $Colors.Green
    Write-Host "  â€¢ -ForcePullImages changes imagePullPolicy from 'Never' to 'Always'" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Forces Kubernetes to pull latest Docker images from registry" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Automatically restarts deployments to use new images" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Works with both 'deploy' and 'restart' actions" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Supports individual service updates (-Service api/wcf)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "ðŸ—„ï¸ Cache Management:" -ForegroundColor $Colors.Green
    Write-Host "  â€¢ clear-cache (default): Clear only CreditTransfer:* application keys" -ForegroundColor $Colors.White
    Write-Host "  â€¢ clear-cache -Service db: Clear entire database 0" -ForegroundColor $Colors.White
    Write-Host "  â€¢ clear-cache -Service all: Clear all Redis databases (requires confirmation)" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Automatic Redis health check before clearing cache" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Shows remaining key count after cache operations" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "ðŸ'¡ Troubleshooting:" -ForegroundColor $Colors.Yellow
    Write-Host "  â€¢ If deployment fails: .\manage-deployment.ps1 -Action diagnose" -ForegroundColor $Colors.White
    Write-Host "  â€¢ For quick operations: add -SkipKubernetesCheck -SkipIstio" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Check service mesh: .\manage-deployment.ps1 -Action observability" -ForegroundColor $Colors.White
    Write-Host "  â€¢ Access Kiali: .\manage-deployment.ps1 -Action port-forward -Service kiali" -ForegroundColor $Colors.White
    Write-Host ""
}

function Show-Status {
    Write-Status "Checking deployment status..."
    Write-Host ""
    
    # Check Istio status
    Write-Host "ðŸ•¸ï¸ Istio Service Mesh Status:" -ForegroundColor $Colors.Yellow
    if (Test-IstioInstalled) {
        Write-Host "  âœ… Istio control plane: Installed" -ForegroundColor $Colors.Green
        kubectl get pods -n istio-system
        Write-Host ""
        
        # Check sidecar injection
        $injectionStatus = kubectl get namespace credittransfer -o jsonpath='{.metadata.labels.istio-injection}' 2>$null
        if ($injectionStatus -eq "enabled") {
            Write-Host "  âœ… Sidecar injection: Enabled for credittransfer namespace" -ForegroundColor $Colors.Green
        } else {
            Write-Host "  âš ï¸ Sidecar injection: Not enabled for credittransfer namespace" -ForegroundColor $Colors.Yellow
        }
    } else {
        Write-Host "  âŒ Istio control plane: Not installed" -ForegroundColor $Colors.Red
    }
    Write-Host ""
    
    Write-Host "ðŸ"Š Application Pods Status:" -ForegroundColor $Colors.Yellow
    kubectl get pods -n credittransfer
    Write-Host ""
    
    Write-Host "ðŸŒ Service Status:" -ForegroundColor $Colors.Yellow
    kubectl get svc -n credittransfer
    Write-Host ""
    
    Write-Host "ðŸ"— Istio Configuration:" -ForegroundColor $Colors.Yellow
    kubectl get gateway,virtualservice,destinationrule -n credittransfer
    Write-Host ""
    
    # Check observability components
    Write-Host "ðŸ'ï¸ Observability Stack Status:" -ForegroundColor $Colors.Yellow
    
    # Check Kiali
    $kialiPod = kubectl get pods -n istio-system -l app=kiali --no-headers 2>$null
    if ($kialiPod -and $kialiPod -match "Running") {
        Write-Host "  âœ… Kiali: Running" -ForegroundColor $Colors.Green
    } else {
        Write-Host "  âŒ Kiali: Not running" -ForegroundColor $Colors.Red
    }
    
    # Check Jaeger
    $jaegerPod = kubectl get pods -n credittransfer -l app=jaeger --no-headers 2>$null
    if ($jaegerPod -and $jaegerPod -match "Running") {
        Write-Host "  âœ… Jaeger: Running" -ForegroundColor $Colors.Green
    } else {
        $jaegerPodIstio = kubectl get pods -n istio-system -l app=jaeger --no-headers 2>$null
        if ($jaegerPodIstio -and $jaegerPodIstio -match "Running") {
            Write-Host "  âœ… Jaeger: Running (in istio-system)" -ForegroundColor $Colors.Green
        } else {
            Write-Host "  âŒ Jaeger: Not running" -ForegroundColor $Colors.Red
        }
    }
    
    # Check Prometheus
    $prometheusPod = kubectl get pods -n credittransfer -l app=prometheus --no-headers 2>$null
    if ($prometheusPod -and $prometheusPod -match "Running") {
        Write-Host "  âœ… Prometheus: Running" -ForegroundColor $Colors.Green
    } else {
        $prometheusPodIstio = kubectl get pods -n istio-system -l app=prometheus --no-headers 2>$null
        if ($prometheusPodIstio -and $prometheusPodIstio -match "Running") {
            Write-Host "  âœ… Prometheus: Running (in istio-system)" -ForegroundColor $Colors.Green
        } else {
            Write-Host "  âŒ Prometheus: Not running" -ForegroundColor $Colors.Red
        }
    }
    
    # Check Grafana
    $grafanaPod = kubectl get pods -n credittransfer -l app=grafana --no-headers 2>$null
    if ($grafanaPod -and $grafanaPod -match "Running") {
        Write-Host "  âœ… Grafana: Running" -ForegroundColor $Colors.Green
    } else {
        $grafanaPodIstio = kubectl get pods -n istio-system -l app=grafana --no-headers 2>$null
        if ($grafanaPodIstio -and $grafanaPodIstio -match "Running") {
            Write-Host "  âœ… Grafana: Running (in istio-system)" -ForegroundColor $Colors.Green
        } else {
            Write-Host "  âŒ Grafana: Not running" -ForegroundColor $Colors.Red
        }
    }
    
    # Check OpenTelemetry Collector
    $otelPod = kubectl get pods -n credittransfer -l app=otel-collector --no-headers 2>$null
    if ($otelPod -and $otelPod -match "Running") {
        Write-Host "  âœ… OpenTelemetry Collector: Running" -ForegroundColor $Colors.Green
    } else {
        Write-Host "  âŒ OpenTelemetry Collector: Not running" -ForegroundColor $Colors.Red
    }
    
    Write-Host ""
}

function Show-ObservabilityStatus {
    Write-Host ""
    Write-Host "ðŸ'ï¸ Observability Stack Status & Access URLs" -ForegroundColor $Colors.Green
    Write-Host "=============================================" -ForegroundColor $Colors.Green
    Write-Host ""
    
    Write-Host "ðŸ•¸ï¸ Service Mesh Visualization:" -ForegroundColor $Colors.Yellow
    $kialiPod = kubectl get pods -n istio-system -l app=kiali --no-headers 2>$null
    if ($kialiPod -and $kialiPod -match "Running") {
        Write-Host "  âœ… Kiali (Service Mesh Dashboard)" -ForegroundColor $Colors.Green
        Write-Host "     Access: kubectl port-forward -n istio-system svc/kiali 20001:20001" -ForegroundColor $Colors.White
        Write-Host "     URL: http://localhost:20001/kiali" -ForegroundColor $Colors.Blue
        Write-Host "     Features: Service topology, traffic flow, configuration validation" -ForegroundColor $Colors.White
    } else {
        Write-Host "  âŒ Kiali: Not running" -ForegroundColor $Colors.Red
    }
    Write-Host ""
    
    Write-Host "ðŸ"Š Metrics & Monitoring:" -ForegroundColor $Colors.Yellow
    
    # Prometheus
    $prometheusPod = kubectl get pods -n istio-system -l app=prometheus --no-headers 2>$null
    if (-not $prometheusPod) {
        $prometheusPod = kubectl get pods -n credittransfer -l app=prometheus --no-headers 2>$null
        $prometheusNs = "credittransfer"
    } else {
        $prometheusNs = "istio-system"
    }
    
    if ($prometheusPod -and $prometheusPod -match "Running") {
        Write-Host "  âœ… Prometheus (Metrics Collection)" -ForegroundColor $Colors.Green
        Write-Host "     Access: kubectl port-forward -n $prometheusNs svc/prometheus 9093:9090" -ForegroundColor $Colors.White
        Write-Host "     URL: http://localhost:9093" -ForegroundColor $Colors.Blue
        Write-Host "     Note: Using port 9093 to avoid Windows port conflicts" -ForegroundColor $Colors.Yellow
    } else {
        Write-Host "  âŒ Prometheus: Not running" -ForegroundColor $Colors.Red
    }
    
    # Grafana
    $grafanaPod = kubectl get pods -n istio-system -l app=grafana --no-headers 2>$null
    if (-not $grafanaPod) {
        $grafanaPod = kubectl get pods -n credittransfer -l app=grafana --no-headers 2>$null
        $grafanaNs = "credittransfer"
    } else {
        $grafanaNs = "istio-system"
    }
    
    if ($grafanaPod -and $grafanaPod -match "Running") {
        Write-Host "  âœ… Grafana (Metrics Visualization)" -ForegroundColor $Colors.Green
        Write-Host "     Access: kubectl port-forward -n $grafanaNs svc/grafana 3000:3000" -ForegroundColor $Colors.White
        Write-Host "     URL: http://localhost:3000" -ForegroundColor $Colors.Blue
        Write-Host "     Features: Istio dashboards, service metrics, performance monitoring" -ForegroundColor $Colors.White
    } else {
        Write-Host "  âŒ Grafana: Not running" -ForegroundColor $Colors.Red
    }
    Write-Host ""
    
    Write-Host "ðŸ" Distributed Tracing:" -ForegroundColor $Colors.Yellow
    
    # Jaeger
    $jaegerPod = kubectl get pods -n istio-system -l app=jaeger --no-headers 2>$null
    if (-not $jaegerPod) {
        $jaegerPod = kubectl get pods -n credittransfer -l app=jaeger --no-headers 2>$null
        $jaegerNs = "credittransfer"
    } else {
        $jaegerNs = "istio-system"
    }
    
    if ($jaegerPod -and $jaegerPod -match "Running") {
        Write-Host "  âœ… Jaeger (Distributed Tracing)" -ForegroundColor $Colors.Green
        Write-Host "     Access: kubectl port-forward -n $jaegerNs svc/jaeger 16686:16686" -ForegroundColor $Colors.White
        Write-Host "     URL: http://localhost:16686" -ForegroundColor $Colors.Blue
        Write-Host "     Features: Request tracing, latency analysis, dependency mapping" -ForegroundColor $Colors.White
    } else {
        Write-Host "  âŒ Jaeger: Not running" -ForegroundColor $Colors.Red
    }
    
    # OpenTelemetry Collector
    $otelPod = kubectl get pods -n credittransfer -l app=otel-collector --no-headers 2>$null
    if ($otelPod -and $otelPod -match "Running") {
        Write-Host "  âœ… OpenTelemetry Collector (Telemetry Pipeline)" -ForegroundColor $Colors.Green
        Write-Host "     Features: Telemetry collection, processing, and export" -ForegroundColor $Colors.White
    } else {
        Write-Host "  âŒ OpenTelemetry Collector: Not running" -ForegroundColor $Colors.Red
    }
    Write-Host ""
    
    Write-Host "ðŸŽ¯ Quick Access Commands:" -ForegroundColor $Colors.Green
    Write-Host "  # Service Mesh Visualization" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service kiali" -ForegroundColor $Colors.Blue
    Write-Host ""
    Write-Host "  # Distributed Tracing" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service jaeger" -ForegroundColor $Colors.Blue
    Write-Host ""
    Write-Host "  # Metrics & Dashboards" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service prometheus" -ForegroundColor $Colors.Blue
    Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service grafana" -ForegroundColor $Colors.Blue
    Write-Host ""
    Write-Host "  # All Observability Tools" -ForegroundColor $Colors.White
    Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service observability" -ForegroundColor $Colors.Blue
    Write-Host ""
}

function Start-Deployment {
    Write-Status "Deploying Credit Transfer System with Istio Service Mesh..."
    
    # Install Istio if not already installed and not skipped
    if (-not $SkipIstio -and -not (Test-IstioInstalled)) {
        Write-Status "Istio not found. Installing Istio service mesh..."
        if (-not (Install-Istio)) {
            Write-Error "Failed to install Istio. Deployment may continue without service mesh features."
        } else {
            # Install Istio addons
            if (-not (Install-IstioAddons)) {
                Write-Warning "Failed to install some Istio addons. Continuing with main deployment..."
            }
        }
    } elseif (-not $SkipIstio) {
        Write-Success "Istio already installed. Ensuring observability addons are deployed..."
        Install-IstioAddons
    }
    
    # Create namespace and enable sidecar injection
    Write-Status "Creating credittransfer namespace..."
    kubectl apply -f k8s-manifests/namespace.yaml
    
    # Apply main application manifests first
    Write-Status "Applying main Kubernetes manifests..."
    kubectl apply -f k8s-manifests/credittransfer-config.yaml
    kubectl apply -f k8s-manifests/credittransfer-api.yaml
    kubectl apply -f k8s-manifests/credittransfer-wcf.yaml
    kubectl apply -f k8s-manifests/keycloak.yaml
    kubectl apply -f k8s-manifests/monitoring.yaml
    kubectl apply -f k8s-manifests/nobill-integration.yaml
    kubectl apply -f k8s-manifests/opentelemetry.yaml
    kubectl apply -f k8s-manifests/postgres.yaml
    kubectl apply -f k8s-manifests/redis.yaml
    
    # Handle force image pull if requested
    if ($ForcePullImages) {
        Write-Status "Force pull images requested - updating image pull policy and restarting services..."
        if (Force-ImagePull -ServiceName "all") {
            Write-Success "âœ… Successfully updated images with latest versions"
        } else {
            Write-Warning "âš ï¸ Image pull completed with some warnings"
        }
    }
    
    # Wait for core services to be ready before applying Istio resources
    Write-Status "Waiting for core services to be ready..."
    
    Write-Progress "Waiting for Keycloak..."
    kubectl wait --for=condition=ready pod -l app=keycloak -n credittransfer --timeout=300s
    
    Write-Progress "Waiting for API service..."
    kubectl wait --for=condition=ready pod -l app=credittransfer-api -n credittransfer --timeout=300s
    
    Write-Progress "Waiting for WCF service..."
    kubectl wait --for=condition=ready pod -l app=credittransfer-wcf -n credittransfer --timeout=300s
    
    # Apply Istio-specific resources after main services are running
    if (-not $SkipIstio -and (Test-IstioInstalled)) {
        Write-Status "Applying Istio service mesh resources..."
        
        # Enable sidecar injection and restart deployments
        if (Enable-IstioSidecarInjection) {
            Write-Success "âœ… Istio sidecar injection enabled and deployments restarted"
        } else {
            Write-Warning "âš ï¸ Issues with sidecar injection - service mesh features may be limited"
        }
        
        # Apply Istio networking resources
        Apply-IstioResources
        
        # Verify sidecar injection worked
        Start-Sleep -Seconds 30  # Give pods time to fully restart
        Verify-IstioSidecarInjection
    }
    
    # Wait for observability components
    Write-Progress "Waiting for observability components..."
    
    kubectl wait --for=condition=ready pod -l app=jaeger -n credittransfer --timeout=180s
    kubectl wait --for=condition=ready pod -l app=prometheus -n credittransfer --timeout=180s
    kubectl wait --for=condition=ready pod -l app=grafana -n credittransfer --timeout=180s
    
    Write-Success "âœ… Deployment completed successfully!"
    if ($ForcePullImages) {
        Write-Success "âœ… All services updated with latest Docker images!"
    }
    if (-not $SkipIstio -and (Test-IstioInstalled)) {
        Write-Success "âœ… Istio service mesh is active with sidecar injection enabled!"
    }
    Write-Host ""
    Show-ObservabilityStatus
}

function Deploy-IstioOnly {
    Write-Status "Installing Istio Service Mesh and Observability Stack..."
    
    if (Test-IstioInstalled) {
        Write-Success "Istio already installed. Installing/updating observability addons..."
    } else {
        if (-not (Install-Istio)) {
            Write-Error "Failed to install Istio"
            return
        }
    }
    
    if (-not (Install-IstioAddons)) {
        Write-Error "Failed to install Istio addons"
        return
    }
    
    # Enable sidecar injection for credittransfer namespace if it exists
    $namespaceExists = kubectl get namespace credittransfer 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Enabling Istio sidecar injection for credittransfer namespace..."
        if (Enable-IstioSidecarInjection) {
            Write-Success "âœ… Istio sidecar injection enabled and services restarted"
        }
        
        # Apply Istio networking resources
        Apply-IstioResources
        
        # Verify sidecar injection
        Start-Sleep -Seconds 30
        Verify-IstioSidecarInjection
    } else {
        Write-Status "credittransfer namespace not found - sidecar injection will be enabled during deployment"
        kubectl label namespace credittransfer istio-injection=enabled --overwrite 2>$null
    }
    
    Write-Success "âœ… Istio service mesh and observability stack deployed successfully!"
    Write-Host ""
    Show-ObservabilityStatus
}

function Show-KubernetesStatus {
    Write-Host ""
    Write-Host "ðŸ"Š Kubernetes & Istio Diagnostics:" -ForegroundColor $Colors.Yellow
    
    # Check Docker Desktop status
    if (Test-DockerDesktopRunning) {
        Write-Host "  âœ… Docker Desktop: Running" -ForegroundColor $Colors.Green
    } else {
        Write-Host "  âŒ Docker Desktop: Not Running" -ForegroundColor $Colors.Red
    }
    
    # Check available contexts
    Write-Host "  ðŸ"‹ Available kubectl contexts:" -ForegroundColor $Colors.Blue
    $contexts = kubectl config get-contexts 2>$null
    if ($contexts) {
        $contexts | ForEach-Object { Write-Host "    $_" -ForegroundColor $Colors.White }
    } else {
        Write-Host "    No contexts found" -ForegroundColor $Colors.Red
    }
    
    # Check current context
    $currentContext = kubectl config current-context 2>$null
    if ($currentContext) {
        Write-Host "  ðŸŽ¯ Current context: $currentContext" -ForegroundColor $Colors.Blue
    } else {
        Write-Host "  ðŸŽ¯ Current context: None" -ForegroundColor $Colors.Red
    }
    
    # Test cluster connectivity
    Write-Host "  ðŸ"— Cluster connectivity:" -ForegroundColor $Colors.Blue
    $clusterTest = kubectl cluster-info 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    âœ… Cluster is accessible" -ForegroundColor $Colors.Green
    } else {
        Write-Host "    âŒ Cannot connect to cluster" -ForegroundColor $Colors.Red
    }
    
    # Check Istio status
    Write-Host "  ðŸ•¸ï¸ Istio Service Mesh:" -ForegroundColor $Colors.Blue
    if (Test-IstioInstalled) {
        Write-Host "    âœ… Istio control plane: Installed" -ForegroundColor $Colors.Green
        
        # Check Istio version
        $localIstioctl = Join-Path $PSScriptRoot "istio-1.26.2\bin\istioctl.exe"
        if (Test-Path $localIstioctl) {
            $istioVersion = & $localIstioctl version --output json 2>$null | ConvertFrom-Json
            if ($istioVersion) {
                Write-Host "    ðŸ"‹ Client version: $($istioVersion.clientVersion.version)" -ForegroundColor $Colors.White
                if ($istioVersion.meshVersion) {
                    Write-Host "    ðŸ"‹ Mesh version: $($istioVersion.meshVersion[0].Info.version)" -ForegroundColor $Colors.White
                }
            }
        }
        
        # Check sidecar injection
        $injectionStatus = kubectl get namespace credittransfer -o jsonpath='{.metadata.labels.istio-injection}' 2>$null
        if ($injectionStatus -eq "enabled") {
            Write-Host "    âœ… Sidecar injection: Enabled for credittransfer namespace" -ForegroundColor $Colors.Green
        } else {
            Write-Host "    âš ï¸ Sidecar injection: Not enabled for credittransfer namespace" -ForegroundColor $Colors.Yellow
        }
    } else {
        Write-Host "    âŒ Istio control plane: Not installed" -ForegroundColor $Colors.Red
    }
    
    Write-Host ""
}

function Start-PortForward {
    param([string]$ServiceName)
    
    switch ($ServiceName) {
        "api" {
            Write-Status "Port forwarding API service to localhost:6000..."
            kubectl port-forward -n credittransfer svc/credittransfer-api 6000:80
        }
        "wcf" {
            Write-Status "Port forwarding WCF service to localhost:6001..."
            kubectl port-forward -n credittransfer svc/credittransfer-wcf 6001:80
        }
        "keycloak" {
            Write-Status "Port forwarding Keycloak to localhost:6002..."
            kubectl port-forward -n credittransfer svc/keycloak 6002:8080
        }
        "prometheus" {
            # Check which namespace Prometheus is in
            $prometheusNs = "credittransfer"
            $prometheusSvc = kubectl get svc prometheus -n istio-system --no-headers 2>$null
            if ($prometheusSvc) {
                $prometheusNs = "istio-system"
            }
            Write-Status "Port forwarding Prometheus from namespace: $prometheusNs"
            Write-Status "Trying port 9093 to avoid conflicts with 9090/9092..."
            kubectl port-forward -n $prometheusNs svc/prometheus 9093:9090
        }
        "grafana" {
            # Check which namespace Grafana is in
            $grafanaNs = "credittransfer"
            $grafanaPod = kubectl get pods -n istio-system -l app=grafana --no-headers 2>$null
            if ($grafanaPod) {
                $grafanaNs = "istio-system"
            }
            Write-Status "Port forwarding Grafana to localhost:3000..."
            kubectl port-forward -n $grafanaNs svc/grafana 3000:3000
        }
        "jaeger" {
            # Check which namespace Jaeger is in and what service name to use
            $jaegerNs = "credittransfer"
            $jaegerSvcName = "jaeger"
            
            # Check if jaeger service exists in credittransfer namespace
            $jaegerSvc = kubectl get svc jaeger -n credittransfer --no-headers 2>$null
            if (-not $jaegerSvc) {
                # Check istio-system namespace for jaeger-collector
                $jaegerCollectorSvc = kubectl get svc jaeger-collector -n istio-system --no-headers 2>$null
                if ($jaegerCollectorSvc) {
                    $jaegerNs = "istio-system"
                    $jaegerSvcName = "jaeger-collector"
                    Write-Status "Using jaeger-collector service in istio-system namespace"
                }
            }
            
            Write-Status "Port forwarding Jaeger from namespace: $jaegerNs, service: $jaegerSvcName"
            kubectl port-forward -n $jaegerNs svc/$jaegerSvcName 16686:16686
        }
        "kiali" {
            Write-Status "Port forwarding Kiali to localhost:20001..."
            kubectl port-forward -n istio-system svc/kiali 20001:20001
        }
        "otel" {
            Write-Status "Port forwarding OpenTelemetry Collector to localhost:4318..."
            kubectl port-forward -n credittransfer svc/otel-collector 4318:4318
        }
        "redis" {
            Write-Status "Port forwarding Redis to localhost:6379..."
            kubectl port-forward -n credittransfer svc/redis 6379:6379
        }
        "observability" {
            Write-Status "Starting all observability tool port forwards in background..."
            
            # Determine correct namespaces and service names
            $jaegerNs = "credittransfer"
            $jaegerSvcName = "jaeger"
            $jaegerSvc = kubectl get svc jaeger -n credittransfer --no-headers 2>$null
            if (-not $jaegerSvc) {
                $jaegerCollectorSvc = kubectl get svc jaeger-collector -n istio-system --no-headers 2>$null
                if ($jaegerCollectorSvc) {
                    $jaegerNs = "istio-system"
                    $jaegerSvcName = "jaeger-collector"
                }
            }
            
            $prometheusNs = "credittransfer"
            $prometheusSvc = kubectl get svc prometheus -n istio-system --no-headers 2>$null
            if ($prometheusSvc) { $prometheusNs = "istio-system" }
            
            $grafanaNs = "credittransfer"
            $grafanaPod = kubectl get pods -n istio-system -l app=grafana --no-headers 2>$null
            if ($grafanaPod) { $grafanaNs = "istio-system" }
            
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n istio-system svc/kiali 20001:20001" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n $jaegerNs svc/$jaegerSvcName 16686:16686" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n $prometheusNs svc/prometheus 9093:9090" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n $grafanaNs svc/grafana 3000:3000" -WindowStyle Minimized
            Write-Success "All observability port forwards started in background windows"
            Write-Host ""
            Write-Host "Observability Access URLs:" -ForegroundColor $Colors.Yellow
            Write-Host "  ðŸ•¸ï¸ Kiali (Service Mesh): http://localhost:20001/kiali" -ForegroundColor $Colors.White
            Write-Host "  ðŸ" Jaeger (Tracing): http://localhost:16686" -ForegroundColor $Colors.White
            Write-Host "  ðŸ"Š Prometheus (Metrics): http://localhost:9093" -ForegroundColor $Colors.White
            Write-Host "  ðŸ"ˆ Grafana (Dashboards): http://localhost:3000" -ForegroundColor $Colors.White
        }
        "all" {
            Write-Status "Starting all port forwards in background..."
            
            # Determine correct namespaces and service names for observability tools
            $jaegerNs = "credittransfer"
            $jaegerSvcName = "jaeger"
            $jaegerSvc = kubectl get svc jaeger -n credittransfer --no-headers 2>$null
            if (-not $jaegerSvc) {
                $jaegerCollectorSvc = kubectl get svc jaeger-collector -n istio-system --no-headers 2>$null
                if ($jaegerCollectorSvc) {
                    $jaegerNs = "istio-system"
                    $jaegerSvcName = "jaeger-collector"
                }
            }
            
            $prometheusNs = "credittransfer"
            $prometheusSvc = kubectl get svc prometheus -n istio-system --no-headers 2>$null
            if ($prometheusSvc) { $prometheusNs = "istio-system" }
            
            $grafanaNs = "credittransfer"
            $grafanaPod = kubectl get pods -n istio-system -l app=grafana --no-headers 2>$null
            if ($grafanaPod) { $grafanaNs = "istio-system" }
            
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n credittransfer svc/credittransfer-api 6000:80" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n credittransfer svc/credittransfer-wcf 6001:80" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n credittransfer svc/keycloak 6002:8080" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n istio-system svc/kiali 20001:20001" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n $jaegerNs svc/$jaegerSvcName 16686:16686" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n $prometheusNs svc/prometheus 9093:9090" -WindowStyle Minimized
            Start-Process powershell -ArgumentList "-Command", "kubectl port-forward -n $grafanaNs svc/grafana 3000:3000" -WindowStyle Minimized
            Write-Success "All port forwards started in background windows"
            Write-Host ""
            Write-Host "Application Access URLs:" -ForegroundColor $Colors.Yellow
            Write-Host "  ðŸŒ API: http://localhost:6000/health" -ForegroundColor $Colors.White
            Write-Host "  ðŸ"§ WCF: http://localhost:6001/health" -ForegroundColor $Colors.White
            Write-Host "  ðŸ" Keycloak: http://localhost:6002/admin" -ForegroundColor $Colors.White
            Write-Host ""
            Write-Host "Observability Access URLs:" -ForegroundColor $Colors.Yellow
            Write-Host "  ðŸ•¸ï¸ Kiali (Service Mesh): http://localhost:20001/kiali" -ForegroundColor $Colors.White
            Write-Host "  ðŸ" Jaeger (Tracing): http://localhost:16686" -ForegroundColor $Colors.White
            Write-Host "  ðŸ"Š Prometheus (Metrics): http://localhost:9093" -ForegroundColor $Colors.White
            Write-Host "  ðŸ"ˆ Grafana (Dashboards): http://localhost:3000" -ForegroundColor $Colors.White
        }
        default {
            Write-Error "Unknown service: $ServiceName"
            Write-Host "Available services: api, wcf, keycloak, prometheus, grafana, jaeger, kiali, otel, redis, observability, all" -ForegroundColor $Colors.Yellow
        }
    }
}

function Restart-Service {
    param([string]$ServiceName)
    
    if ($ForcePullImages) {
        Write-Status "Restarting services with forced image pull..."
        if (Force-ImagePull -ServiceName $ServiceName) {
            Write-Success "âœ… Successfully restarted services with latest images"
        } else {
            Write-Warning "âš ï¸ Restart completed with some warnings"
        }
    } else {
        if ($ServiceName -eq "all") {
            Write-Status "Restarting all services..."
            kubectl rollout restart deployment -n credittransfer
        } else {
            Write-Status "Restarting $ServiceName..."
            kubectl rollout restart deployment $ServiceName -n credittransfer
        }
    }
}

function Show-Logs {
    param([string]$ServiceName)
    
    if ($ServiceName -eq "all") {
        Write-Status "Showing logs for all services..."
        kubectl logs -n credittransfer --tail=50 -l app=credittransfer-api
        kubectl logs -n credittransfer --tail=50 -l app=credittransfer-wcf
    } else {
        Write-Status "Showing logs for $ServiceName..."
        kubectl logs -n credittransfer --tail=100 -l app=$ServiceName
    }
}

function Run-Tests {
    Write-Status "Running service tests..."
    
    # Test API health
    Write-Status "Testing API health endpoint..."
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:6000/health" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Success "API health check passed"
        }
    } catch {
        Write-Error "API health check failed: $($_.Exception.Message)"
    }
    
    # Test WCF health
    Write-Status "Testing WCF health endpoint..."
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:6001/health" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Success "WCF health check passed"
        }
    } catch {
        Write-Error "WCF health check failed: $($_.Exception.Message)"
    }
    
    # Test Keycloak token generation
    Write-Status "Testing Keycloak authentication..."
    try {
        if ($ConvertToPublic) {
            # Test public client authentication (no client_secret)
            $body = "grant_type=password&client_id=credittransfer-api&username=admin&password=admin123"
            Write-Status "Testing public client authentication (no client_secret)..."
        } else {
            # Test confidential client authentication (with client_secret)
            $body = "grant_type=password&client_id=credittransfer-api&client_secret=credittransfer-secret-2024&username=admin&password=admin123"
            Write-Status "Testing confidential client authentication (with client_secret)..."
        }
        
        $response = Invoke-RestMethod -Uri "http://localhost:6002/realms/credittransfer/protocol/openid-connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body $body
        if ($response.access_token) {
            Write-Success "Keycloak authentication test passed"
        }
    } catch {
        Write-Error "Keycloak authentication test failed: $($_.Exception.Message)"
        if ($ConvertToPublic) {
            Write-Warning "If this fails, the client may not be converted to public yet. Try running setup-keycloak first."
        }
    }
    
    # Test observability endpoints
    Write-Status "Testing observability endpoints..."
    
    # Test Kiali
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:20001/kiali/api/status" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Success "Kiali health check passed"
        }
    } catch {
        Write-Warning "Kiali health check failed (may not be port-forwarded): $($_.Exception.Message)"
    }
    
    # Test Jaeger
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:16686/api/services" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Success "Jaeger health check passed"
        }
    } catch {
        Write-Warning "Jaeger health check failed (may not be port-forwarded): $($_.Exception.Message)"
    }
}

function Remove-Deployment {
    Write-Warning "This will delete all Credit Transfer resources from Kubernetes!"
    $confirm = Read-Host "Are you sure? (y/N)"
    if ($confirm -eq "y" -or $confirm -eq "Y") {
        Write-Status "Cleaning up deployment..."
        kubectl delete namespace credittransfer
        
        # Also offer to clean up Istio
        Write-Warning "Do you also want to uninstall Istio service mesh? (y/N)"
        $istioConfirm = Read-Host
        if ($istioConfirm -eq "y" -or $istioConfirm -eq "Y") {
            $localIstioctl = Join-Path $PSScriptRoot "istio-1.26.2\bin\istioctl.exe"
            if (Test-Path $localIstioctl) {
                Write-Status "Uninstalling Istio..."
                & $localIstioctl uninstall --purge -y
                kubectl delete namespace istio-system
                Write-Success "Istio uninstalled"
            }
        }
        
        Write-Success "Deployment cleaned up"
    } else {
        Write-Status "Cleanup cancelled"
    }
}

function Setup-Keycloak {
    Write-Status "Running Keycloak setup..."
    
    if ($ConvertToPublic) {
        Write-Status "Setting up Keycloak with public client conversion..."
        .\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://localhost:6002" -ConvertToPublic
    } else {
        Write-Status "Setting up Keycloak with standard configuration..."
        .\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://localhost:6002"
    }
}

function Clear-RedisCache {
    param(
        [string]$ClearType = "app" # app, db, all
    )
    
    Write-Status "Clearing Redis cache..."
    
    # Check if Redis is running
    $redisPod = kubectl get pods -n credittransfer -l app=redis --no-headers 2>$null
    if (-not ($redisPod -and $redisPod -match "Running")) {
        Write-Error "Redis pod is not running. Please check your deployment."
        return $false
    }
    
    switch ($ClearType.ToLower()) {
        "app" {
            Write-Status "Clearing application cache keys (CreditTransfer:*)..."
            $result = kubectl exec -n credittransfer deployment/redis -- redis-cli -a "CreditTransfer2024!" --eval "return redis.call('del', unpack(redis.call('keys', 'CreditTransfer:*')))" 0 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "âœ… Application cache keys cleared successfully"
                Write-Status "Cleared keys matching pattern: CreditTransfer:*"
            } else {
                Write-Warning "âš ï¸ No application cache keys found to clear"
            }
        }
        "db" {
            Write-Status "Clearing database 0 (all keys in current database)..."
            kubectl exec -n credittransfer deployment/redis -- redis-cli -a "CreditTransfer2024!" FLUSHDB
            if ($LASTEXITCODE -eq 0) {
                Write-Success "âœ… Database 0 cleared successfully"
            } else {
                Write-Error "âŒ Failed to clear database 0"
                return $false
            }
        }
        "all" {
            Write-Warning "This will clear ALL Redis data across all databases!"
            $confirm = Read-Host "Are you sure? (y/N)"
            if ($confirm -eq "y" -or $confirm -eq "Y") {
                Write-Status "Clearing all Redis data..."
                kubectl exec -n credittransfer deployment/redis -- redis-cli -a "CreditTransfer2024!" FLUSHALL
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "âœ… All Redis data cleared successfully"
                } else {
                    Write-Error "âŒ Failed to clear all Redis data"
                    return $false
                }
            } else {
                Write-Status "Cache clear cancelled"
                return $true
            }
        }
        default {
            Write-Error "Unknown clear type: $ClearType"
            Write-Host "Available options: app (default), db, all" -ForegroundColor $Colors.Yellow
            return $false
        }
    }
    
    # Show cache status after clearing
    Write-Status "Current Redis status:"
    $dbSize = kubectl exec -n credittransfer deployment/redis -- redis-cli -a "CreditTransfer2024!" DBSIZE 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Database 0 contains $dbSize keys" -ForegroundColor $Colors.White
    }
    
    return $true
}

# Main execution
Write-Host ""
Write-Host "ðŸš€ Credit Transfer System - Deployment Management" -ForegroundColor $Colors.Green
Write-Host "Enhanced with Istio Service Mesh `& Comprehensive Observability" -ForegroundColor $Colors.Green
Write-Host "=============================================================" -ForegroundColor $Colors.Green
Write-Host ""
Write-Host "Usage: .\manage-deployment.ps1 -Action `<action`> [-Service `<service`>] [-SkipKubernetesCheck] [-ConvertToPublic] [-SkipIstio] [-ForcePullImages]" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "Actions:" -ForegroundColor $Colors.White
Write-Host "  status         - Show deployment status (default)" -ForegroundColor $Colors.White
Write-Host "  deploy         - Deploy all services with Istio and observability" -ForegroundColor $Colors.White
Write-Host "  deploy-istio   - Install Istio service mesh and observability addons" -ForegroundColor $Colors.White
Write-Host "  restart        - Restart services" -ForegroundColor $Colors.White
Write-Host "  logs           - Show service logs" -ForegroundColor $Colors.White
Write-Host "  port-forward   - Set up port forwarding (includes observability tools)" -ForegroundColor $Colors.White
Write-Host "  test           - Run service tests" -ForegroundColor $Colors.White
Write-Host "  cleanup        - Clean up deployment" -ForegroundColor $Colors.White
Write-Host "  setup-keycloak - Run Keycloak setup (use -ConvertToPublic for public client)" -ForegroundColor $Colors.White
Write-Host "  setup-k8s      - Setup Docker Desktop Kubernetes" -ForegroundColor $Colors.White
Write-Host "  diagnose       - Show detailed Kubernetes and Istio diagnostics" -ForegroundColor $Colors.White
Write-Host "  observability  - Show observability stack status and URLs" -ForegroundColor $Colors.White
Write-Host "  clear-cache    - Clear Redis cache (app keys only, db, or all)" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "Services:" -ForegroundColor $Colors.White
Write-Host "  all            - All services (default)" -ForegroundColor $Colors.White
Write-Host "  api            - Credit Transfer API" -ForegroundColor $Colors.White
Write-Host "  wcf            - Credit Transfer WCF" -ForegroundColor $Colors.White
Write-Host "  keycloak       - Keycloak authentication" -ForegroundColor $Colors.White
Write-Host "  prometheus     - Prometheus metrics" -ForegroundColor $Colors.White
Write-Host "  grafana        - Grafana monitoring" -ForegroundColor $Colors.White
Write-Host "  jaeger         - Jaeger tracing" -ForegroundColor $Colors.White
Write-Host "  kiali          - Kiali service mesh visualization" -ForegroundColor $Colors.White
Write-Host "  otel           - OpenTelemetry collector" -ForegroundColor $Colors.White
Write-Host "  redis          - Redis cache server" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "Switches:" -ForegroundColor $Colors.White
Write-Host "  -SkipKubernetesCheck - Skip automatic Kubernetes setup" -ForegroundColor $Colors.White
Write-Host "  -ConvertToPublic     - Convert Keycloak client to public (no client_secret)" -ForegroundColor $Colors.White
Write-Host "  -SkipIstio          - Skip Istio installation (for quick operations)" -ForegroundColor $Colors.White
Write-Host "  -ForcePullImages    - Force pull latest Docker images (changes imagePullPolicy to Always)" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "Examples:" -ForegroundColor $Colors.Yellow
Write-Host "  .\manage-deployment.ps1 -Action status" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action deploy" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action deploy -ForcePullImages" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action restart -ForcePullImages" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action restart -Service api -ForcePullImages" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action deploy-istio" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action observability" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action port-forward -Service kiali" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action setup-k8s" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action setup-keycloak" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action setup-keycloak -ConvertToPublic" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action diagnose" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action clear-cache" -ForegroundColor $Colors.White
Write-Host "  .\manage-deployment.ps1 -Action clear-cache -Service all" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "ðŸŽ¯ Observability Features:" -ForegroundColor $Colors.Green
Write-Host "  â€¢ Istio Service Mesh with automatic sidecar injection" -ForegroundColor $Colors.White
Write-Host "  â€¢ Kiali for service mesh topology and health visualization" -ForegroundColor $Colors.White
Write-Host "  â€¢ OpenTelemetry for distributed tracing and metrics collection" -ForegroundColor $Colors.White
Write-Host "  â€¢ Jaeger for end-to-end distributed tracing" -ForegroundColor $Colors.White
Write-Host "  â€¢ Prometheus for metrics collection and alerting" -ForegroundColor $Colors.White
Write-Host "  â€¢ Grafana for metrics visualization and dashboards" -ForegroundColor $Colors.White
Write-Host "  â€¢ Request tracing across all microservices" -ForegroundColor $Colors.White
Write-Host "  â€¢ Traffic management with circuit breakers and retries" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "ðŸ"§ Automatic Features:" -ForegroundColor $Colors.Green
Write-Host "  â€¢ Automatically installs Istio service mesh" -ForegroundColor $Colors.White
Write-Host "  â€¢ Deploys comprehensive observability stack" -ForegroundColor $Colors.White
Write-Host "  â€¢ Enables automatic sidecar injection" -ForegroundColor $Colors.White
Write-Host "  â€¢ Sets up traffic management and fault injection" -ForegroundColor $Colors.White
Write-Host "  â€¢ Configures distributed tracing with 100% sampling" -ForegroundColor $Colors.White
Write-Host "  â€¢ Enhanced diagnostics for service mesh issues" -ForegroundColor $Colors.White
Write-Host "  â€¢ Smart image pulling with policy management" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "ðŸ–¼ï¸ Image Management:" -ForegroundColor $Colors.Green
Write-Host "  â€¢ -ForcePullImages changes imagePullPolicy from 'Never' to 'Always'" -ForegroundColor $Colors.White
Write-Host "  â€¢ Forces Kubernetes to pull latest Docker images from registry" -ForegroundColor $Colors.White
Write-Host "  â€¢ Automatically restarts deployments to use new images" -ForegroundColor $Colors.White
Write-Host "  â€¢ Works with both 'deploy' and 'restart' actions" -ForegroundColor $Colors.White
Write-Host "  â€¢ Supports individual service updates (-Service api/wcf)" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "ðŸ—„ï¸ Cache Management:" -ForegroundColor $Colors.Green
Write-Host "  â€¢ clear-cache (default): Clear only CreditTransfer:* application keys" -ForegroundColor $Colors.White
Write-Host "  â€¢ clear-cache -Service db: Clear entire database 0" -ForegroundColor $Colors.White
Write-Host "  â€¢ clear-cache -Service all: Clear all Redis databases (requires confirmation)" -ForegroundColor $Colors.White
Write-Host "  â€¢ Automatic Redis health check before clearing cache" -ForegroundColor $Colors.White
Write-Host "  â€¢ Shows remaining key count after cache operations" -ForegroundColor $Colors.White
Write-Host ""
Write-Host "ðŸ'¡ Troubleshooting:" -ForegroundColor $Colors.Yellow
Write-Host "  â€¢ If deployment fails: .\manage-deployment.ps1 -Action diagnose" -ForegroundColor $Colors.White
Write-Host "  â€¢ For quick operations: add -SkipKubernetesCheck -SkipIstio" -ForegroundColor $Colors.White
Write-Host "  â€¢ Check service mesh: .\manage-deployment.ps1 -Action observability" -ForegroundColor $Colors.White
Write-Host "  â€¢ Access Kiali: .\manage-deployment.ps1 -Action port-forward -Service kiali" -ForegroundColor $Colors.White
Write-Host ""

# Skip Kubernetes check for help action or if explicitly requested
if ($Action.ToLower() -ne "help" -and -not $SkipKubernetesCheck) {
    Write-Host ""
    if (-not (Ensure-KubernetesRunning)) {
        Write-Error "Failed to setup Kubernetes. Use '-SkipKubernetesCheck' to bypass this check."
        exit 1
    }
    Write-Host ""
}

switch ($Action.ToLower()) {
    "help" { Show-Help }
    "status" { Show-Status }
    "deploy" { Start-Deployment }
    "deploy-istio" { Deploy-IstioOnly }
    "deploy-otel" { Install-OpenTelemetryOperator }
    "observability" { Show-ObservabilityStatus }
    "restart" { Restart-Service -ServiceName $Service }
    "logs" { Show-Logs -ServiceName $Service }
    "port-forward" { Start-PortForward -ServiceName $Service }
    "test" { Run-Tests }
    "cleanup" { Remove-Deployment }
    "setup-keycloak" { Setup-Keycloak }
    "clear-cache" { 
        $cacheType = if ($Service -eq "all" -and $PSBoundParameters.ContainsKey("Service") -eq $false) { "app" } else { $Service }
        Clear-RedisCache -ClearType $cacheType 
    }
    "setup-k8s" { 
        if (Ensure-KubernetesRunning) {
            Write-Success "Docker Desktop Kubernetes setup completed successfully!"
        } else {
            Write-Error "Failed to setup Docker Desktop Kubernetes"
        }
    }
    "diagnose" {
        Write-Host ""
        Write-Host "ðŸ"Š Comprehensive Kubernetes & Istio Diagnostic Report" -ForegroundColor $Colors.Green
        Write-Host "====================================================" -ForegroundColor $Colors.Green
        Show-KubernetesStatus
        
        Write-Host "ðŸ›ï¸ Manual Commands to Try:" -ForegroundColor $Colors.Yellow
        Write-Host "  # Kubernetes" -ForegroundColor $Colors.White
        Write-Host "  docker info" -ForegroundColor $Colors.White
        Write-Host "  kubectl config get-contexts" -ForegroundColor $Colors.White
        Write-Host "  kubectl config use-context docker-desktop" -ForegroundColor $Colors.White
        Write-Host "  kubectl cluster-info" -ForegroundColor $Colors.White
        Write-Host "  kubectl get nodes" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "  # Istio Service Mesh" -ForegroundColor $Colors.White
        Write-Host "  istio-1.26.2\bin\istioctl.exe version" -ForegroundColor $Colors.White
        Write-Host "  istio-1.26.2\bin\istioctl.exe proxy-status" -ForegroundColor $Colors.White
        Write-Host "  kubectl get pods -n istio-system" -ForegroundColor $Colors.White
        Write-Host "  kubectl get gateway,virtualservice,destinationrule -n credittransfer" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "  # Observability" -ForegroundColor $Colors.White
        Write-Host "  kubectl get pods -n istio-system -l app=kiali" -ForegroundColor $Colors.White
        Write-Host "  kubectl get pods -n credittransfer -l app=jaeger" -ForegroundColor $Colors.White
        Write-Host "  kubectl logs -n credittransfer deployment/otel-collector" -ForegroundColor $Colors.White
        Write-Host ""
    }
    default {
        Write-Error "Unknown action: $Action"
        Show-Help
    }
} 
