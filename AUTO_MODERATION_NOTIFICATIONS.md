# Auto-Moderation Notifications Implementation

## Overview

When posts are automatically moderated during creation (auto-approved, auto-rejected, or flagged for review), users now receive **instant notifications and realtime updates** about their post's moderation status.

## What Changed

### 1. Community Service - Full Auto-Moderation Notifications

**File**: `src/Services/Community/Community.Application/Services/CommunityService.cs`

**Before**:
- Auto-rejected posts threw an exception (400 error) without notifying user
- Auto-approved posts had no notification
- PendingReview posts had notifications

**After**:
- Auto-rejected posts are created and user receives notification + realtime event
- Auto-approved posts now send notification + realtime event
- PendingReview posts still get notification + realtime event

**Flow**:
```
User Creates Post
    ↓
ModerationService.CheckPost()
    ├─ Auto-Rejected → Create post (StatusRejected) + Notify user
    ├─ Pending Review → Create post (StatusPendingReview) + Notify user
    └─ Auto-Approved → Create post (StatusActive) + Notify user
```

### 2. Company Service - Auto-Moderation Notifications

**File**: `src/Services/Company/Company.Application/Services/CompanyPostService.cs`

Same changes as Community service:
- Auto-rejected posts now stored + notified
- Auto-approved posts now notified
- PendingReview posts still notified

### 3. Updated Controllers

**Community**: `src/Services/Community/Community.API/Controllers/CommunityController.cs`
- Returns 400 + rejection reason for auto-rejected posts
- Returns 202 + reason for pending review posts
- Returns 201 + post data for auto-approved posts

**Company**: `src/Services/Company/Company.API/Controllers/CompanyPostController.cs`
- Same response codes as Community service

## Event Types Sent

### For Auto-Rejected Posts

**Notification Event** (`PostRejectedNotificationEvent`):
```json
{
    "eventType": "post.rejected",
    "userId": "123",
    "title": "Your post was rejected",
    "content": "Your community post was automatically rejected. Reason: Contains banned keywords",
    "type": "POST_REJECTED",
    "actorId": "SYSTEM",
    "actorType": "SYSTEM"
}
```

**Realtime Event** (`PostModerationEvent`):
```json
{
    "eventType": "post.moderation",
    "postId": 42,
    "userId": "123",
    "status": "REJECTED",
    "reason": "Contains banned keywords",
    "postType": "Community",
    "title": "Your post was rejected",
    "content": "Your community post was automatically rejected. Reason: Contains banned keywords"
}
```

### For Pending Review Posts

**Notification Event** (`PostPendingReviewNotificationEvent`):
```json
{
    "eventType": "post.pending.review",
    "userId": "123",
    "title": "Your post is under review",
    "content": "Your community post is awaiting manual review. Reason: Suspicious link detected",
    "type": "POST_PENDING_REVIEW",
    "actorType": "SYSTEM"
}
```

**Realtime Event** (`PostModerationEvent`):
```json
{
    "eventType": "post.moderation",
    "postId": 42,
    "userId": "123",
    "status": "PENDING_REVIEW",
    "reason": "Suspicious link detected",
    "postType": "Community"
}
```

### For Auto-Approved Posts

**Notification Event** (`PostApprovedNotificationEvent`):
```json
{
    "eventType": "post.approved",
    "userId": "123",
    "title": "Your post was approved",
    "content": "Your community post has been approved and is now live.",
    "type": "POST_APPROVED",
    "actorId": "SYSTEM",
    "actorType": "SYSTEM",
    "approverNotes": "Auto-approved by content moderation system"
}
```

**Realtime Event** (`PostModerationEvent`):
```json
{
    "eventType": "post.moderation",
    "postId": 42,
    "userId": "123",
    "status": "APPROVED",
    "reason": "Auto-approved by content moderation system",
    "postType": "Community"
}
```

## HTTP Response Codes

| Status | Scenario | Response |
|--------|----------|----------|
| 201 | Post auto-approved | `{ "message": "...", "data": {...} }` |
| 202 | Post pending review | `{ "message": "Post awaiting manual review", "reason": "...", "data": {...} }` |
| 400 | Post auto-rejected | `{ "message": "Post was rejected by content moderation", "reason": "...", "data": {...} }` |

## User Experience

### Scenario 1: Post with Banned Keywords

```
User: Submits post with "viagra"
    ↓
Service: Auto-rejects (StatusRejected)
    ↓
Notifications Sent:
  • DB Notification: "Your post was rejected - Contains banned keywords"
  • Realtime Event: Instant UI update showing rejection reason
    ↓
Response: HTTP 400 + rejection details
    ↓
User: Sees rejection reason immediately in UI
```

### Scenario 2: Post with Suspicious Link

```
User: Submits post with shortener URL (bit.ly)
    ↓
Service: Flags for review (StatusPendingReview)
    ↓
Notifications Sent:
  • DB Notification: "Your post is under review - Suspicious link detected"
  • Realtime Event: Instant UI update showing pending status
    ↓
Response: HTTP 202 + pending reason
    ↓
User: Sees pending status and can wait for admin review
    ↓
Admin: Approves → PostApprovedNotificationEvent → User gets approval notification
```

### Scenario 3: Clean Post

