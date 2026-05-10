# Notification System Timeout - Root Cause Analysis

**Date:** 2026-05-06  
**Status:** 🔴 CRITICAL - Root cause identified  
**Issue:** HTTP timeouts preventing FCM and SignalR delivery  

---

## Executive Summary

**Problem:** Realtime Service cannot reach Notification Service in Azure
- ❌ FCM notifications timing out (10-second timeout)
- ⚠️ SignalR may be blocked waiting for FCM call

**Root Cause:** Incorrect service URL in Realtime Service configuration
- **Current:** `http://notification-service:5001` (Docker internal DNS)
- **Required:** `https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` (Azure FQDN)

**Impact:** 
- 100% of FCM notifications failing
- SignalR delivery may also be affected
- All notifications lost when app offline

---

## Evidence

### Error Logs from Realtime Service

```
2026-05-05T17:49:07.991252+00:00
Error sending FCM aggregated notification to user 2
System.Threading.Tasks.TaskCanceledException: 
  The request was canceled due to the configured HttpClient.Timeout 
  of 10 seconds elapsing.
```

**Repeat Pattern:**
- 17:49:07 - Timeout
- 17:49:15 - Timeout
- 17:49:22 - Timeout
- Every 6-8 seconds = consistent failure

### Configuration Mismatch

**In Realtime.API/Program.cs (line 125-129):**
```csharp
var notificationServiceUrl = builder.Configuration["Services:NotificationService:Url"] 
    ?? "http://notification-service:5001";  // ← WRONG for Azure!
```

**DNS Resolution:**
- Works in Docker Compose (internal DNS)
- FAILS in Azure Container Apps (different networking)

**Correct URL in Azure:**
```
https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
```

---

## Why This Happened

### In Docker Compose
```yaml
services:
  realtime-service:
    ...
  notification-service:
    ...
```
- Services can reach each other via service name: `http://notification-service:5001`
- This works because Docker's internal DNS resolves it

### In Azure Container Apps
- Each container app has its own FQDN
- Services must use full HTTPS URL
- Internal DNS name `notification-service` won't resolve
- Result: Connection timeout after 10 seconds

---

## Technical Details

### Network Path Analysis

**Docker Compose:**
```
realtime-service:5001 
    ↓ (local DNS)
notification-service:5001 
    ✅ Works
```

**Azure Container Apps:**
```
realtime-service (internal IP)
    ↓ (needs HTTPS FQDN)
notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
    ✅ Works
```

**Current (Broken) Setup:**
```
realtime-service (internal IP)
    ↓ (tries Docker DNS)
http://notification-service:5001
    ❌ DNS resolution fails
    ❌ Connection timeout
    ❌ 10-second wait then failure
```

---

## Solution

### Fix: Update Notification Service URL Configuration

**Option 1: Azure Key Vault (Recommended)**
1. Add to Azure Key Vault: `Services--NotificationService--Url`
2. Value: `https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
3. Realtime Service reads from Key Vault automatically

**Option 2: Environment Variable**
1. Add to Realtime Container App: `Services__NotificationService__Url`
2. Value: `https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`

**Option 3: Code Change (Not recommended for prod)**
- Update hardcoded URL fallback in Program.cs

---

## Implementation Steps

### Step 1: Set the Configuration Value

```powershell
# Add to Azure Key Vault
az keyvault secret set \
  --vault-name sskv2604282023545 \
  --name "Services--NotificationService--Url" \
  --value "https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io"
```

### Step 2: Redeploy Realtime Service

```powershell
# Rebuild with no cache
docker build -f src/Services/Realtime/Realtime.API/Dockerfile -t realtime-service --no-cache .

# Push to ACR
docker tag realtime-service skillsnapacr2604282023545.azurecr.io/realtime-service:latest
docker push skillsnapacr2604282023545.azurecr.io/realtime-service:latest

# Redeploy to Azure Container Apps
az containerapp update \
  --name realtime-service \
  --resource-group skillsnap-rg-2604282023 \
  --image skillsnapacr2604282023545.azurecr.io/realtime-service:latest
```

### Step 3: Verify Fix

```powershell
# Check logs for no timeout errors
az containerapp logs show \
  --name realtime-service \
  --resource-group skillsnap-rg-2604282023 \
  --tail 50

# Should see NO "TaskCanceledException" or timeout errors
```

---

## Why SignalR May Also Be Failing

The `NewMessageNotificationEventConsumer` calls FCM in a background task:

```csharp
// Send FCM (fire and forget, but if awaited elsewhere...)
var fcmTask = _notificationServiceClient.SendAggregatedMessageNotificationAsync(...)
```

If FCM is timing out:
1. The awaited task fails and throws exception
2. Message processing may stop or be delayed
3. SignalR hub may not complete the send
4. Both notification types fail

---

## Prevention for Future Deployments

### 1. Environment-Specific Configuration
- Keep Docker Compose URLs for local development
- Use Azure FQDN for production
- Use configuration to switch between them

### 2. Service Discovery Pattern
- Consider using Azure Service Bus instead of direct HTTP
- Or use Azure Container Apps' built-in service discovery

### 3. Testing
- Test inter-service communication before production
- Verify all service endpoints are reachable
- Check timeout values are appropriate

---

## Timeline

| Action | Time | Status |
|--------|------|--------|
| Add Key Vault secret | 5 min | TODO |
| Rebuild Docker image | 10 min | TODO |
| Push to ACR | 5 min | TODO |
| Redeploy to Azure | 5 min | TODO |
| Verify logs (no errors) | 5 min | TODO |
| Test end-to-end | 10 min | TODO |
| **Total** | **40 minutes** | TODO |

---

## Success Criteria

✅ Realtime Service logs show NO timeout errors  
✅ FCM notifications send successfully  
✅ SignalR messages deliver within 2 seconds  
✅ End-to-end: Send message → Receive notification  

---

## Additional Configuration to Check

While fixing this, also verify:
- [ ] Notification Service URL configuration is correct
- [ ] Notification Service is actually running and responsive
- [ ] Azure firewall rules don't block container-to-container communication
- [ ] HTTPS certificate is valid and trusted
- [ ] Both services in same Container Apps environment

---
