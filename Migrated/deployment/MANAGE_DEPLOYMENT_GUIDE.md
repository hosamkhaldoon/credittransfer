# 🛠️ Credit Transfer System - Enhanced Management Script Guide
## **With Istio Service Mesh & Comprehensive Observability**

## 📋 **Overview**

The `manage-deployment.ps1` script is your **enterprise-grade command center** for managing the Credit Transfer System deployment on Kubernetes with **Istio service mesh** and **comprehensive observability features**. It provides automated operations for deployment, monitoring, testing, and troubleshooting with **built-in Kiali, Jaeger, Prometheus, Grafana, and OpenTelemetry integration**.

**Location:** `Migrated/deployment/manage-deployment.ps1`

---

## 🚀 **Basic Usage**

### **Command Syntax**
```powershell
.\manage-deployment.ps1 -Action <action> [-Service <service>] [-SkipKubernetesCheck] [-ConvertToPublic] [-SkipIstio] [-ForcePullImages]
```

### **Parameters**
- **`-Action`** (Required): The operation to perform
- **`-Service`** (Optional): Specific service to target (default: "all")
- **`-SkipKubernetesCheck`** (Optional): Skip automatic Kubernetes setup validation
- **`-ConvertToPublic`** (Optional): Convert Keycloak client to public (no client_secret)
- **`-SkipIstio`** (Optional): Skip Istio installation for quick operations
- **`-ForcePullImages`** (Optional): **NEW!** Force pull latest Docker images by changing imagePullPolicy to 'Always'

---

## 📝 **Available Actions**

### **1. `help` - Show Enhanced Usage Information**
```powershell
.\manage-deployment.ps1 -Action help
```
**Description:** Displays complete usage instructions including all new Istio and observability features  
**Use Case:** When you need to see all available commands and observability capabilities

---

### **2. `status` - Enhanced Deployment Status**
```powershell
.\manage-deployment.ps1 -Action status
```
**Description:** Shows comprehensive system status including:
- **🕸️ Istio Service Mesh Status** (control plane, sidecar injection)
- **📊 Application Pods Status** (with sidecar information)
- **🌐 Service Status** (endpoints and ports)
- **🔗 Istio Configuration** (Gateway, VirtualService, DestinationRules)
- **👁️ Observability Stack Status** (Kiali, Jaeger, Prometheus, Grafana, OpenTelemetry)

**Sample Output:**
```
🕸️ Istio Service Mesh Status:
  ✅ Istio control plane: Installed
NAME                                     READY   STATUS    RESTARTS   AGE
istiod-644659cc67-8sjpv                  1/1     Running   0          10m
istio-proxy                              1/1     Running   0          8m

  ✅ Sidecar injection: Enabled for credittransfer namespace

📊 Application Pods Status:
NAME                                        READY   STATUS    RESTARTS   AGE
credittransfer-api-5ddd677cc8-6jhpm         2/2     Running   0          5m
credittransfer-wcf-6d6488ddcd-ht9nf         2/2     Running   0          5m

👁️ Observability Stack Status:
  ✅ Kiali: Running
  ✅ Jaeger: Running
  ✅ Prometheus: Running
  ✅ Grafana: Running
  ✅ OpenTelemetry Collector: Running
```

---

### **3. `deploy` - Deploy with Istio Service Mesh**
```powershell
.\manage-deployment.ps1 -Action deploy
```
**Description:** Performs **enterprise-grade deployment** with Istio service mesh:
1. **Installs Istio** (if not already present) with demo profile and 100% trace sampling
2. **Deploys observability addons** (Kiali, Jaeger, Prometheus, Grafana)
3. **Enables sidecar injection** for credittransfer namespace
4. **Applies all Kubernetes manifests** from `k8s-manifests/` directory
5. **Waits for core services** and observability components to be ready
6. **Shows observability access URLs**

**Use Cases:**
- Initial system deployment with full service mesh
- Complete stack deployment including monitoring
- Production-ready deployment with observability

**Expected Output:**
```
[INFO] Deploying Credit Transfer System with Istio Service Mesh...
[INFO] Istio not found. Installing Istio service mesh...
[SUCCESS] ✅ Istio control plane installed successfully!
[SUCCESS] ✅ Istio observability addons installed successfully!
[INFO] Enabling Istio sidecar injection for credittransfer namespace...
[INFO] Applying Kubernetes manifests...
[INFO] Waiting for core services to be ready...
[SUCCESS] ✅ Deployment completed successfully!

👁️ Observability Stack Status & Access URLs
=============================================
🕸️ Kiali (Service Mesh): http://localhost:20001/kiali
🔍 Jaeger (Tracing): http://localhost:16686
📊 Prometheus (Metrics): http://localhost:9090
📈 Grafana (Dashboards): http://localhost:3000
```

