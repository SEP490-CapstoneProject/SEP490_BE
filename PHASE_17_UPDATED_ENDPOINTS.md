# Phase 17: Sponsored Feed Injection - Updated Architecture

**Status**: ✅ COMPLETE  
**Last Updated**: 29-May-2026 16:45 UTC+7

## API Endpoints Summary

All three services now have consistent separate endpoints for feed with sponsored posts:

### Portfolio Service
**Base Route**: `api/feed`
- `GET /api/feed/portfolio` ✅ **NEW** - Portfolio feed with filters + sponsored posts
  - Query params: `blockType`, `includeCompliments`, `complimentState`, `hasCompliment`, `page`, `pageSize`, `status`, `q`, `rankBy`, `sort`
  - Response: Unified format with offset-based pagination
  
- `GET /api/feed/sponsored-posts/ranked` ✅ **NEW** - Authority endpoint
  - Query params: `userId`
  - Response: Ranked & frequency-capped sponsors for other services

### Company Service
**Base Route**: `api/company-posts`
- `GET /api/company-posts/feed` ✅ **NEW** - All company posts with sponsored injection
  - Query params: `cursor`, `limit`
  - Response: Cursor-based pagination with mixed feed
  
- `GET /api/company-posts/company/{id}/feed` ✅ **NEW** - Company-specific posts with sponsors
  - Query params: `cursor`, `limit`
  - Response: Cursor-based pagination with mixed feed

### Community Service
**Base Route**: `api/community`
- `GET /api/community/posts` ✅ **ORIGINAL** - Regular feed (unchanged)
  - Query params: `pageSize`, `cursor`, `q`
  - Response: Normal community posts (NO sponsored posts)
  
- `GET /api/community/posts/feed` ✅ **NEW** - Feed with sponsored injection
  - Query params: `pageSize`, `cursor`, `q`
  - Response: Cursor-based pagination with mixed feed + sponsored posts

- `GET /api/community/posts/{id}` - Get single post
- `GET /api/community/posts/{postId}/comments` - Get post comments
- `GET /api/community/posts/user/{userId}` - Get posts by user
- `POST /api/community/posts` - Create post
- `POST /api/community/posts/{id}/like` - Like post
- `DELETE /api/community/posts/{id}` - Delete post
- `POST /api/community/posts/{postId}/comments` - Add comment

## Architecture Highlights

### Consistency Across Services ✅
- All three services have **separate** sponsor-injection endpoints
- Original/existing endpoints remain **unchanged**
- Zero breaking changes
- New endpoints follow same naming conventions

### Service Communication
```
Company/Community Services
         ↓
    HTTP Client
         ↓
Portfolio.API /api/feed/sponsored-posts/ranked
         ↓
Returns ranked & frequency-capped sponsors
         ↓
Each service injects locally (5-10 item intervals)
```

### Response Format (Unified)
```json
{
  "items": [
    {
      "type": "PortfolioDto|CompanyPostFeedDto|CommunityPostDto",
      "isSponsored": false,
      "sponsoredLabel": null,
      "data": { /* DTO content */ }
    },
    {
      "type": "SponsoredPostDto",
      "isSponsored": true,
      "sponsoredLabel": "Sponsored",
      "data": { /* SponsoredPostDto */ }
    }
  ],
  "cursor": "...",
  "hasMore": true,
  "pageSize": 20
}
```

## Build Status ✅

| Service | Status | File Changed |
|---------|--------|------------|
| Portfolio | ✅ SUCCESS | FeedController.cs (filters added) |
| Company | ✅ SUCCESS | CompanyFeedController.cs (created) |
| Community | ✅ SUCCESS | CommunityController.cs (new endpoint added) |

## Implementation Details

### Injection Algorithm
- **Interval**: Randomized 5-10 items (prevents predictability)
- **Prevention**: No consecutive sponsored posts
- **Threshold**: Requires ≥5 normal items to start injecting
- **Fallback**: Graceful degradation if Portfolio service unavailable

### Frequency Cap Management
- **Authority**: Portfolio service (via IMemoryCache)
- **Limit**: Max 3 impressions/day per sponsored post per user
- **Storage**: In-memory (single instance) 
- **Note**: Resets on Portfolio service restart

## Backward Compatibility ✅

- `GET /api/community/posts` - Unchanged, returns normal feed
- `GET /api/company-posts/*` - New endpoints, no changes to existing
- `GET /api/feed/*` - New endpoints, existing routes preserved

## Endpoint Comparison

| Endpoint | Service | Type | Status | Sponsors |
|----------|---------|------|--------|----------|
| `/api/feed/portfolio` | Portfolio | NEW | ✅ ACTIVE | ✅ Yes |
| `/api/company-posts/feed` | Company | NEW | ✅ ACTIVE | ✅ Yes |
| `/api/community/posts` | Community | ORIGINAL | ✅ UNCHANGED | ❌ No |
| `/api/community/posts/feed` | Community | NEW | ✅ ACTIVE | ✅ Yes |

## Next Steps

1. **Testing**: Create test data (20+ items per service + 5 sponsors)
2. **Deployment**: Deploy Portfolio → Company → Community (in order)
3. **Monitoring**: Check logs for HTTP client errors
4. **Validation**: Test all three feed endpoints in production

## Files Modified

1. `Portfolio.API/Controllers/FeedController.cs` - Added all filters + sponsored posts
2. `Company.API/Controllers/CompanyFeedController.cs` - Created new controller
3. `Community.API/Controllers/CommunityController.cs` - Added new GetFeedWithSponsorship endpoint
4. `RecruitmentPlatform.Contracts/DTOs/UnifiedFeedItemDto.cs` - Shared DTO for unified response
