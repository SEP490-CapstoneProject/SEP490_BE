# Subscription Service Guide

## Overview

Subscription Service quản lý subscription plans, billing, và entitlements cho users trong SkillSnap platform. Service này sử dụng:
- **Redis** để cache entitlements (O(1) lookup)
- **RabbitMQ** để publish/consume events
- **SQL Server** để lưu trữ plans, subscriptions
- **Lua scripts** để atomic quota enforcement

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              Subscription Service                            │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐            │
│  │   Plans    │  │PlanFeatures│  │Subscription│            │
│  │  (DB)      │  │   (DB)     │  │   (DB)     │            │
│  └────────────┘  └────────────┘  └────────────┘            │
│                          │                                   │
│                          ▼                                   │
│                  ┌──────────────────┐                        │
│                  │   Redis Cache    │                        │
│                  │ user:{id}:       │                        │
│                  │   entitlements   │                        │
│                  │ user:{id}:usage  │                        │
│                  └──────────────────┘                        │
│                          │                                   │
│                          ▼                                   │
│                  Publish Events                              │
│                  (RabbitMQ)                                  │
└─────────────────────────────────────────────────────────────┘
```

## Database Schema

### Plans
```sql
CREATE TABLE Plans (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Price DECIMAL(10,2) NOT NULL,
    BillingCycle INT NOT NULL, -- 1=Monthly, 2=Yearly
    Status INT NOT NULL DEFAULT 1, -- 1=Active, 0=Inactive
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2
);
```

### PlanFeatures
```sql
CREATE TABLE PlanFeatures (
    Id INT PRIMARY KEY IDENTITY,
    PlanId INT NOT NULL FOREIGN KEY REFERENCES Plans(Id),
    FeatureKey NVARCHAR(50) NOT NULL,
    FeatureName NVARCHAR(100) NOT NULL,
    Value NVARCHAR(50) NOT NULL,
    Type INT NOT NULL, -- 1=Boolean, 2=Number, 3=Text
    Status INT NOT NULL DEFAULT 1
);
```

### Subscriptions
```sql
CREATE TABLE Subscriptions (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    PlanId INT NOT NULL FOREIGN KEY REFERENCES Plans(Id),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Active, 2=Expired, 3=Cancelled
    PaymentStatus INT NOT NULL DEFAULT 0,
    AutoRenew BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2
);
```

## Seed Data - Plans

| Plan | Price | Features |
|------|-------|----------|
| **Free** | $0/mo | MAX_APPLY=5, MAX_PORTFOLIOS=1, AI_MATCHING=false |
| **Pro** | $9.99/mo | MAX_APPLY=20, MAX_PORTFOLIOS=5, AI_MATCHING=true, BOOST_PROFILE=true |
| **Premium** | $19.99/mo | MAX_APPLY=-1 (unlimited), all features true |

## API Endpoints

### Plans (Public)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/plans` | List all active plans | ❌ |
| GET | `/api/plans/{id}` | Get plan details with features | ❌ |

### Subscriptions (User)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/subscriptions/subscribe` | Subscribe to a plan | ✅ |
| POST | `/api/subscriptions/upgrade` | Upgrade subscription | ✅ |
| GET | `/api/subscriptions/me` | Get my subscription | ✅ |
| POST | `/api/subscriptions/{id}/cancel` | Cancel subscription | ✅ |
| GET | `/api/subscriptions/entitlements/{userId}` | Get user entitlements (for other services) | Internal |

### Admin - Plan Management
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/admin/plans` | Create new plan | Admin |
| PUT | `/api/admin/plans/{id}` | Update plan | Admin |
| DELETE | `/api/admin/plans/{id}` | Delete plan (no active subs) | Admin |
| PATCH | `/api/admin/plans/{id}/toggle-active` | Toggle plan active status | Admin |

### Admin - Plan Features
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/admin/plans/{id}/features` | Add feature to plan | Admin |
| PUT | `/api/admin/plans/{id}/features/{featureId}` | Update feature | Admin |
| DELETE | `/api/admin/plans/{id}/features/{featureId}` | Delete feature | Admin |

