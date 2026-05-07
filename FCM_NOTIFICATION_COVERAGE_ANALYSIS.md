# FCM Notification Coverage Analysis

**Status:** Comprehensive analysis of current FCM implementation  
**Last Updated:** 2026-05-05  

---

## Executive Summary

✅ **FCM IS implemented** for push notifications while app is closed  
⚠️ **Coverage is PARTIAL** - Only certain notification types use FCM  
❌ **Real-time chat messages DO NOT use FCM** - Only SignalR/WebSocket (requires app to be open)  

---

## Notification Types Breakdown

### Supported Notification Types (22 Total)

#### ✅ Types WITH FCM Support (via Notification Service)
These notifications go through **Dual-Channel System** (FCM + SignalR realtime):

**Community-Related Notifications:**
- `POST_FAVORITE` - Someone liked your post
- `POST_COMMENT` - Someone commented on your post  
- `COMMENT_REPLY` - Someone replied to your comment
- `COMMUNITY_REPORT_REVIEW` - Admin reviewed your reported content
- `POST_REJECTED` - Your community post was rejected
- `POST_APPROVED` - Your community post was approved
- `POST_PENDING_REVIEW` - Your community post is pending review

**Job & Application Notifications:**
- `JOB_APPLICATION_CREATED` - You applied to a job
- `JOB_APPLICATION_RECEIVED` - Someone applied for your job post
- `JOB_APPLICATION_STATUS_UPDATED` - Job application status changed

**Connection & Network Notifications:**
- `CONNECTION_REQUEST_CREATED` - Someone sent you a connection request
- `CONNECTION_REQUEST_ACCEPTED` - Someone accepted your connection request
- `PORTFOLIO_COMPLIMENT_CREATED` - Someone gave you a compliment
- `PORTFOLIO_REJECTED` - Your portfolio was rejected
- `PORTFOLIO_APPROVED` - Your portfolio was approved
- `PORTFOLIO_PENDING_REVIEW` - Your portfolio is pending review

**Other System Notifications:**
- `SYSTEM` - General system notifications
- `MODERATION` - Moderation-related notifications

#### ❌ Types WITHOUT FCM Support (Realtime-Only)

**Chat & Direct Messages:**
- `MESSAGE` - **Does NOT have FCM** - Only works via SignalR WebSocket
- `CHAT` - **Does NOT have FCM** - Only works via SignalR WebSocket
- `DIRECT_MESSAGE` - **Does NOT have FCM** - Only works via SignalR WebSocket
- `MENTION` - **Does NOT have FCM** - Only works via SignalR WebSocket

**Real-time Counter Events (No Notification Created):**
- `POST_FAVORITE_CHANGED` - Real-time counter only (never creates a notification)

---

## Current Implementation Details

### How FCM Coverage Works

**File:** `Notification.Infrastructure.Messaging.RabbitMQConsumer.cs`

**Event Binding Keys:**
```
"post.#"           → Community events
"connection.#"     → Connection events
"portfolio.#"      → Portfolio events
"job.#"            → Job events
"system.#"         → System events
```

**Listening Event Types:**
```csharp
private static readonly HashSet<string> NotificationEventTypes = new(StringComparer.OrdinalIgnoreCase)
{
    "post.favorite",                    // ✅ WITH FCM
    "post.comment.created",             // ✅ WITH FCM
    "post.reply.created",               // ✅ WITH FCM
    "post.report.removed",              // ✅ WITH FCM
    "post.report.created",              // ✅ WITH FCM
    "post.rejected",                    // ✅ WITH FCM
    "post.approved",                    // ✅ WITH FCM
    "post.pending.review",              // ✅ WITH FCM
    "job.application.created",          // ✅ WITH FCM
    "job.application.received",         // ✅ WITH FCM
    "job.application.status.updated",   // ✅ WITH FCM
    "connection.request.created",       // ✅ WITH FCM
    "connection.request.accepted",      // ✅ WITH FCM
    "portfolio.compliment.created",     // ✅ WITH FCM
    "portfolio.rejected",               // ✅ WITH FCM
    "portfolio.approved",               // ✅ WITH FCM
    "portfolio.pending.review"          // ✅ WITH FCM
};
```

### Why Chat Messages Do NOT Have FCM

**Current Architecture:**

```
Chat Message Created
    ↓
Connection Service publishes "message.new" event
    ↓
RabbitMQ
    ├─→ RealtimeService (NewMessageNotificationEventConsumer)
    │   └─→ SignalR Realtime Notification (app must be open)
    │
    └─→ ChatHub (separate SignalR Hub)
        └─→ Direct socket delivery to receiver
            (app must be open and in room)
```

