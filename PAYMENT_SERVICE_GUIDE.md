# Payment Service Guide

## Overview

Production-grade Payment Service cho SkillSnap Platform, hỗ trợ **VNPay** và **MoMo** với các tính năng enterprise-level:

- ✅ **Idempotency**: Hash-based event ID từ webhook payload
- ✅ **Race Condition Prevention**: Processing state + RowVersion conditional update
- ✅ **Transactional Webhook**: Tất cả steps trong 1 transaction
- ✅ **Outbox Pattern**: Exponential backoff + Dead Letter sau 5 retries
- ✅ **Anti-fraud Validation**: Kiểm tra amount, currency
- ✅ **Payment Expiration**: Auto-expire pending payments sau 15 phút

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Client                                      │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        API Gateway (YARP)                            │
│                    /api/payments/* → Payment Service                 │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Payment Service                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │  PaymentsController │  │  WebhookController │  │ AdminController │    │
│  └──────────────┘  └──────────────┘  └──────────────┘               │
│           │                │                 │                       │
│           ▼                ▼                 ▼                       │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │                   Application Layer                       │       │
│  │   PaymentService    │    WebhookHandler                   │       │
│  └──────────────────────────────────────────────────────────┘       │
│           │                │                                         │
│           ▼                ▼                                         │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │                   Infrastructure Layer                    │       │
│  │   VNPayProvider  │  MoMoProvider  │  Repositories        │       │
│  └──────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────┘
         │           │               │
         ▼           ▼               ▼
    ┌─────────┐ ┌─────────┐   ┌───────────────┐
    │  VNPay  │ │  MoMo   │   │   SQL Server  │
    │ Sandbox │ │ Sandbox │   │  (PaymentDb)  │
    └─────────┘ └─────────┘   └───────────────┘
                                     │
                                     ▼
                            ┌───────────────┐
                            │   RabbitMQ    │
                            │ payment_events│
                            └───────────────┘
                                     │
                                     ▼
                            ┌───────────────┐
                            │ Subscription  │
                            │   Service     │
                            └───────────────┘
```

## API Endpoints

### Public Endpoints (Authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/payments/create` | Tạo payment mới |
| GET | `/api/payments/{paymentId}` | Lấy thông tin payment |
| GET | `/api/payments/my-history` | Lịch sử payment của user |

### Webhook Endpoints (No Auth)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET/POST | `/api/payments/webhook/vnpay` | VNPay IPN callback |
| GET/POST | `/api/payments/webhook/momo` | MoMo IPN callback |

### Return Endpoints (No Auth)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/payments/return/vnpay` | VNPay return URL → redirect UI |
| GET | `/api/payments/return/momo` | MoMo return URL → redirect UI |

### Admin Endpoints (Admin Role)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/payments` | Lấy tất cả payments (với filters) |
| GET | `/api/admin/payments/{paymentId}` | Chi tiết payment |

## Database Schema

### Payments Table

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary Key |
| UserId | INT | User ID |
| PlanId | INT | Subscription Plan ID |
| Amount | DECIMAL(18,2) | Amount in VND |
| Currency | VARCHAR(10) | Currency code (default: VND) |
| Provider | INT | 0=VNPay, 1=MoMo |
| Status | INT | 0=Pending, 1=Processing, 2=Success, 3=Failed, 4=Cancelled, 5=Expired |
| PaymentUrl | VARCHAR(1000) | Provider payment URL |
| TransactionId | VARCHAR(100) | Provider transaction ID |
| OrderCode | VARCHAR(50) | Internal order code (unique) |
| RowVersion | INT | Optimistic concurrency |
| ExpiresAt | DATETIME | Payment expiration time |
| CreatedAt | DATETIME | Created timestamp |
| UpdatedAt | DATETIME | Last update timestamp |
| PaidAt | DATETIME | Payment success timestamp |

### PaymentHistories Table

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary Key |
| PaymentId | GUID | FK to Payments |
| OldStatus | VARCHAR(50) | Previous status |
| NewStatus | VARCHAR(50) | New status |
| Action | VARCHAR(100) | Action type |
| RawData | NVARCHAR(MAX) | Raw webhook payload |
| CorrelationId | VARCHAR(100) | Correlation ID for tracing |
| CreatedAt | DATETIME | Created timestamp |

### ProcessedEvents Table (Idempotency)

| Column | Type | Description |
|--------|------|-------------|
| EventId | VARCHAR(100) | Primary Key (SHA256 hash) |
| EventType | VARCHAR(50) | Event type |
| RawHash | VARCHAR(64) | SHA256 of raw payload |
| ProcessedAt | DATETIME | Processing timestamp |

### OutboxEvents Table

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary Key (auto-increment) |
| EventId | VARCHAR(100) | Event ID |
| EventType | VARCHAR(50) | Event type |
| Payload | NVARCHAR(MAX) | JSON payload |
| Status | INT | 0=Pending, 1=Published, 2=Failed, 3=DeadLetter |
| RetryCount | INT | Number of retries |
| NextRetryAt | DATETIME | Next retry time |
| LastError | VARCHAR(500) | Last error message |
| CreatedAt | DATETIME | Created timestamp |

## Payment Flow

### 1. Create Payment

```
Client                    Payment Service              VNPay/MoMo
   │                            │                          │
   │  POST /api/payments/create │                          │
   │  {planId: 2, provider: 0}  │                          │
   │────────────────────────────►                          │
   │                            │                          │
   │                   1. Validate plan exists             │
   │                   2. Get price from DB (NOT client)   │
   │                   3. Check existing pending payment   │
   │                   4. Create payment record            │
   │                   5. Generate order code              │
   │                            │                          │
   │                            │   Create payment URL     │
   │                            │─────────────────────────►│
   │                            │                          │
   │                            │◄─────────────────────────│
   │                            │     Payment URL          │
   │                            │                          │
   │  {paymentId, paymentUrl,   │                          │
   │   orderCode}               │                          │
   │◄───────────────────────────│                          │
```

### 2. Webhook Processing (CRITICAL)

```
VNPay/MoMo              Payment Service                 RabbitMQ
    │                         │                            │
    │  POST /webhook/vnpay    │                            │
    │  (query params)         │                            │
    │────────────────────────►│                            │
    │                         │                            │
    │              ┌──────────┴──────────┐                 │
    │              │ 1. Validate signature │                │
    │              │ 2. Parse webhook data │                │
    │              │ 3. Compute SHA256 hash│                │
    │              │ 4. Check idempotency  │                │
    │              │ 5. Find payment       │                │
    │              │ 6. Anti-fraud check   │                │
    │              └──────────┬──────────┘                 │
    │                         │                            │
    │              ┌──────────┴──────────┐                 │
    │              │ BEGIN TRANSACTION   │                 │
    │              │                     │                 │
    │              │ 7. Update status    │                 │
    │              │    (conditional)    │                 │
    │              │ 8. Save history     │                 │
    │              │ 9. Mark processed   │                 │
    │              │ 10. Add to outbox   │                 │
    │              │                     │                 │
    │              │ COMMIT TRANSACTION  │                 │
    │              └──────────┬──────────┘                 │
    │                         │                            │
    │  {RspCode: "00"}        │                            │
    │◄────────────────────────│                            │
    │                         │                            │
    │                         │  Outbox Processor          │
    │                         │  (Background)              │
    │                         │───────────────────────────►│
    │                         │  payment.succeeded event   │
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "PaymentDb": "Server=sqlserver;Database=PaymentDb;..."
  },
  "Jwt": {
    "Secret": "your-256-bit-secret",
    "Issuer": "skillsnap-api",
    "Audience": "skillsnap-client"
  },
  "VNPay": {
    "TmnCode": "YOUR_TMN_CODE",
    "HashSecret": "YOUR_HASH_SECRET",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "http://your-domain/api/payments/return/vnpay",
    "IpnUrl": "http://your-domain/api/payments/webhook/vnpay"
  },
  "MoMo": {
    "PartnerCode": "YOUR_PARTNER_CODE",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "Endpoint": "https://test-payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "http://your-domain/api/payments/return/momo",
    "IpnUrl": "http://your-domain/api/payments/webhook/momo"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": "5672",
    "Username": "guest",
    "Password": "guest"
  },
  "Services": {
    "SubscriptionService": "http://subscription-service:8080"
  }
}
```

## Docker

### Run with Docker Compose

```bash
# Build and run
docker-compose up -d payment-service

# View logs
docker logs -f payment-service

# Check health
curl http://localhost:5014/api/payments/health
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| ConnectionStrings__PaymentDb | SQL Server connection string |
| Jwt__Secret | JWT signing key (min 32 chars) |
| VNPay__TmnCode | VNPay merchant code |
| VNPay__HashSecret | VNPay hash secret |
| MoMo__PartnerCode | MoMo partner code |
| MoMo__AccessKey | MoMo access key |
| MoMo__SecretKey | MoMo secret key |
| RabbitMQ__Host | RabbitMQ hostname |
| Services__SubscriptionService | Subscription service URL |

## Event Contract

### payment.succeeded

Published to RabbitMQ exchange `payment_events` with routing key `payment.succeeded`:

```json
{
  "eventType": "payment.succeeded",
  "eventId": "payment_<paymentId>_<guid>",
  "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": 123,
  "planId": 2,
  "amount": 99000,
  "provider": "VNPay",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Security Considerations

1. **Signature Validation**: Luôn validate signature từ VNPay/MoMo trước khi xử lý
2. **Amount Validation**: So sánh amount từ webhook với amount trong DB
3. **Idempotency**: Hash-based check ngăn duplicate processing
4. **Race Condition**: RowVersion conditional update ngăn concurrent updates
5. **Rate Limiting**: Recommend thêm rate limiting cho webhook endpoints

## Background Services

### OutboxProcessorService

- Chạy mỗi 5 giây
- Process pending events
- Exponential backoff: 2^n * 10 seconds
- Max 5 retries → Dead Letter

### PaymentExpirationService

- Chạy mỗi 1 phút
- Expire pending payments sau 15 phút
- Bulk update status to Expired

## Testing

### Create Payment (cURL)

```bash
curl -X POST http://localhost:5014/api/payments/create \
  -H "Authorization: Bearer <jwt_token>" \
  -H "Content-Type: application/json" \
  -d '{"planId": 2, "provider": 0}'
```

### Simulate VNPay Webhook

```bash
curl "http://localhost:5014/api/payments/webhook/vnpay?\
vnp_TxnRef=ORD202401151030001234&\
vnp_Amount=9900000&\
vnp_ResponseCode=00&\
vnp_TransactionNo=14069999&\
vnp_SecureHash=<computed_hash>"
```

## Monitoring

- **Logs**: Structured logging with correlation IDs
- **Health**: `/api/payments/health` endpoint
- **Metrics**: Consider adding Prometheus metrics
- **Alerts**: Monitor dead letter queue size

## Troubleshooting

### Payment stuck in Pending

1. Check if ExpiresAt has passed → will be auto-expired
2. Check webhook logs for errors
3. Check ProcessedEvents for hash collision

### Webhook returning 400

1. Validate signature computation
2. Check amount/currency match
3. Check if already processed (idempotency)

### Events not publishing

1. Check RabbitMQ connection
2. Check OutboxEvents table for failures
3. Check dead letter queue