### Admin - Subscription Management
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/subscriptions` | List all with filters | Admin |
| GET | `/api/admin/subscriptions/{id}` | Subscription details | Admin |
| GET | `/api/admin/users/{userId}/subscriptions` | User's subscription history | Admin |
| POST | `/api/admin/subscriptions/{id}/cancel` | Admin cancel with reason | Admin |
| POST | `/api/admin/subscriptions/{id}/extend` | Extend by months | Admin |
| POST | `/api/admin/subscriptions/{id}/refund` | Issue refund | Admin |

### Admin - Analytics
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/admin/analytics/overview` | MRR, ARR, churn, totals | Admin |
| GET | `/api/admin/analytics/revenue` | Revenue by plan, daily breakdown | Admin |
| GET | `/api/admin/analytics/subscriptions-by-plan` | Counts per plan status | Admin |
| GET | `/api/admin/analytics/churn` | Churn rate analysis | Admin |

### Health Checks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Overall service health |
| GET | `/health/redis` | Redis connectivity |
| GET | `/health/rabbitmq` | RabbitMQ connectivity |

## Redis Design

### Entitlements Cache
```
Key: user:{userId}:entitlements
Value: {
  "version": 1,
  "planId": 2,
  "planName": "Pro",
  "features": {
    "MAX_APPLY": 20,
    "MAX_PORTFOLIOS": 5,
    "AI_MATCHING": true
  },
  "expiredAt": "2026-12-31T23:59:59Z"
}
TTL: subscription.EndDate + 1 day buffer
```

### Usage Tracking (Atomic)
```
Key: user:{userId}:usage:{featureKey}:{yyyy-MM}
Value: Integer counter
TTL: 2 months (auto-reset via key versioning)
```

## Atomic Quota Enforcement (Lua Script)

```lua
-- Prevents race condition
local key = KEYS[1]
local limit = tonumber(ARGV[1])
if limit == -1 then return redis.call('INCR', key) end -- unlimited
local current = redis.call('INCR', key)
if current > limit then
    redis.call('DECR', key)
    return -1  -- Quota exceeded
end
return current  -- Success
```

## Events (RabbitMQ)

### Published Events
| Event | Routing Key | When |
|-------|-------------|------|
| SubscriptionActivatedEvent | subscription.activated | Subscription activated |
| SubscriptionUpgradedEvent | subscription.upgraded | Plan upgraded |
| SubscriptionExpiredEvent | subscription.expired | Subscription expired |
| SubscriptionCancelledEvent | subscription.cancelled | User cancelled |

### Consumed Events
| Event | Routing Key | Action |
|-------|-------------|--------|
| PaymentSucceededEvent | payment.succeeded | Activate subscription |

## 3-Level Fallback Strategy

```csharp
// Level 1: Redis cache (fast path)
var cached = await _redis.GetEntitlementsAsync(userId);
if (cached != null && cached.Version == 1 && cached.ExpiredAt > DateTime.UtcNow)
    return cached;

// Level 2: HTTP call to Subscription Service
var entitlements = await _httpClient.GetEntitlementsAsync(userId);
if (entitlements != null)
{
    await _redis.SetEntitlementsAsync(userId, entitlements);
    return entitlements;
}

// Level 3: Default free tier
return DefaultFreeTierEntitlements;
```

## Idempotency & Event Reliability

### ProcessedEvents Table
```sql
CREATE TABLE ProcessedEvents (
    EventId NVARCHAR(100) PRIMARY KEY,
    EventType NVARCHAR(50) NOT NULL,
    ProcessedAt DATETIME2 NOT NULL
);
```

### Outbox Pattern (RabbitMQ failures)
```sql
CREATE TABLE OutboxEvents (
    Id INT PRIMARY KEY IDENTITY,
    EventId NVARCHAR(100) NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Published, 2=Failed
    RetryCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL
);
```