```
User: Submits clean post
    ↓
Service: Auto-approves (StatusActive)
    ↓
Notifications Sent:
  • DB Notification: "Your post was approved"
  • Realtime Event: Instant UI update showing live status
    ↓
Response: HTTP 201 + post data
    ↓
User: Post is immediately live and visible
```

## Files Modified

1. `src/Services/Community/Community.Application/Services/CommunityService.cs`
   - Updated `CreatePostAsync()` to publish notifications for all moderation states
   - Added realtime event publishing for instant feedback

2. `src/Services/Community/Community.API/Controllers/CommunityController.cs`
   - Updated response handling to include rejection reasons
   - Returns 400 for auto-rejected posts

3. `src/Services/Company/Company.Application/Services/CompanyPostService.cs`
   - Updated `CreatePostAsync()` to publish notifications for all moderation states
   - Changed from throwing exception on rejection to creating rejected post

4. `src/Services/Company/Company.API/Controllers/CompanyPostController.cs`
   - Updated response handling to include rejection reasons
   - Returns 400 for auto-rejected posts

## Testing

### Test Auto-Rejection

```bash
# Create post with banned keyword
curl -X POST http://localhost/api/community/posts \
  -H "Authorization: Bearer $TOKEN" \
  -F "postJson={\"description\":\"Best viagra prices\"}" \
  -F "files=@image.jpg"

# Expected Response (HTTP 400):
{
    "message": "Post was rejected by content moderation",
    "reason": "Contains banned keywords: viagra",
    "data": {
        "id": 1,
        "description": "Best viagra prices",
        "reviewStatus": 4,
        "reviewReason": "Contains banned keywords: viagra",
        "reviewedAt": "2026-04-29T21:00:00Z"
    }
}

# Expected Events:
# 1. PostRejectedNotificationEvent → Notification service
# 2. PostModerationEvent (status: REJECTED) → Realtime service → SignalR
```

### Test Pending Review

```bash
# Create post with suspicious link
curl -X POST http://localhost/api/community/posts \
  -H "Authorization: Bearer $TOKEN" \
  -F "postJson={\"description\":\"Check this http://bit.ly/xyz\"}"

# Expected Response (HTTP 202):
{
    "message": "Post awaiting manual review",
    "reason": "Suspicious link detected: URL shortener (bit.ly)",
    "data": {
        "id": 2,
        "description": "Check this http://bit.ly/xyz",
        "reviewStatus": 3,
        "reviewReason": "Suspicious link detected: URL shortener (bit.ly)",
        "reviewedAt": "2026-04-29T21:00:00Z"
    }
}

# Expected Events:
# 1. PostPendingReviewNotificationEvent → Notification service
# 2. PostModerationEvent (status: PENDING_REVIEW) → Realtime service → SignalR
```

### Test Auto-Approval

```bash
# Create clean post
curl -X POST http://localhost/api/community/posts \
  -H "Authorization: Bearer $TOKEN" \
  -F "postJson={\"description\":\"Great tips from my GitHub profile\"}"

# Expected Response (HTTP 201):
{
    "id": 3,
    "description": "Great tips from my GitHub profile",
    "reviewStatus": 1,
    "reviewReason": null,
    "reviewedAt": null
}

# Expected Events:
# 1. PostApprovedNotificationEvent → Notification service
# 2. PostModerationEvent (status: APPROVED) → Realtime service → SignalR
```

## Frontend Integration

### Listen for Auto-Moderation Events

```typescript
// Already connected to Realtime hub
connection.on("post.moderation", (event: PostModerationEvent) => {
    switch(event.status) {
        case "APPROVED":
            showSuccess(`Your post is now live!`);
            markPostAsLive(event.postId);
            break;
        case "REJECTED":
            showError(`Your post was rejected: ${event.reason}`);
            markPostAsRejected(event.postId, event.reason);
            break;
        case "PENDING_REVIEW":
            showWarning(`Your post is being reviewed: ${event.reason}`);
            markPostAsPending(event.postId, event.reason);
            break;
    }
    // Update notification count
    incrementNotificationBadge();
});
```

## Benefits

1. **Instant Feedback**: Users know immediately if their post was rejected, approved, or pending
2. **Clear Reasons**: Rejection/pending reasons help users understand what to fix
3. **Dual Notifications**: Both persistent (DB) and realtime (SignalR) ensure users don't miss it
4. **Consistent Experience**: Same notification flow for auto and manual moderation
5. **Better UX**: No more confusion about post status

## Deployment Checklist

- [ ] Rebuild Community service with changes
- [ ] Rebuild Company service with changes
- [ ] Redeploy both services to Azure
- [ ] Verify RabbitMQ is routing post.moderation events
- [ ] Verify Realtime service receives and broadcasts events
- [ ] Test all three scenarios (auto-approve, auto-reject, pending)
- [ ] Update frontend to handle PostModerationEvent
- [ ] Monitor notification service for events
- [ ] Confirm users receive both notification and realtime updates

## Performance Considerations

- Each post creation now publishes 2 events (notification + realtime)
- Events are async, non-blocking
- Uses existing RabbitMQ infrastructure
- No additional database queries

## Future Enhancements

1. Add email notifications for pending review posts
2. Add batch processing for moderation status updates
3. Add analytics for auto-moderation accuracy
4. Add user appeal process for rejected posts
5. Add moderation dashboard with real-time statistics
