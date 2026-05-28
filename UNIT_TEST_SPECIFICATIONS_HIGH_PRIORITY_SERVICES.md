Agent completed. agent_id: high-priority-analysis, agent_type: explore, status: completed, description: Analyzing high-priority service details, elapsed: 238s, total_turns: 0, duration: 237s

**Return Type:** `Task<IEnumerable<Message>>`

**Business Logic:**
1. Validate room exists
2. Query all messages for room ordered by CreatedAt ASC (oldest first)
3. Return message list
4. Optional: Load with sender info enrichment

**Database Operations:**
```sql
SELECT *
FROM Message
WHERE MessageRoomId = @roomId
ORDER BY CreatedAt ASC
```

**Example Response:**
```json
{
  "messages": [
    {
      "id": 2001,
      "messageRoomId": 501,
      "userId": 123,
      "content": "Hi, how are you?",
      "createdAt": "2025-06-15T15:35:00+07:00",
      "status": 1
    },
    {
      "id": 2002,
      "messageRoomId": 501,
      "userId": 456,
      "content": "I'm doing well, thanks!",
      "createdAt": "2025-06-15T15:36:00+07:00",
      "status": 1
    }
  ]
}
```

---

### Test Coverage Plan

#### CreateConnection Tests

**Happy Path:**
```gherkin
Scenario: Successfully create connection request
  Given authenticated user 123
  When creating connection request to user 456 with profileId 42
  Then connection created with PENDING status
  And createAt set to current time
  And id auto-generated
  And recipient 456 receives realtime notification via SignalR
  And recipient 456 receives notification event for notification service
  And Connection entity returned
```

**Validation Tests:**
- UserIdFrom missing → 400 Bad Request
- UserIdTo missing → 400 Bad Request
- UserIdFrom == UserIdTo → 400 Bad Request (self-connection)
- User not authenticated → 401 Unauthorized
- ProfileId invalid → 400 Bad Request

**Authorization Tests:**
- Authenticated user's ID must match UserIdFrom
- Creating connection for another user → 403 Forbidden

**Concurrency Tests:**
- Two simultaneous connection requests → both processed
- Duplicate connection requests (same users) → allowed (separate connections)
- No race condition in auto-increment ID

**Realtime Tests:**
- SignalR message sent to recipient's group
- Multiple recipient connections all receive notification
- Offline recipients don't block response

---

#### CreateMessage Tests

**Happy Path:**
```gherkin
Scenario: Send message in active room
  Given room 501 with two connected users
  And authenticated user 123
  When sending message "Hi, how are you?"
  Then message created with UNREAD status
  And message timestamp set to now
  And room.LastMessAt updated to message timestamp
  And message broadcast to all users in room via SignalR
  And message broadcast to Realtime Service
  And MessageDto returned with id
```

**Validation Tests:**
- Content empty or null → rejected
- Content > 5000 chars → truncated or rejected
- Room doesn't exist → 404 Not Found
- User not part of room → 403 Forbidden

**Status Handling Tests:**
- Status = 0 (UNREAD) → correctly set
- Status = 1 (READ) → correctly set
- Status = invalid value → default to UNREAD
- Status = 2 (DELIVERED) → correctly set

**Concurrency Tests:**
- Multiple messages simultaneously → all saved in order
- Race condition in LastMessAt update → handled with locking
- No message loss on rapid-fire messages

**Realtime Broadcasting Tests:**
- SignalR broadcast to room group succeeds
- Multiple clients in room all receive message
- Offline users don't block broadcast
- Realtime Service publish async (non-blocking)

---

#### GetRoomMessages Tests

**Happy Path:**
```gherkin
Scenario: Load all messages from room
  Given room 501 with 50 messages
  And authenticated user 123 in room
  When requesting room messages
  Then all 50 messages returned
  And ordered by CreatedAt ascending (oldest first)
  And message list with IEnumerable<Message> returned
```

**Empty Room Tests:**
- Room with 0 messages → return empty list
- Room created but no messages yet → return empty

**Ordering Tests:**
- Messages ordered chronologically oldest to newest
- Timestamps preserved exactly as stored

