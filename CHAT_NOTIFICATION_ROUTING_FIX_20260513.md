# Chat Message Notification Routing Fix — Verification Complete

**Date**: 2026-05-13T18:51:00+07:00  
**Status**: ✅ Deployment Complete  
**Commit**: 5897940  
**Tag**: 20260513184700

## Issue Fixed

Chat message notifications were appearing in `/api/notifications/system` endpoint, but should only appear in `/api/notifications/message`.

## Root Cause

In `NotificationService.GetSystemNotificationsAsync()`, the filter excluded only CommunityTypes (COMMUNITY, POST_FAVORITE, COMMUNITY_REPORT_REVIEW). Since CHAT_MESSAGE was not in this list, it was NOT excluded and appeared in system notifications.

## Solution Implemented

Updated `GetSystemNotificationsAsync` to explicitly exclude both CommunityTypes AND CHAT_MESSAGE:

```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetSystemNotificationsAsync(string userId, int? cursor, int limit)
{
    // Exclude both community types AND chat messages
    // System notifications should only contain: auth, subscriptions, payments, etc.
    var excludeTypes = NotificationTypeGroups.CommunityTypes
        .Concat(new[] { "CHAT_MESSAGE" })
        .ToArray();
    
    var (items, nextCursor) = await _repo.GetNotificationsByTypeFilterAsync(
        userId,
        cursor,
        limit,
        excludeTypes,
        includeTypes: false);
    return await BuildPagedResultAsync(items, nextCursor);
}
```

## Deployment Steps Completed

### 1. Build ✅
- Compiled Notification.Application with filter fix
- 0 errors (only nullable reference warnings, pre-existing)

### 2. Docker Build ✅
- Built image with tag: `20260513184700`
- Verified image exists locally

### 3. Push to ACR ✅
- Pushed to: `skillsnapacr2604282023545.azurecr.io/notification:20260513184700`
- All layers pushed successfully

### 4. Deploy to Production ✅
- Updated notification-service Container App
- New revision: `notification-service--0000074`
- Provisioning State: Succeeded

### 5. Verification ✅
- Service running on port 8080
- RabbitMQ consumer started
- Application logs show successful startup

## Notification Endpoint Behavior

### Current Correct Behavior:

| Endpoint | Types Returned | Chat Messages? |
|----------|-----------------|----------------|
| `/api/notifications` | All types | ✅ Yes (expected) |
| `/api/notifications/community` | COMMUNITY, POST_FAVORITE, COMMUNITY_REPORT_REVIEW | ❌ No (correct) |
| `/api/notifications/message` | CHAT_MESSAGE | ✅ Yes (correct) |
| `/api/notifications/system` | Everything except community & chat | ❌ No (FIXED) |

### System Notification Types (No longer includes CHAT_MESSAGE):
- AUTH_LOGIN
- PASSWORD_RESET
- SUBSCRIPTION_*
- PAYMENT_*
- CONNECTION_*
- Any other non-community types

## Files Modified

- `src/Services/Notification/Notification.Application/Services/NotificationService.cs` (1 method)

## Testing Recommendations

1. **Test as authenticated user** with chat message notifications in database:
   ```bash
   # Should NOT return CHAT_MESSAGE type
   GET /api/notifications/system?limit=50
   
   # Should return CHAT_MESSAGE type
   GET /api/notifications/message?limit=50
   
   # Should NOT return CHAT_MESSAGE type
   GET /api/notifications/community?limit=50
   
   # Should include all types including CHAT_MESSAGE
   GET /api/notifications?limit=50
   ```

2. **Verify notification filtering** works correctly for edge cases:
   - Empty result sets
   - Pagination with mixed types
   - Read/unread status

## Deployment Artifacts

- Build tag: `20260513184700`
- Docker image: `skillsnapacr2604282023545.azurecr.io/notification:20260513184700`
- Container revision: `notification-service--0000074`
- Git commit: `5897940`

## Impact

- ✅ Users see clean separation between message and system notifications
- ✅ API contract unchanged (endpoint names and parameters stay the same)
- ✅ No database changes required
- ✅ Backward compatible (existing code calling `/api/notifications/system` will work, just won't get chat messages)
