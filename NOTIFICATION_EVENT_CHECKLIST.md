# Notification Event Publishing Checklist

## Purpose
This checklist ensures that all notification events are properly published when implementing new features or modifying existing ones.

---

## Before Commenting Out or Removing Event Publishing

❌ **DO NOT** comment out or remove event publishing without understanding the consequences

### Questions to Ask Before Removal
- [ ] Is this event used elsewhere in the codebase?
- [ ] Will removing this break downstream systems?
- [ ] Are there duplicate notifications that could be handled differently?
- [ ] Have you tested the change in production-like environment?

### Better Alternatives to Removal
Instead of removing event publishing, consider:
1. **Idempotency Check** - Add duplicate detection via EventId cache
2. **Aggregation** - Group similar events with time windows
3. **Filtering** - Use event type or user preferences to control notification sending
4. **Rate Limiting** - Limit notification frequency per user/type

---

## When Adding New Features That Need Notifications

### Checklist
- [ ] **Identify which event type(s)** are needed for your feature
- [ ] **Choose or create event class** with proper structure
- [ ] **Implement event publishing** in service layer
- [ ] **Add event handler** in RabbitMQConsumer if new type
- [ ] **Register notification publishers** in DI container
- [ ] **Test event publishing** in local environment
- [ ] **Verify notification is created** in database
- [ ] **Test SignalR realtime** delivery to UI
- [ ] **Test FCM push** delivery to mobile (if applicable)
- [ ] **Document the new event** in README

### Event Publishing Template
```csharp
// 1. Create event object
var notificationEvent = new MyNotificationEvent
{
    EventId = Guid.NewGuid().ToString("N"),
    EventType = "feature.action.type",  // Use kebab-case
    Version = 1,
    UserId = recipientUserId.ToString(),  // Who receives notification
    ActorId = actorUserId.ToString(),     // Who triggered it
    ActorType = "USER",  // or "COMPANY", "SYSTEM"
    ObjectId = resourceId.ToString(),     // Related resource
    Title = "User-friendly title",
    Content = "Descriptive content",
    Type = "NOTIFICATION_CATEGORY",
    CreatedAt = VietnamTime.Now()
};

// 2. Publish to RabbitMQ
await _notificationPublisher.PublishMyNotificationAsync(notificationEvent);

// 3. Publish realtime event (if immediate UI update needed)
var realtimeEvent = new MyRealtimeEvent { ... };
await _eventPublisher.PublishMyRealtimeEventAsync(realtimeEvent);
```

---

## All Supported Notification Event Types

### Community Posts (8 types)
| Event Type | Triggered When | Recipient |
|-----------|----------------|-----------|
| `post.favorite` | User likes post | Post owner |
| `post.comment.created` | User comments on post | Post owner |
| `post.reply.created` | User replies to comment | Comment author |
| `post.report.created` | User reports post | Admins/Moderators |
| `post.report.removed` | Post removed by moderator | Post owner |
| `post.rejected` | Post auto-rejected | Post owner |
| `post.approved` | Post auto-approved | Post owner |
| `post.pending.review` | Post needs review | Post owner |

### Connections (2 types)
| Event Type | Triggered When | Recipient |
|-----------|----------------|-----------|
| `connection.request.created` | User sends connection request | Recipient user |
| `connection.request.accepted` | User accepts connection | Requester user |

### Portfolios (4 types)
| Event Type | Triggered When | Recipient |
|-----------|----------------|-----------|
| `portfolio.compliment.created` | User compliments portfolio | Portfolio owner |
| `portfolio.rejected` | Portfolio auto-rejected | Portfolio owner |
| `portfolio.approved` | Portfolio auto-approved | Portfolio owner |
| `portfolio.pending.review` | Portfolio needs review | Portfolio owner |

### Job Applications (3 types)
| Event Type | Triggered When | Recipient |
|-----------|----------------|-----------|
| `job.application.created` | Applicant submits application | Recruiter/Company |
| `job.application.received` | Company receives application | Recruiter |
| `job.application.status.updated` | Application status changes | Applicant |