**Authorization Tests:**
- User not in room → 403 Forbidden (or return empty)
- Unauthenticated request → 401 Unauthorized

---

### Data Flow Documentation - Connection Service

#### CreateConnection Data Flow

```
API Request (CreateConnectionRequest: userIdFrom, userIdTo, profileId)
    ↓
ConnectionController.CreateConnection()
    ↓
[VALIDATE AUTHENTICATION]
├─ Extract userId from JWT token
└─ Verify userId == request.UserIdFrom

    ↓
[CREATE CONNECTION ENTITY]
├─ Connection conn = new Connection {
│   UserIdFrom: request.UserIdFrom,
│   UserIdTo: request.UserIdTo,
│   ProfileId: request.ProfileId,
│   Status: "PENDING",
│   CreateAt: VietnamTime.Now(),
│   ConnectionAt: null,
│   Rooms: [] (sanitized)
│ }
└─ IConnectionService.CreateConnectionAsync(conn)

    ↓
[SANITIZE IN SERVICE]
├─ conn.Id = 0 (ensure insert)
├─ conn.Rooms = [] (clear any nested data)
├─ conn.CreateAt = VietnamTime.Now() (enforce server time)
└─ conn.Status = "PENDING" (enforce initial status)

    ↓
[PERSIST TO DATABASE]
├─ IConnectionRepository.CreateAsync(conn)
│   └─ DbContext.Connection.Add(conn)
│   └─ DbContext.SaveChangesAsync()
│       └─ INSERT Connection (...) VALUES (...)
│       └─ SELECT SCOPE_IDENTITY() → get generated Id
└─ Return persisted entity with Id

    ↓
[REALTIME NOTIFICATION - GROUP BROADCAST]
├─ IHubContext<ChatHub>.Clients.Group($"user_{created.UserIdTo}")
│   .SendAsync("ConnectionRequested", {
│     connectionId: created.Id,
│     fromUserId: created.UserIdFrom,
│     toUserId: created.UserIdTo,
│     profileId: created.ProfileId,
│     status: created.Status,
│     createdAt: created.CreateAt
│   })
└─ (Non-blocking, async fire-and-forget)

    ↓
[REALTIME SERVICE NOTIFICATION]
├─ IConnectionEventPublisher.PublishConnectionRequestedAsync(
│   created.Id,
│   created.UserIdFrom,
│   created.UserIdTo,
│   created.ProfileId,
│   created.CreateAt
│ )
└─ (Async event to Realtime Service, non-blocking)

    ↓
[ACTOR INFO ENRICHMENT FOR NOTIFICATION]
├─ GetUserProfileAsync(created.UserIdFrom)
│   └─ HTTP GET /api/users/{userId} from UserProfile Service
│       Returns: { Id, Name, Avatar, Role }
└─ Store as actorProfile

    ↓
[NOTIFICATION EVENT PUBLISHING]
├─ IConnectionEventPublisher.PublishConnectionRequestNotificationAsync({
│   EventType: "connection.request.created",
│   UserId: created.UserIdTo.ToString(),
│   ActorId: created.UserIdFrom.ToString(),
│   ActorType: "USER",
│   ObjectId: created.Id.ToString(),
│   Title: "Đã nhận yêu cầu kết nối",
│   Content: $"{actorProfile.Name} vừa gửi cho bạn một yêu cầu kết nối.",
│   Type: "CONNECTION_REQUEST_SENT",
│   Author: actorProfile,
│   CreatedAt: created.CreateAt
│ })
└─ (Consumed by Notification Service to create notification record)

    ↓
[RETURN RESPONSE]
└─ 201 Created
    Location: /api/connection/{created.Id}
    Body: created Connection entity
```

**Service Dependencies:**
- `IConnectionRepository` - Database access
- `IHubContext<ChatHub>` - SignalR broadcasting
- `IConnectionEventPublisher` - Event publishing
- User Profile Service (HTTP) - Actor enrichment

**Real-time Channels:**
1. SignalR Group: `user_{userId}` - Direct user notifications
2. Realtime Service Events - Cross-service real-time
3. Notification Service Events - Durable notification records

---

#### CreateMessage Data Flow