**Why FCM Was Not Added for Chat:**
1. **Real-time nature** - Chat messages are typically expected in real-time
2. **High frequency** - Messages come frequently (would spam device push notifications)
3. **Debouncing implemented** - NewMessageDebouncer groups messages within 2 seconds
4. **Different architecture** - Uses separate ChatHub instead of notification entity
5. **User preference** - Muting/notification settings primarily for community notifications

**Problem:** If user closes app, chat messages won't be delivered until app reopens

---

## FCM Priority & TTL Settings

**File:** `Notification.Application.Mappers.FcmNotificationMapper.cs`

### Priority Levels

**High Priority (Immediate Delivery):**
```csharp
"MESSAGE", "CHAT", "DIRECT_MESSAGE", "MENTION", 
"JOB_APPLICATION_STATUS_CHANGED"
```
- Sends with high priority to FCM
- Android: Priority.High
- iOS: apns-priority: 10 (highest)

**Normal Priority (Can be delayed):**
```csharp
"POST_FAVORITE", "POST_COMMENT", "COMMENT_REPLY", 
"CONNECTION_REQUEST", "CONNECTION_ACCEPTED"
```
- Standard delivery priority
- May batch with other notifications

### Time-to-Live (TTL) Settings

**1 Hour TTL:**
- Message/Chat types
- These expire quickly (doesn't make sense to notify about an old message hours later)

**24 Hours TTL:**
- All other notification types
- Community, job, connection notifications can be relevant for 24+ hours

---

## Dual-Channel Flow Diagram

### For Supported Notifications (WITH FCM)

```
Event (e.g., post.favorite)
    ↓
RabbitMQ Notification Consumer
    ↓
Check: Is valid notification type? → Yes
Check: Valid content? → Yes
Check: Not duplicate? → Yes
Check: Not self-action? → Yes
    ↓
Create NotificationEntity
    ↓
INotificationPublishingService.SendDualChannelAsync()
    ├─→ FCM Channel (Async, Fire & Forget)
    │   ├─→ Get device tokens for user
    │   ├─→ Map notification to FCM payload
    │   ├─→ Call Google Firebase Admin SDK
    │   └─→ Log result (success/failure)
    │
    └─→ Realtime Channel (Sync, Primary)
        ├─→ Publish to RabbitMQ realtime events
        ├─→ Realtime service picks up event
        ├─→ Send via SignalR Hub to connected clients
        └─→ If online: instant delivery
           If offline: waits for reconnect
```

### For Chat Messages (Realtime-Only, NO FCM)

```
Message Created
    ↓
Connection Service publishes "message.new" event
    ↓
RabbitMQ
    ├─→ RealtimeService.NewMessageNotificationEventConsumer
    │   ├─→ Add to NewMessageDebouncer (group within 2 seconds)
    │   ├─→ Publish to realtime.message.new event
    │   └─→ SignalR Realtime Hub sends to user
    │       (if online: instant, if offline: missed)
    │
    └─→ ChatHub (WebSocket)
        └─→ Direct delivery to room members
            (requires both sender and receiver online)
```

---

## FCM Data Payload Structure

**Sent to Mobile App:**

```json
{
  "data": {
    "notificationId": "12345",
    "notificationType": "POST_FAVORITE",
    "deepLink": "app://notification/12345",
    "actorId": "user-456",
    "objectId": "post-789",
    "eventId": "event-xyz"
  },
  "notification": {
    "title": "John Doe liked your post",
    "body": "Your post about React got 5 likes"
  },
  "android": {
    "priority": "high"
  },
  "apns": {
    "headers": {
      "apns-priority": "10"
    }
  }
}
```

---

## Notification Aggregation (What Actually Gets Sent)

### Types WITH Aggregation

**Favorite Aggregation:**
```
Event 1: John liked post
Event 2 (within 10 sec): Jane liked post
Event 3 (within 10 sec): Bob liked post
    ↓
Aggregated into SINGLE notification:
"3 people liked your post"
```

**Comment Reply Aggregation:**
```
Same user replies multiple times → Combined into single notification
```

### Types WITHOUT Aggregation

All other notification types sent immediately without aggregation.

---

## Configuration & Device Tokens

### Device Token Management

**API Endpoint:** `POST /api/device-tokens`

**Required:**
- `deviceToken` - FCM device token from mobile app
- `deviceType` - "ANDROID" or "IOS"
- `appVersion` - App version for compatibility tracking

**Flow:**
1. Mobile app registers with Firebase Cloud Messaging
2. Gets device token from FCM
3. Sends token to `/api/device-tokens` endpoint
4. Backend stores in `DeviceTokenEntity` table
5. When sending notification, looks up all device tokens for user
6. Sends via `IFcmService.SendMulticastAsync()`

### Database Storage

**Table:** `DeviceTokens`

```sql
SELECT 
    UserId,
    DeviceToken,
    DeviceType,
    AppVersion,
    IsActive,
    CreatedAt,
    UpdatedAt
FROM DeviceTokens
WHERE UserId = ?;
```

---

## Known Limitations

### 1. **Chat Messages NOT Supported**
- Status: ❌ Not implemented
- Impact: Chat messages only work when app is online
- Solution Required: Add FCM support to message notification system

### 2. **Message Debouncing**
- Current: Groups within 2 seconds
- Problem: User receives single "2 new messages" instead of individual messages
- When app offline: Messages arrive as single notification

### 3. **FCM Failures**
- Retry: Exponential backoff (max 3 retries)
- Permanent failure: Notification logged, not retried
- User won't see notification if:
  - Device token is invalid/expired
  - Device not registered with FCM
  - Quota limits exceeded

### 4. **Priority Collision**
- Chat messages marked as HIGH priority in mapper (correct)
- But never actually sent via FCM (incorrect)
- Priority settings unused for chat

---

## Action Items

### ✅ Already Implemented
- [x] FCM service integrated with Google Firebase Admin SDK
- [x] Device token registration API
- [x] Dual-channel publishing for 17 notification types
- [x] Idempotency checking to prevent duplicates
- [x] Retry logic for failed FCM sends
- [x] Aggregation for likes and comment replies
- [x] Priority & TTL configuration per type

### ⚠️ Partial Implementation
- [ ] Chat message FCM delivery (currently SignalR-only)
- [ ] Direct message FCM delivery (currently SignalR-only)
- [ ] Mention notification FCM (currently SignalR-only)

### ❌ Not Implemented
- [ ] Message read receipts via FCM
- [ ] Notification sound/vibration customization per type
- [ ] User notification preference UI (enable/disable per type)
- [ ] Notification analytics/tracking dashboard

---

## Testing Checklist

**For Supported Notification Types (WITH FCM):**
- [ ] Send notification with app closed → Appears in system tray
- [ ] Send notification with app open → Appears both realtime and FCM
- [ ] Invalid device token → Fails gracefully, not retried
- [ ] Device offline then online → Notification delivered when app opens

**For Chat Messages (Currently Realtime-Only):**
- [ ] Send message with app closed → **Message is LOST** (no FCM)
- [ ] Send message with app open → Delivered via WebSocket
- [ ] Send multiple messages → Debounced into single realtime event

---

## Recommended Next Steps

### Phase 1: Add Chat Message FCM Support
1. Create new event handler for chat messages
2. Generate FCM notification for each message (or aggregate)
3. Test with offline devices
4. Deploy to staging

### Phase 2: User Notification Preferences
1. Create UI for users to enable/disable FCM per type
2. Respect user preferences when sending

### Phase 3: Enhanced Analytics
1. Track delivery success/failure rates
2. Monitor user engagement with push notifications
3. Dashboard for notification performance

---

## Summary Table

| Notification Type | FCM Support | Realtime Support | Aggregation | Priority |
|-------------------|------------|-----------------|------------|----------|
| POST_FAVORITE | ✅ | ✅ | Yes (3 ppl liked) | Normal |
| POST_COMMENT | ✅ | ✅ | No | Normal |
| COMMENT_REPLY | ✅ | ✅ | Yes (by user) | Normal |
| CONNECTION_REQUEST | ✅ | ✅ | No | Normal |
| CONNECTION_ACCEPTED | ✅ | ✅ | No | Normal |
| JOB_APPLICATION | ✅ | ✅ | No | High |
| PORTFOLIO_COMPLIMENT | ✅ | ✅ | No | Normal |
| PORTFOLIO_REJECTED | ✅ | ✅ | No | High |
| PORTFOLIO_APPROVED | ✅ | ✅ | No | High |
| COMMUNITY_REPORT | ✅ | ✅ | No | Normal |
| SYSTEM | ✅ | ✅ | No | Normal |
| **MESSAGE** (Chat) | ❌ | ✅ | Yes (2sec debounce) | HIGH (unused) |
| **CHAT** (Direct) | ❌ | ✅ | No | HIGH (unused) |
| **MENTION** | ❌ | ✅ | No | HIGH (unused) |
| POST_FAVORITE_CHANGED | ❌ (realtime-only counter) | ✅ | No | N/A |

---

## Conclusion

**FCM Status:** ✅ **Partially Implemented**

✅ **Working Well:**
- 11 core notification types have FCM support
- Dual-channel system (realtime + FCM) ensures delivery
- Device token management robust
- Retry logic and idempotency handling solid
- Aggregation reduces notification spam

❌ **Missing:**
- Chat/direct messages still require app to be open
- 3 notification types (MESSAGE, CHAT, MENTION) don't use FCM
- Could miss urgent communication when app closed

**Recommendation:** Add FCM support for chat/direct messages to complete the implementation and ensure no messages are lost when app is closed.
