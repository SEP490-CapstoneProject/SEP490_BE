# Embedding Test Results - 2026-04-24

## Test Objective
Determine if new record creation (Portfolio/CompanyPost) can generate embedding successfully or if OpenAI quota blocker prevents any embedding generation.

## Test Scenario 1: Company Post Creation
**Test Date**: 2026-04-24 14:55-14:58 UTC  
**Service**: company-service (revision `company-service--0000020`)  
**User**: Company A  

### Action
- Attempted to update/create company post via API `POST /api/company-posts`
- Note: Due to endpoint requiring multipart form-data, test deferred in favor of direct log analysis

### Result
✅ **Confirmed OpenAI Quota Blocker**

From company-service logs:
```
2026-04-24T14:55:49.2788157+00:00: Received HTTP response headers after 358.8418ms - 429
2026-04-24T14:55:49.2810871+00:00: OpenAI embedding request failed: TooManyRequests
"message": "You exceeded your current quota, please check your plan and billing details..."
"code": "insufficient_quota"
```

**Backfill Worker Response**:
```
2026-04-24T14:55:49.2922421+00:00: Company backfill paused due to OpenAI quota/429 at post 21
```

### Conclusion
**New company post embedding generation will FAIL immediately with 429 TooManyRequests** because:
1. OpenAI API key returns `insufficient_quota` error
2. Backfill worker correctly detects quota error and pauses
3. Any new posting attempt would hit the same quota limit

---

## Test Scenario 2: Portfolio Update
**Test Date**: 2026-04-24 21:58 UTC  
**Service**: portfolio-service (revision `portfolio-service--0000041`)  
**User**: thinh1@gmail.com  
**Portfolio ID**: 44  

### Action
- Updated portfolio with new INTRO block containing test description

### Response
✅ **Portfolio update succeeded (HTTP 200)**

### Issue Discovered
⚠️ **RabbitMQ Connectivity Failure - Not OpenAI**

From portfolio-service logs:
```
2026-04-24T14:58:23.6690581+00:00: BrokerUnreachableException: None of the specified endpoints were reachable
2026-04-24T14:58:23.6690618+00:00: System.Net.Sockets.SocketException: Name or service not known
2026-04-24T14:58:23.6692387+00:00: Failed to publish embedding event for portfolio 44
```

**Embedding Consumer Reaction**:
```
2026-04-24T14:58:31.7683636+00:00: Portfolio embedding consumer failed to connect RabbitMQ, retrying in 15 seconds.
```

### Root Cause Analysis
The portfolio-service cannot connect to RabbitMQ broker (trying to resolve hostname "rabbitmq" or similar hostname, which fails with `Name or service not known`). This is **environment configuration issue**, not application logic:

1. **Likely Cause**: RabbitMQ hostname/endpoint misconfigured in portfolio-service appsettings or Azure Container Apps environment variables
2. **Impact**: Even though portfolio update succeeds, the embedding event cannot be published to RabbitMQ queue
3. **Result**: `PortfolioEmbeddingConsumer` cannot receive the message, so embedding never generates

### Conclusion
**Portfolio embedding failed due to RabbitMQ connectivity, not OpenAI quota** - this is a **secondary blocker** independent of the OpenAI quota issue.

---

## Summary of Blockers

| Blocker | Severity | Status | Impact |
|---------|----------|--------|--------|
| **OpenAI API Quota Exceeded (429)** | CRITICAL | Active | ❌ ALL embedding generation fails immediately with 429 response |
| **RabbitMQ Connectivity (Portfolio)** | CRITICAL | Active | ❌ Embedding events cannot be published/consumed |

## Recommendations

### Immediate Actions
1. **Upgrade OpenAI Plan or Check Billing**
   - User needs to check OpenAI account:
     - Usage vs. quota limits
     - Billing/payment status
     - Consider switching to provisioned endpoint for guaranteed quota
   - Verify API key is correct and active

2. **Fix RabbitMQ Configuration (Portfolio Service)**
   - Check portfolio-service deployment:
     - Verify RabbitMQ hostname/endpoint in appsettings.json
     - Confirm Azure Container Apps environment variables for RabbitMQ connection
     - Test RabbitMQ connectivity from portfolio-service pod
   - Compare with working company-service RabbitMQ config

### After Fixes
- **Test 1**: Create new portfolio → embedding consumer receives message → calls OpenAI → generates embedding
- **Test 2**: Create new company post → backfill worker picks up during sweep → calls OpenAI → generates embedding
- **Expected Result**: New records reach `EmbeddingStatus=Ready` automatically, matching APIs return results

---

## Test Evidence

### Company-Service Logs
- OpenAI HTTP 429 response timestamp: `2026-04-24T14:55:49.2788157+00:00`
- Backfill worker pause confirmation: `2026-04-24T14:55:49.2922421+00:00`
- Worker completed with 0 successes: `2026-04-24T14:55:49.4014725+00:00`

### Portfolio-Service Logs
- RabbitMQ connection failure: `2026-04-24T14:58:23.6690581+00:00`
- Publishing failure for portfolio 44: `2026-04-24T14:58:23.66904+00:00`
- Consumer retry: `2026-04-24T14:58:31.7683636+00:00`

---

## Notes
- Company-service embedding infrastructure is sound (backfill worker, consumer logic, retry handling all working)
- Portfolio-service embedding logic is implemented but cannot function due to RabbitMQ connectivity
- Once both blockers are resolved, embedding pipeline should work automatically for new records
- Existing records with `EmbeddingStatus=Pending` will be progressively backfilled as quota allows
