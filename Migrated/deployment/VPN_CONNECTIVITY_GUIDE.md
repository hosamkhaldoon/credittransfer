# VPN Connectivity Guide for CreditTransfer Services

## Network Architecture Overview

The CreditTransfer system requires connectivity to external services that are only accessible via VPN:

- **External SQL Server**: `10.1.133.31:1433` - Configuration and transaction database
- **NoBill Service**: `10.1.132.98:80` - Core billing system integration

## Connection Status

✅ **Database (10.1.133.31)**: Connected and operational  
❌ **NoBill Service (10.1.132.98)**: No route to host (VPN required)

## Environment Configuration Options

### Option 1: VPN Connection (Production/Staging)

```bash
# Ensure VPN connection is active before starting services
# Check connectivity to required services
ping 10.1.133.31  # Database server
ping 10.1.132.98  # NoBill service

# Start the CreditTransfer services only after VPN connectivity is confirmed
docker-compose up -d
```

### Option 2: Mock Services (Development/Testing)

For development environments without VPN access, configure mock services:

```yaml
# docker-compose.override.yml for development
version: '3.8'
services:
  credittransfer-api:
    environment:
      - NobillCalls_ServiceUrl=http://mock-nobill:8080/MockNobillService
      - USE_MOCK_NOBILL=true
      
  mock-nobill:
    image: wiremock/wiremock:latest
    ports:
      - "8080:8080"
    volumes:
      - ./mock-services/nobill:/home/wiremock
```

### Option 3: Health Check Configuration

Configure health checks to gracefully handle VPN connectivity:

```yaml
# appsettings.json
{
  "HealthCheck": {
    "NobillService": {
      "Enabled": true,
      "TimeoutSeconds": 30,
      "TestMsisdn": "96898455550",
      "RequireForOverallHealth": false  // Don't fail overall health if VPN is down
    }
  }
}
```

## Docker Container Networking

When running in Docker containers, ensure proper network configuration:

```yaml
# docker-compose.yml network configuration
networks:
  default:
    driver: bridge
    ipam:
      config:
        - subnet: 172.30.0.0/16  # Avoid conflicts with VPN subnets
```

## Health Check Interpretation

### Current Health Status Interpretation:

```json
{
  "overallStatus": "UNHEALTHY",  // Due to NoBill service unavailable
  "components": [
    {
      "component": "SQL_Server_Database",
      "status": "HEALTHY",           // ✅ Database working correctly
      "statusMessage": "Database fully operational - X active configurations available"
    },
    {
      "component": "NoBill_Service", 
      "status": "UNHEALTHY",         // ❌ VPN connectivity required
      "statusMessage": "NoBill service connection failed",
      "errorCode": "NOBILL_CONNECTION_ERROR"
    }
  ]
}
```

### What This Means:

1. **Database Operations**: ✅ Fully functional
2. **Configuration Management**: ✅ Working correctly
3. **Credit Transfers**: ❌ Will fail without NoBill connectivity
4. **System Administration**: ✅ Available for configuration and monitoring

## Troubleshooting Steps

### 1. Verify VPN Connectivity
```bash
# Test external service connectivity
curl -v http://10.1.132.98:80/health
telnet 10.1.132.98 80

# Test database connectivity
telnet 10.1.133.31 1433
```

### 2. Check Container Network Configuration
```bash
# Inspect container networking
docker network ls
docker network inspect credittransfer_default

# Check container connectivity
docker exec -it credittransfer-api-1 ping 10.1.132.98
```

### 3. Review Service Logs
```bash
# Check for network-related errors
docker logs credittransfer-api-1 | grep -i "nobill\|network\|connection"
```

## Production Deployment Checklist

- [ ] VPN connection established and stable
- [ ] Connectivity verified to both 10.1.133.31:1433 and 10.1.132.98:80
- [ ] Docker network configuration avoids IP conflicts
- [ ] Health check endpoints configured appropriately
- [ ] Monitoring alerts configured for VPN connectivity loss
- [ ] Fallback procedures documented for VPN outages

## Development vs Production Configuration

| Environment | Database | NoBill Service | Health Check Strategy |
|-------------|----------|----------------|----------------------|
| **Development** | External DB or Local SQL | Mock Service | Lenient (continue without NoBill) |
| **Staging** | External DB (VPN) | Real NoBill (VPN) | Strict (require all services) |
| **Production** | External DB (VPN) | Real NoBill (VPN) | Strict (require all services) |

## Notes

- The manual service execution working suggests the host machine has proper VPN connectivity
- Docker containers may need additional network configuration to use the host's VPN connection
- Consider using `--network host` mode for Docker containers if VPN connectivity is required 