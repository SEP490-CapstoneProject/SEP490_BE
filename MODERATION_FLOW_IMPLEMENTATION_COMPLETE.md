# Moderation Flow Implementation Complete

**Date**: 2026-05-13T18:22:00+07:00  
**Status**: ✅ Implementation Complete (Ready for Deployment)  
**Commit**: 6ab733e

## Overview

Successfully implemented unified moderation flow across Community, Company, and Portfolio services with role-targeted admin/moderator triage notifications.

## What Was Implemented

### 1. Event Contract Unification
- **File**: `PostPendingReviewNotificationEvent.cs` (Community, Company), `PortfolioPendingReviewNotificationEvent.cs`
- **Change**: Added `TargetRoles: string[]?` field to all pending-review events
- **Purpose**: Identify which roles should receive triage notifications for manual review

### 2. Publisher-Side Triage Events

#### Community Service
- **File**: `src/Services/Community/Community.Application/Services/CommunityService.cs`
- **Change**: When post status = PendingReview, publish TWO events:
  1. **Owner notification**: Original event to post creator (no TargetRoles)
  2. **Triage notification**: New event with `TargetRoles = ["ADMIN", "MODERATOR"]` for reviewer queue
- **Flow**:
  ```
  Auto-moderation failed → ReviewStatus = PendingReview
  → Publish (owner): "Your post is under review"
  → Publish (triage): "Post #123 needs admin/moderator review"
  ```

#### Company Service
- **File**: `src/Services/Company/Company.Application/Services/CompanyPostService.cs`
- **Change**: Same dual-event pattern for company job posts
- **Flow**: Auto-failed → PendingReview → (owner + triage notifications)

#### Portfolio Service
- **File**: `src/Services/Portfolio/Portfolio.Application/Services/PortfolioService.cs`
- **Change**: When portfolio ModerationStatus = "PendingReview" (auto-moderation), publish triage event
- **Note**: Portfolio previously had only owner-facing pending notifications; now also notifies admin/moderator

### 3. Consumer-Side Role-Targeted Handling

#### Notification Consumer
- **File**: `src/Services/Notification/Notification.Infrastructure/Messaging/RabbitMQConsumer.cs`
- **Change**: Added `HandleRoleTargetedPendingReviewAsync` method
- **Flow**:
  1. Detect if event has `TargetRoles` and is pending-review type
  2. Call `IRecipientResolverClient.GetActiveUserIdsByRolesAsync(targetRoles)` to resolve admin/moderator user IDs
  3. For each recipient, create notification with role-targeted metadata
  4. Send FCM push + publish realtime event to admin/moderator queues
- **Special handling**: Same idempotency + aggregation logic as post.report.created

### 4. Portfolio Admin Moderation APIs

#### Admin Controller
- **File**: `src/Services/Portfolio/Portfolio.API/Controllers/AdminPortfolioModerationController.cs` (NEW)
- **Route**: `/api/portfolio/admin`
- **Authorization**: `[Authorize(Roles = "ADMIN,MODERATOR")]`
- **Endpoints**:
  - `GET /api/portfolio/admin/pending` → List pending portfolios (paginated)
  - `POST /api/portfolio/admin/{id}/approve` → Approve with optional notes
  - `POST /api/portfolio/admin/{id}/reject` → Reject with required reason

#### Service Methods
- **File**: `src/Services/Portfolio/Portfolio.Application/Services/PortfolioService.cs`
- **Methods**:
  - `ApprovePortfolioAsync(portfolioId, reviewerId, actorRole, notes)` → Set status=Approved, publish owner notification + realtime event
  - `RejectPortfolioAsync(portfolioId, reviewerId, actorRole, reason)` → Set status=Rejected, publish owner notification + realtime event

#### Repository Method
- **File**: `src/Services/Portfolio/Portfolio.Infrastructure/Repositories/PortfolioRepository.cs`
- **Method**: `GetPendingForModerationAsync(page, pageSize)` → Query portfolios where ModerationStatus = "PendingReview"

#### DTOs
- **File**: `src/Services/Portfolio/Portfolio.Application/DTOs/PortfolioDto.cs`
- **Added**:
  - `ModerationStatus` field to response DTO
  - `ModerationReason` field to response DTO
  - `ModeratedAt` field to response DTO
  - `ApprovePortfolioRequest` DTO
  - `RejectPortfolioRequest` DTO

## Build Status

✅ All services built successfully:
- Community: 0 errors
- Company: 0 errors
- Portfolio: 0 errors
- Notification: 0 warnings (only nullable reference warnings)

## Deployment Instructions

