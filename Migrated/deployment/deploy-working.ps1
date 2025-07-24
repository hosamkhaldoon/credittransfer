# Deployment script for Credit Transfer System
param(
    [string]$Action = "deploy",
    [switch]$SkipIstio = $false
)

# Color definitions
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

function Test-KubernetesRunning {
    try {
        $clusterInfo = kubectl cluster-info *>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Kubernetes cluster is responsive"
            return $true
        }
        return $false
    }
    catch {
        return $false
    }
}

function Install-Istio {
    Write-Status "Setting up Istio..."
    
    # Check if istioctl already exists in PATH
    $istioctl = Get-Command istioctl -ErrorAction SilentlyContinue
    if ($istioctl) {
        Write-Status "istioctl already exists in PATH"
        return $true
    }
    
    # Check if Istio directory already exists
    $existingIstioDir = Get-ChildItem -Directory -Filter "istio-*" | Select-Object -First 1
    if ($existingIstioDir) {
        Write-Status "Istio directory already exists at $($existingIstioDir.FullName)"
        # Add istioctl to PATH
        $env:PATH = "$($existingIstioDir.FullName)\bin;$env:PATH"
        return $true
    }
    
    # Download and extract Istio if not found
    $istioVersion = "1.26.2"
    $downloadUrl = "https://github.com/istio/istio/releases/download/$istioVersion/istio-$istioVersion-win.zip"
    $outputFile = "istio.zip"
    
    Write-Status "Downloading Istio $istioVersion..."
    Invoke-WebRequest -Uri $downloadUrl -OutFile $outputFile
    
    Write-Status "Extracting Istio..."
    Expand-Archive -Path $outputFile -DestinationPath . -Force
    Remove-Item $outputFile
    
    # Find Istio directory
    $istioDir = Get-ChildItem -Directory -Filter "istio-*" | Select-Object -First 1
    if (-not $istioDir) {
        Write-Error "Istio directory not found!"
        return $false
    }
    
    # Add istioctl to PATH
    $env:PATH = "$($istioDir.FullName)\bin;$env:PATH"
    
    # Install Istio with demo profile
    Write-Status "Installing Istio with demo profile..."
    & "$($istioDir.FullName)\bin\istioctl.exe" install --set profile=demo -y
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install Istio"
        return $false
    }
    
    # Create istio-system namespace if it doesn't exist
    kubectl create namespace istio-system --dry-run=client -o yaml | kubectl apply -f -
    
    # Install Prometheus with proper configuration
    Write-Status "Installing Prometheus..."
    $prometheusYaml = @"
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus
  namespace: istio-system
  labels:
    app: prometheus
    release: istio
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s
    scrape_configs:
    - job_name: 'istio-mesh'
      kubernetes_sd_configs:
      - role: endpoints
        namespaces:
          names:
          - istio-system
      relabel_configs:
      - source_labels: [__meta_kubernetes_service_name, __meta_kubernetes_endpoint_port_name]
        action: keep
        regex: istio-telemetry;prometheus
    - job_name: 'kubernetes-pods'
      kubernetes_sd_configs:
      - role: pod
      relabel_configs:
      - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
        action: keep
        regex: true
      - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_path]
        action: replace
        target_label: __metrics_path__
        regex: (.+)
      - source_labels: [__address__, __meta_kubernetes_pod_annotation_prometheus_io_port]
        action: replace
        regex: ([^:]+)(?::\d+)?;(\d+)
        replacement: ${1}:${2}
        target_label: __address__
      - source_labels: [__meta_kubernetes_namespace]
        action: replace
        target_label: kubernetes_namespace
      - source_labels: [__meta_kubernetes_pod_name]
        action: replace
        target_label: kubernetes_pod_name
    - job_name: 'istio-policy'
      kubernetes_sd_configs:
      - role: endpoints
        namespaces:
          names:
          - istio-system
      relabel_configs:
      - source_labels: [__meta_kubernetes_service_name, __meta_kubernetes_endpoint_port_name]
        action: keep
        regex: istio-policy;http-policy-monitoring
    - job_name: 'istio-telemetry'
      kubernetes_sd_configs:
      - role: endpoints
        namespaces:
          names:
          - istio-system
      relabel_configs:
      - source_labels: [__meta_kubernetes_service_name, __meta_kubernetes_endpoint_port_name]
        action: keep
        regex: istio-telemetry;http-monitoring
    - job_name: 'pilot'
      kubernetes_sd_configs:
      - role: endpoints
        namespaces:
          names:
          - istio-system
      relabel_configs:
      - source_labels: [__meta_kubernetes_service_name, __meta_kubernetes_endpoint_port_name]
        action: keep
        regex: istiod;http-monitoring
