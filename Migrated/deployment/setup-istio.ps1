# Setup Istio and Observability Tools
param(
    [string]$Action = "install"
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

Write-Host ""
Write-Host "🚀 Istio Setup with Observability Tools" -ForegroundColor $Colors.Green
Write-Host "=======================================" -ForegroundColor $Colors.Green
Write-Host ""

if ($Action.ToLower() -eq "install") {
    # Step 1: Download Istio
    Write-Status "Downloading Istio..."
    $istioVersion = "1.26.2"
    $downloadUrl = "https://github.com/istio/istio/releases/download/$istioVersion/istio-$istioVersion-win.zip"
    $outputFile = "istio.zip"
    
    Invoke-WebRequest -Uri $downloadUrl -OutFile $outputFile
    Expand-Archive -Path $outputFile -DestinationPath . -Force
    Remove-Item $outputFile
    
    # Find the Istio directory
    $istioDir = Get-ChildItem -Directory -Filter "istio-*" | Select-Object -First 1
    if (-not $istioDir) {
        Write-Error "Istio directory not found!"
        exit 1
    }
    
    # Add istioctl to PATH
    $env:PATH = "$($istioDir.FullName)\bin;$env:PATH"
    
    # Step 2: Install Istio
    Write-Status "Installing Istio..."
    & "$($istioDir.FullName)\bin\istioctl.exe" install --set profile=demo -y
    
    # Step 3: Enable Istio Injection for credittransfer namespace
    Write-Status "Enabling Istio injection for credittransfer namespace..."
    kubectl create namespace credittransfer --dry-run=client -o yaml | kubectl apply -f -
    kubectl label namespace credittransfer istio-injection=enabled --overwrite
    
    # Step 4: Install Addons
    Write-Status "Installing Istio addons..."
    kubectl apply -f "$($istioDir.FullName)\samples\addons\prometheus.yaml"
    kubectl apply -f "$($istioDir.FullName)\samples\addons\grafana.yaml"
    kubectl apply -f "$($istioDir.FullName)\samples\addons\jaeger.yaml"
    kubectl apply -f "$($istioDir.FullName)\samples\addons\kiali.yaml"
    
    # Step 5: Configure Gateway and VirtualService
    Write-Status "Configuring Istio Gateway..."
    @"
apiVersion: networking.istio.io/v1alpha3
kind: Gateway
metadata:
  name: credittransfer-gateway
  namespace: credittransfer
spec:
  selector:
    istio: ingressgateway
  servers:
  - port:
      number: 80
      name: http
      protocol: HTTP
    hosts:
    - "*"
---
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: credittransfer-vs
  namespace: credittransfer
spec:
  hosts:
  - "*"
  gateways:
  - credittransfer-gateway
  http:
  - match:
    - uri:
        prefix: /api
    route:
    - destination:
        host: credittransfer-api
        port:
          number: 80
  - match:
    - uri:
        prefix: /wcf
    route:
    - destination:
        host: credittransfer-wcf
        port:
          number: 80
"@ | Out-File -FilePath "istio-gateway.yaml" -Encoding UTF8

    kubectl apply -f istio-gateway.yaml
    
    # Step 6: Restart deployments to enable injection
    Write-Status "Restarting deployments to enable Istio injection..."
    kubectl rollout restart deployment -n credittransfer credittransfer-api
    kubectl rollout restart deployment -n credittransfer credittransfer-wcf
    
    Write-Success "✅ Istio installation complete!"
    Write-Host ""
    Write-Host "🌐 Access URLs (after port-forwarding):" -ForegroundColor $Colors.Yellow
    Write-Host "Kiali:      http://localhost:20001" -ForegroundColor $Colors.White
    Write-Host "Grafana:    http://localhost:3000" -ForegroundColor $Colors.White
    Write-Host "Jaeger:     http://localhost:16686" -ForegroundColor $Colors.White
    Write-Host "Prometheus: http://localhost:9090" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "To enable port-forwarding, run:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\\deploy-working.ps1 -Action port-forward" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "To verify installation:" -ForegroundColor $Colors.Yellow
    Write-Host "  kubectl get pods -n istio-system" -ForegroundColor $Colors.White
    Write-Host ""
    
} elseif ($Action.ToLower() -eq "uninstall") {
    Write-Status "Uninstalling Istio..."
    
    # Remove Gateway and VirtualService
    kubectl delete gateway -n credittransfer credittransfer-gateway
    kubectl delete virtualservice -n credittransfer credittransfer-vs
    
    # Remove Istio injection label
    kubectl label namespace credittransfer istio-injection-
    
    # Remove addons
    $istioDir = Get-ChildItem -Directory -Filter "istio-*" | Select-Object -First 1
    if ($istioDir) {
        kubectl delete -f "$($istioDir.FullName)\samples\addons\kiali.yaml"
        kubectl delete -f "$($istioDir.FullName)\samples\addons\jaeger.yaml"
        kubectl delete -f "$($istioDir.FullName)\samples\addons\grafana.yaml"
        kubectl delete -f "$($istioDir.FullName)\samples\addons\prometheus.yaml"
    }
    
    # Uninstall Istio
    & "$($istioDir.FullName)\bin\istioctl.exe" uninstall --purge -y
    
    Write-Success "✅ Istio uninstallation complete!"
} else {
    Write-Host "Available actions:" -ForegroundColor $Colors.Yellow
    Write-Host "  install   - Install Istio and observability tools" -ForegroundColor $Colors.White
    Write-Host "  uninstall - Remove Istio and all its components" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Usage: .\\setup-istio.ps1 -Action install" -ForegroundColor $Colors.White
}

Write-Host "" 