## Background Services

1. **PaymentEventConsumer** - Listens for payment.succeeded events
2. **OutboxProcessorService** - Retries failed event publishes every 10s
3. **SubscriptionPreloadService** - Preloads active subscriptions to Redis on startup

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=SubscriptionServiceDb;..."
  },
  "Redis": {
    "ConnectionString": "redis:6379"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key",
    "Issuer": "SkillSnapAuth",
    "Audience": "SkillSnapUsers"
  }
}
```

## Feature Authorization Middleware

Other services can use `[RequireFeature]` attribute:

```csharp
[RequireFeature("MAX_APPLY")]
public async Task<IActionResult> CreateApplication()
{
    // Middleware already checked quota atomically
    // Just create the application
}
```

## Usage by Other Services

### Application Service Integration
```csharp
public class EntitlementChecker : IEntitlementChecker
{
    public async Task<bool> TryIncrementUsageAsync(int userId, string featureKey)
    {
        var entitlements = await GetEntitlementsWithFallbackAsync(userId);
        var limit = entitlements.Features[featureKey] as int? ?? 0;
        
        var (success, _) = await _redis.TryIncrementUsageAsync(userId, featureKey, limit);
        return success;
    }
}
```

## Docker Configuration

### docker-compose.yml
```yaml
subscription-service:
  build:
    context: .
    dockerfile: src/Services/Subscription/Dockerfile
  ports:
    - "5008:8080"
  environment:
    - ConnectionStrings__DefaultConnection=Server=sqlserver;...
    - Redis__ConnectionString=redis:6379
    - RabbitMQ__Host=rabbitmq
  depends_on:
    - sqlserver
    - rabbitmq
    - redis
```

## Running Locally

```bash
# Build
cd src/Services/Subscription/Subscription.API
dotnet build

# Run
dotnet run

# Run with Docker
docker-compose up subscription-service
```

## Testing

### Test Subscribe Flow
```bash
# 1. Subscribe
curl -X POST http://localhost:5008/api/subscriptions/subscribe \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"planId": 2}'

# 2. Check entitlements
curl http://localhost:5008/api/subscriptions/entitlements/1

# 3. Check my subscription
curl http://localhost:5008/api/subscriptions/me \
  -H "Authorization: Bearer <token>"
```

### Test Admin Plan Management
```bash
# Create new plan
curl -X POST http://localhost:5008/api/admin/plans \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Enterprise",
    "description": "Enterprise plan for companies",
    "price": 49.99,
    "billingCycle": 1
  }'

# Update plan
curl -X PUT http://localhost:5008/api/admin/plans/4 \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"price": 59.99}'

# Toggle plan active
curl -X PATCH http://localhost:5008/api/admin/plans/4/toggle-active \
  -H "Authorization: Bearer <admin-token>"

# Delete plan (fails if has active subscriptions)
curl -X DELETE http://localhost:5008/api/admin/plans/4 \
  -H "Authorization: Bearer <admin-token>"
```

### Test Admin Feature Management
```bash
# Add feature to plan
curl -X POST http://localhost:5008/api/admin/plans/2/features \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "featureKey": "PRIORITY_SUPPORT",
    "featureName": "Priority Support",
    "value": "true",
    "type": 1
  }'

# Update feature
curl -X PUT http://localhost:5008/api/admin/plans/2/features/16 \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"value": "false"}'

# Delete feature
curl -X DELETE http://localhost:5008/api/admin/plans/2/features/16 \
  -H "Authorization: Bearer <admin-token>"
```

### Test Admin Subscription Management
```bash
# List subscriptions with filters
curl "http://localhost:5008/api/admin/subscriptions?status=Active&pageSize=10" \
  -H "Authorization: Bearer <admin-token>"

# Get subscription details
curl http://localhost:5008/api/admin/subscriptions/1 \
  -H "Authorization: Bearer <admin-token>"

# Get user's subscription history
curl http://localhost:5008/api/admin/users/1/subscriptions \
  -H "Authorization: Bearer <admin-token>"

