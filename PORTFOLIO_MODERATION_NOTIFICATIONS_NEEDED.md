# Portfolio Moderation Notifications - Status Check

**Issue**: Portfolio moderation is NOT sending notifications to users when rejected/pending review.

## Current Implementation

### ✅ What Portfolio HAS:
- ModerationService integration via `ApplyModerationAndEmbeddingAsync`
- Moderation result stored (ModerationStatus, ModerationReason, ModeratedAt)
- Auto-rejects set Status="inactive" (line 678)
- Rejected portfolios NOT visible to public

### ❌ What Portfolio is MISSING:
1. **No notification events** when portfolio is rejected/pending review
2. **No realtime events** for instant user feedback
3. **No HTTP status code changes** - always returns 201 even on rejection
4. **No event publishers** - only has `IPortfolioNotificationEventPublisher` for compliments

## Comparison

### Community/Company Services (IMPLEMENTED ✅)
```
Post rejected in CreatePostAsync
    ↓
Publishes PostRejectedNotificationEvent → Notification Service
Publishes PostModerationEvent → Realtime Service
Returns HTTP 400 with rejection reason
```

### Portfolio Service (NOT IMPLEMENTED ❌)
```
Portfolio rejected in CreatePortfolioAsync
    ↓
NO notification published
NO realtime event published
Always returns HTTP 201 (success)
User never knows portfolio was rejected
```

## Required Changes

### 1. Add Notification Event Publishing Interface
**File**: `Portfolio.Application/Interfaces/IPortfolioRejectionEventPublisher.cs`
```csharp
public interface IPortfolioRejectionEventPublisher
{
    Task PublishPortfolioApprovedAsync(object evt);
    Task PublishPortfolioRejectedAsync(object evt);
    Task PublishPortfolioModerationEventAsync(object evt); // Realtime
}
```

### 2. Implement Event Publishers
**File**: `Portfolio.Infrastructure/Messaging/PortfolioRejectionEventPublisher.cs`
- Publish to `portfolio.approved` topic
- Publish to `portfolio.rejected` topic
- Publish to `portfolio.moderation` topic (for realtime)

### 3. Update PortfolioService
**File**: `Portfolio.Application/Services/PortfolioService.cs`
- Inject `IPortfolioRejectionEventPublisher`
- After `ApplyModerationAndEmbeddingAsync`, check moderation result
- Publish notifications if Rejected/PendingReview
- Update `CreatePortfolioAsync` to return HTTP status based on moderation

### 4. Update API Response Handling
**File**: `Portfolio.API/Controllers/PortfolioController.cs`
- Return 400 if portfolio rejected
- Return 202 if portfolio pending review
- Return 201 if portfolio approved
- Include rejection reason in response body

### 5. Add Realtime Event Type
**File**: `RecruitmentPlatform.Contracts/Realtime/RealtimeEvents.cs`
- Reuse `PostModerationEvent` or create `PortfolioModerationEvent`
- Publish when portfolio moderation completes

## Files to Modify

1. `src/Services/Portfolio/Portfolio.Application/Interfaces/IPortfolioRejectionEventPublisher.cs` (CREATE)
2. `src/Services/Portfolio/Portfolio.Infrastructure/Messaging/PortfolioRejectionEventPublisher.cs` (CREATE)
3. `src/Services/Portfolio/Portfolio.Application/Services/PortfolioService.cs` (MODIFY)
4. `src/Services/Portfolio/Portfolio.API/Controllers/PortfolioController.cs` (MODIFY)
5. `src/Shared/RecruitmentPlatform.Contracts/Realtime/RealtimeEvents.cs` (MODIFY - if needed)

## Priority
**HIGH** - Users creating portfolios with moderation issues receive no feedback
