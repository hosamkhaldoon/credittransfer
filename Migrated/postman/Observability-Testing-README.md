# Credit Transfer Observability Testing

This comprehensive testing collection validates the OpenTelemetry integration and observability features of the Credit Transfer system.

## Overview

The **Credit Transfer Observability Testing** collection provides automated validation for:
- ✅ **Trace ID Collection & Verification** - Distributed tracing across services
- ✅ **Metrics Endpoint Testing** - Prometheus metrics validation
- ✅ **Performance Baseline Establishment** - Response time benchmarking
- ✅ **Business KPI Monitoring** - Custom transfer metrics validation
- ✅ **Service Health Validation** - Complete observability stack testing

## Quick Start

### 1. Prerequisites

Ensure the complete observability stack is running:

```bash
# Navigate to the Migrated directory
cd Migrated

# Start all services with observability stack
docker-compose up -d

# Wait for services to be ready (3-4 minutes for complete stack)
# Verify all services are running
docker-compose ps
```

### 2. Import Collection

1. Open Postman
2. Click **Import**
3. Import both files:
   - `CreditTransfer-Observability-Collection.json`
   - `CreditTransfer-Environment.json` (if not already imported)

### 3. Set Environment

Select the **Credit Transfer Environment** and verify these URLs:
- `keycloak_url`: http://localhost:8080
- `rest_api_url`: http://localhost:5002
- `wcf_service_url`: http://localhost:5001
- `prometheus_url`: http://localhost:9090
- `jaeger_url`: http://localhost:16686

### 4. Run Complete Validation

**Option A: Run Entire Collection**
1. Right-click the collection
2. Select **Run Collection**
3. Click **Run Credit Transfer Observability Testing**

**Option B: Manual Step-by-Step**
1. **Observability Health Checks** - Verify all services are accessible
2. **Metrics Endpoint Testing** - Validate Prometheus metrics
3. **Authentication with Trace Tracking** - Get tokens with tracing
4. **Trace ID Collection & Verification** - Test distributed tracing
5. **Performance Baseline Establishment** - Measure response times
6. **Observability Validation Summary** - Generate comprehensive report

## Collection Structure

### 1. 🏥 Observability Health Checks
- **REST API Health & Observability Info** - Verify API service and metadata
- **WCF Service Health & Observability Info** - Verify SOAP service status
- **Prometheus Check** - Ensure metrics collection is accessible
- **Jaeger Check** - Verify tracing UI is available

### 2. 📊 Metrics Endpoint Testing
- **REST API Prometheus Metrics** - Validate `/metrics` endpoint
  - Tests for business KPIs: `credit_transfer_*` metrics
  - HTTP instrumentation validation
  - Metrics format verification
- **WCF Service Prometheus Metrics** - Validate WCF metrics endpoint
  - ASP.NET Core metrics validation
  - Service-specific metrics check

### 3. 🔐 Authentication with Trace Tracking
- **Get Keycloak Token (With Trace Collection)** - Enhanced authentication
  - Automatic token extraction and storage
  - Trace ID collection from response headers
  - JWT validation for subsequent requests

### 4. 🔗 Trace ID Collection & Verification
- **REST API Transfer Credit (With Trace Tracking)** - End-to-end tracing
  - Performance measurement (start/end time tracking)
  - Transaction ID validation
  - Trace header extraction (`traceparent`, `trace-id`)
  - Business logic validation
- **WCF Service Transfer Credit (With Trace Tracking)** - SOAP tracing
  - SOAP response validation
  - WCF-specific trace collection
  - Performance comparison with REST API

### 5. ⚡ Performance Baseline Establishment
- **REST API Performance Test (5 Requests)** - Automated benchmarking
  - Multiple request execution
  - Response time statistical analysis
  - Performance threshold validation (< 5 seconds)
  - Baseline establishment for future comparisons
- **Metrics Collection After Load** - Post-load metrics analysis
  - Business metric extraction
  - Transfer attempt/success counting
  - HTTP request volume tracking

### 6. 📈 Observability Validation Summary
- **Generate Observability Report** - Comprehensive analysis
  - Service health summary
  - Metrics validation results
  - Trace propagation verification
  - Performance baseline reporting
  - Business metrics analysis
  - Actionable recommendations

## Expected Test Results

### ✅ Successful Health Checks
```
✅ REST API: Running (Service: Credit Transfer REST API, Version: 1.0.0)
✅ WCF Service: Running (SOAP Endpoint: /CreditTransferService.svc)
✅ Prometheus: Accessible (Metrics UI available)
✅ Jaeger: Accessible (Tracing UI available)
```

### 📊 Metrics Validation
```
✅ REST API /metrics: Available (Contains # HELP, # TYPE)
✅ Business KPIs Present:
   - credit_transfer_attempts_total
   - credit_transfer_successes_total
   - credit_transfer_failures_total
   - credit_transfer_duration_seconds
✅ HTTP Instrumentation: http_request_* metrics present
```

### 🔗 Distributed Tracing
```
📊 REST Transfer Trace ID: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
📊 WCF Transfer Trace ID: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b8-01
✅ Trace Propagation: Working (Correlated trace IDs)
```

### ⚡ Performance Baseline
```
📈 REST API Performance Baseline:
   Average: 245.60ms
   Min: 198ms
   Max: 312ms
✅ All requests under 5-second threshold
```

### 💼 Business Metrics
```
📊 Transfer Attempts: 3
📊 Transfer Successes: 2
📊 HTTP Requests: 15
✅ Business KPIs accurately recorded
```

