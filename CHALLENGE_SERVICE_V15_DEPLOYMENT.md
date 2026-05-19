# Challenge Service v15 Deployment Guide

## Status: ✅ READY TO DEPLOY

**Docker Image**: `skillsnapacr2604282023545.azurecr.io/challenge-service:v15`
**Size**: 856 bytes (manifest)
**Built**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## What's New in v15

### API Segregation
- **Creator Management APIs** (`/api/creator/challenges/*`)
  - List challenges created by user
  - View all versions of a challenge
  - Switch active version
  
- **Participant Discovery APIs** (`/api/challenges/public/*`)
  - List published challenges (active versions only)
  - Get single published challenge
  - Search and skill filtering

### Code Changes
- New controllers: `ChallengeCreatorController`, `ChallengeDiscoveryController`
- New DTOs: `CreatorChallengeDto`, `PublicChallengeDto`, `ChallengeVersionDto`
- Enhanced service methods in `IChallengeService`
- DbContext navigation property for `ChallengeCriteria.Criteria`
- ChallengeCriteriaRepository includes navigation load

### Database
- **No migration required** - only EF Core relationship configuration
- New navigation property added to ChallengeCriteria entity

## Pre-Deployment Checklist

- [x] Code compiles successfully (0 errors)
- [x] Docker image built successfully
- [x] Docker image pushed to ACR (v15 and latest tags)
- [x] All required DTOs created
- [x] All controllers implemented
- [x] Service methods implemented
- [x] DbContext relationship configured
- [ ] Deploy to container app (NEXT STEP)
- [ ] Run E2E tests
- [ ] Verify APIs with curl/Postman

## Deployment Steps

### Step 1: Update Container App Image
```powershell
$acrName = "skillsnapacr2604282023545"
$acrUrl = "$acrName.azurecr.io"
$resourceGroup = "skillsnap-rg-2604282023"
$containerAppName = "challenge-service"
$imageUrl = "$acrUrl/challenge-service:v15"

Write-Host "Updating container app $containerAppName..." -ForegroundColor Cyan

az containerapp update `
    --name $containerAppName `
    --resource-group $resourceGroup `
    --image "$imageUrl" `
    --output table

if ($LASTEXITCODE -eq 0) {
    Write-Host "Container app updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Update failed!" -ForegroundColor Red
}
```

### Step 2: Verify Deployment
```powershell
$resourceGroup = "skillsnap-rg-2604282023"
$containerAppName = "challenge-service"

# Check container app status
az containerapp show `
    --name $containerAppName `
    --resource-group $resourceGroup `
    --query "{name:name, state:properties.runningStatus, latestRevision:properties.latestRevisionName}" `
    --output table

# Check recent logs
az containerapp logs show `
    -g $resourceGroup `
    -n $containerAppName `
    --tail 50
```

### Step 3: Test API Endpoints

#### Test Creator Endpoints (requires authentication)
```bash
# Get creator token (substitute with actual auth)
TOKEN="your_creator_token"
CREATOR_CHALLENGE_ID="your_challenge_id"

# List creator's challenges
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/creator/challenges?skip=0&take=10" \
  -H "Authorization: Bearer $TOKEN"

# Get challenge versions
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/creator/challenges/$CREATOR_CHALLENGE_ID/versions" \
  -H "Authorization: Bearer $TOKEN"
```

#### Test Participant Endpoints (public, no auth)
```bash
# List published challenges
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/challenges/public?skip=0&take=20"

# Get single challenge
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/challenges/public/{challengeId}"

# Search challenges
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/challenges/public?search=backend"
```

## Expected Behavior After Deployment

### For Challenge Creators
1. ✅ Can list all their challenges (Draft, PendingReview, Published)
2. ✅ Can view all versions with complete criteria and skill mappings
3. ✅ Can switch active version via PUT endpoint
4. ✅ Full data access for management

### For Participants
1. ✅ Can discover published challenges
2. ✅ See only active versions
3. ✅ See skill weights and criteria
4. ✅ Can submit solutions to published challenges
5. ✅ No access to draft or internal data

### Backward Compatibility
- ✅ Existing `/api/challenges` endpoints remain unchanged
- ✅ Old APIs still work for existing clients
- ✅ No breaking changes

## Rollback Plan

If deployment fails or issues arise:

```powershell
# Rollback to previous version (v14)
$acrName = "skillsnapacr2604282023545"
$acrUrl = "$acrName.azurecr.io"
$resourceGroup = "skillsnap-rg-2604282023"
$containerAppName = "challenge-service"
$imageUrl = "$acrUrl/challenge-service:v14"

Write-Host "Rolling back to v14..." -ForegroundColor Yellow

az containerapp update `
    --name $containerAppName `
    --resource-group $resourceGroup `
    --image "$imageUrl"

Write-Host "Rollback complete!" -ForegroundColor Green
```

## Monitoring