```
API Request (CreateMessageRequest: roomId, content)
    ↓
ConnectionController.CreateMessage()
    ↓
[VALIDATE]
├─ Room exists (roomId)
├─ User authenticated and in room
├─ Content not empty
└─ Content length <= 5000

    ↓
[SANITIZE INPUT IN SERVICE]
├─ message.Id = 0 (ensure insert)
├─ message.Room = null (clear nested object)
├─ message.CreatedAt = VietnamTime.Now() (enforce server time)
├─ If message.Status not in [1, 2]:
│   └─ message.Status = 0 (UNREAD default)
└─ IConnectionService.CreateMessageAsync(message)

    ↓
[INSERT MESSAGE]
├─ IConnectionRepository.CreateMessageAsync(message)
│   └─ DbContext.Message.Add(message)
│   └─ DbContext.SaveChangesAsync()
│       └─ INSERT Message (MessageRoomId, UserId, Content, CreatedAt, Status)
│       └─ SELECT SCOPE_IDENTITY() → get generated Id
└─ Return created message with Id

    ↓
[UPDATE ROOM LAST MESSAGE TIME]
├─ IConnectionRepository.GetRoomByIdAsync(created.MessageRoomId)
│   └─ SELECT * FROM Room WHERE Id = @roomId
├─ room.LastMessAt = created.CreatedAt
├─ IConnectionRepository.UpdateRoomAsync(room)
│   └─ UPDATE Room SET LastMessAt = @timestamp WHERE Id = @roomId
│   └─ DbContext.SaveChangesAsync()
└─ Return void

    ↓
[REALTIME BROADCAST TO ROOM - SIGNALR]
├─ IHubContext<ChatHub>.Clients.Group($"room_{roomId}")
│   .SendAsync("MessageReceived", {
│     id: created.Id,
│     roomId: created.MessageRoomId,
│     userId: created.UserId,
│     content: created.Content,
│     createdAt: created.CreatedAt,
│     status: created.Status
│   })
└─ (Non-blocking, fire-and-forget)

    ↓
[REALTIME SERVICE BROADCAST]
├─ IConnectionEventPublisher.PublishMessageCreatedAsync({
│   messageId: created.Id,
│   roomId: roomId,
│   userId: created.UserId,
│   content: created.Content,
│   createdAt: created.CreatedAt
│ })
└─ (Async event to Realtime Service)

    ↓
[NOTIFICATION FOR OFFLINE USERS]
├─ If recipient user offline:
│   └─ INotificationEventPublisher.PublishChatMessageNotificationAsync({
│       eventType: "connection.message.created",
│       recipientId: otherUserId,
│       actorId: created.UserId,
│       objectId: created.Id,
│       title: "New message",
│       content: created.Content,
│       timestamp: created.CreatedAt
│     })
└─ (Consumed by Notification Service for push notifications)

    ↓
[RETURN RESPONSE]
└─ 201 Created
    Body: MessageDto {
      id: created.Id,
      roomId: created.MessageRoomId,
      userId: created.UserId,
      content: created.Content,
      createdAt: created.CreatedAt,
      status: created.Status
    }
```

**External Service Calls:**
- None synchronous (all async for real-time)

**Transaction Scope:**
- Message INSERT and Room UPDATE in separate transactions
- Message creation guaranteed (auto-retry on Room update failure)

**Real-time Broadcasting:**
- SignalR broadcast to room group
- Realtime Service event async
- Notification event async (offline handling)

---

## 5. NOTIFICATION SERVICE

### Service Architecture

**Controllers Location:** `D:\Capstone\src\Services\Notification\Notification.API\Controllers\`
- `NotificationController.cs` - Notification retrieval and management
- `FcmController.cs` - Firebase Cloud Messaging setup
- `DeviceTokenController.cs` - Device token management

**Service Layer:** `Notification.Application\Services\`
- `NotificationService.cs` - Notification retrieval and filtering
- `NotificationPublishingService.cs` - Event consumption and creation
- `FcmService.cs` - Firebase push notification sending
- `AggregationFlushService.cs` - Batch notification aggregation

**Domain Layer:** `Notification.Domain\Entities\`
- `NotificationEntity.cs` - Notification record
- `DeviceTokenEntity.cs` - User device tokens
- `NotificationSettingsEntity.cs` - User preferences
- `PushNotificationLogEntity.cs` - Delivery logs

---

### Critical Function Specifications

#### 5.1 GetNotifications

**Function Signature:**
```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetNotificationsAsync(
    string userId, int? cursor, int limit)