"@
    $prometheusYaml | kubectl apply -f -

    # Create Prometheus deployment and service
    Write-Status "Creating Prometheus deployment and service..."
    $prometheusDeploymentYaml = @"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: prometheus
  namespace: istio-system
  labels:
    app: prometheus
spec:
  replicas: 1
  selector:
    matchLabels:
      app: prometheus
  template:
    metadata:
      labels:
        app: prometheus
    spec:
      containers:
      - name: prometheus
        image: prom/prometheus:v2.45.0
        args:
          - '--storage.tsdb.retention=6h'
          - '--config.file=/etc/prometheus/prometheus.yml'
          - '--web.enable-lifecycle'
        ports:
        - name: http
          containerPort: 9090
        readinessProbe:
          httpGet:
            path: /-/ready
            port: 9090
          initialDelaySeconds: 10
          periodSeconds: 5
        livenessProbe:
          httpGet:
            path: /-/healthy
            port: 9090
          initialDelaySeconds: 10
          periodSeconds: 5
        volumeMounts:
        - name: config-volume
          mountPath: /etc/prometheus
      volumes:
      - name: config-volume
        configMap:
          name: prometheus
---
apiVersion: v1
kind: Service
metadata:
  name: prometheus
  namespace: istio-system
  labels:
    app: prometheus
spec:
  type: NodePort
  ports:
  - name: http
    port: 9090
    targetPort: 9090
    nodePort: 30090
  selector:
    app: prometheus
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: prometheus
  namespace: istio-system
  labels:
    app: prometheus
    release: istio
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: prometheus
  labels:
    app: prometheus
    release: istio
rules:
- apiGroups: [""]
  resources:
  - nodes
  - services
  - endpoints
  - pods
  verbs: ["get", "list", "watch"]
- apiGroups: [""]
  resources:
  - configmaps
  verbs: ["get"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: prometheus
  labels:
    app: prometheus
    release: istio
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: prometheus
subjects:
- kind: ServiceAccount
  name: prometheus
  namespace: istio-system
"@
    $prometheusDeploymentYaml | kubectl apply -f -

    # Wait for Prometheus to be ready with simplified checks
    Write-Status "Waiting for Prometheus to be ready..."
    $retryCount = 0
    $maxRetries = 12
    do {
        $ready = $false
        $podStatus = kubectl get pods -n istio-system -l app=prometheus -o jsonpath='{.items[0].status.phase}' 2>&1
        $readyStatus = kubectl get pods -n istio-system -l app=prometheus -o jsonpath='{.items[0].status.conditions[?(@.type=="Ready")].status}' 2>&1
        
        if ($podStatus -eq "Running" -and $readyStatus -eq "True") {
            # Try to access Prometheus directly via NodePort
            try {
                $minikubeIP = kubectl get nodes -o jsonpath='{.items[0].status.addresses[?(@.type=="InternalIP")].address}' 2>&1
                $response = Invoke-WebRequest -Uri "http://${minikubeIP}:30090/-/ready" -TimeoutSec 5 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                    Write-Success "Prometheus is ready and responding at http://${minikubeIP}:30090"
                }
            }
            catch {
                Write-Warning "Prometheus API not yet accessible..."
            }
        }
        
        if (-not $ready) {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Status "Waiting for Prometheus to be ready (Attempt $retryCount of $maxRetries)..."
                Start-Sleep -Seconds 10
            }
        }
    } while (-not $ready -and $retryCount -lt $maxRetries)

    if (-not $ready) {
        Write-Warning "Prometheus is not ready after $maxRetries attempts. Check the logs with: kubectl logs -n istio-system -l app=prometheus"
        Write-Warning "You can still try accessing Prometheus at http://<minikube-ip>:30090"
    }
    
    # Install other Istio addons
    Write-Status "Installing other Istio addons..."
    kubectl apply -f "$($istioDir.FullName)\samples\addons\grafana.yaml"
    kubectl apply -f "$($istioDir.FullName)\samples\addons\jaeger.yaml"
    kubectl apply -f "$($istioDir.FullName)\samples\addons\kiali.yaml"
    
    # Verify monitoring stack
    Write-Status "Verifying monitoring stack..."
    $monitoringServices = @(
        @{ Name = "Prometheus"; Label = "app=prometheus" },
        @{ Name = "Grafana"; Label = "app=grafana" },
        @{ Name = "Jaeger"; Label = "app=jaeger" },
        @{ Name = "Kiali"; Label = "app=kiali" }
    )
    
    foreach ($service in $monitoringServices) {
        $retryCount = 0
        $maxRetries = 5
        $ready = $false
        
        do {
            $status = kubectl get pods -n istio-system -l $service.Label -o jsonpath='{.items[0].status.phase}' 2>&1
            if ($status -eq "Running") {
                $ready = $true
                Write-Success "$($service.Name) is ready"
            } else {
                $retryCount++
                if ($retryCount -lt $maxRetries) {
                    Write-Status "Waiting for $($service.Name) to be ready (Attempt $retryCount of $maxRetries)..."
                    Start-Sleep -Seconds 5
                }
            }
        } while (-not $ready -and $retryCount -lt $maxRetries)
        
        if (-not $ready) {
            Write-Warning "$($service.Name) is not ready after $maxRetries attempts"
        }
    }
    
    Write-Success "Istio setup completed successfully!"
    return $true
}

