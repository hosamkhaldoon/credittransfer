# Credit Transfer System - SQL Server & NoBill Integration Troubleshooting Guide

## 🚨 Issues Encountered and Solutions

### 1. SQL Server Connection Issues

#### **Problem**: 
- Health check showing `UNHEALTHY` status for SQL Server Database
- Error: "A connection was successfully established with the server, but then an error occurred during the pre-login handshake"
- Application working locally but failing in Kubernetes

#### **Root Cause**:
The issue was **NOT a .NET 8 compatibility problem** but a **Kubernetes network connectivity issue**:
- Minikube isolates cluster network from host network by default
- Pods inside Kubernetes cluster cannot directly reach external IP addresses (like `10.1.133.31`)
- Direct connection attempts from pods to SQL Server failed due to network isolation

#### **Solution Implemented**:
Created an external service configuration that properly routes traffic from Kubernetes to external SQL Server:

```yaml
# External SQL Service Configuration
apiVersion: v1
kind: Service
metadata:
  name: external-sql-server-direct
  namespace: credittransfer
spec:
  ports:
  - port: 1433
    targetPort: 1433
    protocol: TCP
---
apiVersion: v1
kind: Endpoints
metadata:
  name: external-sql-server-direct
  namespace: credittransfer
subsets:
- addresses:
  - ip: 10.1.133.31  # Your SQL Server IP
  ports:
  - port: 1433
    protocol: TCP
```

#### **Connection String Update**:
```
Before: Server=10.1.133.31,1433;Database=CreditTransfer;...
After:  Server=external-sql-server-direct,1433;Database=CreditTransfer;...
```

### 2. NoBill Service Integration

#### **Problem**:
- NoBill service calls failing from Kubernetes environment
- External web service not accessible from cluster

#### **Root Cause**:
Same network isolation issue - pods cannot reach external NoBill service at `10.1.132.98`

#### **Solution Implemented**:
- Used existing external-proxy with NGINX configuration for HTTP traffic
- Configured proper upstream backend pointing to NoBill server
- Updated service URL to use internal cluster service

#### **Configuration**:
```yaml
# NGINX Configuration for NoBill Proxy
upstream nobill_backend {
  server 10.1.132.98:80;
}
server {
  listen 80;
  location / {
    proxy_pass http://nobill_backend;
    proxy_http_version 1.1;
    proxy_set_header Connection "";
    proxy_connect_timeout 30s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
  }
}
```

#### **Service URL Update**:
```
Before: http://10.1.132.98/NobillProxy/NobillCalls.asmx
After:  http://external-proxy:31080/NobillProxy/NobillCalls.asmx
```

### 3. Network Testing Commands

#### **Test SQL Server Connectivity**:
```bash
# Test from inside cluster
kubectl exec -n credittransfer nettest -- nslookup external-sql-server-direct

# Test direct IP (this will fail in Minikube)
kubectl exec -n credittransfer nettest -- ping 10.1.133.31
```

#### **Test NoBill Service Connectivity**:
```bash
# Test proxy service
kubectl exec -n credittransfer nettest -- ping external-proxy

# Test service resolution
kubectl exec -n credittransfer nettest -- nslookup external-proxy
```

### 4. Health Check Verification

#### **Check Overall System Health**:
```bash
curl http://localhost:5006/api/CreditTransfer/health/system
```

#### **Expected Healthy Response**:
```json
{
  "overallStatus": "HEALTHY" or "DEGRADED",
  "components": [
    {
      "component": "SQL_Server_Database",
      "status": "HEALTHY",
      "statusMessage": "Database fully operational - 70 active configurations available"
    },
    {
      "component": "NoBill_Service", 
      "status": "HEALTHY",
      "statusMessage": "NoBill service responsive"
    }
  ]
}
```

## 🔧 Key Configuration Files

### 1. **Required Files** (Keep These):
- `k8s-manifests/external-sql-service.yaml` - **CRITICAL** for SQL Server connectivity
- `k8s-manifests/credittransfer-config.yaml` - Updated with correct service names
- `k8s-manifests/proxy-service.yaml` - For NoBill HTTP proxy (optional if using direct external service)

### 2. **Optional Files** (Can Remove):
- `k8s-manifests/network-policy.yaml` - Not needed in current setup
- `k8s-manifests/test-pod.yaml` - Only for debugging (can keep for troubleshooting)
- `haproxy.cfg` (root directory) - Duplicate, use the one in proxy-service.yaml

## 🚀 Deployment Commands Summary

1. **Apply External SQL Service**:
```bash
kubectl apply -f k8s-manifests/external-sql-service.yaml
```

2. **Update Configuration**:
```bash
kubectl apply -f k8s-manifests/credittransfer-config.yaml
```

3. **Restart Services to Pick Up Changes**:
```bash
kubectl rollout restart deployment credittransfer-api -n credittransfer
kubectl rollout restart deployment credittransfer-wcf -n credittransfer
```

4. **Verify Health**:
```bash
kubectl port-forward -n credittransfer svc/credittransfer-api 5006:80
curl http://localhost:5006/api/CreditTransfer/health/system
```

## 📊 Final Results

After implementing the solutions:
- ✅ **SQL Server Database**: HEALTHY (connected to `10.1.133.31`)
- ✅ **NoBill Service**: HEALTHY (connected to `10.1.132.98`)
- ✅ **Overall System Health**: 80% (DEGRADED but functional)
- ✅ **70 Configuration Items**: Successfully loaded from database

## 🔍 Key Learnings

1. **Kubernetes Network Isolation**: Minikube isolates cluster network from host network
2. **External Service Pattern**: Use Kubernetes Services with Endpoints to access external resources
3. **Not a .NET Issue**: The problem was infrastructure, not application compatibility
4. **Proxy Pattern**: External-proxy can be used for complex routing scenarios
5. **Health Checks are Critical**: They quickly identified the root cause

## 🛠️ For Future Deployments

1. Always test network connectivity first in new Kubernetes environments
2. Use external services for accessing resources outside the cluster
3. Implement comprehensive health checks to quickly identify issues
4. Consider using cloud-managed databases to avoid network complexity
5. Test both locally and in Kubernetes to isolate network vs application issues 