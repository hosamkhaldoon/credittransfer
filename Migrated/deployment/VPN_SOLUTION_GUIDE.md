# 🔐 VPN Proxy Solution for NoBill Service

## 🎯 **Problem**
The NoBill service at `http://10.1.132.98/NobillProxy.UAT/NobillCalls.asmx` requires **AppGate VPN connection** which is only available on the host machine, not inside Kubernetes containers.

## 🔧 **Solution**
Create a **host proxy** that runs on your machine (with VPN access) and forwards requests from containers to the VPN-protected NoBill service.

## 🚀 **Implementation Steps**

### **Step 1: Start the VPN Proxy**
```powershell
# Navigate to deployment directory
cd "E:\LP\UpgradeStack\CreditTransfer\Migrated\deployment"

# Start the VPN proxy (runs on host with VPN access)
.\host-proxy.ps1

# The proxy will:
# - Listen on: http://localhost:9099  
# - Forward to: http://10.1.132.98 (through your VPN)
# - Allow container access via: http://host.docker.internal:9099
```

### **Step 2: Update Kubernetes Configuration**
```powershell
# Apply the updated ConfigMap (already updated)
kubectl apply -f k8s-manifests/credittransfer-config.yaml

# Clear Redis cache to reload configuration
kubectl exec deployment/redis -- redis-cli FLUSHDB

# Restart the API to pick up new configuration
kubectl rollout restart deployment/credittransfer-api -n credittransfer
```

### **Step 3: Verify the Solution**
```powershell
# Wait for deployment to be ready
kubectl rollout status deployment/credittransfer-api -n credittransfer

# Test the health endpoint
kubectl port-forward service/credittransfer-api 6001:80 -n credittransfer
# Then visit: http://localhost:6001/api/CreditTransfer/health/system
```

## 🔍 **How It Works**

### **Network Flow:**
```
[Container] → http://host.docker.internal:9099/NobillProxy.UAT/NobillCalls.asmx
     ↓
[Host Proxy] → http://10.1.132.98/NobillProxy.UAT/NobillCalls.asmx (through VPN)
     ↓
[NoBill Service] ← VPN-protected service responds
     ↓
[Container] ← Response forwarded back through proxy
```

### **Configuration Changes:**
- **Old:** `http://nobill-external/NobillProxy.UAT/NobillCalls.asmx`
- **New:** `http://host.docker.internal:9099/NobillProxy.UAT/NobillCalls.asmx`

## 🎯 **Benefits**
- ✅ **VPN Access**: Proxy runs on host with VPN connection
- ✅ **Container Access**: Containers can reach proxy via `host.docker.internal`
- ✅ **No Network Changes**: No complex Kubernetes networking required
- ✅ **Transparent**: NoBill service sees normal HTTP requests
- ✅ **Debugging**: Proxy logs all requests for troubleshooting

## 🔧 **Proxy Features**
- **Dual Backend**: Node.js (preferred) or PowerShell fallback
- **Request Logging**: See all traffic between containers and NoBill
- **Error Handling**: Proper error responses for debugging
- **Health Monitoring**: Monitor VPN connectivity issues
- **Easy Management**: Start/stop with simple commands

## 🚨 **Troubleshooting**

### **Proxy Not Starting**
```powershell
# Check if port is in use
netstat -an | findstr ":9099"

# Stop existing proxy
.\host-proxy.ps1 -Stop

# Try different port
.\host-proxy.ps1 -Port 9098
```

### **VPN Connectivity Issues**
```powershell
# Test VPN from host machine
curl http://10.1.132.98/NobillProxy.UAT/NobillCalls.asmx

# Check AppGate VPN status
# Ensure VPN is connected and stable
```

### **Container Access Issues**
```powershell
# Test from inside container
kubectl exec deployment/credittransfer-api -- curl http://host.docker.internal:9099/NobillProxy.UAT/NobillCalls.asmx

# Verify Docker Desktop allows host.docker.internal
```

## 📋 **Configuration Verification**

### **Current ConfigMap Settings:**
```yaml
NobillCalls__ServiceUrl: "http://host.docker.internal:9099/NobillProxy.UAT/NobillCalls.asmx"
NobillCalls__UserName: "testuser"
NobillCalls__Password: "testpass"
NobillCalls__TimeoutSeconds: "30"
```

### **Expected Health Check Results:**
- **SQL Server Database**: ✅ HEALTHY
- **Redis Cache**: ✅ HEALTHY  
- **Configuration System**: ✅ HEALTHY
- **NoBill Service**: ✅ HEALTHY (through VPN proxy)
- **External Dependencies**: ✅ HEALTHY

## 🔄 **Maintenance Commands**

```powershell
# Start proxy
.\host-proxy.ps1

# Stop proxy
.\host-proxy.ps1 -Stop

# Use different port
.\host-proxy.ps1 -Port 9098

# Monitor proxy logs
# (logs will appear in console where proxy is running)

# Test proxy directly
curl http://localhost:9099/NobillProxy.UAT/NobillCalls.asmx

# Check container configuration
kubectl exec deployment/credittransfer-api -- env | findstr -i nobill
```

---

## 🎯 **Summary**
This solution provides **VPN-aware proxy** that runs on your host machine and forwards container requests to VPN-protected services. The proxy has access to your AppGate VPN connection and can successfully reach the NoBill UAT service while exposing a simple HTTP endpoint that containers can access via `host.docker.internal`. 