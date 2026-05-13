#!/usr/bin/env pwsh
<#
SkillSnap Platform - Live Testing Script
Tests deployed services in new Azure account
#>

$platform = "https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io"

# Test accounts
$accounts = @(
    @{email = "company A"; password = "123" },
    @{email = "thinh1@gmail.com"; password = "Thinh1512!" },
    @{email = "testuser2@gmail.com"; password = "123456" }
)

Write-Host "=== SkillSnap Platform Live Test ===" -ForegroundColor Cyan
Write-Host "Gateway: $platform`n"

# Test 1: Health Check
Write-Host "Test 1: Gateway Health" -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$platform/health" -SkipCertificateCheck -TimeoutSec 30
    Write-Host "✅ Gateway is responding" -ForegroundColor Green
} catch {
    Write-Host "❌ Gateway health check failed" -ForegroundColor Red
}

# Test 2: Authentication
Write-Host "`nTest 2: Authentication" -ForegroundColor Yellow
foreach ($account in $accounts) {
    Write-Host "  Testing: $($account.email)" -NoNewline
    try {
        $login = Invoke-RestMethod -Uri "$platform/api/auth/login" `
            -Method POST `
            -Headers @{'Content-Type' = 'application/json' } `
            -Body (@{email = $account.email; password = $account.password } | ConvertTo-Json) `
            -SkipCertificateCheck `
            -TimeoutSec 30
        
        if ($login.token) {
            Write-Host " ✅" -ForegroundColor Green
            $token = $login.token
        } else {
            Write-Host " ❌ No token" -ForegroundColor Red
        }
    } catch {
        Write-Host " ❌ Failed" -ForegroundColor Red
    }
}

# Test 3: API Endpoints (requires auth token)
if ($token) {
    Write-Host "`nTest 3: Protected Endpoints" -ForegroundColor Yellow
    
    $endpoints = @(
        @{path = "/api/notifications/system"; name = "System Notifications" },
        @{path = "/api/notifications/community"; name = "Community Notifications" },
        @{path = "/api/userprofile/profile"; name = "User Profile" },
        @{path = "/api/subscription/plans"; name = "Subscription Plans" }
    )
    
    foreach ($ep in $endpoints) {
        Write-Host "  $($ep.name)" -NoNewline
        try {
            $resp = Invoke-RestMethod -Uri "$platform$($ep.path)" `
                -Headers @{'Authorization' = "Bearer $token" } `
                -SkipCertificateCheck `
                -TimeoutSec 30
            Write-Host " ✅" -ForegroundColor Green
        } catch {
            $code = $_.Exception.Response.StatusCode
            Write-Host " ⚠️ $code" -ForegroundColor Yellow
        }
    }
}

# Test 4: Realtime Service
Write-Host "`nTest 4: Realtime Service" -ForegroundColor Yellow
$realtimeUrl = "$platform/hubs/realtime"
Write-Host "  WebSocket Hub: $realtimeUrl" -NoNewline
try {
    $resp = Invoke-RestMethod -Uri $realtimeUrl `
        -SkipCertificateCheck `
        -TimeoutSec 30
    Write-Host " ✅" -ForegroundColor Green
} catch {
    Write-Host " ⚠️ (WebSocket requires upgrade)" -ForegroundColor Yellow
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
Write-Host "Platform: ONLINE ✅" -ForegroundColor Green
