# External Integration Commands - SQL Server & NoBill

## Summary of Commands Used to Fix SQL Server and NoBill Integration

### 1. Create External SQL Service Configuration

**File**: `k8s-manifests/external-sql-service.yaml`
```bash
# Apply the external SQL service configuration
kubectl apply -f k8s-manifests/external-sql-service.yaml
```

### 2. Update Application Configuration

**File**: `k8s-manifests/credittransfer-config.yaml`
```bash
# Apply updated configuration with correct service names
kubectl apply -f k8s-manifests/credittransfer-config.yaml
```

### 3. Restart Services to Pick Up New Configuration

```bash
# Restart API service
kubectl rollout restart deployment credittransfer-api -n credittransfer

# Restart WCF service
kubectl rollout restart deployment credittransfer-wcf -n credittransfer

# Wait for services to be ready
kubectl rollout status deployment credittransfer-api -n credittransfer
kubectl rollout status deployment credittransfer-wcf -n credittransfer
```

### 4. Test Network Connectivity

```bash
# Test SQL Server service resolution
kubectl exec -n credittransfer nettest -- nslookup external-sql-server-direct

# Test external proxy resolution
kubectl exec -n credittransfer nettest -- nslookup external-proxy

# Test direct IP connectivity (will fail in Minikube)
kubectl exec -n credittransfer nettest -- ping 10.1.133.31
```

### 5. Verify System Health

```bash
# Set up port forwarding
kubectl port-forward -n credittransfer svc/credittransfer-api 5006:80

# Test health endpoint
curl http://localhost:5006/api/CreditTransfer/health/system

# Or use PowerShell
Invoke-WebRequest -Uri "http://localhost:5006/api/CreditTransfer/health/system" | ConvertFrom-Json
```

### 6. Check Pod Status

```bash
# Check all pods in namespace
kubectl get pods -n credittransfer

# Check specific service pods
kubectl get pods -n credittransfer -l app=credittransfer-api
kubectl get pods -n credittransfer -l app=external-proxy

# Check pod logs if needed
kubectl logs -n credittransfer deployment/credittransfer-api
kubectl logs -n credittransfer deployment/external-proxy -c haproxy
```

### 7. Verify Services and Endpoints

```bash
# Check services
kubectl get services -n credittransfer

# Check endpoints (important for external services)
kubectl get endpoints -n credittransfer external-sql-server-direct

# Describe service for details
kubectl describe service external-sql-server-direct -n credittransfer
```

## All-in-One Deployment Command

The `deploy-working.ps1` script now includes all these commands automatically:

```powershell
# Full deployment with external integrations
.\deploy-working.ps1 -Action deploy

# Test external connectivity after deployment
.\deploy-working.ps1 -Action test-external

# Check deployment status
.\deploy-working.ps1 -Action status
```

## Key Configuration Changes Made

### 1. Connection String Updates
```
# Before (direct IP - fails in Kubernetes)
Server=10.1.133.31,1433;Database=CreditTransfer;...

# After (using Kubernetes service)
Server=external-sql-server-direct,1433;Database=CreditTransfer;...
```

### 2. NoBill Service URL Updates
```
# Before (direct IP - fails in Kubernetes)
http://10.1.132.98/NobillProxy/NobillCalls.asmx

# After (using proxy service)
http://external-proxy:31080/NobillProxy/NobillCalls.asmx
```

### 3. External Service Configuration
Created `external-sql-service.yaml` with:
- Service definition pointing to external SQL Server
- Endpoints mapping to actual IP address (10.1.133.31:1433)
- Proper port configuration (1433)

### 4. Proxy Service Configuration
Updated `proxy-service.yaml` with:
- HAProxy for SQL Server traffic (port 1433)
- NGINX for HTTP traffic to NoBill (port 80)
- Proper upstream configurations

## Files Created/Modified

### ✅ **Required Files** (Keep These):
- `k8s-manifests/external-sql-service.yaml` - **CRITICAL** for SQL connectivity
- `k8s-manifests/credittransfer-config.yaml` - Updated connection strings
- `k8s-manifests/proxy-service.yaml` - For NoBill HTTP proxy
- `k8s-manifests/test-pod.yaml` - For connectivity testing
- `deploy-working.ps1` - Enhanced with external integration commands
- `TROUBLESHOOTING_GUIDE.md` - Complete troubleshooting documentation

### ❌ **Removed Files** (No Longer Needed):
- `k8s-manifests/network-policy.yaml` - Not needed with external services
- `haproxy.cfg` (root directory) - Duplicate, config is in proxy-service.yaml

## Testing Commands Summary

```bash
# 1. Deploy everything
.\deploy-working.ps1 -Action deploy

# 2. Test external connectivity
.\deploy-working.ps1 -Action test-external

# 3. Manual health check
kubectl port-forward -n credittransfer svc/credittransfer-api 5006:80
curl http://localhost:5006/api/CreditTransfer/health/system

# 4. Check pod status
kubectl get pods -n credittransfer

# 5. View logs if needed
kubectl logs -n credittransfer deployment/credittransfer-api --tail=50
```

## Success Criteria

After running all commands, you should see:
- ✅ **SQL_Server_Database**: HEALTHY
- ✅ **NoBill_Service**: HEALTHY  
- ✅ **Overall System Health**: 80%+ (DEGRADED or HEALTHY)
- ✅ **70+ Configuration Items**: Loaded from database
- ✅ **External Services**: Resolvable via DNS 