# Notification Service Guide

**Base URL:** `http://localhost:5011` (dev) | `http://notification-service:80` (Docker)  
**Swagger UI:** `http://localhost:5011/swagger`  
**SignalR Hub:** `ws://localhost:5011/hubs/notifications`

---

## Tổng quan kiến trúc

```
Other Services (Community, Connection, ...)
        │
        │  publish event (RabbitMQ)
        ▼
Exchange: skillsnap.events  (topic)
        │
        ▼
Queue: notification.events
 binding: post.*, connection.*, portfolio.*, job.*, system.*
        │
        ▼
RabbitMQConsumer (BackgroundService)
  │  save to DB
  │  resolve actor (UserProfile Service + memory cache)
  │  push DTO via SignalR
        │
        ▼
NotificationHub  ──► user_{userId} group ──► Frontend (realtime)
```

- **Dead-letter queue (DLQ):** Nếu xử lý lỗi → message vào `notification.events.dlq` (không mất dữ liệu)
- **Redis:** Cache số lượng thông báo chưa đọc per-user (TTL 5 phút)
- **Actor Resolution:** Chỉ gọi UserProfile Service, cache trong memory 2 phút

---

## Xác thực (Authentication)

Notification Service dùng **JWT Bearer Token** lấy từ Auth Service.

**Trên Swagger UI:** Click **Authorize 🔒** → nhập `Bearer <token>`

**Với HTTP client:**
```
Authorization: Bearer <access_token>
```

**Với SignalR WebSocket** — token phải truyền qua query string:
```
ws://localhost:5011/hubs/notifications?access_token=<token>
```

---

## REST API

### 1. Lấy danh sách thông báo

**`GET /api/notifications`** 🔒 Bắt buộc đăng nhập

Phân trang theo cursor (id DESC — mới nhất trước).

**Query Parameters:**

| Tên | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `cursor` | int? | — | `nextCursor` của trang trước |
| `limit` | int | 20 | Số thông báo mỗi trang (1–50) |

**Request — trang đầu:**
```http
GET /api/notifications?limit=20
Authorization: Bearer <token>
```

**Request — trang tiếp theo:**
```http
GET /api/notifications?cursor=85&limit=20
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": 102,
      "userId": "user_abc123",
      "title": "Bình luận mới",
      "content": "Nguyen Van A đã bình luận bài viết của bạn",
      "type": "COMMUNITY",
      "objectId": "post_xyz789",
      "actor": {
        "id": "user_def456",
        "name": "Nguyen Van A",
        "avatarUrl": "https://cdn.example.com/avatars/def456.jpg"
      },
      "createdAt": "2026-03-15T13:00:00Z",
      "isRead": false
    },
    {
      "id": 101,
      "userId": "user_abc123",
      "title": "Yêu thích bài viết",
      "content": "Tran Thi B đã thích bài viết của bạn",
      "type": "COMMUNITY",
      "objectId": "post_xyz789",
      "actor": {
        "id": "user_ghi789",
        "name": "Tran Thi B",
        "avatarUrl": null
      },
      "createdAt": "2026-03-15T12:45:00Z",
      "isRead": true
    }
  ],
  "nextCursor": 101,
  "hasMore": true
}
```

> **Lưu ý cursor:** `nextCursor` là `id` của bài cuối cùng trong trang. Truyền vào request tiếp theo để lấy trang sau. Nếu `hasMore = false` thì đã hết.

---

### 2. Lấy số lượng thông báo chưa đọc

**`GET /api/notifications/unread-count`** 🔒

Kết quả được cache Redis 5 phút. Tự động invalidate khi có thông báo mới hoặc mark-read.

**Request:**
```http
GET /api/notifications/unread-count
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
{
  "count": 5
}
```

---

### 3. Đánh dấu một thông báo đã đọc

**`PUT /api/notifications/{id}/read`** 🔒

**Request:**
```http
PUT /api/notifications/102/read
Authorization: Bearer <token>
```

