# Notification System Test Plan

**Date:** 2026-05-06  
**Objective:** Verify SignalR (realtime) and FCM (push) notifications are working  
**Status:** IN PROGRESS

---

## Test Scenarios

### Test 1: Service Health Check

**Verify services are running and responsive**

**Tests:**
```bash
# Check Realtime Service
curl -s https://realtime-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/swagger/index.html

# Check Notification Service
curl -s https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/swagger/index.html
```

**Expected:** HTTP 200, Swagger UI loads

---

### Test 2: SignalR Connection Test

**Verify WebSocket connection to realtime hubs**

**Test A: Connect to /hubs/realtime**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://api-gateway-url/hubs/realtime", {
        accessTokenFactory: () => "your-jwt-token",
        transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect()
    .build();

connection.start().then(() => {
    console.log("✅ Connected to realtime hub");
}).catch(err => {
    console.error("❌ Connection failed:", err);
});
```

**Expected:** Connection established, no errors

**Test B: Connect to /hubs/chat**
```javascript
const chatConnection = new signalR.HubConnectionBuilder()
    .withUrl("https://api-gateway-url/hubs/chat", {
        accessTokenFactory: () => "your-jwt-token",
        transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect()
    .build();

chatConnection.start().then(() => {
    console.log("✅ Connected to chat hub");
}).catch(err => {
    console.error("❌ Connection failed:", err);
});
```

**Expected:** Connection established, no errors

---

### Test 3: FCM Endpoint Test

**Verify FCM endpoints are responding**

**Test A: Test aggregated notification endpoint**
```bash
curl -X POST https://notification-service.../api/fcm/send-aggregated-notification \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "test-user-id",
    "messageCount": 3
  }'
```

**Expected:** HTTP 200, Response: `{"success": true, "message": "..."}`

**Test B: Test single message notification endpoint**
```bash
curl -X POST https://notification-service.../api/fcm/send-message-notification \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "test-user-id",
    "messageId": "msg-123",
    "roomId": "45",
    "senderName": "John Doe"
  }'
```

**Expected:** HTTP 200, Response: `{"success": true, "message": "..."}`

---

### Test 4: Device Token Registration

**Verify device tokens are being registered**

**Check database:**
```sql
SELECT COUNT(*) as token_count FROM DEVICE_TOKENS WHERE IS_ACTIVE = 1;
SELECT TOP 10 USER_ID, TOKEN, IS_ACTIVE, CREATED_AT FROM DEVICE_TOKENS;
```

**Expected:** Tokens exist and are marked active

**Test endpoint:**
```bash
curl -X POST https://realtime-service.../api/device-tokens/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer token" \
  -d '{
    "token": "test-fcm-token-xyz123",
    "platform": "Android",
    "appVersion": "1.0"
  }'
```

**Expected:** HTTP 200, Token registered successfully

---

### Test 5: End-to-End Notification Test

**Trigger event and verify delivery through both channels**

**Scenario A: App Online (SignalR)**
1. Connect client to realtime hub
2. Listen for notifications
3. Trigger event (send message, create post, etc.)
4. Verify notification received in < 2 seconds

**Scenario B: App Offline (FCM)**
1. Close app or disconnect from SignalR
2. Trigger event from another user
3. Verify FCM push notification received
4. Verify message appears when app reopens

**Scenario C: Message Batching**
1. Trigger 5 messages within 2 seconds
2. Verify single aggregated notification sent (not 5!)
3. Verify all messages in database

---

### Test 6: Log Verification

**Ensure no timeout or error patterns**

**Check logs:**
```bash
az containerapp logs show --name realtime-service \
  --resource-group skillsnap-rg-2604282023 \
  --tail 200
