param(
    [string]$Action = "status",
    [string]$Service = "all"
)

Write-Host "Script started successfully!" -ForegroundColor Green
Write-Host "Action parameter: $Action" -ForegroundColor Yellow
Write-Host "Service parameter: $Service" -ForegroundColor Yellow

switch ($Action.ToLower()) {
    "help" { Write-Host "Help action executed" -ForegroundColor Cyan }
    "deploy" { Write-Host "Deploy action executed" -ForegroundColor Cyan }
    "status" { Write-Host "Status action executed" -ForegroundColor Cyan }
    default { Write-Host "Unknown action: $Action" -ForegroundColor Red }
}

Write-Host "Script completed successfully!" -ForegroundColor Green 