**Response `204 No Content`** (không có body)

---

### 4. Đánh dấu tất cả đã đọc

**`PUT /api/notifications/read-all`** 🔒

**Request:**
```http
PUT /api/notifications/read-all
Authorization: Bearer <token>
```

**Response `204 No Content`**

---

## SignalR — Nhận thông báo realtime

### Kết nối

```javascript
import * as signalR from "@microsoft/signalr";

const token = "your-jwt-token";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5011/hubs/notifications", {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

// Lắng nghe sự kiện thông báo mới
connection.on("ReceiveNotification", (notification) => {
  console.log("Thông báo mới:", notification);
  // notification có cùng cấu trúc với UserNotificationDto
  // { id, userId, title, content, type, objectId, actor, createdAt, isRead }
});

await connection.start();
console.log("Đã kết nối SignalR");
```

### Payload nhận được (`ReceiveNotification`)

```json
{
  "id": 103,
  "userId": "user_abc123",
  "title": "Yêu thích bài viết",
  "content": "Nguyen Van A đã thích bài viết của bạn",
  "type": "COMMUNITY",
  "objectId": "post_xyz789",
  "actor": {
    "id": "user_def456",
    "name": "Nguyen Van A",
    "avatarUrl": "https://cdn.example.com/avatars/def456.jpg"
  },
  "createdAt": "2026-03-15T13:10:00Z",
  "isRead": false
}
```

> **Group:** Mỗi user join group `user_{userId}` khi kết nối. Thông báo chỉ đẩy đến đúng user.

---

## Cách push event từ các service khác

Để tạo thông báo, các service khác **publish một message lên RabbitMQ** với đúng exchange và routing key. Notification Service sẽ tự động nhận, lưu DB và đẩy realtime.

### Cấu hình RabbitMQ

| Thuộc tính | Giá trị |
|---|---|
| Exchange | `skillsnap.events` |
| Exchange type | `topic` |
| Routing key pattern | `post.*` / `connection.*` / `portfolio.*` / `job.*` / `system.*` |

### Định dạng message (JSON)

```json
{
  "eventType": "post.liked",
  "userId": "<người nhận thông báo>",
  "actorId": "<người thực hiện hành động>",
  "actorType": "USER",
  "objectId": "<id của đối tượng, ví dụ postId>",
  "title": "Tiêu đề thông báo",
  "content": "Nội dung mô tả thông báo",
  "type": "COMMUNITY",
  "createdAt": "2026-03-15T13:00:00Z"
}
```

**Các giá trị `actorType`:**

| Giá trị | Ý nghĩa |
|---|---|
| `USER` | Người dùng thường (gọi UserProfile Service để resolve tên, avatar) |
| `SYSTEM` | Thông báo hệ thống (không resolve actor) |

**Các giá trị `type` (loại thông báo):**

| Giá trị | Dùng cho |
|---|---|
| `COMMUNITY` | Bài viết community (like, comment, reply, share) |
| `CONNECTION` | Kết nối bạn bè (accepted, rejected) |
| `PORTFOLIO` | Portfolio (approved) |
| `JOB` | Việc làm (invitation) |
| `SYSTEM` | Thông báo hệ thống |

### Routing keys được hỗ trợ

| Routing Key | Ý nghĩa |
|---|---|
| `post.liked` | Ai đó thích bài viết của bạn |
| `post.commented` | Ai đó bình luận bài viết của bạn |
| `post.replied` | Ai đó reply comment của bạn |
| `post.shared` | Ai đó chia sẻ bài viết |
| `post.mentioned` | Ai đó mention bạn trong bài |
| `connection.accepted` | Yêu cầu kết nối được chấp nhận |
| `connection.rejected` | Yêu cầu kết nối bị từ chối |
| `portfolio.approved` | Portfolio được duyệt |
| `job.invitation` | Được mời ứng tuyển |
| `system.announcement` | Thông báo hệ thống |

---

## Ví dụ: Community Service publish event