function Test-PortAvailable {
    param(
        [int]$Port
    )
    try {
        $endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Loopback, $Port)
        $socket = New-Object System.Net.Sockets.Socket(
            [System.Net.Sockets.AddressFamily]::InterNetwork,
            [System.Net.Sockets.SocketType]::Stream,
            [System.Net.Sockets.ProtocolType]::Tcp
        )
        $socket.Bind($endpoint)
        $socket.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Get-NextAvailablePort {
    param(
        [int]$StartPort,
        [int]$MaxPort = 65535
    )
    $port = $StartPort
    while (-not (Test-PortAvailable -Port $port) -and $port -lt $MaxPort) {
        $port++
    }
    if ($port -ge $MaxPort) {
        throw "No available ports found between $StartPort and $MaxPort"
    }
    return $port
}

function Wait-ForPod {
    param(
        [string]$Namespace,
        [string]$Label,
        [int]$TimeoutSeconds = 120
    )
    
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $ready = $false
    
    while (-not $ready -and $timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        # Try to get pod status with better error handling
        $pod = kubectl get pods -n $Namespace -l $Label -o jsonpath="{.items[0].status.phase}" 2>&1
        $containerReady = kubectl get pods -n $Namespace -l $Label -o jsonpath="{.items[0].status.containerStatuses[0].ready}" 2>&1
        $containerStatus = kubectl get pods -n $Namespace -l $Label -o jsonpath="{.items[0].status.containerStatuses[0].state}" 2>&1
        
        # Check if we got valid responses (not error messages)
        if ($pod -and $pod -ne "error:" -and $containerReady -and $containerReady -ne "error:") {
            if ($pod -eq "Running" -and $containerReady -eq "true") {
                $ready = $true
                Write-Success "[OK] Pod $Label is ready"
                return $true
            }
        }
        
        Write-Status "Waiting for pod $Label to be ready... ($([math]::Round($timer.Elapsed.TotalSeconds))s)"
        if ($pod -and $pod -ne "error:") {
            Write-Status "Pod Phase: $pod, Container Ready: $containerReady"
        } else {
            Write-Status "Pod not found or not accessible yet..."
        }
        Start-Sleep -Seconds 5
    }
    
    if (-not $ready) {
        Write-Warning "Pod $Label did not become ready within $TimeoutSeconds seconds"
        Write-Status "Pod status: $(kubectl get pods -n $Namespace -l $Label 2>&1)"
        return $false
    }
}

function Start-PortForward {
    param(
        [string]$Namespace,
        [string]$Service,
        [int]$LocalPort,
        [int]$RemotePort,
        [string]$Label,
        [int]$RetryCount = 5,
        [int]$RetryDelaySeconds = 10
    )
    
    # First check if pod is ready using the provided label
    if (-not (Wait-ForPod -Namespace $Namespace -Label $Label -TimeoutSeconds 120)) {
        Write-Error "Pod for service $Service is not ready"
        return $null
    }
    
    # Find an available port
    try {
        $actualPort = Get-NextAvailablePort -StartPort $LocalPort
        if ($actualPort -ne $LocalPort) {
            Write-Warning "Port $LocalPort is in use, using port $actualPort instead"
        }
    }
    catch {
        Write-Error "Failed to find available port: $_"
        return $null
    }
    
    # Try to start port forwarding
    for ($i = 1; $i -le $RetryCount; $i++) {
        Write-Status "Attempting port forward for $Service (Attempt $i of $RetryCount)..."
        
        # Kill any existing port-forward processes
        Get-Process | Where-Object { $_.CommandLine -like "*kubectl port-forward*" -and $_.CommandLine -like "*$actualPort*" } | Stop-Process -Force
        
        $job = Start-Job -ScriptBlock {
            param($ns, $svc, $local, $remote)
            kubectl port-forward -n $ns svc/$svc ${local}:${remote}
        } -ArgumentList $Namespace, $Service, $actualPort, $RemotePort
        
        # Wait for port forward to establish
        Start-Sleep -Seconds 5
        
        # Check if port forward is working
        try {
            $testConnection = Test-NetConnection -ComputerName localhost -Port $actualPort -WarningAction SilentlyContinue
            if ($testConnection.TcpTestSucceeded) {
                Write-Success "[OK] Port forward established for $Service on port $actualPort"
                return $actualPort
            }
        }
        catch {
            Write-Warning "Port forward test failed: $_"
        }
        
        # Clean up failed attempt
        Stop-Job $job -ErrorAction SilentlyContinue
        Remove-Job $job -ErrorAction SilentlyContinue
        
        if ($i -lt $RetryCount) {
            Write-Status "Retrying in $RetryDelaySeconds seconds..."
            Start-Sleep -Seconds $RetryDelaySeconds
        }
    }
    
    Write-Error "Failed to establish port forward for $Service after $RetryCount attempts"
    return $null
}

Write-Host ""
Write-Host "Credit Transfer System - Deployment" -ForegroundColor $Colors.Green
Write-Host "===================================" -ForegroundColor $Colors.Green
Write-Host ""

# Check Kubernetes connectivity
Write-Status "Checking Kubernetes connectivity..."
try {
    $nodes = kubectl get nodes --no-headers *>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Kubernetes is accessible"
        Write-Host "Available nodes:"
        $nodeOutput = $(kubectl get nodes)
        $nodeOutput | ForEach-Object { Write-Host $_ }
    }
    else {
        Write-Error "Kubernetes is not accessible"
        exit 1
    }
}
catch {
    Write-Error "Failed to check Kubernetes: $($_.Exception.Message)"
    exit 1
}