---

### **4. `deploy-istio` - Install Only Istio & Observability**
```powershell
.\manage-deployment.ps1 -Action deploy-istio
```
**Description:** Installs only the Istio service mesh and observability stack:
- **Istio Control Plane** with demo profile
- **Kiali** for service mesh visualization
- **Jaeger** for distributed tracing  
- **Prometheus** for metrics collection
- **Grafana** for dashboards and monitoring

**Use Cases:**
- Adding service mesh to existing deployment
- Updating observability stack
- Istio-only installation

---

### **5. `observability` - Show Observability Stack Status**
```powershell
.\manage-deployment.ps1 -Action observability
```
**Description:** Displays comprehensive observability stack information:
- **Service Mesh Visualization** (Kiali status and access)
- **Metrics & Monitoring** (Prometheus and Grafana status)  
- **Distributed Tracing** (Jaeger and OpenTelemetry status)
- **Quick Access Commands** for each tool
- **Access URLs** with port forwarding instructions

**Sample Output:**
```
👁️ Observability Stack Status & Access URLs
============================================

🕸️ Service Mesh Visualization:
  ✅ Kiali (Service Mesh Dashboard)
     Access: kubectl port-forward -n istio-system svc/kiali 20001:20001
     URL: http://localhost:20001/kiali
     Features: Service topology, traffic flow, configuration validation

📊 Metrics & Monitoring:
  ✅ Prometheus (Metrics Collection)
     URL: http://localhost:9090
  ✅ Grafana (Metrics Visualization)  
     URL: http://localhost:3000
     Features: Istio dashboards, service metrics, performance monitoring

🔍 Distributed Tracing:
  ✅ Jaeger (Distributed Tracing)
     URL: http://localhost:16686
     Features: Request tracing, latency analysis, dependency mapping
  ✅ OpenTelemetry Collector (Telemetry Pipeline)
     Features: Telemetry collection, processing, and export
```

---

### **6. `restart` - Restart Services**
```powershell
# Restart all services
.\manage-deployment.ps1 -Action restart

# Restart specific service
.\manage-deployment.ps1 -Action restart -Service credittransfer-api
```
**Description:** Performs rolling restart of deployments with **sidecar-aware operations**
- **All services:** Restarts every deployment in the namespace (including sidecar containers)
- **Specific service:** Restarts only the specified deployment

**Use Cases:**
- Apply configuration changes
- Recover from service issues
- Force pod recreation with updated sidecar configuration

---

### **7. `logs` - View Service Logs**
```powershell
# View logs for all services
.\manage-deployment.ps1 -Action logs

# View logs for specific service
.\manage-deployment.ps1 -Action logs -Service credittransfer-api
```
**Description:** Displays recent log entries with **sidecar-aware logging**
- **All services:** Shows logs from API and WCF services (both app and sidecar containers)
- **Specific service:** Shows logs from the specified service

**Use Cases:**
- Troubleshooting service issues
- Monitoring application behavior
- Debugging deployment problems
- Analyzing Istio sidecar logs

---

### **8. `port-forward` - Enhanced Port Forwarding with Observability**
```powershell
# Start all port forwards (applications + observability)
.\manage-deployment.ps1 -Action port-forward -Service all

# Start only observability tools
.\manage-deployment.ps1 -Action port-forward -Service observability

# Start specific services
.\manage-deployment.ps1 -Action port-forward -Service api
.\manage-deployment.ps1 -Action port-forward -Service kiali
.\manage-deployment.ps1 -Action port-forward -Service jaeger
.\manage-deployment.ps1 -Action port-forward -Service prometheus
.\manage-deployment.ps1 -Action port-forward -Service grafana
```

**Description:** Creates port forwards with **intelligent namespace detection** for all services

#### **Complete Port Mappings:**
| Service | Local Port | Kubernetes Port | URL | Purpose |
|---------|------------|-----------------|-----|---------|
| **API** | `6000` | `80` | http://localhost:6000/health | Credit Transfer REST API |
| **WCF** | `6001` | `80` | http://localhost:6001/health | Credit Transfer WCF Service |
| **Keycloak** | `6002` | `8080` | http://localhost:6002/admin | Authentication Management |
| **Kiali** | `20001` | `20001` | http://localhost:20001/kiali | Service Mesh Visualization |
| **Jaeger** | `16686` | `16686` | http://localhost:16686 | Distributed Tracing |
| **Prometheus** | `9090` | `9090` | http://localhost:9090 | Metrics Collection |
| **Grafana** | `3000` | `3000` | http://localhost:3000 | Monitoring Dashboards |
| **OpenTelemetry** | `4318` | `4318` | http://localhost:4318 | Telemetry Collection |