```

**Look for:**
- ❌ TaskCanceledException
- ❌ timeout
- ❌ Error sending FCM
- ✅ Consumer started
- ✅ Message processed

---

## Manual Testing Steps

### Step 1: Verify Services Running
```powershell
az containerapp show --name realtime-service --query "properties.runningStatus"
az containerapp show --name notification-service --query "properties.runningStatus"
```

**Expected:** "Running" for both

### Step 2: Verify Connectivity
```bash
# From client/browser, check WebSocket connection
# Open DevTools → Network → Look for WebSocket connections
# Should see: ws:// or wss:// connections established
```

**Expected:** WebSocket connections established (no errors)

### Step 3: Test with Real Message
1. User A opens app (connects to realtime hub)
2. User B sends message to User A
3. Check browser console for `OnMessageReceived` event
4. Verify message appears in UI within 2 seconds

### Step 4: Test FCM
1. User A closes app
2. User B sends message to User A
3. Wait 5 seconds
4. Check device for push notification
5. Tap notification and verify app opens to correct message

### Step 5: Monitor Logs
```powershell
# Watch logs in real-time
az containerapp logs show --name realtime-service \
  --follow
```

**Expected:** See message processing, no errors

---

## Success Criteria

✅ Services are healthy and running  
✅ WebSocket connections to both hubs established  
✅ FCM endpoints responding with 200 OK  
✅ Device tokens registered in database  
✅ SignalR notifications delivered within 2 seconds  
✅ FCM push notifications received when app closed  
✅ Message batching working (5 messages = 1 notification)  
✅ NO timeout errors in logs  
✅ NO TaskCanceledException  
✅ All consumers running  

---

## Test Results Template

### Test Results - [DATE]

**Tester:** _______________  
**Date/Time:** _______________  

| Test | Expected | Actual | Status |
|------|----------|--------|--------|
| Realtime Service Health | 200 | | ✅/❌ |
| Notification Service Health | 200 | | ✅/❌ |
| SignalR /hubs/realtime | Connected | | ✅/❌ |
| SignalR /hubs/chat | Connected | | ✅/❌ |
| FCM aggregated endpoint | 200 OK | | ✅/❌ |
| FCM message endpoint | 200 OK | | ✅/❌ |
| Device tokens registered | > 0 | | ✅/❌ |
| SignalR notification delivery | < 2 sec | | ✅/❌ |
| FCM push received | Yes | | ✅/❌ |
| Message batching | 1 notification | | ✅/❌ |
| Log errors (none) | 0 errors | | ✅/❌ |

---

## Troubleshooting

### If SignalR not connecting
1. Check JWT token is valid
2. Verify API Gateway YARP config has /hubs/ routes
3. Check WebSocket is enabled on API Gateway
4. Verify CORS settings allow your domain

### If FCM not sending
1. Verify device tokens exist in database
2. Check Firebase credentials in Key Vault
3. Test FCM endpoint directly with curl
4. Check for HTTP timeout errors in logs

### If messages timing out
1. Verify Notification Service URL in Key Vault (should be HTTPS FQDN)
2. Check Notification Service is running
3. Verify network connectivity between services
4. Check container logs for exceptions

---

## Quick Test Commands

```powershell
# Check service health
curl -I https://realtime-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
curl -I https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io

# Check logs for errors
az containerapp logs show --name realtime-service --tail 50 | grep -i error
az containerapp logs show --name notification-service --tail 50 | grep -i error

# Check device tokens
sqlcmd -S skillsnapsql2604282023545.database.windows.net \
  -d skillsnap-db \
  -U sqladmin \
  -Q "SELECT COUNT(*) FROM DEVICE_TOKENS WHERE IS_ACTIVE = 1"
```

---

## Status

- [ ] Service Health: VERIFIED
- [ ] SignalR Connections: VERIFIED
- [ ] FCM Endpoints: VERIFIED
- [ ] Device Tokens: VERIFIED
- [ ] End-to-End Test: VERIFIED
- [ ] Log Check: VERIFIED
- [ ] All Tests Passed: READY

---
