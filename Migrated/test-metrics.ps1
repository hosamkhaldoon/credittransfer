# Test API endpoints and check metrics
Write-Host "Testing API endpoints to generate metrics..." -ForegroundColor Green

# Test health endpoint (should have metrics)
Write-Host "Calling health endpoint..."
$health = Invoke-RestMethod -Uri "http://localhost:5002/api/credittransfer/health" -Method Get
Write-Host "Health response: $($health.status)"

# Test root endpoint 
Write-Host "Calling root endpoint..."
$root = Invoke-RestMethod -Uri "http://localhost:5002/" -Method Get
Write-Host "Root response: $($root.Service)"

# Get metrics
Write-Host "`nFetching metrics..." -ForegroundColor Yellow
$metricsResponse = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -Method Get

# Search for our custom metrics
Write-Host "`nSearching for custom metrics..." -ForegroundColor Yellow
$apiRequestsMetrics = $metricsResponse.Content -split "`n" | Where-Object { $_ -match "api_requests_total" }
$creditTransferMetrics = $metricsResponse.Content -split "`n" | Where-Object { $_ -match "credit_transfer_" }

Write-Host "`nAPI Requests Metrics:" -ForegroundColor Cyan
$apiRequestsMetrics | ForEach-Object { Write-Host $_ }

Write-Host "`nCredit Transfer Business Metrics:" -ForegroundColor Cyan  
$creditTransferMetrics | ForEach-Object { Write-Host $_ }

Write-Host "`nAll custom metrics found:" -ForegroundColor Green
$customMetrics = $metricsResponse.Content -split "`n" | Where-Object { 
    $_ -match "api_requests_total|api_success_total|api_errors_total|api_request_duration|credit_transfer_"
}
$customMetrics | ForEach-Object { Write-Host $_ }

if ($customMetrics.Count -eq 0) {
    Write-Host "No custom metrics found! This indicates a configuration issue." -ForegroundColor Red
} else {
    Write-Host "`nFound $($customMetrics.Count) custom metrics!" -ForegroundColor Green
} 