## Automated Report Generation

The collection automatically generates a comprehensive observability report in the console:

```
🎯 ============ OBSERVABILITY VALIDATION REPORT ============

🟢 SERVICE HEALTH:
   REST API: ✅ Running
   WCF Service: ✅ Running
   Prometheus: ✅ Accessible
   Jaeger: ✅ Accessible

📊 METRICS VALIDATION:
   REST API /metrics: ✅ Available
   WCF Service /metrics: ✅ Available
   Business KPIs: ✅ credit_transfer_* metrics present
   HTTP Instrumentation: ✅ http_request_* metrics present

🔗 DISTRIBUTED TRACING:
   REST API Trace ID: 00-4bf92f3577b34da6a3ce929d0e0e4736...
   WCF Service Trace ID: 00-4bf92f3577b34da6a3ce929d0e0e4736...
   Trace Propagation: ✅ Working

⚡ PERFORMANCE BASELINE:
   REST API Average: 245.60ms
   REST API Min: 198ms
   REST API Max: 312ms

💼 BUSINESS METRICS:
   Transfer Attempts: 3
   Transfer Successes: 2
   HTTP Requests: 15

🎯 RECOMMENDATIONS:
   ✅ Set up Prometheus alerting rules
   ✅ Configure Grafana dashboards
   ✅ Implement log aggregation

🎉 ============ OBSERVABILITY VALIDATION COMPLETE ============
```

## Environment Variables Used

The collection automatically manages these environment variables:

### Service URLs
- `keycloak_url`, `rest_api_url`, `wcf_service_url`
- `prometheus_url`, `jaeger_url`

### Authentication Tokens
- `access_token` - JWT token (auto-extracted)
- `refresh_token` - Refresh token

### Test Data
- `test_source_msisdn`, `test_destination_msisdn`

### Observability Data
- `rest_transfer_trace_id`, `wcf_transfer_trace_id`
- `rest_avg_duration`, `rest_min_duration`, `rest_max_duration`
- `transfer_attempts_count`, `transfer_successes_count`
- `rest_api_metrics_baseline`, `rest_api_metrics_after_load`

### Status Tracking
- `observability_validation_complete`
- `observability_report_timestamp`

## Troubleshooting

### Common Issues

1. **Metrics Endpoint Not Found (404)**
   ```
   Solution: Ensure OpenTelemetry packages are properly installed
   Check: Program.cs contains .MapPrometheusScrapingEndpoint()
   ```

2. **No Trace IDs in Headers**
   ```
   Solution: Verify OpenTelemetry trace instrumentation is configured
   Check: ActivitySource is registered and used in controllers
   ```

3. **Business Metrics Missing**
   ```
   Solution: Confirm custom meters are registered in dependency injection
   Check: CreditTransferService has Meter and Counter instances
   ```

4. **High Response Times**
   ```
   Solution: Check service resource allocation
   Recommendation: Monitor metrics for bottlenecks
   ```

### Service Verification Commands

```bash
# Check service health
curl http://localhost:5002/health
curl http://localhost:5001/health

# Verify metrics endpoints
curl http://localhost:5002/metrics | grep credit_transfer
curl http://localhost:5001/metrics | grep aspnetcore

# Check observability UIs
curl http://localhost:9090/api/v1/status/config  # Prometheus
curl http://localhost:16686/api/services         # Jaeger
```

## Integration with CI/CD

The collection can be automated in CI/CD pipelines using Newman:

```bash
# Install Newman (Postman CLI)
npm install -g newman

# Run observability validation
newman run CreditTransfer-Observability-Collection.json \
  -e CreditTransfer-Environment.json \
  --reporters cli,junit \
  --reporter-junit-export observability-results.xml

# Check exit code for success/failure
echo $?  # 0 = success, 1 = failure
```

## Metrics and Alerting

### Key Metrics to Monitor

1. **Business Metrics**
   - `credit_transfer_attempts_total` - Total transfer attempts
   - `credit_transfer_successes_total` - Successful transfers
   - `credit_transfer_failures_total` - Failed transfers
   - `credit_transfer_duration_seconds` - Transfer response times

2. **System Metrics**
   - `http_requests_received_total` - HTTP request volume
   - `aspnetcore_request_duration_seconds` - ASP.NET response times
   - `process_cpu_seconds_total` - CPU usage
   - `dotnet_gc_collections_total` - Garbage collection

### Recommended Alerts

```yaml
# Prometheus alerting rules
groups:
  - name: credit_transfer_alerts
    rules:
      - alert: HighTransferFailureRate
        expr: rate(credit_transfer_failures_total[5m]) > 0.1
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High transfer failure rate detected"
          
      - alert: SlowTransferResponse
        expr: histogram_quantile(0.95, credit_transfer_duration_seconds) > 5
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "95th percentile transfer time above 5 seconds"
```

## Next Steps

After running observability validation:

1. **Review Grafana Dashboards** - http://localhost:3000
2. **Explore Jaeger Traces** - http://localhost:16686  
3. **Analyze Prometheus Metrics** - http://localhost:9090
4. **Set Up Production Monitoring** - Configure alerting rules
5. **Implement Log Aggregation** - ELK stack or similar
6. **Performance Optimization** - Based on baseline measurements

The observability validation provides a solid foundation for production monitoring and ensures the Credit Transfer system meets enterprise-grade observability standards! 🎯📊✨ 