# Admin cancel subscription
curl -X POST http://localhost:5008/api/admin/subscriptions/1/cancel \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"reason": "User requested cancellation", "issueRefund": true}'

# Extend subscription
curl -X POST http://localhost:5008/api/admin/subscriptions/1/extend \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"months": 3}'

# Issue refund
curl -X POST http://localhost:5008/api/admin/subscriptions/1/refund \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Service issue compensation"}'
```

### Test Admin Analytics
```bash
# Analytics overview (MRR, ARR, churn)
curl "http://localhost:5008/api/admin/analytics/overview" \
  -H "Authorization: Bearer <admin-token>"

# Revenue analytics with date range
curl "http://localhost:5008/api/admin/analytics/revenue?startDate=2026-01-01&endDate=2026-03-31" \
  -H "Authorization: Bearer <admin-token>"

# Subscriptions by plan
curl http://localhost:5008/api/admin/analytics/subscriptions-by-plan \
  -H "Authorization: Bearer <admin-token>"

# Churn rate
curl "http://localhost:5008/api/admin/analytics/churn?startDate=2026-01-01&endDate=2026-03-31" \
  -H "Authorization: Bearer <admin-token>"
```

## Admin API Request/Response Examples

### Create Plan Feature Request
```json
{
  "featureKey": "MAX_JOBS_VIEW",
  "featureName": "Max Job Views Per Day",
  "value": "100",
  "type": 2  // 1=Boolean, 2=Number, 3=Text
}
```

### Subscription Filter Query Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | int | Filter by user ID |
| `planId` | int | Filter by plan ID |
| `status` | string | Active, Pending, Expired, Cancelled |
| `startDate` | datetime | Subscriptions starting after |
| `endDate` | datetime | Subscriptions ending before |
| `pageNumber` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 10) |

### Analytics Overview Response
```json
{
  "totalUsers": 1250,
  "activeSubscriptions": 890,
  "totalRevenue": 45000.00,
  "churnRate": 3.5,
  "mrr": 8500.00,
  "arr": 102000.00,
  "generatedAt": "2026-03-23T14:00:00Z"
}
```

### Revenue Analytics Response
```json
{
  "totalRevenue": 25000.00,
  "revenueByPlan": {
    "Pro": 15000.00,
    "Premium": 10000.00
  },
  "dailyRevenue": [
    {"date": "2026-03-01", "revenue": 850.00},
    {"date": "2026-03-02", "revenue": 920.00}
  ],
  "startDate": "2026-03-01",
  "endDate": "2026-03-31"
}
```

## Admin Audit Logging

All admin actions are logged to `AdminAuditLogs` table:

| Column | Description |
|--------|-------------|
| `AdminUserId` | ID of admin performing action |
| `Action` | Create, Update, Delete, ToggleActive, AdminCancel, Extend, Refund, etc. |
| `EntityType` | Plan, Subscription, PlanFeature |
| `EntityId` | ID of affected entity |
| `OldValues` | JSON of values before change |
| `NewValues` | JSON of values after change |
| `CreatedAt` | Timestamp of action |

## Metrics

| Metric | Description |
|--------|-------------|
| `subscription.redis.hits` | Cache hit count |
| `subscription.redis.misses` | Cache miss count |
| `subscription.fallback.calls` | Fallback to default tier |
| `subscription.quota.exceeded` | Quota exceeded count |
| `subscription.events.published` | Events published |
| `subscription.events.failed` | Failed event publishes |

## Troubleshooting

### Redis Connection Failed
- Check Redis health: `curl localhost:5008/health/redis`
- Service continues with HTTP fallback

### RabbitMQ Connection Failed
- Check RabbitMQ health: `curl localhost:5008/health/rabbitmq`
- Events stored in OutboxEvents table for retry

### Quota Not Updating
- Check Redis key: `user:{userId}:usage:{featureKey}:{yyyy-MM}`
- Usage resets monthly via key versioning