switch ($Action.ToLower()) {
    "deploy" {
        # Install Istio if not skipped
        if (-not $SkipIstio) {
            if (-not (Install-Istio)) {
                Write-Error "Failed to install Istio. Deployment aborted."
                exit 1
            }
            
            # Wait for Istio components to be ready
            Write-Status "Waiting for Istio components to be ready..."
            Start-Sleep -Seconds 30
        }
        
    Write-Status "Starting Credit Transfer System deployment..."
    
        # Create and label namespace
        Write-Status "Creating and configuring credittransfer namespace..."
        kubectl create namespace credittransfer --dry-run=client -o yaml | kubectl apply -f -
        kubectl label namespace credittransfer istio-injection=enabled --overwrite
        
        # Apply external services for SQL Server and NoBill integration
        Write-Status "Configuring external service integrations..."
        
        # Apply external SQL service configuration
        if (Test-Path "k8s-manifests/external-sql-service.yaml") {
            Write-Status "Applying external SQL Server service configuration..."
            kubectl apply -f k8s-manifests/external-sql-service.yaml
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Failed to apply external SQL service. Continuing with deployment..."
            } else {
                Write-Success "External SQL Server service configured successfully"
            }
        } else {
            Write-Warning "External SQL service configuration not found. You may need to configure SQL connectivity manually."
        }
    
    # Apply configurations
    Write-Status "Applying configurations..."
    kubectl apply -f k8s-manifests/credittransfer-config.yaml
    
    # Deploy databases
    Write-Status "Deploying databases..."
    kubectl apply -f k8s-manifests/postgres.yaml
    kubectl apply -f k8s-manifests/redis.yaml
    
    # Deploy authentication
    Write-Status "Deploying Keycloak..."
    kubectl apply -f k8s-manifests/keycloak.yaml
        
        # Deploy external proxy for NoBill integration (if exists)
        if (Test-Path "k8s-manifests/external-services.yaml") {
            Write-Status "Deploying external proxy for NoBill integration..."
            kubectl apply -f k8s-manifests/external-services.yaml
            if ($LASTEXITCODE -eq 0) {
                Write-Success "External proxy for NoBill service deployed"
            } else {
                Write-Warning "Failed to deploy external proxy. NoBill integration may not work."
            }
        }
    
    # Deploy main services
    Write-Status "Deploying Credit Transfer services..."
    kubectl apply -f k8s-manifests/credittransfer-api.yaml
    kubectl apply -f k8s-manifests/credittransfer-wcf.yaml
    
        # Deploy monitoring
    Write-Status "Deploying monitoring stack..."
    kubectl apply -f k8s-manifests/monitoring.yaml
    kubectl apply -f k8s-manifests/opentelemetry.yaml
    
    Write-Success "All manifests applied successfully!"
    
        # Wait for external integrations to be ready
        Write-Status "Waiting for external service integrations to be ready..."
        Start-Sleep -Seconds 10
        
        # Restart services to pick up external service configurations
        Write-Status "Restarting services to apply external service configurations..."
        kubectl rollout restart deployment credittransfer-api -n credittransfer
        kubectl rollout restart deployment credittransfer-wcf -n credittransfer
        
        # Wait for services to be ready
    Write-Status "Waiting for services to be ready..."
        Start-Sleep -Seconds 30
        
        # Check deployment status
        Write-Status "Checking deployment status..."
    kubectl get pods -n credittransfer
    
        # Test connectivity to external services
        Write-Status "Testing external service connectivity..."
        
        # Test SQL Server connectivity
        $sqlTest = kubectl exec -n credittransfer nettest -- nslookup external-sql-server-direct 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "[OK] SQL Server service is resolvable"
        } else {
            Write-Warning "[WARN] SQL Server service resolution failed. Check external-sql-service.yaml configuration."
        }
        
        # Test external proxy connectivity  
        $proxyTest = kubectl exec -n credittransfer nettest -- nslookup external-proxy 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "[OK] External proxy service is resolvable"
        } else {
            Write-Warning "[WARN] External proxy service resolution failed. NoBill integration may not work."
        }
        
        Write-Success "[OK] Deployment completed with external service integration!"
        Write-Host ""
        Write-Host "To verify system health:" -ForegroundColor $Colors.Yellow
        Write-Host "  kubectl port-forward -n credittransfer svc/credittransfer-api 5006:80" -ForegroundColor $Colors.White
        Write-Host "  curl http://localhost:5006/api/CreditTransfer/health/system" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "Expected healthy components:" -ForegroundColor $Colors.Yellow
        Write-Host "  - SQL_Server_Database: HEALTHY (external SQL Server integration)" -ForegroundColor $Colors.White
        Write-Host "  - NoBill_Service: HEALTHY (external NoBill integration)" -ForegroundColor $Colors.White
        Write-Host "  - Redis_Cache: HEALTHY" -ForegroundColor $Colors.White
        Write-Host "  - Configuration_System: HEALTHY" -ForegroundColor $Colors.White
    Write-Host ""
        Write-Host "For troubleshooting, see: TROUBLESHOOTING_GUIDE.md" -ForegroundColor $Colors.Yellow
    Write-Host ""
    Write-Host "To access services (once running):" -ForegroundColor $Colors.Yellow
    Write-Host "  kubectl port-forward -n credittransfer svc/credittransfer-api 5002:80" -ForegroundColor $Colors.White
    Write-Host "  kubectl port-forward -n credittransfer svc/credittransfer-wcf 5001:80" -ForegroundColor $Colors.White
        Write-Host "  kubectl port-forward -n credittransfer svc/keycloak 8080:8080" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "To verify Istio injection:" -ForegroundColor $Colors.Yellow
        Write-Host "  kubectl get namespace credittransfer --show-labels" -ForegroundColor $Colors.White
        Write-Host "  kubectl get pods -n credittransfer" -ForegroundColor $Colors.White
    }
    
    "status" {
    Write-Status "Checking deployment status..."
    Write-Host ""
        
        Write-Host "Istio system status:" -ForegroundColor $Colors.Yellow
        $istioStatus = $(kubectl get pods -n istio-system *>&1)
        if ($istioStatus) {
            $istioStatus | ForEach-Object { Write-Host $_ }
        }
        else {
            Write-Warning "Istio system not found"
        }
        Write-Host ""
        
    Write-Host "Namespace status:" -ForegroundColor $Colors.Yellow
        $ns = $(kubectl get namespace credittransfer --show-labels *>&1)
        if ($ns) {
            $ns | ForEach-Object { Write-Host $_ }
        }
        else {
            Write-Warning "Namespace 'credittransfer' not found"
        }
    Write-Host ""
        
    Write-Host "Pods status:" -ForegroundColor $Colors.Yellow
        $pods = $(kubectl get pods -n credittransfer *>&1)
        if ($pods) {
            $pods | ForEach-Object { Write-Host $_ }
        }
        else {
            Write-Warning "No pods found in namespace 'credittransfer'"
        }
    Write-Host ""
        
    Write-Host "Services status:" -ForegroundColor $Colors.Yellow
        $services = $(kubectl get services -n credittransfer --no-headers=true *>&1)
        if ($services) {
            kubectl get services -n credittransfer | Select-Object -First 1
            $services | ForEach-Object { Write-Host $_ }
        }
        else {
            Write-Warning "No services found in namespace 'credittransfer'"
        }
        Write-Host ""
    }
    
    "port-forward" {
        Write-Status "Starting port forwarding for all services..."
        Write-Host ""
        
        # Define service ports
        $services = @(
            @{
                Name = "Credit Transfer API"
                ServiceName = "credittransfer-api"
                LocalPort = 5002
                ContainerPort = 80
                HealthEndpoint = "/health"
                SwaggerEndpoint = "/swagger"
            },
            @{
                Name = "Credit Transfer WCF"
                ServiceName = "credittransfer-wcf"
                LocalPort = 5001
                ContainerPort = 80
                HealthEndpoint = "/health"
                WsdlEndpoint = "/CreditTransferService.svc?wsdl"
            },
            @{
                Name = "Keycloak"
                ServiceName = "keycloak"
                LocalPort = 8080
                ContainerPort = 8080
                AdminEndpoint = "/auth/admin"
                Credentials = "admin / admin123"
            }
        )
        
        foreach ($service in $services) {
            $actualPort = Start-PortForward -Namespace "credittransfer" -Service $service.ServiceName -LocalPort $service.LocalPort -RemotePort $service.ContainerPort -Label "app=$($service.ServiceName)"
            if ($actualPort) {
                # Store the actual port used (it might be different from LocalPort if that was in use)
                
                $service.ActualPort = $actualPort
            }
            else {
                Write-Warning "Failed to set up port forwarding for $($service.Name)"
                continue
            }
        }
        
        Write-Success "[OK] Port forwarding started for all services!"
        Write-Host ""
        Write-Host "Service URLs:" -ForegroundColor $Colors.Yellow
        Write-Host "Main Services:" -ForegroundColor $Colors.White
        
        foreach ($service in $services) {
            if ($service.ActualPort) {
                $baseUrl = "http://localhost:$($service.ActualPort)"
                
                Write-Host "  $($service.Name):" -ForegroundColor $Colors.White
                Write-Host "    Base URL: $baseUrl" -ForegroundColor $Colors.Blue
                
                if ($service.SwaggerEndpoint) {
                    Write-Host "    Swagger: $baseUrl$($service.SwaggerEndpoint)" -ForegroundColor $Colors.Blue
                }
                if ($service.WsdlEndpoint) {
                    Write-Host "    WSDL: $baseUrl$($service.WsdlEndpoint)" -ForegroundColor $Colors.Blue
                }
                if ($service.HealthEndpoint) {
                    Write-Host "    Health: $baseUrl$($service.HealthEndpoint)" -ForegroundColor $Colors.Blue
                }
                if ($service.AdminEndpoint) {
                    Write-Host "    Admin: $baseUrl$($service.AdminEndpoint)" -ForegroundColor $Colors.Blue
                }
                if ($service.Credentials) {
                    Write-Host "    Credentials: $($service.Credentials)" -ForegroundColor $Colors.Magenta
                }
                Write-Host ""
            }
        }
        
        Write-Host "Note: Services will be accessible after port forwarding is established (may take a few seconds)" -ForegroundColor $Colors.Yellow
        Write-Host "To stop port forwarding, close the minimized PowerShell windows" -ForegroundColor $Colors.Yellow
        Write-Host ""
        
        # Wait a few seconds to check service health
        Write-Status "Checking service health..."
        Start-Sleep -Seconds 5
        
        foreach ($service in $services) {
            if ($service.HealthEndpoint -and $service.ActualPort) {
                $healthUrl = "http://localhost:$($service.ActualPort)$($service.HealthEndpoint)"
                try {
                    $response = Invoke-WebRequest -Uri $healthUrl -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
                    if ($response.StatusCode -eq 200) {
                        Write-Success "[OK] $($service.Name) is healthy"
                    }
                    else {
                        Write-Warning "[WARN] $($service.Name) health check returned status $($response.StatusCode)"
                    }
                }
                catch {
                    Write-Warning "[WARN] $($service.Name) health check failed. Service may still be starting..."
                }
            }
        }
    }
    
    "istio-tools" {
        Write-Status "Starting port forwarding for Istio monitoring tools..."
        Write-Host ""
        
        # Define Istio tool ports with correct labels and service names
        $istioTools = @(
            @{
                Name = "Grafana"
                ServiceName = "grafana"
                Label = "app.kubernetes.io/name=grafana"
                LocalPort = 3000
                ContainerPort = 3000
                Description = "Metrics visualization and dashboards"
                DefaultCredentials = "admin / admin"
            },
            @{
                Name = "Prometheus"
                ServiceName = "prometheus"
                Label = "app=prometheus"
                LocalPort = 9090
                ContainerPort = 9090
                Description = "Metrics collection and querying"
                QueryEndpoint = "/graph"
            },
            @{
                Name = "Kiali"
                ServiceName = "kiali"
                Label = "app=kiali"
                LocalPort = 20001
                ContainerPort = 20001
                Description = "Service mesh observability and management"
                DefaultCredentials = "admin / admin"
            },
            @{
                Name = "Jaeger"
                ServiceName = "tracing"
                Label = "app=jaeger"
                LocalPort = 16686
                ContainerPort = 80
                Description = "Distributed tracing"
            }
        )
        
        foreach ($tool in $istioTools) {
            $actualPort = Start-PortForward -Namespace "istio-system" -Service $tool.ServiceName -LocalPort $tool.LocalPort -RemotePort $tool.ContainerPort -Label $tool.Label
            if ($actualPort) {
                # Store the actual port used
                $tool.ActualPort = $actualPort
            }
            else {
                Write-Warning "Failed to set up port forwarding for $($tool.Name)"
                continue
            }
        }
        
        Write-Success "[OK] Port forwarding started for Istio monitoring tools!"
        Write-Host ""
        Write-Host "Istio Monitoring Tools:" -ForegroundColor $Colors.Yellow
        
        foreach ($tool in $istioTools) {
            if ($tool.ActualPort) {
                $baseUrl = "http://localhost:$($tool.ActualPort)"
                
                Write-Host "  $($tool.Name):" -ForegroundColor $Colors.White
                Write-Host "    URL: $baseUrl" -ForegroundColor $Colors.Blue
                Write-Host "    Description: $($tool.Description)" -ForegroundColor $Colors.White
                
                if ($tool.DefaultCredentials) {
                    Write-Host "    Default Credentials: $($tool.DefaultCredentials)" -ForegroundColor $Colors.Magenta
                }
                if ($tool.QueryEndpoint) {
                    Write-Host "    Query Interface: $baseUrl$($tool.QueryEndpoint)" -ForegroundColor $Colors.Blue
                }
                Write-Host ""
            }
        }
        
        Write-Host "Note: Tools will be accessible after port forwarding is established" -ForegroundColor $Colors.Yellow
        Write-Host "To stop port forwarding, close the minimized PowerShell windows" -ForegroundColor $Colors.Yellow
        Write-Host ""
        
        # Wait a few seconds to check tool health
        Write-Status "Checking tool health..."
        Start-Sleep -Seconds 5
        
        foreach ($tool in $istioTools) {
            if ($tool.ActualPort) {
                $healthUrl = "http://localhost:$($tool.ActualPort)"
                try {
                    $response = Invoke-WebRequest -Uri $healthUrl -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
                    if ($response.StatusCode -eq 200) {
                        Write-Success "[OK] $($tool.Name) is accessible"
                    }
                    else {
                        Write-Warning "[WARN] $($tool.Name) returned status $($response.StatusCode)"
                    }
                }
                catch {
                    Write-Warning "[WARN] $($tool.Name) health check failed. Tool may still be starting..."
                }
            }
        }
    }
    
    "test-external" {
        Write-Status "Testing external service connectivity..."
        Write-Host ""
        
        # Test if nettest pod exists
        $nettest = kubectl get pod nettest -n credittransfer 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Status "Creating nettest pod for connectivity testing..."
            kubectl apply -f k8s-manifests/test-pod.yaml
            Start-Sleep -Seconds 10
        }
        
        Write-Status "Testing SQL Server connectivity..."
        $sqlTest = kubectl exec -n credittransfer nettest -- nslookup external-sql-server-direct 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "[OK] SQL Server service (external-sql-server-direct) is resolvable"
        } else {
            Write-Error "[FAIL] SQL Server service resolution failed"
            Write-Host "Details: $sqlTest"
        }
        
        Write-Status "Testing external proxy connectivity..."
        $proxyTest = kubectl exec -n credittransfer nettest -- nslookup external-proxy 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "[OK] External proxy service is resolvable"
        } else {
            Write-Error "[FAIL] External proxy service resolution failed"
            Write-Host "Details: $proxyTest"
        }
        
        Write-Status "Testing API health endpoint..."
        Write-Status "Setting up port forwarding to test health endpoint..."
        
        # Start port forwarding in background
        $portForwardJob = Start-Job -ScriptBlock {
            kubectl port-forward -n credittransfer svc/credittransfer-api 5007:80
        }
        
        Start-Sleep -Seconds 5
        
        try {
            $healthResponse = Invoke-WebRequest -Uri "http://localhost:5007/api/CreditTransfer/health/system" -TimeoutSec 10 -ErrorAction Stop
            $healthData = $healthResponse.Content | ConvertFrom-Json
            
            Write-Success "[OK] Health endpoint is accessible"
            Write-Host ""
            Write-Host "System Health Summary:" -ForegroundColor $Colors.Yellow
            Write-Host "  Overall Status: $($healthData.overallStatus)" -ForegroundColor $(if ($healthData.overallStatus -eq "HEALTHY") { $Colors.Green } elseif ($healthData.overallStatus -eq "DEGRADED") { $Colors.Yellow } else { $Colors.Red })
            Write-Host "  Health Percentage: $($healthData.summary.overallHealthPercentage)" -ForegroundColor $Colors.Blue
            Write-Host ""
            Write-Host "Component Status:" -ForegroundColor $Colors.Yellow
            
            foreach ($component in $healthData.components) {
                $statusColor = $Colors.White
                if ($component.status -eq "HEALTHY") {
                    $statusColor = $Colors.Green
                } elseif ($component.status -eq "DEGRADED") {
                    $statusColor = $Colors.Yellow
                } elseif ($component.status -eq "UNHEALTHY") {
                    $statusColor = $Colors.Red
                }
                Write-Host "  - $($component.component): $($component.status)" -ForegroundColor $statusColor
                Write-Host "    Message: $($component.statusMessage)" -ForegroundColor $Colors.White
            }
        }
        catch {
            Write-Error "[FAIL] Health endpoint test failed: $($_.Exception.Message)"
        }
        finally {
            # Clean up port forwarding
            Stop-Job $portForwardJob -ErrorAction SilentlyContinue
            Remove-Job $portForwardJob -ErrorAction SilentlyContinue
        }
        Write-Success "External connectivity testing completed!"
    }
    
    "cleanup" {
    Write-Warning "This will delete the credittransfer namespace and all resources!"
    $confirm = Read-Host "Are you sure? (y/N)"
    if ($confirm -eq "y" -or $confirm -eq "Y") {
        Write-Status "Cleaning up deployment..."
        kubectl delete namespace credittransfer
            
            # Clean up Istio if installed
            $istioDir = Get-ChildItem -Directory -Filter "istio-*" | Select-Object -First 1
            if ($istioDir) {
                Write-Status "Removing Istio..."
                & "$($istioDir.FullName)\bin\istioctl.exe" uninstall --purge -y
                Remove-Item -Path $istioDir.FullName -Recurse -Force
            }
        Write-Success "Cleanup completed"
        }
        else {
        Write-Status "Cleanup cancelled"
        }
    }
    
    default {
    Write-Host "Available actions:" -ForegroundColor $Colors.Yellow
        Write-Host "  deploy         - Deploy all services (includes Istio setup and external integrations)" -ForegroundColor $Colors.White
        Write-Host "  status         - Show deployment status" -ForegroundColor $Colors.White
        Write-Host "  port-forward   - Set up port forwarding and show service URLs" -ForegroundColor $Colors.White
        Write-Host "  istio-tools    - Set up port forwarding for Istio monitoring tools (Grafana, Prometheus, Kiali, Jaeger)" -ForegroundColor $Colors.White
        Write-Host "  test-external  - Test SQL Server and NoBill connectivity" -ForegroundColor $Colors.White
        Write-Host "  cleanup        - Remove all deployed resources" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "Options:" -ForegroundColor $Colors.Yellow
        Write-Host "  -SkipIstio     - Skip Istio installation (use with deploy)" -ForegroundColor $Colors.White
    Write-Host ""
        Write-Host "Usage Examples:" -ForegroundColor $Colors.Yellow
        Write-Host "  .\deploy-working.ps1 -Action deploy" -ForegroundColor $Colors.White
        Write-Host "  .\deploy-working.ps1 -Action deploy -SkipIstio" -ForegroundColor $Colors.White
        Write-Host "  .\deploy-working.ps1 -Action port-forward" -ForegroundColor $Colors.White
        Write-Host "  .\deploy-working.ps1 -Action istio-tools" -ForegroundColor $Colors.White
        Write-Host "  .\deploy-working.ps1 -Action test-external" -ForegroundColor $Colors.White
        Write-Host "  .\deploy-working.ps1 -Action status" -ForegroundColor $Colors.White
    }
}