#### **Enhanced Service Options:**
- **`all`**: All application services + all observability tools (8 port forwards)
- **`observability`**: Only observability tools (Kiali, Jaeger, Prometheus, Grafana)
- **Individual services**: Any specific service by name

**Sample Output for `observability`:**
```
[INFO] Starting all observability tool port forwards in background...
[SUCCESS] All observability port forwards started in background windows

Observability Access URLs:
  🕸️ Kiali (Service Mesh): http://localhost:20001/kiali
  🔍 Jaeger (Tracing): http://localhost:16686
  📊 Prometheus (Metrics): http://localhost:9090
  📈 Grafana (Dashboards): http://localhost:3000
```

---

### **9. `test` - Enhanced Service Testing with Observability**
```powershell
.\manage-deployment.ps1 -Action test
```
**Description:** Performs comprehensive automated health checks:
1. **API Health Test:** Calls `http://localhost:6000/health`
2. **WCF Health Test:** Calls `http://localhost:6001/health`
3. **Keycloak Auth Test:** Attempts JWT token generation
4. **Kiali Health Test:** Validates Kiali API status
5. **Jaeger Health Test:** Checks Jaeger services endpoint

**Sample Output:**
```
[INFO] Running service tests...
[INFO] Testing API health endpoint...
[SUCCESS] API health check passed
[INFO] Testing WCF health endpoint...
[SUCCESS] WCF health check passed
[INFO] Testing Keycloak authentication...
[SUCCESS] Keycloak authentication test passed
[INFO] Testing observability endpoints...
[SUCCESS] Kiali health check passed
[SUCCESS] Jaeger health check passed
```

**Prerequisites:** Port forwards must be active for observability tests

---

### **10. `diagnose` - Comprehensive Kubernetes & Istio Diagnostics**
```powershell
.\manage-deployment.ps1 -Action diagnose
```
**Description:** Provides detailed diagnostic information:
- **Kubernetes cluster connectivity** and context status
- **Docker Desktop** status and configuration
- **Istio control plane** version and health
- **Sidecar injection** configuration
- **Observability components** health status
- **Manual commands** for troubleshooting

**Sample Output:**
```
🔍 Comprehensive Kubernetes & Istio Diagnostic Report
====================================================

🔍 Kubernetes & Istio Diagnostics:
  ✅ Docker Desktop: Running
  📋 Available kubectl contexts:
    * docker-desktop
  🎯 Current context: docker-desktop
  🔗 Cluster connectivity:
    ✅ Cluster is accessible

  🕸️ Istio Service Mesh:
    ✅ Istio control plane: Installed
    📋 Client version: 1.26.2
    📋 Mesh version: 1.26.2
    ✅ Sidecar injection: Enabled for credittransfer namespace

🛠️ Manual Commands to Try:
  # Kubernetes
  docker info
  kubectl config get-contexts
  kubectl cluster-info
  kubectl get nodes

  # Istio Service Mesh
  istio-1.26.2\bin\istioctl.exe version
  istio-1.26.2\bin\istioctl.exe proxy-status
  kubectl get pods -n istio-system
  kubectl get gateway,virtualservice,destinationrule -n credittransfer

  # Observability
  kubectl get pods -n istio-system -l app=kiali
  kubectl get pods -n credittransfer -l app=jaeger
  kubectl logs -n credittransfer deployment/otel-collector
```

---

### **11. `cleanup` - Enhanced Clean Up with Istio Option**
```powershell
.\manage-deployment.ps1 -Action cleanup
```
**Description:** Completely removes the Credit Transfer System with **Istio cleanup option**:
- Prompts for confirmation (safety measure)
- Deletes the entire `credittransfer` namespace
- **Offers to uninstall Istio** and remove `istio-system` namespace
- Removes all pods, services, configurations, and service mesh components

**⚠️ WARNING:** This is destructive and cannot be undone!

**Sample Interaction:**
```
[WARNING] This will delete all Credit Transfer resources from Kubernetes!
Are you sure? (y/N): y
[INFO] Cleaning up deployment...
[WARNING] Do you also want to uninstall Istio service mesh? (y/N): y
[INFO] Uninstalling Istio...
[SUCCESS] Istio uninstalled
[SUCCESS] Deployment cleaned up
```

---

