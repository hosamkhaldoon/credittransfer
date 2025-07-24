#!/usr/bin/env pwsh
# Host Proxy for VPN-Protected NoBill Service
# This script creates a local HTTP proxy that forwards requests to the VPN-protected NoBill service

param(
    [int]$Port = 9099,
    [string]$TargetUrl = "http://10.1.132.98",
    [switch]$Stop
)

if ($Stop) {
    Write-Host "🛑 Stopping existing proxy processes..." -ForegroundColor Yellow
    Get-Process | Where-Object {$_.ProcessName -eq "python" -and $_.CommandLine -like "*SimpleHTTPServer*"} | Stop-Process -Force -ErrorAction SilentlyContinue
    Get-Process | Where-Object {$_.ProcessName -eq "node" -and $_.CommandLine -like "*http-proxy-middleware*"} | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Proxy processes stopped" -ForegroundColor Green
    return
}

Write-Host "🚀 Starting VPN NoBill Proxy..." -ForegroundColor Green
Write-Host "📡 Listening on: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "🎯 Forwarding to: $TargetUrl (through VPN)" -ForegroundColor Cyan
Write-Host "🔗 Container access: http://host.docker.internal:$Port" -ForegroundColor Yellow

# Create a simple Node.js proxy script
$proxyScript = @"
const http = require('http');
const { createProxyMiddleware } = require('http-proxy-middleware');

const proxy = createProxyMiddleware({
  target: '$TargetUrl',
  changeOrigin: true,
  logLevel: 'info',
  onProxyReq: (proxyReq, req, res) => {
    console.log('🔄 Proxying:', req.method, req.url, '-> $TargetUrl' + req.url);
  },
  onProxyRes: (proxyRes, req, res) => {
    console.log('✅ Response:', proxyRes.statusCode, req.url);
  },
  onError: (err, req, res) => {
    console.error('❌ Proxy Error:', err.message);
    res.writeHead(502, { 'Content-Type': 'text/plain' });
    res.end('VPN Proxy Error: ' + err.message);
  }
});

const server = http.createServer(proxy);
server.listen($Port, '0.0.0.0', () => {
  console.log('🚀 VPN Proxy running on port $Port');
  console.log('📡 Forwarding to $TargetUrl through VPN');
});
"@

# Save the proxy script
$scriptPath = Join-Path $PSScriptRoot "vpn-proxy.js"
Set-Content -Path $scriptPath -Value $proxyScript -Encoding UTF8

Write-Host "💡 To use this proxy, update your Kubernetes config:" -ForegroundColor Yellow
Write-Host "   NobillCalls__ServiceUrl: http://host.docker.internal:$Port/NobillProxy.UAT/NobillCalls.asmx" -ForegroundColor Magenta

Write-Host ""
Write-Host "🔧 Starting proxy server..." -ForegroundColor Green

# Check if Node.js is available
try {
    $nodeVersion = node --version 2>$null
    if ($nodeVersion) {
        Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
        
        # Install http-proxy-middleware if not available
        if (!(Test-Path "node_modules/http-proxy-middleware")) {
            Write-Host "📦 Installing http-proxy-middleware..." -ForegroundColor Yellow
            npm install http-proxy-middleware 2>$null
        }
        
        # Start the proxy
        Write-Host "🎯 Starting VPN-aware proxy on port $Port..." -ForegroundColor Green
        node $scriptPath
    } else {
        throw "Node.js not found"
    }
} catch {
    Write-Host "⚠️  Node.js not available, using PowerShell HTTP listener..." -ForegroundColor Yellow
    
    # Fallback to PowerShell HTTP listener
    $listener = New-Object System.Net.HttpListener
    $listener.Prefixes.Add("http://+:$Port/")
    $listener.Start()
    
    Write-Host "✅ PowerShell proxy listening on port $Port" -ForegroundColor Green
    Write-Host "🔄 Press Ctrl+C to stop the proxy" -ForegroundColor Yellow
    
    while ($listener.IsListening) {
        try {
            $context = $listener.GetContext()
            $request = $context.Request
            $response = $context.Response
            
            Write-Host "🔄 Request: $($request.HttpMethod) $($request.Url.AbsolutePath)" -ForegroundColor Cyan
            
            # Build target URL
            $targetFullUrl = "$TargetUrl$($request.Url.AbsolutePath)"
            if ($request.Url.Query) {
                $targetFullUrl += $request.Url.Query
            }
            
            try {
                # Forward the request through VPN (this runs on host with VPN access)
                $webRequest = [System.Net.WebRequest]::Create($targetFullUrl)
                $webRequest.Method = $request.HttpMethod
                $webRequest.ContentType = $request.ContentType
                
                # Copy headers
                foreach ($header in $request.Headers.AllKeys) {
                    if ($header -notin @("Host", "Content-Length")) {
                        try { $webRequest.Headers.Add($header, $request.Headers[$header]) } catch {}
                    }
                }
                
                # Copy body for POST requests
                if ($request.HttpMethod -eq "POST" -and $request.ContentLength64 -gt 0) {
                    $webRequest.ContentLength = $request.ContentLength64
                    $requestStream = $webRequest.GetRequestStream()
                    $request.InputStream.CopyTo($requestStream)
                    $requestStream.Close()
                }
                
                # Get response
                $webResponse = $webRequest.GetResponse()
                $responseStream = $webResponse.GetResponseStream()
                
                # Copy response
                $response.StatusCode = [int]$webResponse.StatusCode
                $response.ContentType = $webResponse.ContentType
                
                $buffer = New-Object byte[] 4096
                while (($bytesRead = $responseStream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    $response.OutputStream.Write($buffer, 0, $bytesRead)
                }
                
                $responseStream.Close()
                $webResponse.Close()
                
                Write-Host "✅ Response: $($webResponse.StatusCode)" -ForegroundColor Green
                
            } catch {
                Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
                $response.StatusCode = 502
                $response.ContentType = "text/plain"
                $errorBytes = [System.Text.Encoding]::UTF8.GetBytes("VPN Proxy Error: $($_.Exception.Message)")
                $response.OutputStream.Write($errorBytes, 0, $errorBytes.Length)
            }
            
            $response.Close()
            
        } catch {
            Write-Host "❌ Listener error: $($_.Exception.Message)" -ForegroundColor Red
            break
        }
    }
    
    $listener.Stop()
}

# Cleanup
if (Test-Path $scriptPath) {
    Remove-Item $scriptPath -Force
} 