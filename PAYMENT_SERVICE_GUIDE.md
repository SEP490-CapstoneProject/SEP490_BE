# Payment Service Guide

## Overview

Production-grade Payment Service cho SkillSnap Platform với **PayOS** integration (fintech-grade):

- ✅ **PayOS Integration**: Modern payment gateway (thay thế VNPay/MoMo)
- ✅ **Idempotency**: Dual-check (EventHash + OrderCode) với UNIQUE constraints
- ✅ **Race Condition Prevention**: Processing state + RowVersion optimistic concurrency
- ✅ **Transactional Webhook**: Tất cả steps trong 1 database transaction
- ✅ **Fraud Prevention**: Re-verification với PayOS API sau mỗi webhook
- ✅ **Outbox Pattern**: Event publishing với Dead Letter Queue
- ✅ **Payment Expiration**: Auto-expire pending payments sau 15 phút
- ✅ **Reconciliation**: Background job kiểm tra stuck payments mỗi 5 phút
- ✅ **Resilience**: Polly retry + circuit breaker cho PayOS API calls
- ✅ **Metrics & Monitoring**: Prometheus-compatible metrics endpoint
- ✅ **Alerting**: Configurable alerting service for critical events

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Client                                     │
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
│  │PaymentsController│  │WebhookController│  │MetricsController│        │
│  └──────────────┘  └──────────────┘  └──────────────┘               │
│           │                │                 │                       │
│           ▼                ▼                 ▼                       │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │                   Application Layer                       │       │
│  │   PaymentService  │  WebhookService  │  VerificationService│      │
│  └──────────────────────────────────────────────────────────┘       │
│           │                │                                         │
│           ▼                ▼                                         │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │                   Infrastructure Layer                    │       │
│  │   PayOSProvider  │  Repositories  │  Metrics  │  Alerting │       │
│  └──────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────┘
         │                              │
         ▼                              ▼
    ┌─────────┐                 ┌───────────────┐
    │  PayOS  │                 │   SQL Server  │
    │   API   │                 │  (PaymentDb)  │
    └─────────┘                 └───────────────┘
                                       │
                                       ▼
                              ┌───────────────┐
                              │   RabbitMQ    │
                              │ skillsnap.events│
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
| POST | `/api/payments/webhook/payos` | PayOS webhook callback (JSON) |

### Metrics Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/metrics` | Get metrics (JSON) |
| GET | `/api/metrics/prometheus` | Get metrics (Prometheus format) |
| POST | `/api/metrics/reset` | Reset metrics (Admin only) |

### Admin Endpoints (Admin Role)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/payments` | Lấy tất cả payments (với filters) |
| GET | `/api/admin/payments/{paymentId}` | Chi tiết payment |

## PayOS Configuration

```json
{
  "PayOS": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "ApiKey": "YOUR_PAYOS_API_KEY", 
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY",
    "BaseUrl": "https://api-merchant.payos.vn",
    "ReturnUrl": "https://yourapp.com/payment/result",
    "CancelUrl": "https://yourapp.com/payment/cancel",
    "WebhookUrl": "https://yourapp.com/api/payments/webhook/payos"
  },
  "FeatureFlags": {
    "USE_PAYOS": true,
    "ENABLE_RECONCILIATION": true,
    "ENABLE_DLQ_MONITORING": true,
    "VERIFICATION_ENABLED": true
  }
}
```

### Lấy PayOS Credentials