### **12. `setup-keycloak` - Enhanced Keycloak Configuration**
```powershell
# Standard confidential client setup
.\manage-deployment.ps1 -Action setup-keycloak

# Public client setup (no client_secret)
.\manage-deployment.ps1 -Action setup-keycloak -ConvertToPublic
```
**Description:** Runs comprehensive Keycloak setup with **public/confidential client options**:
- Creates the `credittransfer` realm
- Sets up client configuration (confidential or public)
- Creates user accounts with roles
- Configures authentication settings

---

### **13. `setup-k8s` - Docker Desktop Kubernetes Setup**
```powershell
.\manage-deployment.ps1 -Action setup-k8s
```
**Description:** Automated Docker Desktop Kubernetes configuration:
- Checks Docker Desktop status
- Guides through Kubernetes enablement
- Validates cluster connectivity
- Switches to docker-desktop context

---

## 🖼️ **NEW: Image Management with Force Pull**

### **Overview**
The new `-ForcePullImages` flag addresses a critical deployment scenario: **updating to newer Docker images**. By default, the Kubernetes manifests use `imagePullPolicy: Never` which only uses local images. This new feature allows you to force pull the latest images from your registry.

### **How It Works**
1. **Changes imagePullPolicy** from `Never` to `Always` for Credit Transfer services
2. **Restarts deployments** to trigger pulling of new images
3. **Waits for rollout completion** to ensure services are updated successfully
4. **Works with both** `deploy` and `restart` actions

### **Use Cases**
- **Development iterations**: Pull latest builds after code changes
- **Production deployments**: Deploy updated images to production
- **Hotfix deployments**: Quickly deploy critical fixes
- **Staging updates**: Keep staging environment current with latest builds

### **Enhanced Actions with Image Management**

#### **`deploy` - Deploy with Latest Images**
```powershell
# Deploy with latest Docker images
.\manage-deployment.ps1 -Action deploy -ForcePullImages
```
**What happens:**
1. Deploys all services with Istio service mesh
2. Changes imagePullPolicy to `Always` for API and WCF services
3. Forces restart to pull latest images
4. Waits for all services to be ready with new images

#### **`restart` - Restart with Latest Images**
```powershell
# Restart all services with latest images
.\manage-deployment.ps1 -Action restart -ForcePullImages

# Restart specific service with latest image
.\manage-deployment.ps1 -Action restart -Service api -ForcePullImages
.\manage-deployment.ps1 -Action restart -Service wcf -ForcePullImages
```
**What happens:**
1. Updates imagePullPolicy to `Always`
2. Performs rolling restart of specified services
3. Kubernetes pulls latest images from registry
4. Services restart with updated images

### **Sample Output**
```
[INFO] Restarting services with forced image pull...
[INFO] Updating image pull policy to 'Always' for Credit Transfer services...
[SUCCESS] ✅ Updated credittransfer-api image pull policy to 'Always'
[INFO] Restarting credittransfer-api to pull new images...
[PROGRESS] Waiting for rollout to complete...
[SUCCESS] ✅ Successfully pulled new images and restarted credittransfer-api
```

### **Important Notes**
- **Registry Required**: Your images must be available in a Docker registry (not just local)
- **Network Access**: Kubernetes cluster must have internet access to pull images
- **Image Tags**: Use specific tags (not `latest`) for production deployments
- **Rollback Ready**: Keep previous image versions available for rollback if needed

### **Best Practices**
1. **Test Locally First**: Verify your updated images work locally before deploying
2. **Use Specific Tags**: Tag your images with version numbers or commit hashes
3. **Monitor Rollout**: Watch the deployment progress to ensure successful updates
4. **Have Rollback Plan**: Keep previous working images available for quick rollback

---

## 🎯 **Enhanced Common Usage Workflows**

### **Initial Deployment with Full Observability**
```powershell
# 1. Deploy everything with Istio and observability
.\manage-deployment.ps1 -Action deploy

# 2. Check comprehensive status (Istio + observability)
.\manage-deployment.ps1 -Action status

# 3. Start all port forwards (apps + observability)
.\manage-deployment.ps1 -Action port-forward -Service all

# 4. Run enhanced health tests
.\manage-deployment.ps1 -Action test

# 5. Setup Keycloak
.\manage-deployment.ps1 -Action setup-keycloak

# 6. Check observability stack
.\manage-deployment.ps1 -Action observability
```

### **🆕 Deployment with Latest Images (Development Workflow)**
```powershell
# 1. Deploy with latest Docker images
.\manage-deployment.ps1 -Action deploy -ForcePullImages

# 2. Check comprehensive status
.\manage-deployment.ps1 -Action status

# 3. Start port forwards for testing
.\manage-deployment.ps1 -Action port-forward -Service all

# 4. Test updated services
.\manage-deployment.ps1 -Action test
```