```

**Parameters:**
- `userId: string` - User ID from JWT
- `cursor: int?` - Cursor for pagination (notification ID from previous page)
- `limit: int` - Items per page (clamped 1-50)

**Return Type:** `Task<CursorPagedResult<UserNotificationDto>>`

**Business Logic:**
1. Query notifications for user ordered by CreatedAt DESC
2. Apply cursor pagination (Id < cursor)
3. Load limit + 1 to detect hasMore
4. Build actor maps from stored actor data
5. Resolve missing actor info via HTTP (fallback)
6. Enrich DTOs with actor details
7. Return cursor-paginated result

**Database Operations:**
```sql
SELECT TOP (limit + 1) *
FROM NotificationEntity
WHERE UserId = @userId
AND Id < @cursor (if cursor provided)
ORDER BY CreatedAt DESC
```

**Data Transformation:**
```
NotificationEntity → UserNotificationDto {
  id: n.Id,
  userId: n.UserId,
  title: n.Title,
  content: n.Content,
  type: n.Type,
  objectId: n.ObjectId,
  actor: {
    id: int.Parse(n.ActorId) ?? 0,
    name: n.ActorName,
    avatar: n.ActorAvatar,
    role: n.ActorType == "COMPANY" ? "COMPANY" : "USER"
  },
  createdAt: n.CreatedAt,
  isRead: n.IsRead
}
```

**Example Response:**
```json
{
  "items": [
    {
      "id": 5001,
      "userId": "123",
      "title": "Challenge Graded",
      "content": "Your solution for 'Build Todo API' scored 85/100",
      "type": "CHALLENGE_GRADED",
      "objectId": "550e8400-e29b-41d4-a716-446655440000",
      "actor": {
        "id": 0,
        "name": "System",
        "avatar": null,
        "role": "SYSTEM"
      },
      "createdAt": "2025-06-15T16:00:00Z",
      "isRead": false
    },
    {
      "id": 5000,
      "userId": "123",
      "title": "New Message",
      "content": "John: Hi, how are you?",
      "type": "CHAT_MESSAGE",
      "objectId": "2001",
      "actor": {
        "id": 456,
        "name": "John Doe",
        "avatar": "https://...",
        "role": "USER"
      },
      "createdAt": "2025-06-15T15:36:00Z",
      "isRead": false
    }
  ],
  "nextCursor": 4999,
  "hasMore": true
}
```

---

#### 5.2 GetCommunityNotifications

**Function Signature:**
```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetCommunityNotificationsAsync(
    string userId, int? cursor, int limit)
```

**Parameters:**
- `userId: string` - User ID
- `cursor: int?` - Pagination cursor
- `limit: int` - Items per page

**Return Type:** `Task<CursorPagedResult<UserNotificationDto>>`

**Business Logic:**
1. Query notifications filtered by CommunityTypes only:
   - POST_FAVORITED
   - COMMENT_CREATED
   - REPLY_TO_COMMENT
   - POST_REPORTED
2. Apply cursor pagination
3. Enrich with actor data
4. Return paginated result

**Notification Type Groups:**
```csharp
public static class NotificationTypeGroups
{
    public static readonly string[] CommunityTypes = new[]
    {
        "POST_FAVORITED",
        "POST_SAVED",
        "COMMENT_CREATED",
        "REPLY_TO_COMMENT",
        "POST_REPORTED",
        "COMMENT_FAVORITED"
    };
}
```

---

#### 5.3 GetMessageNotifications

**Function Signature:**
```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetMessageNotificationsAsync(
    string userId, int? cursor, int limit)