### Kịch bản: Ai đó **thích (favorite)** bài viết

Trong `CommunityPostService.cs`, sau khi lưu favorite thành công, publish event:

**Bước 1 — Cài package RabbitMQ.Client vào Community.Infrastructure:**
```xml
<PackageReference Include="RabbitMQ.Client" Version="7.0.0" />
```

**Bước 2 — Tạo `IEventPublisher` interface trong Community.Application:**
```csharp
// Community.Application/Interfaces/IEventPublisher.cs
namespace Community.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, object payload);
}
```

**Bước 3 — Implement `RabbitMQPublisher` trong Community.Infrastructure:**
```csharp
// Community.Infrastructure/Messaging/RabbitMQPublisher.cs
using System.Text;
using System.Text.Json;
using Community.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Community.Infrastructure.Messaging;

public class RabbitMQPublisher : IEventPublisher, IAsyncDisposable
{
    private const string Exchange = "skillsnap.events";
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQPublisher(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            UserName = config["RabbitMQ:UserName"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
        // Khởi tạo bất đồng bộ — gọi InitAsync() trước khi dùng
        _factory = factory;
    }

    private readonly ConnectionFactory _factory;

    private async Task EnsureConnectedAsync()
    {
        if (_connection is null || !_connection.IsOpen)
        {
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true);
        }
    }

    public async Task PublishAsync(string routingKey, object payload)
    {
        await EnsureConnectedAsync();
        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);
        await _channel!.BasicPublishAsync(Exchange, routingKey, body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}
```

**Bước 4 — Đăng ký trong Community `Program.cs`:**
```csharp
builder.Services.AddSingleton<IEventPublisher, RabbitMQPublisher>();
```

**Bước 5 — Gọi publisher trong service khi có favorite:**
```csharp
// Community.Application/Services/CommunityPostService.cs
public async Task FavoritePostAsync(string postId, string userId)
{
    // ... lưu favorite vào DB

    // Lấy thông tin bài viết để biết chủ bài
    var post = await _postRepo.GetByIdAsync(postId);
    if (post == null || post.UserId == userId) return; // không gửi thông báo cho chính mình

    await _eventPublisher.PublishAsync("post.liked", new
    {
        EventType = "post.liked",
        UserId = post.UserId,        // người nhận = chủ bài viết
        ActorId = userId,             // người thực hiện = người like
        ActorType = "USER",
        ObjectId = postId,
        Title = "Yêu thích bài viết",
        Content = "Ai đó đã thích bài viết của bạn",
        Type = "COMMUNITY",
        CreatedAt = DateTime.UtcNow
    });
}
```

---

### Kịch bản: Ai đó **bình luận (comment)** bài viết

```csharp
// Trong CommunityPostService.cs sau khi lưu comment
public async Task AddCommentAsync(string postId, string commenterId, string commentContent)
{
    // ... lưu comment vào DB

    var post = await _postRepo.GetByIdAsync(postId);
    if (post == null || post.UserId == commenterId) return;

    await _eventPublisher.PublishAsync("post.commented", new
    {
        EventType = "post.commented",
        UserId = post.UserId,          // người nhận = chủ bài viết
        ActorId = commenterId,          // người comment
        ActorType = "USER",
        ObjectId = postId,
        Title = "Bình luận mới",
        Content = $"Ai đó đã bình luận bài viết của bạn",
        Type = "COMMUNITY",
        CreatedAt = DateTime.UtcNow
    });
}
```

---

### Kịch bản: Ai đó **reply** comment

```csharp
await _eventPublisher.PublishAsync("post.replied", new
{
    EventType = "post.replied",
    UserId = originalCommentOwnerId,   // người nhận = chủ comment gốc
    ActorId = replierId,
    ActorType = "USER",
    ObjectId = postId,
    Title = "Có người trả lời bình luận",
    Content = "Ai đó đã trả lời bình luận của bạn",
    Type = "COMMUNITY",
    CreatedAt = DateTime.UtcNow
});
```

