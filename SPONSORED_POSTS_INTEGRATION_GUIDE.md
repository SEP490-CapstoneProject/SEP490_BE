# Sponsored Posts Feed Integration Guide

## Overview
Sponsored posts are automatically injected into Portfolio, Company, and Community feeds using the FeedInjectionEngine.

## API Endpoints

### Portfolio Feed (with sponsored posts)
```
GET /api/feed/portfolio
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10)  
- status: string (optional)
- q: string (optional)
- sort: PortfolioSortMode (default: newest)
- rankBy: PortfolioRankBy (default: average)

Response:
{
  "items": [
    {
      "type": "Portfolio|SponsoredPost",
      "isSponsored": bool,
      "sponsoredLabel": "Sponsored" (if sponsored),
      "data": {
        // PortfolioDto or SponsoredPostDto
      }
    }
  ],
  "totalCount": int,
  "currentPage": int,
  "pageSize": int
}
```

### Get Ranked Sponsored Posts (for other services)
```
GET /api/feed/sponsored-posts/ranked
Query Parameters:
- userId: int (optional, for frequency cap calculation)

Response:
[
  {
    "type": "SponsoredPost",
    "isSponsored": true,
    "sponsoredLabel": "Sponsored",
    "data": {
      "id": int,
      "createdBy": int,
      "contentType": "Text|Image|Video",
      "textContent": string,
      "imageUrl": string,
      "videoUrl": string,
      "pointsSpent": int,
      "durationDays": int,
      "startDate": DateTime,
      "expiryDate": DateTime,
      "status": "Active|Expired|Paused|Deleted",
      "clickThroughUrl": string,
      "viewCount": int,
      "clickCount": int,
      "createdAt": DateTime,
      "updatedAt": DateTime
    }
  }
]
```

## Integration Steps for Company/Community Services

### Option 1: Call Portfolio Feed Service (Recommended for Phase 2)
1. Call `GET /api/feed/sponsored-posts/ranked` endpoint
2. Parse response and add to feed model
3. Mix with existing feed items
4. Return combined result to client

### Option 2: Implement Local Injection Engine (Future)
1. Move `FeedInjectionEngine` and `RankingEngine` to Shared library
2. Reference shared library in Company/Community projects
3. Register in Program.cs DI
4. Call injection engine locally

## Injection Algorithm Details

### Sponsor Post Insertion Rules
- Interval: Random 5-10 posts between sponsored items (prevents predictability)
- Consecutiveness: Never place 2 sponsored posts back-to-back
- Frequency Cap: Max 3 impressions/day/user per sponsored post (in-memory, resets on app restart)

### Ranking Score Calculation
```
Score = (Priority × 0.5) + (RemainingRatio × 0.3) + (CTR × 0.2)

Where:
- Priority: 0-100 scale, manually set per sponsored post (default 50)
- RemainingRatio: (MaxImpression - CurrentImpression) / MaxImpression
- CTR: ClickCount / (ViewCount + 1)
```

Higher scores = earlier insertion in the queue.

## SponsoredPost Entity Fields

| Field | Type | Purpose |
|-------|------|---------|
| PriorityScore | decimal(3,2) | Admin preference (0-100, default 50) |
| MaxImpression | int | Total impressions budget (default 1000) |
| CurrentImpression | int | Current impressions count (default 0) |
| MaxClick | int | Total clicks budget (default 100) |
| CurrentClick | int | Current clicks count (default 0) |

## Testing the Integration

1. Create a SponsoredPost via `/api/sponsored-posts` endpoint
2. Create 20+ Portfolio/Company/Community items
3. Call feed endpoint and verify sponsored items appear every 5-10 posts
4. Verify no consecutive sponsored items
5. Call feed multiple times in same day and verify frequency cap works

## Example Request/Response

### Request
```
GET /api/feed/portfolio?page=1&pageSize=20
```

### Response (Simplified)
```json
{
  "items": [
    {
      "type": "Portfolio",
      "isSponsored": false,
      "data": { "portfolioId": 1, "employeeId": 1, ... }
    },
    {
      "type": "Portfolio",
      "isSponsored": false,
      "data": { "portfolioId": 2, "employeeId": 2, ... }
    },
    {
      "type": "SponsoredPost",
      "isSponsored": true,
      "sponsoredLabel": "Sponsored",
      "data": {
        "id": 1,
        "contentType": "Image",
        "imageUrl": "https://...",
        "status": "Active",
        "viewCount": 150,
        "clickCount": 25
      }
    },
    // ... continues with pattern
  ],
  "totalCount": 150,
  "currentPage": 1,
  "pageSize": 20
}
```

## Known Limitations

1. **Frequency Cap Storage**: Currently in-memory (IMemoryCache), resets on app restart
   - Future: Persist to database for production stability
   
2. **Impression Tracking**: Incremented only when sponsored post is shown in feed
   - Not linked to actual user view events (SDK tracking may be needed)

3. **Click Tracking**: Must be updated via `/api/sponsored-posts/{id}/record-click` endpoint
   - Requires frontend to track clicks

4. **No Distributed Caching**: If running in multi-instance deployment, frequency caps won't sync
   - Future: Use Redis for distributed cache

## Migration Notes

Run `dotnet ef database update` to apply pending migrations:
- `AddSponsoredPostRankingFields` - Adds PriorityScore, MaxImpression, CurrentImpression, MaxClick, CurrentClick columns
- `AddSponsoredPostRankingFieldsConfigured` - Adds EF configuration and defaults
