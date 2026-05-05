# Moderation Events - Realtime Notifications Implementation

## Overview

When moderators approve or reject Community, Company, or Portfolio posts, users now receive **both notification records and realtime updates** via SignalR.

## What Was Added

### 1. New Realtime Event Type
**File**: `src/Shared/RecruitmentPlatform.Contracts/Realtime/RealtimeEvents.cs`

```csharp
public sealed class PostModerationEvent : RealtimeEventBase
{
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "APPROVED" or "REJECTED"
    public string Reason { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty; // "Community", "Company", "Portfolio"
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
```

### 2. Community Service Updates

**File**: `src/Services/Community/Community.Application/Interfaces/ICommunityEventPublisher.cs`
- Added `PublishPostModerationEventAsync()` method

**File**: `src/Services/Community/Community.Infrastructure/Services/RabbitMqCommunityEventPublisher.cs`
- Implemented `PublishPostModerationEventAsync()` to route events to `post.moderation` topic

**File**: `src/Services/Community/Community.Application/Services/CommunityService.cs`
- Updated `ApprovePostAsync()` to publish both notification and realtime events
- Updated `RejectPostAsync()` to publish both notification and realtime events

**Event Flow**:
```
User Post Rejected
    ↓
CommunityService.RejectPostAsync()
    ↓
    ├─ Publish PostRejectedNotificationEvent → Notification Service (DB record)
    └─ Publish PostModerationEvent → RabbitMQ → Realtime Service
         ↓
         Send to user via SignalR
```

### 3. Company Service Updates

**File**: `src/Services/Company/Company.Application/Services/CompanyPostService.cs`
- Updated `ApprovePostAsync()` to publish notification events
- Updated `RejectPostAsync()` to publish notification events

**File**: `src/Services/Company/Company.Application/Company.Application.csproj`
- Added reference to `RecruitmentPlatform.Contracts` (for realtime event types)

## How It Works

### When a Post is Approved

1. Admin calls: `POST /api/admin/community-posts/{postId}/approve`
2. Service updates `ReviewStatus = Active`
3. **Notification Event** published:
   - Type: `post.approved`
   - Routed to Notification service
   - Creates DB notification record
4. **Realtime Event** published:
   - Type: `post.moderation`
   - Status: `APPROVED`
   - Routed to user via SignalR
   - User sees instant UI update

### When a Post is Rejected

1. Admin calls: `POST /api/admin/community-posts/{postId}/reject`
2. Service updates `ReviewStatus = Rejected`
3. **Notification Event** published:
   - Type: `post.rejected`
   - Routed to Notification service
   - Creates DB notification record
   - Includes rejection reason
4. **Realtime Event** published:
   - Type: `post.moderation`
   - Status: `REJECTED`
   - Routed to user via SignalR
   - User sees instant UI update with reason

## Event Routing

```
RabbitMQ Topics:
├─ post.approved          → Notification Service
├─ post.rejected          → Notification Service
├─ post.pending.review    → Notification Service
└─ post.moderation        → Realtime Service
                             ↓
                             SignalR (group: user_{UserId})
```

## Frontend Integration

### Listen for Moderation Events

```typescript
// Connect to Realtime service
const connection = new HubConnectionBuilder()
    .withUrl("/hubs/realtime?access_token=" + token)
    .build();

// Handle post approval
connection.on("post.moderation", (event: PostModerationEvent) => {
    if (event.status === "APPROVED") {
        // Show success message
        showNotification("Your post has been approved!");
        // Reload post or mark as live
        reloadPost(event.postId);
    } else if (event.status === "REJECTED") {
        // Show rejection message with reason
        showNotification(`Your post was rejected: ${event.reason}`);
        // Show post in rejected state
        markPostAsRejected(event.postId, event.reason);
    }
});
```

## Event Details

### PostModerationEvent Structure

```json
{
    "eventId": "abc123def456...",
    "eventType": "post.moderation",
    "version": 1,
    "postId": 42,
    "userId": "123",
    "status": "APPROVED|REJECTED",
    "reason": "Clear, high-quality content" | "Contains banned keywords",
    "postType": "Community|Company|Portfolio",
    "title": "Your post has been approved",
    "content": "Your community post has been approved and is now live.",
    "createdAt": "2026-04-29T21:00:00Z"
}
```

## Notification vs Realtime Events

| Aspect | Notification Event | Realtime Event |
|--------|-------------------|----------------|
| Purpose | Persistent record in DB | Instant UI update |
| Destination | Notification service | Connected SignalR clients |
| Storage | Database table | In-memory connection |
| Timing | Async via queue | Immediate |
| Reliability | Persisted until read | Lost if user disconnected |
| Use Case | User notification history | Live feedback |

## Services Updated

- ✅ **Community Service**: Full implementation with realtime events
- ✅ **Company Service**: Notification events (realtime events via embedding topic)
- ⏳ **Portfolio Service**: Uses existing moderation flow (no explicit approval/rejection UI yet)

## Files Modified

1. `src/Shared/RecruitmentPlatform.Contracts/Realtime/RealtimeEvents.cs`
   - Added `PostModerationEvent` class

2. `src/Services/Community/Community.Application/Interfaces/ICommunityEventPublisher.cs`
   - Added `PublishPostModerationEventAsync()` method

3. `src/Services/Community/Community.Infrastructure/Services/RabbitMqCommunityEventPublisher.cs`
   - Implemented realtime event publishing for moderation

4. `src/Services/Community/Community.Application/Services/CommunityService.cs`
   - Updated `ApprovePostAsync()` with realtime event
   - Updated `RejectPostAsync()` with realtime event

5. `src/Services/Company/Company.Application/Services/CompanyPostService.cs`
   - Added notification event publishing in `ApprovePostAsync()`
   - Added notification event publishing in `RejectPostAsync()`

6. `src/Services/Company/Company.Application/Company.Application.csproj`
   - Added reference to `RecruitmentPlatform.Contracts`

## Testing

### Manual Test: Community Post Rejection

```bash
# 1. Login and get token
TOKEN=$(curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}' | jq -r '.token')

# 2. Create a community post
curl -X POST http://localhost/api/community/posts \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"Test post with banned word: viagra"}'
# Should return 202 (Pending Review)

# 3. Get pending posts as admin
curl -X GET http://localhost/api/admin/community-posts/pending \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# 4. Reject the post
curl -X POST http://localhost/api/admin/community-posts/1/reject \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Contains banned keywords"}'

# 5. User connected to Realtime hub should see the rejection event
```

### Expected Realtime Event

```json
{
    "eventType": "post.moderation",
    "postId": 1,
    "userId": "123",
    "status": "REJECTED",
    "reason": "Contains banned keywords",
    "postType": "Community",
    "title": "Your post was rejected",
    "content": "Your community post was rejected. Reason: Contains banned keywords"
}
```

## Deployment Notes

1. All services must be rebuilt with the new changes
2. Realtime service must be configured to handle `post.moderation` events
3. Frontend must subscribe to `post.moderation` events on SignalR connection
4. Key Vault must have proper RabbitMQ configuration

## Next Steps

- Add realtime event publishing to Portfolio service moderation
- Add admin UI for reviewing pending posts and managing moderation
- Implement frontend UI to handle realtime moderation events
- Add notification badge count updates when posts are moderated
- Add moderation statistics/analytics dashboard