1. Đăng ký tại [PayOS.vn](https://payos.vn)
2. Tạo ứng dụng trong Dashboard
3. Copy `ClientId`, `ApiKey`, `ChecksumKey` từ Dashboard

## Database Schema

### Payments Table

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary Key |
| UserId | INT | User ID |
| PlanId | INT | Subscription Plan ID |
| SubscriptionId | INT | FK to Subscription |
| Amount | DECIMAL(18,2) | Amount in VND |
| Currency | VARCHAR(10) | Currency code (default: VND) |
| Provider | INT | 0=PayOS, 99=Legacy |
| Status | INT | 0=Pending, 1=Processing, 2=Succeeded, 3=Failed, 4=Cancelled, 5=Expired |
| PaymentUrl | VARCHAR(1000) | Provider checkout URL |
| TransactionId | VARCHAR(100) | Provider transaction ID |
| OrderCode | VARCHAR(50) | Internal order code (unique, 15-16 digit) |
| RowVersion | ROWVERSION | Optimistic concurrency (EF Core managed) |
| Metadata | NVARCHAR(MAX) | JSON metadata |
| ExpiresAt | DATETIME | Payment expiration time |
| CreatedAt | DATETIME | Created timestamp |
| UpdatedAt | DATETIME | Last update timestamp |
| PaidAt | DATETIME | Payment success timestamp |

### Payment Status Flow

```
                              ┌─────────┐
                              │ Pending │
                              └────┬────┘
                    ┌─────────────┼─────────────┐
                    ▼             ▼             ▼
             ┌───────────┐  ┌──────────┐  ┌─────────┐
             │ Processing│  │ Cancelled│  │ Expired │
             └─────┬─────┘  └──────────┘  └─────────┘
            ┌──────┴──────┐
            ▼             ▼
     ┌───────────┐  ┌──────────┐
     │ Succeeded │  │  Failed  │
     └───────────┘  └──────────┘
```

### ProcessedEvents Table (Idempotency)

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary Key |
| EventId | VARCHAR(100) | Payment ID reference |
| EventType | VARCHAR(50) | Event type (webhook, reconciliation) |
| EventHash | VARCHAR(64) | SHA256 of raw payload (UNIQUE) |
| OrderCode | VARCHAR(50) | Order code (UNIQUE) |
| CorrelationId | VARCHAR(100) | Correlation ID for tracing |
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
Client                    Payment Service                 PayOS
   │                            │                          │
   │  POST /api/payments/create │                          │
   │  {planId: 2}               │                          │
   │────────────────────────────►                          │
   │                            │                          │
   │                   1. Validate plan exists             │
   │                   2. Get price from Subscription Svc  │
   │                   3. Check existing pending payment   │
   │                   4. Create payment record            │
   │                   5. Generate OrderCode (15-16 digit) │
   │                            │                          │
   │                            │   Create payment link    │
   │                            │─────────────────────────►│
   │                            │                          │
   │                            │◄─────────────────────────│
   │                            │     Checkout URL         │
   │                            │                          │
   │  {paymentId, paymentUrl,   │                          │
   │   orderCode, expiresAt}    │                          │
   │◄───────────────────────────│                          │
```

### 2. Webhook Processing (CRITICAL - Fintech Grade)

```
PayOS                   Payment Service                    RabbitMQ
   │                         │                               │
   │  POST /webhook/payos    │                               │
   │  x-signature: <hmac>    │                               │
   │  {code, data, signature}│                               │
   │────────────────────────►│                               │
   │                         │                               │
   │          ┌──────────────┴──────────────┐                │
   │          │ 1. Validate signature (RAW) │                │
   │          │ 2. Compute SHA256 hash      │                │
   │          │ 3. Check idempotency (hash) │                │
   │          │ 4. Parse webhook JSON       │                │
   │          │ 5. Find payment by OrderCode│                │
   │          │ 6. Anti-fraud amount check  │                │
   │          └──────────────┬──────────────┘                │
   │                         │                               │
   │          ┌──────────────┴──────────────┐                │
   │          │ BEGIN TRANSACTION           │                │
   │          │                             │                │
   │          │ 7. Atomic Pending→Processing│                │
   │          │    (RowVersion check)       │                │
   │          │ 8. Re-verify with PayOS API │                │
   │          │ 9. Update to Succeeded/Failed│               │
   │          │ 10. Save payment history    │                │
   │          │ 11. Mark event processed    │                │
   │          │ 12. Add to outbox           │                │
   │          │                             │                │
   │          │ COMMIT TRANSACTION          │                │
   │          └──────────────┬──────────────┘                │
   │                         │                               │
   │  {code: "00", ...}      │                               │
   │◄────────────────────────│                               │
   │                         │                               │
   │                         │  Outbox Processor             │
   │                         │  (Background)                 │
   │                         │──────────────────────────────►│
   │                         │  payment.succeeded event      │
```

## Background Services

### OutboxProcessorService
- Chạy mỗi 5 giây
- Process pending events từ OutboxEvents table
- Publish tới RabbitMQ
- Exponential backoff: 2^n * 10 seconds
- Max 5 retries → Dead Letter

### PaymentExpirationService
- Chạy mỗi 1 phút
- Expire pending payments sau 15 phút
- Bulk update status to Expired

### ReconciliationService
- Chạy mỗi 5 phút
- Kiểm tra stuck payments (Processing > 5 minutes)
- Re-verify với PayOS API
- Auto-resolve or alert

### DLQMonitoringService
- Chạy mỗi 1 giờ
- Monitor Dead Letter Queue size
- Retry failed messages
- Send alerts if threshold exceeded

## Monitoring & Metrics

### Prometheus Metrics

Endpoint: `GET /api/metrics/prometheus`

```prometheus
# Payment metrics
payment_total_created 150
payment_total_succeeded 145
payment_total_failed 5
payment_success_rate 96.67

# Webhook metrics
webhook_total_received 200
webhook_total_processed 195
webhook_total_failed 5

# Latency metrics
payment_latency_avg_ms 250.50
webhook_latency_avg_ms 45.30

# Per-provider metrics
payment_by_provider_created{provider="PayOS"} 150
payment_by_provider_succeeded{provider="PayOS"} 145
```

### Grafana Dashboard

Recommended panels:
1. **Payment Success Rate** - Gauge (target: >99%)
2. **Payments Over Time** - Time series (created vs succeeded)
3. **Webhook Processing Time** - Histogram
4. **DLQ Size** - Stat with threshold alert
5. **Provider Breakdown** - Pie chart

### Alerting Rules

| Alert | Condition | Severity |
|-------|-----------|----------|
| High Failure Rate | failure_rate > 10% | Warning |
| Critical Failure Rate | failure_rate > 20% | Critical |
| DLQ Threshold | queue_size > 50 | Warning |
| DLQ Critical | queue_size > 100 | Critical |
| Webhook Failure | consecutive_failures > 5 | Error |

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
  "PayOS": {
    "ClientId": "YOUR_CLIENT_ID",
    "ApiKey": "YOUR_API_KEY",
    "ChecksumKey": "YOUR_CHECKSUM_KEY",
    "BaseUrl": "https://api-merchant.payos.vn",
    "ReturnUrl": "https://yourapp.com/payment/result",
    "CancelUrl": "https://yourapp.com/payment/cancel",
    "WebhookUrl": "https://yourapp.com/api/payments/webhook/payos"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": "5672",
    "Username": "guest",
    "Password": "guest",
    "Exchange": "skillsnap.events",
    "Queue": "payment.succeeded",
    "DeadLetterExchange": "skillsnap.dlx",
    "DeadLetterQueue": "payment.failed.dlq"
  },
  "BackgroundJobs": {
    "ReconciliationInterval": "00:05:00",
    "ExpirationInterval": "00:01:00",
    "DLQMonitoringInterval": "01:00:00"
  },
  "Resilience": {
    "PayOSApiRetries": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDuration": "00:00:30",
    "Timeout": "00:00:10"
  },
  "FeatureFlags": {
    "USE_PAYOS": true,
    "ENABLE_RECONCILIATION": true,
    "ENABLE_DLQ_MONITORING": true,
    "VERIFICATION_ENABLED": true
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
| PayOS__ClientId | PayOS client ID |
| PayOS__ApiKey | PayOS API key |
| PayOS__ChecksumKey | PayOS checksum key |
| RabbitMQ__Host | RabbitMQ hostname |
| ALERT_WEBHOOK_URL | Optional alerting webhook URL |

## Event Contract

### payment.succeeded

Published to RabbitMQ exchange `skillsnap.events` with routing key `payment.succeeded`:

```json
{
  "eventType": "payment.succeeded",
  "eventId": "payment_<paymentId>_<guid>",
  "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": 123,
  "planId": 2,
  "subscriptionId": 456,
  "amount": 99000,
  "provider": "PayOS",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Security Considerations

1. **Signature Validation**: Validate HMAC SHA256 signature on RAW body BEFORE JSON parsing
2. **Amount Validation**: Triple check - webhook amount vs DB vs API re-verification
3. **Idempotency**: SHA256 hash + OrderCode dual-check with UNIQUE constraints
4. **Race Condition**: RowVersion optimistic concurrency for Pending→Processing transition
5. **Fraud Prevention**: Re-verify every webhook with PayOS API
6. **DLQ Monitoring**: Monitor dead letter queue for stuck/failed events
7. **Rate Limiting**: Recommend adding rate limiting for webhook endpoints

## Testing

### Create Payment (cURL)

```bash
curl -X POST http://localhost:5014/api/payments/create \
  -H "Authorization: Bearer <jwt_token>" \
  -H "Content-Type: application/json" \
  -d '{"planId": 2}'
```

### Check Metrics

```bash
curl http://localhost:5014/api/metrics
curl http://localhost:5014/api/metrics/prometheus
```

## Unit Tests

Run tests:
```bash
cd src/Services/Payment
dotnet test Payment.Tests
```

Test coverage:
- `PayOSSignatureValidatorTests` - Signature validation
- `WebhookServiceTests` - Idempotency & error handling
- `PaymentVerificationServiceTests` - Fraud prevention
- `PaymentStateMachineTests` - State transitions
- `ReconciliationServiceTests` - Stuck payment detection

## Troubleshooting

### Payment stuck in Pending

1. Check if ExpiresAt has passed → will be auto-expired
2. Check PayOS dashboard for payment status
3. Check webhook logs for errors

### Payment stuck in Processing

1. ReconciliationService will auto-check after 5 minutes
2. Check PayOS API for actual payment status
3. Check ProcessedEvents for duplicate webhooks

### Webhook returning 400

1. Validate signature computation (HMAC SHA256)
2. Check amount match between webhook and DB
3. Check if already processed (idempotency)

### Events not publishing

1. Check RabbitMQ connection
2. Check OutboxEvents table for failures
3. Check dead letter queue
4. Review DLQMonitoringService logs

### High DLQ size

1. Check RabbitMQ connectivity
2. Check Subscription Service availability
3. Review failed message payloads
4. Consider manual re-processing