```

**Parameters:**
- `userId: string` - User ID
- `cursor: int?` - Pagination cursor
- `limit: int` - Items per page

**Return Type:** `Task<CursorPagedResult<UserNotificationDto>>`

**Business Logic:**
1. Query notifications filtered by Type = "CHAT_MESSAGE" only
2. Apply cursor pagination
3. Enrich with actor data
4. Return paginated result

**Example Response:**
```json
{
  "items": [
    {
      "id": 5000,
      "userId": "123",
      "title": "New Message from John",
      "content": "Hi, how are you?",
      "type": "CHAT_MESSAGE",
      "objectId": "2001",
      "actor": {
        "id": 456,
        "name": "John Doe",
        "avatar": "https://...",
        "role": "USER"
      },
      "createdAt": "2025-06-15T15:36:00Z",
      "isRead": false
    }
  ],
  "nextCursor": null,
  "hasMore": false
}
```

---

#### 5.4 PublishNotification

**Function Signature:**
```csharp
public async Task<NotificationCreatedEventDto> BuildCreatedEventAsync(
    NotificationEntity entity)
```

**Parameters:**
- `entity: NotificationEntity` - Notification to publish

**Return Type:** `Task<NotificationCreatedEventDto>`

**Business Logic:**
1. Extract notification data from entity
2. Build event DTO with:
   - Notification ID, type, user ID
   - Actor information (resolved or stored)
   - Content, title, timestamp
3. Return DTO for event publishing
4. Used for push notifications via FCM

**Event Structure:**
```json
{
  "eventType": "notification.created",
  "notificationId": 5001,
  "userId": "123",
  "title": "Challenge Graded",
  "content": "Your solution scored 85/100",
  "type": "CHALLENGE_GRADED",
  "objectId": "550e8400...",
  "actor": {
    "id": 0,
    "name": "System",
    "avatar": null,
    "role": "SYSTEM"
  },
  "createdAt": "2025-06-15T16:00:00Z"
}
```

---

#### 5.5 AggregateNotifications

**Function Signature:**
```csharp
public async Task<List<NotificationEntity>> AggregateNotificationsAsync(
    string userId, string aggregationType)
```

**Parameters:**
- `userId: string` - User to aggregate for
- `aggregationType: string` - Aggregation key (e.g., "POST_{postId}", "COMMENT_REPLIES")

**Return Type:** `Task<List<NotificationEntity>>`

**Business Logic:**
1. Query recent notifications matching aggregation key
2. Example aggregations:
   - Multiple "comment_created" on same post → single aggregated notification
   - Multiple "reply_to_comment" on same comment → single notification "3 people replied"
   - Multiple "post_favorited" on same post → "5 people favorited your post"
3. Combine message, update CreatedAt to latest
4. Return aggregated notification list

**Aggregation Examples:**

**Before Aggregation:**
```json
[
  {
    "id": 5010,
    "type": "POST_FAVORITED",
    "content": "John favorited your post",
    "actorId": "456",
    "actorName": "John",
    "createdAt": "2025-06-15T16:05:00Z"
  },
  {
    "id": 5009,
    "type": "POST_FAVORITED",
    "content": "Jane favorited your post",
    "actorId": "789",
    "actorName": "Jane",
    "createdAt": "2025-06-15T16:10:00Z"
  },
  {
    "id": 5008,
    "type": "POST_FAVORITED",
    "content": "Bob favorited your post",
    "actorId": "999",
    "actorName": "Bob",
    "createdAt": "2025-06-15T16:15:00Z"
  }
]
```

**After Aggregation:**
```json
[
  {
    "id": 5010,
    "type": "POST_FAVORITED_AGGREGATED",
    "content": "Jane, Bob and 1 other person favorited your post",
    "actorId": "789,999,456",
    "actorName": "Jane, Bob, John",
    "aggregationCount": 3,
    "createdAt": "2025-06-15T16:15:00Z"
  }
]
```

---

### Test Coverage Plan

#### GetNotifications Tests

**Happy Path:**
```gherkin
Scenario: Load user notifications with cursor pagination
  Given user "123" with 50 unread notifications
  When requesting notifications with limit 20
  Then 20 notifications returned
  And nextCursor points to oldest notification ID
  And hasMore true if more exist
  And actor info enriched (stored data used, HTTP fallback if needed)
  And notifications ordered chronologically (newest first)
