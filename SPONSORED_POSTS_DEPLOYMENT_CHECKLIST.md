# Sponsored Posts Feed Deployment Checklist

## Pre-Deployment Verification ✅

- [x] FeedController created with `/api/feed/portfolio` endpoint
- [x] RankingEngine implemented with scoring algorithm
- [x] FeedInjectionEngine implemented with injection algorithm
- [x] UnifiedFeedItemDto created for unified feed representation
- [x] FeedInjectionEngine & RankingEngine registered in Program.cs DI
- [x] SponsoredPost entity extended with ranking fields:
  - [x] PriorityScore (decimal 0-100, default 50)
  - [x] MaxImpression (int, default 1000)
  - [x] CurrentImpression (int, default 0)
  - [x] MaxClick (int, default 100)
  - [x] CurrentClick (int, default 0)
- [x] EF migrations created for new fields
- [x] Portfolio.API builds successfully without errors
- [x] Integration guide documented

## Migration Steps

1. **Before Deployment**
   - [ ] Backup Azure SQL database (Backup portfolio_db snapshot)
   - [ ] Test migrations locally: `dotnet ef database update`

2. **Deployment**
   - [ ] Build release: `dotnet build -c Release`
   - [ ] Deploy Portfolio.API to Azure (new revision)
   - [ ] Azure App Service runs migrations automatically on startup (if configured)
   - [ ] Or manually run: `dotnet ef database update` on deployed instance

3. **Post-Deployment Verification**
   - [ ] App starts without errors (check Application Insights logs)
   - [ ] Database migrations applied successfully
   - [ ] GET /api/feed/portfolio endpoint accessible
   - [ ] GET /api/feed/sponsored-posts/ranked endpoint accessible
   - [ ] Swagger UI shows new endpoints

## Testing Steps

### Unit/Integration Tests
- [ ] Create 5+ portfolio items (via POST /api/portfolio)
- [ ] Create 3+ sponsored posts (via POST /api/sponsored-posts)
- [ ] Call GET /api/feed/portfolio?page=1&pageSize=20
  - Verify items are mixed (Portfolio + SponsoredPost)
  - Verify sponsored posts appear every 5-10 items
  - Verify no consecutive sponsored posts

### Frequency Cap Testing
- [ ] Call feed endpoint multiple times in same day as same user
  - Verify frequency cap shows only 3 sponsored impressions
  - Verify different sponsored posts still appear if available

### Pagination Testing
- [ ] Call with pageSize=100, verify injection still works across pages
- [ ] Call with different page numbers, verify consistency

## Rollback Plan

If issues occur:
1. Revert to previous app deployment version in Azure
2. Restore database from backup if migrations fail
3. Check Application Insights logs for specific errors

## Known Issues & Mitigation

| Issue | Impact | Mitigation |
|-------|--------|-----------|
| Frequency cap in-memory | Resets on app restart | Monitor memory usage; persist to DB if needed |
| No distributed cache | Multi-instance sync issues | Use Redis for distributed cache in future |
| IMemoryCache not thread-safe in some scenarios | Race conditions | Monitor and add locking if needed |

## Monitoring Post-Deployment

1. Check Azure Application Insights
   - Monitor error rates on /api/feed/portfolio endpoint
   - Monitor response times (target: <500ms)
   - Check memory usage (frequent cache operations)

2. Database Metrics
   - Monitor RewardPointTransaction table growth
   - Monitor SponsoredPost table for new records
   - Check index usage on frequently queried columns

3. Business Metrics
   - Track sponsored post impressions (CurrentImpression field)
   - Track frequency cap hits per day
   - Monitor sponsored post engagement (ViewCount, ClickCount)

## Rollout Timeline

- **Day 1**: Deploy to staging (test portfolio environment)
- **Day 2**: Test end-to-end with sample data
- **Day 3**: Deploy to production with monitoring
- **Day 4+**: Monitor metrics and gather feedback

## Next Phases

1. **Phase 2**: Integrate sponsored posts into Company service
   - Create /api/company-posts/feed/with-sponsored endpoint
   - Call Portfolio /api/feed/sponsored-posts/ranked endpoint
   - Mix with company posts

2. **Phase 3**: Integrate sponsored posts into Community service
   - Similar pattern as Company service

3. **Phase 4**: Production Stability
   - Persist frequency cap to database
   - Implement distributed caching (Redis)
   - Add comprehensive logging/metrics
   - Create admin dashboard for sponsored post management

## Sign-Off

- [ ] Technical lead verified implementation
- [ ] Product manager approved test results
- [ ] DevOps approved deployment plan
- [ ] Ready for production deployment