### **🆕 Hot Update Workflow (Production Updates)**
```powershell
# 1. Update all services with latest images
.\manage-deployment.ps1 -Action restart -ForcePullImages

# 2. Verify services are running with new images
.\manage-deployment.ps1 -Action status

# 3. Run health checks to verify updates
.\manage-deployment.ps1 -Action test

# 4. Monitor with observability tools
.\manage-deployment.ps1 -Action port-forward -Service observability
```

### **🆕 Individual Service Update Workflow**
```powershell
# Update just the API service with latest image
.\manage-deployment.ps1 -Action restart -Service api -ForcePullImages

# Update only WCF service with latest image
.\manage-deployment.ps1 -Action restart -Service wcf -ForcePullImages

# Verify specific service health
.\manage-deployment.ps1 -Action logs -Service credittransfer-api
```

### **Service Mesh Development Workflow**
```powershell
# Deploy only Istio and observability (for existing apps)
.\manage-deployment.ps1 -Action deploy-istio

# Start observability tools
.\manage-deployment.ps1 -Action port-forward -Service observability

# Access Kiali for service mesh visualization
# URL: http://localhost:20001/kiali

# Access Jaeger for request tracing  
# URL: http://localhost:16686

# Check service mesh status
.\manage-deployment.ps1 -Action observability
```

### **Daily Operations with Observability**
```powershell
# Check comprehensive system status
.\manage-deployment.ps1 -Action status

# Start observability tools only
.\manage-deployment.ps1 -Action port-forward -Service observability

# Run enhanced health checks
.\manage-deployment.ps1 -Action test

# View service mesh topology in Kiali
# Access distributed traces in Jaeger
# Monitor metrics in Grafana
```

### **Advanced Troubleshooting Workflow**
```powershell
# Comprehensive diagnostics
.\manage-deployment.ps1 -Action diagnose

# Check observability stack status
.\manage-deployment.ps1 -Action observability

# View application logs (with sidecar logs)
.\manage-deployment.ps1 -Action logs -Service credittransfer-api

# Check Istio configuration
kubectl get gateway,virtualservice,destinationrule -n credittransfer

# View request traces in Jaeger
# Analyze metrics in Prometheus/Grafana
# Validate configuration in Kiali
```

---

## 🔧 **Advanced Configuration Options**

### **Skip Options for Performance**
```powershell
# Skip Kubernetes validation for quick operations
.\manage-deployment.ps1 -Action status -SkipKubernetesCheck

# Skip Istio installation if not needed
.\manage-deployment.ps1 -Action deploy -SkipIstio

# Combine skip options for fastest execution
.\manage-deployment.ps1 -Action restart -Service api -SkipKubernetesCheck -SkipIstio
```

### **Keycloak Client Configuration**
```powershell
# Confidential client (with client_secret) - default
.\manage-deployment.ps1 -Action setup-keycloak

# Public client (no client_secret) - for frontend apps
.\manage-deployment.ps1 -Action setup-keycloak -ConvertToPublic

# Test with appropriate authentication method
.\manage-deployment.ps1 -Action test -ConvertToPublic
```

---

## 📊 **Complete Service Names Reference**

### **Application Services**
| Service Name | Description | Port | Namespace |
|--------------|-------------|------|-----------|
| `api` | Credit Transfer API | 6000 | credittransfer |
| `wcf` | Credit Transfer WCF | 6001 | credittransfer |
| `keycloak` | Authentication Server | 6002 | credittransfer |

### **Observability Services**  
| Service Name | Description | Port | Namespace |
|--------------|-------------|------|-----------|
| `kiali` | Service Mesh Visualization | 20001 | istio-system |
| `jaeger` | Distributed Tracing | 16686 | istio-system/credittransfer |
| `prometheus` | Metrics Collection | 9090 | istio-system/credittransfer |
| `grafana` | Monitoring Dashboards | 3000 | istio-system/credittransfer |
| `otel` | OpenTelemetry Collector | 4318 | credittransfer |

### **Service Groups**
| Group Name | Description | Services Included |
|------------|-------------|-------------------|
| `all` | Everything | All application + observability services |
| `observability` | Observability Stack | Kiali, Jaeger, Prometheus, Grafana |

### **Kubernetes Deployment Names**
For `restart` and `logs` actions:
- `credittransfer-api`, `credittransfer-wcf`, `keycloak`
- `jaeger`, `prometheus`, `grafana`, `