```

**Pagination Tests:**
- cursor = null → return first batch
- cursor = non-existent ID → graceful handling (return empty)
- limit = 0 → clamped to 1
- limit = 100 → clamped to 50
- Empty notifications → return empty items, hasMore = false

**Actor Enrichment Tests:**
- Stored actor data present → use directly (no HTTP call)
- Stored data missing → HTTP GET to UserProfile/Company service
- HTTP call fails → use fallback actor with minimal info
- Multiple same actor → deduplicate HTTP calls

**Type Filtering Tests:**
- All types included in GetNotifications
- Only community types in GetCommunityNotifications
- Only CHAT_MESSAGE in GetMessageNotifications

---

#### PublishNotification Tests

**Happy Path:**
```gherkin
Scenario: Build notification event for publishing
  Given notification entity with:
    - Type: "CHALLENGE_GRADED"
    - UserId: "123"
    - ActorId: null (system notification)
    - Content and title
  When building event
  Then event created with all fields mapped
  And timestamp preserved
  And actor data included (null for system)
  And event returned as NotificationCreatedEventDto
```

**Data Mapping Tests:**
- All NotificationEntity fields map to EventDto correctly
- Timestamps in ISO8601 format
- ActorId nullable handling
- Content truncation if > 500 chars

**Event Publishing Tests:**
- Event published to message queue (RabbitMQ/Azure Service Bus)
- FCM push triggered for users with device tokens
- Retry on delivery failure
- Log delivery status

---

#### AggregateNotifications Tests

**Happy Path:**
```gherkin
Scenario: Aggregate multiple same-type notifications
  Given 5 notifications: "John favorited", "Jane favorited", "Bob favorited", ...
  When aggregating by POST_FAVORITED
  Then single aggregated notification returned
  And content: "Jane, Bob and 2 other people favorited your post"
  And actorIds combined: "789,999,..."
  And createdAt = latest timestamp
```

**Aggregation Rules Tests:**
- POST_FAVORITED on same post → aggregated
- COMMENT_CREATED on same post → aggregated
- REPLY_TO_COMMENT on same comment → aggregated
- Different post IDs → not aggregated
- Mixed types → not aggregated

**Edge Cases:**
- 2 notifications → "John and Jane favorited..."
- 3+ notifications → "Jane, Bob and X other(s) favorited..."
- 10+ aggregated → "Jane, Bob and 8 others favorited..."

---

### Data Flow Documentation - Notification Service

#### GetNotifications Data Flow

```
API Request (userId, cursor, limit)
    ↓
NotificationController.GetNotifications()
    ↓
[EXTRACT USER ID FROM JWT]
├─ User.FindFirst(ClaimTypes.NameIdentifier)
├─ User.FindFirst("sub")
└─ Throw UnauthorizedAccessException if missing

    ↓
[CLAMP LIMIT]
└─ limit = Math.Clamp(limit, 1, 50)

    ↓
INotificationService.GetNotificationsAsync(userId, cursor, limit)
    ↓
[FETCH NOTIFICATIONS]
├─ INotificationRepository.GetNotificationsAsync(userId, cursor, limit)
│   └─ SELECT TOP (limit + 1) *
│      FROM NotificationEntity
│      WHERE UserId = @userId
│      AND (cursor == null OR Id < @cursor)
│      ORDER BY CreatedAt DESC
├─ Determine hasMore = count > limit
├─ If hasMore: take first limit only
└─ Return notifications list

    ↓
[BUILD ACTOR MAP - DEDUPLICATED]
├─ Extract unique (ActorId, ActorType, ActorName, ActorAvatar)
├─ For actors with stored data (ActorName not null):
│   └─ Use stored data directly (no HTTP call)
└─ For actors with missing stored data:
    └─ Collect list for HTTP enrichment

    ↓