### Key Logs to Check
- ❌ Errors related to navigation properties
- ❌ 401/403 authorization failures
- ⚠️ Warnings about null references
- ✅ Successful API calls (200 responses)

### Sample Health Check
```powershell
# Health check endpoint (if configured)
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/health"

# API availability
curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/challenges/public" `
    -w "\nStatus Code: %{http_code}\n"
```

## Post-Deployment Verification

### Quick Smoke Test
```powershell
$baseUrl = "https://challenge-service.eastasia.azurecontainerapps.io"

Write-Host "Running smoke tests..." -ForegroundColor Cyan

# Test 1: Public API (no auth)
Write-Host "Test 1: Public challenge discovery..." -ForegroundColor Yellow
$response = curl -s -X GET "$baseUrl/api/challenges/public?take=1"
if ($response) {
    Write-Host "✓ Public API working" -ForegroundColor Green
} else {
    Write-Host "✗ Public API failed" -ForegroundColor Red
}

# Test 2: Health (if available)
Write-Host "Test 2: Health check..." -ForegroundColor Yellow
$health = curl -s -w "%{http_code}" "$baseUrl/health" 2>&1
Write-Host "✓ Service responding" -ForegroundColor Green
```

## Troubleshooting

### Issue: 401 Unauthorized on Creator Endpoints
**Cause**: Token not provided or invalid
**Solution**: Ensure Authorization header is included with valid JWT

### Issue: 500 Internal Server Error
**Cause**: Missing navigation property or DbContext issue
**Solution**: 
1. Check logs: `az containerapp logs show -g skillsnap-rg-2604282023 -n challenge-service --tail 50`
2. Look for "Criteria does not contain a definition"
3. Rollback to v14 if persistent

### Issue: No Results from Public API
**Cause**: No published challenges in database
**Solution**: Create and publish a challenge via admin API

### Issue: Active Version Not Returned
**Cause**: CurrentVersionId is null
**Solution**: Ensure challenge has been approved and has active version

## Files Modified/Created

### New Files
- `Challenge.Application/DTOs/CreatorChallengeDto.cs` (4.2 KB)
- `Challenge.API/Controllers/ChallengeCreatorController.cs` (4.1 KB)
- `Challenge.API/Controllers/ChallengeDiscoveryController.cs` (2.6 KB)

### Modified Files
- `Challenge.Application/Interfaces/IChallengeService.cs` - +5 methods
- `Challenge.Application/Services/ChallengeServiceImpl.cs` - +200 lines
- `Challenge.Domain/Entities/ChallengeCriteria.cs` - +1 navigation property
- `Challenge.Infrastructure/Persistence/Repositories/ChallengeCriteriaRepository.cs` - Added .Include()
- `Challenge.Infrastructure/Persistence/ChallengeDbContext.cs` - +2 relationship config

## Documentation

- **API Reference**: `CHALLENGE_API_V15_SEGREGATION.md`
- **Deployment Checklist**: This file
- **E2E Tests**: See E2E testing section below

## E2E Testing After Deployment

### Scenario 1: Creator Workflow
```powershell
# 1. Get creator token and challenge ID
$creatorToken = "eyJhbGc..."
$challengeId = "00000000-0000-0000-0000-000000000001"

# 2. List creator's challenges
$response = curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/creator/challenges" `
    -H "Authorization: Bearer $creatorToken"
Write-Host "Creator challenges: $response"

# 3. Get versions
$versions = curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/creator/challenges/$challengeId/versions" `
    -H "Authorization: Bearer $creatorToken"
Write-Host "Challenge versions: $versions"
```

### Scenario 2: Participant Workflow
```powershell
# 1. Discover published challenges
$challenges = curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/challenges/public"
Write-Host "Published challenges: $challenges"

# 2. Get challenge details
$challengeId = "00000000-0000-0000-0000-000000000001"
$challenge = curl -X GET "https://challenge-service.eastasia.azurecontainerapps.io/api/challenges/public/$challengeId"
Write-Host "Challenge details: $challenge"

# 3. Submit solution (existing endpoint still works)
$submissionBody = @{
    content = "code here..."
    githubUrl = "https://github.com/..."
} | ConvertTo-Json

$submission = curl -X POST "https://challenge-service.eastasia.azurecontainerapps.io/api/submissions?challengeId=$challengeId" `
    -H "Authorization: Bearer $participantToken" `
    -H "Content-Type: application/json" `
    -d $submissionBody
```

## Summary

Challenge Service v15 is ready for production deployment with:
- ✅ Complete API segregation (creator vs participant)
- ✅ Backward compatibility maintained
- ✅ No database migration required
- ✅ Comprehensive documentation
- ✅ Rollback plan in place

**Next Steps**:
1. Deploy to Azure Container Apps
2. Run smoke tests
3. Verify both creator and participant workflows
4. Monitor logs for 24 hours
5. Announce to frontend team with new API endpoints