### Step 1: Push to ACR
```bash
TAG='20260513182202'
docker push skillsnap.azurecr.io/community:$TAG
docker push skillsnap.azurecr.io/company:$TAG
docker push skillsnap.azurecr.io/portfolio:$TAG
docker push skillsnap.azurecr.io/notification:$TAG
```

### Step 2: Update Container Apps
```bash
# Community
az containerapp update \
  --name community-api \
  --resource-group $RG \
  --image "skillsnap.azurecr.io/community:20260513182202"

# Company
az containerapp update \
  --name company-api \
  --resource-group $RG \
  --image "skillsnap.azurecr.io/company:20260513182202"

# Portfolio
az containerapp update \
  --name portfolio-api \
  --resource-group $RG \
  --image "skillsnap.azurecr.io/portfolio:20260513182202"

# Notification
az containerapp update \
  --name notification-api \
  --resource-group $RG \
  --image "skillsnap.azurecr.io/notification:20260513182202"
```

### Step 3: Verify Migrations
Check Notification service logs for migration execution:
```bash
az containerapp logs show \
  --name notification-api \
  --resource-group $RG \
  --follow
```

Expected log: `Migrations applied successfully` (existing, not new)

### Step 4: Test Moderation Flow

#### Test Community/Company Pending Review Triage
1. Create a post that fails auto-moderation
2. Verify admin/moderator receives "Post #X needs review" notification
3. Verify post creator receives "Your post is under review" notification

#### Test Portfolio Admin Approval
1. Get pending portfolios:
   ```bash
   curl -H "Authorization: Bearer $ADMIN_TOKEN" \
     https://portfolio-api.example.com/api/portfolio/admin/pending
   ```
2. Approve portfolio:
   ```bash
   curl -X POST \
     -H "Authorization: Bearer $ADMIN_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"notes":"Looks great"}' \
     https://portfolio-api.example.com/api/portfolio/admin/1/approve
   ```
3. Verify portfolio creator receives approval notification

#### Test Portfolio Admin Rejection
```bash
curl -X POST \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Portfolio does not meet quality standards"}' \
  https://portfolio-api.example.com/api/portfolio/admin/1/reject
```

## Files Changed

### Event Models
- Community: `src/Services/Community/Community.Application/Models/Events/PostPendingReviewNotificationEvent.cs`
- Company: `src/Services/Company/Company.Application/Models/Events/PostPendingReviewNotificationEvent.cs`
- Portfolio: `src/Services/Portfolio/Portfolio.Application/Models/Events/PortfolioPendingReviewNotificationEvent.cs`

### Services
- Community: `src/Services/Community/Community.Application/Services/CommunityService.cs`
- Company: `src/Services/Company/Company.Application/Services/CompanyPostService.cs`
- Portfolio: `src/Services/Portfolio/Portfolio.Application/Services/PortfolioService.cs`
- Notification: `src/Services/Notification/Notification.Infrastructure/Messaging/RabbitMQConsumer.cs`

### Repositories & Interfaces
- Portfolio Repo: `src/Services/Portfolio/Portfolio.Infrastructure/Repositories/PortfolioRepository.cs`
- Portfolio Interfaces: `src/Services/Portfolio/Portfolio.Application/Interfaces/IPortfolioRepository.cs`, `IPortfolioService.cs`

### Controllers & DTOs
- Portfolio Admin Controller: `src/Services/Portfolio/Portfolio.API/Controllers/AdminPortfolioModerationController.cs` (NEW)
- Portfolio DTOs: `src/Services/Portfolio/Portfolio.Application/DTOs/PortfolioDto.cs`

## Next Steps

1. ✅ Implement moderation flow (code is ready)
2. ⏳ Deploy services to production
3. ⏳ Monitor logs for role-targeted notifications
4. ⏳ Test end-to-end moderation workflows
5. ⏳ Document in FE integration guide

## Testing Checklist

- [ ] Community post fails auto-moderation → Both notifications sent
- [ ] Company post fails auto-moderation → Both notifications sent
- [ ] Portfolio fails auto-moderation → Triage notification sent to admin
- [ ] Portfolio admin GET /pending returns PendingReview portfolios
- [ ] Portfolio admin POST approve works with notes
- [ ] Portfolio admin POST reject works with reason
- [ ] Approved portfolio owner receives notification
- [ ] Rejected portfolio owner receives notification
- [ ] Moderator role can approve/reject (not just ADMIN)
- [ ] Idempotency: duplicate events don't create multiple notifications

## Notes

- All triage events use `UserId = string.Empty` to signal role-targeted delivery (not owner-specific)
- ActorId/ActorType typically null/SYSTEM for auto-moderation events, set during manual approval
- FCM push + realtime events sent to all recipients
- Follows same precedent as post.report.created handler (already in production)
