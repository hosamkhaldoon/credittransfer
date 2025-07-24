# Simple VPN Proxy for NoBill Service
# This proxy runs on the host machine (with VPN access) and forwards container requests

param(
    [int]$Port = 9099,
    [string]$TargetUrl = "http://10.1.132.98"
)

Write-Host "🚀 Starting Simple VPN Proxy..." -ForegroundColor Green
Write-Host "📡 Listening on: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "🎯 Forwarding to: $TargetUrl (through VPN)" -ForegroundColor Cyan
Write-Host "🔗 Container access: http://host.docker.internal:$Port" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the proxy" -ForegroundColor Yellow
Write-Host ""

# Create HTTP listener
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://+:$Port/")

try {
    $listener.Start()
    Write-Host "✅ Proxy listening on port $Port" -ForegroundColor Green
    
    while ($listener.IsListening) {
        try {
            # Get the request
            $context = $listener.GetContext()
            $request = $context.Request
            $response = $context.Response
            
            # Build target URL
            $targetFullUrl = "$TargetUrl$($request.Url.AbsolutePath)"
            if ($request.Url.Query) {
                $targetFullUrl += $request.Url.Query
            }
            
            Write-Host "🔄 $(Get-Date -Format 'HH:mm:ss') - $($request.HttpMethod) $($request.Url.AbsolutePath)" -ForegroundColor Cyan
            
            try {
                # Create web request to forward through VPN
                $webRequest = [System.Net.WebRequest]::Create($targetFullUrl)
                $webRequest.Method = $request.HttpMethod
                $webRequest.Timeout = 30000 # 30 seconds
                
                # Copy headers (skip problematic ones)
                foreach ($header in $request.Headers.AllKeys) {
                    if ($header -notin @("Host", "Content-Length", "Connection", "Expect", "Proxy-Connection", "TE", "Trailer", "Transfer-Encoding", "Upgrade")) {
                        try { 
                            $webRequest.Headers.Add($header, $request.Headers[$header]) 
                        } catch {
                            # Skip headers that can't be added
                        }
                    }
                }
                
                # Set content type if present
                if ($request.ContentType) {
                    $webRequest.ContentType = $request.ContentType
                }
                
                # Copy body for POST requests
                if ($request.HttpMethod -eq "POST" -and $request.ContentLength64 -gt 0) {
                    $webRequest.ContentLength = $request.ContentLength64
                    $requestStream = $webRequest.GetRequestStream()
                    $request.InputStream.CopyTo($requestStream)
                    $requestStream.Close()
                }
                
                # Get response from VPN-protected service
                $webResponse = $webRequest.GetResponse()
                $responseStream = $webResponse.GetResponseStream()
                
                # Copy response status and headers
                $response.StatusCode = [int]$webResponse.StatusCode
                $response.ContentType = $webResponse.ContentType
                
                # Copy response headers
                foreach ($headerName in $webResponse.Headers.AllKeys) {
                    try {
                        $response.Headers.Add($headerName, $webResponse.Headers[$headerName])
                    } catch {
                        # Skip headers that can't be added
                    }
                }
                
                # Copy response body
                $buffer = New-Object byte[] 4096
                while (($bytesRead = $responseStream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    $response.OutputStream.Write($buffer, 0, $bytesRead)
                }
                
                $responseStream.Close()
                $webResponse.Close()
                
                Write-Host "✅ $(Get-Date -Format 'HH:mm:ss') - Response: $($webResponse.StatusCode)" -ForegroundColor Green
                
            } catch [System.Net.WebException] {
                $webEx = $_.Exception
                if ($webEx.Response) {
                    # Handle HTTP error responses
                    $errorResponse = $webEx.Response
                    $response.StatusCode = [int]$errorResponse.StatusCode
                    $response.ContentType = "text/plain"
                    
                    $errorStream = $errorResponse.GetResponseStream()
                    $reader = New-Object System.IO.StreamReader($errorStream)
                    $errorContent = $reader.ReadToEnd()
                    $reader.Close()
                    $errorStream.Close()
                    
                    $errorBytes = [System.Text.Encoding]::UTF8.GetBytes($errorContent)
                    $response.OutputStream.Write($errorBytes, 0, $errorBytes.Length)
                    
                    Write-Host "⚠️  $(Get-Date -Format 'HH:mm:ss') - HTTP Error: $($errorResponse.StatusCode)" -ForegroundColor Yellow
                } else {
                    # Handle connection errors
                    $response.StatusCode = 502
                    $response.ContentType = "text/plain"
                    $errorMessage = "VPN Proxy Error: $($webEx.Message)"
                    $errorBytes = [System.Text.Encoding]::UTF8.GetBytes($errorMessage)
                    $response.OutputStream.Write($errorBytes, 0, $errorBytes.Length)
                    
                    Write-Host "❌ $(Get-Date -Format 'HH:mm:ss') - Connection Error: $($webEx.Message)" -ForegroundColor Red
                }
            } catch {
                # Handle other errors
                $response.StatusCode = 500
                $response.ContentType = "text/plain"
                $errorMessage = "Proxy Error: $($_.Exception.Message)"
                $errorBytes = [System.Text.Encoding]::UTF8.GetBytes($errorMessage)
                $response.OutputStream.Write($errorBytes, 0, $errorBytes.Length)
                
                Write-Host "❌ $(Get-Date -Format 'HH:mm:ss') - Error: $($_.Exception.Message)" -ForegroundColor Red
            }
            
            $response.Close()
            
        } catch {
            Write-Host "❌ Listener error: $($_.Exception.Message)" -ForegroundColor Red
            break
        }
    }
} catch {
    Write-Host "❌ Failed to start proxy: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Try running as Administrator or use a different port" -ForegroundColor Yellow
} finally {
    if ($listener.IsListening) {
        $listener.Stop()
    }
    Write-Host "🛑 Proxy stopped" -ForegroundColor Yellow
} 