---

### Kịch bản: Connection Service — Chấp nhận kết nối

```csharp
await _eventPublisher.PublishAsync("connection.accepted", new
{
    EventType = "connection.accepted",
    UserId = requesterId,              // người gửi lời kết nối ban đầu
    ActorId = acceptorId,              // người chấp nhận
    ActorType = "USER",
    ObjectId = connectionId,
    Title = "Yêu cầu kết nối được chấp nhận",
    Content = "Ai đó đã chấp nhận lời mời kết nối của bạn",
    Type = "CONNECTION",
    CreatedAt = DateTime.UtcNow
});
```

---

### Kịch bản: Thông báo hệ thống (không có actor)

```csharp
await _eventPublisher.PublishAsync("system.announcement", new
{
    EventType = "system.announcement",
    UserId = targetUserId,
    ActorId = (string?)null,
    ActorType = "SYSTEM",
    ObjectId = (string?)null,
    Title = "Thông báo hệ thống",
    Content = "Hệ thống sẽ bảo trì lúc 2:00 AM ngày mai",
    Type = "SYSTEM",
    CreatedAt = DateTime.UtcNow
});
```

---

## Luồng xử lý đầy đủ

```
Community Service
  │ (1) User A thích bài của User B
  ▼
RabbitMQPublisher.PublishAsync("post.liked", { userId: "B", actorId: "A", ... })
  │
  ▼ Exchange: skillsnap.events, routing key: post.liked
  │
  ▼
Queue: notification.events
  │
  ▼
Notification.RabbitMQConsumer (nhận message)
  │ (2) Tạo NotificationEntity trong DB
  │ (3) Gọi UserProfile để resolve tên/avatar của User A (cache 2 phút)
  │ (4) Invalidate Redis cache unread count của User B
  │ (5) Push UserNotificationDto qua SignalR → group "user_B"
  ▼
User B nhận realtime qua "ReceiveNotification" event
  │
  ▼ (Sau đó User B mở app, gọi REST API)
GET /api/notifications → lấy danh sách (có thông báo vừa tạo)
GET /api/notifications/unread-count → { count: 1 }
PUT /api/notifications/{id}/read → đánh dấu đã đọc
```

---

## Cấu hình môi trường

### docker-compose environment cho Notification Service

```yaml
notification-service:
  environment:
    - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=NotificationServiceDb;...
    - JwtSettings__SecretKey=your-256-bit-secret-key-here-make-it-long
    - JwtSettings__Issuer=SkillSnapAuth
    - JwtSettings__Audience=SkillSnapUsers
    - RabbitMQ__HostName=rabbitmq
    - RabbitMQ__UserName=guest
    - RabbitMQ__Password=guest
    - RabbitMQ__Port=5672
    - Redis__ConnectionString=redis:6379
    - ServiceUrls__UserProfile=http://userprofile-service:8080
```

### docker-compose environment cho service gửi event (ví dụ Community)

```yaml
community-service:
  environment:
    - RabbitMQ__HostName=rabbitmq
    - RabbitMQ__UserName=guest
    - RabbitMQ__Password=guest
```

---

## Kiểm tra RabbitMQ Management UI

Sau khi chạy Docker, vào `http://localhost:15672` (user: `guest` / pass: `guest`) để:

- Xem queue `notification.events` có nhận message không
- Xem queue `notification.events.dlq` có message lỗi không
- Publish test message thủ công

**Publish test message từ Management UI:**
1. Vào tab **Exchanges** → chọn `skillsnap.events`
2. Mục **Publish message** → Routing key: `post.liked`
3. Payload:
```json
{
  "eventType": "post.liked",
  "userId": "test-user-id",
  "actorId": "actor-user-id",
  "actorType": "USER",
  "objectId": "post-123",
  "title": "Yêu thích bài viết",
  "content": "Ai đó đã thích bài viết của bạn",
  "type": "COMMUNITY",
  "createdAt": "2026-03-15T13:00:00Z"
}
```