---

## Debugging Checklist When Notifications Don't Work

### 1. Verify Event Publishing
- [ ] Check logs: Did the event publish successfully?
- [ ] Check RabbitMQ: Is the message in the queue?
- [ ] Check event structure: Does it have all required fields?

### 2. Verify RabbitMQ Consumer
- [ ] Is RabbitMQConsumer running?
- [ ] Check logs: Any deserialization errors?
- [ ] Check logs: Any actor resolver failures?
- [ ] Check RabbitMQ Dead Letter Queue: Any events sent to DLQ?

### 3. Verify Database
- [ ] Does notification exist in Notifications table?
- [ ] Check timestamp: Is it recent?
- [ ] Check fields: UserId, Type, Content populated correctly?

### 4. Verify Real-time Delivery
- [ ] Is SignalR hub receiving events?
- [ ] Are clients connected to hub?
- [ ] Check SignalR logs for any errors

### 5. Verify FCM Delivery
- [ ] Are device tokens registered?
- [ ] Check FCM logs for send failures
- [ ] Check FCM analytics for delivery status

---

## Service URLs to Use

### For Service-to-Service Calls (Internal)
```
https://service-name.internal.environment-id.region.azurecontainerapps.io
```

### For External Access (Frontend, Mobile)
```
https://service-name.environment-id.region.azurecontainerapps.io
```

**Important:** Always use `.internal.` for service-to-service calls in Azure Container Apps!

---

## Key Design Patterns Used

### 1. Dual Events Pattern
- **Notification Event** (e.g., "post.favorite") → Persisted, aggregated, SLA-based delivery
- **Realtime Event** (e.g., "post.favorite.changed") → Immediate, non-persisted, UI updates

### 2. Idempotency Protection
Events are deduplicated via EventId in Redis cache (24-hour TTL)
```csharp
var idempotencyKey = $"notification-event:{evt.EventId}";
// Only process if not already cached
```

### 3. Aggregation Window
Similar events are grouped and combined:
- First event in window: Added to Redis bucket
- Subsequent events: Merged into same bucket
- After window expires: Single aggregated notification created

Example: "5 people liked your post" instead of 5 separate notifications

### 4. Actor Data Enrichment
Actor information is stored with notification:
```csharp
ActorName = evt.Author?.Name,      // Avoid re-fetching later
ActorAvatar = evt.Author?.Avatar,  // Reduce service-to-service calls
```

---

## Testing Notification Events

### Local Testing
```bash
# 1. Trigger the action (like, comment, etc.)
curl -X POST https://localhost/api/community/posts/1/favorite \
  -H "Authorization: Bearer $TOKEN"

# 2. Check notification created
curl https://localhost/api/notifications \
  -H "Authorization: Bearer $TOKEN"

# 3. Check database
SELECT * FROM Notifications WHERE UserId = 9 ORDER BY CreatedAt DESC
```

### Production Testing
1. Use Gateway URL instead of localhost
2. Test with real tokens and users
3. Monitor logs for any errors
4. Check multiple delivery channels (DB, SignalR, FCM)

---

## Related Files
- Notification Consumer: `src/Services/Notification/Notification.Infrastructure/Messaging/RabbitMQConsumer.cs`
- Event Models: `src/Services/*/Application/Models/Events/`
- Event Publishers: `src/Services/*/Infrastructure/Services/RabbitMq*EventPublisher.cs`
- Notification Service API: `src/Services/Notification/Notification.API/`

---

## Quick Links
- [Notification System Complete Fix](./NOTIFICATION_SYSTEM_COMPLETE_FIX.md)
- [Notification Types Audit](./NOTIFICATION_TYPES_AUDIT.md)
- [RabbitMQ Setup Guide](./RABBITMQ_TO_CLOUDAMQP_MIGRATION.md)