[HTTP ENRICHMENT - PARALLEL BATCH CALLS]
├─ If missing actors:
│   ├─ Group by ActorType (USER vs COMPANY)
│   ├─ IActorResolverClient.ResolveBatchAsync(userActorIds)
│   │   └─ HTTP GET /api/actors/batch?ids=456,789
│   │       Returns: [{id, name, avatar, role}, ...]
│   └─ Merge with stored data
└─ Build actorMap: {ActorId → ActorDto}

    ↓
[BUILD DTOS]
└─ For each notification:
    ├─ Create UserNotificationDto {
    │   id: n.Id,
    │   userId: n.UserId,
    │   title: n.Title,
    │   content: n.Content,
    │   type: n.Type,
    │   objectId: n.ObjectId,
    │   actor: actorMap.Get(n.ActorId) ?? BuildFallback(n.ActorId),
    │   createdAt: n.CreatedAt,
    │   isRead: n.IsRead
    │ }
    └─ Add to items list

    ↓
[RETURN PAGINATED RESULT]
└─ CursorPagedResult<UserNotificationDto> {
    Items: dtos,
    NextCursor: hasMore ? notifications.Last().Id : null,
    HasMore: hasMore
  }
```

**Service Dependencies:**
- `INotificationRepository` - Notification query
- `IActorResolverClient` - HTTP enrichment (fallback only)

**Caching Strategy:**
- Actor info cached 5 minutes in Redis
- Notification list NOT cached (real-time)

---

#### PublishNotification Data Flow

```
Event from Message Queue (any service)
    ↓
NotificationPublishingService (Consumer)
    ↓
[RECEIVE EVENT]
├─ Event payload:
│   {
│     eventType: "challenge.submission.graded",
│     submissionId: "...",
│     userId: "123",
│     score: 85,
│     timestamp: "2025-06-15T16:00:00Z",
│     actorId: null (system),
│     actorType: "SYSTEM",
│     actorName: "Challenge System",
│     title: "Challenge Graded",
│     content: "..."
│   }
└─ Deserialize event

    ↓
[CREATE NOTIFICATION ENTITY]
├─ NotificationEntity {
│   Id: auto-generated,
│   UserId: event.userId,
│   Title: event.title,
│   Content: event.content,
│   Type: MapEventTypeToNotificationType(event.eventType),
│   ObjectId: event.objectId,
│   ActorId: event.actorId,
│   ActorType: event.actorType,
│   ActorName: event.actorName,
│   ActorAvatar: event.actorAvatar,
│   CreatedAt: UtcNow,
│   IsRead: false
│ }
└─ INotificationRepository.AddAsync(entity)

    ↓
[PERSIST TO DATABASE]
└─ DbContext.SaveChangesAsync()
    └─ INSERT NotificationEntity

    ↓
[BUILD CREATED EVENT]
├─ INotificationService.BuildCreatedEventAsync(entity)
│   └─ NotificationCreatedEventDto {
│       eventType: "notification.created",
│       notificationId: entity.Id,
│       userId: entity.UserId,
│       ...
│     }
└─ Return event

    ↓
[PUBLISH TO FCM]
├─ IDeviceTokenService.GetDeviceTokensAsync(entity.UserId)
│   └─ SELECT * FROM DeviceTokenEntity
│      WHERE UserId = @userId AND IsActive = true
├─ For each device token:
│   ├─ IFcmService.SendNotificationAsync({
│   │   token: deviceToken,
│   │   title: entity.Title,
│   │   body: entity.Content,
│   │   data: {
│   │     notificationId: entity.Id,
│   │     type: entity.Type,
│   │     objectId: entity.ObjectId
│   │   }
│   │ })
│   │   └─ HTTP POST to Firebase Cloud Messaging
│   │       Returns: {success, messageId}
│   └─ INSERT PushNotificationLogEntity {
│       notificationId: entity.Id,
│       deviceTokenId: token.Id,
│       sentAt: UtcNow,
│       status: "SENT",
│       messageId: response.messageId
│     }
└─ Log any failures for retry

    ↓
[CACHE INVALIDATION]
├─ Remove from Redis: $"unread:{userId}"
└─ Update unread count cache

    ↓
[RETURN ASYNC]
└─ Consumer completes (notification fully processed)
```

**Event Sources:**
- Challenge Service (submission.graded)
- Community Service (post.created, comment.created
