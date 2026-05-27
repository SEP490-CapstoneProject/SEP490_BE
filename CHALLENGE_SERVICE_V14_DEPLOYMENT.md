# Challenge Service v14 Deployment Checklist - Skill Standardization

**Status**: ✅ Build Complete | ⏳ Deployment Ready

## Phase 1: ✅ Development Complete

### Code Changes Applied
- ✅ `GeminiAIClient.AnalyzeChallengeAsync` enhanced with skill generation rules (40+ lines)
- ✅ All files compile without build errors
- ✅ Build completed successfully with 2 warnings (pre-existing style issues)

### Files Modified
```
src/Services/Challenge/Challenge.Infrastructure/Clients/GeminiAIClient.cs
  - Lines 24: Model defaults to "gemini-3.1-flash-lite"
  - Lines 34-83: Enhanced prompt with CRITICAL SKILL GENERATION RULES
  - Enforces canonical naming, reusable skills, measurable competencies
  - Examples: C#, ASP.NET Core, REST API Design, Database Design
  - Anti-examples: SecurityEngineering, PasswordHashing, Chat App Logic
```

### Support Files Created
- `SKILL_CLEANUP_COMPLETE.sql` - Database cleanup script
- `SKILL_AUDIT_QUERY.sql` - Skill audit/classification queries
- `SKILL_STANDARDIZATION_GUIDE.md` - Implementation guide

---

## Phase 2: 🚀 Deployment (Azure Container Apps)

### Step 1: Build Docker Image
```pwsh
cd D:\Capstone

# Build challenge-service v14
docker build -f src/Services/Challenge/Challenge.API/Dockerfile `
  -t challenge-service:v14 `
  .

# Verify image created
docker images | Select-String challenge-service
```

### Step 2: Push to Registry
```pwsh
# Tag for Azure Container Registry
$registryName = "skillsnap"
$imageName = "$registryName.azurecr.io/challenge-service:v14"

docker tag challenge-service:v14 $imageName

# Login to ACR (if not already logged in)
az acr login --name $registryName

# Push image
docker push $imageName

# Verify push
az acr repository show --name $registryName --image challenge-service:v14
```

### Step 3: Create New Revision in Azure Container Apps
```pwsh
# Update deployment with new image
# Via Azure Portal OR CLI:

az containerapp update `
  --name challenge-service `
  --resource-group skillsnap-rg-2604282023 `
  --image $imageName `
  --query properties.latestRevisionFqdn

# OR via Azure Portal:
# 1. Navigate to Container Apps > challenge-service
# 2. Click "Revisions" 
# 3. Click "Create new revision"
# 4. Update image to: skillsnap.azurecr.io/challenge-service:v14
# 5. Click "Create"
```

### Step 4: Verify Deployment Health
```pwsh
# Wait for new revision to be ready
do {
    $status = az containerapp show `
      --name challenge-service `
      --resource-group skillsnap-rg-2604282023 `
      --query 'properties.provisioningState' `
      --output tsv
    
    Write-Host "Status: $status"
    Start-Sleep -Seconds 5
} while ($status -ne "Succeeded")

# Test health endpoint
$gateway = "https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io"
Invoke-RestMethod -Uri "$gateway/health" -SkipCertificateCheck
```

---

## Phase 3: 🗑️ Database Cleanup (Azure SQL)

### Step 1: Connect to Azure SQL
```pwsh
# Get SQL Server address from Azure Portal
# Resource: ChallengeServiceDb

$sqlServer = "skillsnap.database.windows.net"  # Replace with your server
$database = "ChallengeServiceDb"
$username = "sqladmin"  # Your SQL admin username
$password = "YourPassword"  # Your SQL admin password

# Using sqlcmd
sqlcmd -S $sqlServer -d $database -U $username -P $password -i D:\Capstone\SKILL_CLEANUP_COMPLETE.sql -o cleanup_result.log
```

### Step 2: Verify Cleanup
```sql
-- Run this to confirm all tables are empty
SELECT 
    'CHALLENGES' as TableName, COUNT(*) as RowCount FROM CHALLENGES
UNION ALL
SELECT 'CHALLENGE_VERSIONS', COUNT(*) FROM CHALLENGE_VERSIONS
UNION ALL
SELECT 'SUBMISSIONS', COUNT(*) FROM SUBMISSIONS
UNION ALL
SELECT 'SKILLS', COUNT(*) FROM SKILLS
UNION ALL
SELECT 'EVALUATION_CRITERIA', COUNT(*) FROM EVALUATION_CRITERIA
UNION ALL
SELECT 'CHALLENGE_CRITERIA', COUNT(*) FROM CHALLENGE_CRITERIA
UNION ALL
SELECT 'CRITERIA_SKILL_MAPPINGS', COUNT(*) FROM CRITERIA_SKILL_MAPPINGS
UNION ALL
SELECT 'USER_SKILLS', COUNT(*) FROM USER_SKILLS
UNION ALL
SELECT 'SKILL_POINT_TRANSACTIONS', COUNT(*) FROM SKILL_POINT_TRANSACTIONS;

-- Expected result: All counts = 0
```

---

## Phase 4: 🧪 E2E Testing

### Test Scenario: Create Challenge with New Standards
**Objective**: Verify AI generates ONLY canonical, reusable skills

#### Step 1: Login
```pwsh
$gateway = "https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io"
$loginUrl = "$gateway/api/auth/login"

$loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body '{
    "email": "company A",
    "password": "123"
  }'

$token = $loginResponse.token
Write-Host "✅ Logged in successfully"
```

#### Step 2: Create Challenge
```pwsh
$createChallengeUrl = "$gateway/api/challenges"

$challengePayload = @{
    title = "Build a REST API with C#"
    description = "Create a RESTful API service using ASP.NET Core with proper authentication and database design"
    expectedSolution = "Use controllers, dependency injection, Entity Framework Core, and implement JWT authentication"
    visibility = "draft"
} | ConvertTo-Json

$challengeResponse = Invoke-RestMethod -Uri $createChallengeUrl -Method Post `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $challengePayload

$challengeId = $challengeResponse.id
Write-Host "✅ Challenge created: $challengeId"
```

#### Step 3: Submit for Review
```pwsh
$submitUrl = "$gateway/api/challenges/$challengeId/submit-review"

$reviewResponse = Invoke-RestMethod -Uri $submitUrl -Method Post `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }

Write-Host "✅ Submitted for review"
Write-Host "Generated Skills:"
$reviewResponse.skillWeights | ForEach-Object { Write-Host "  - $_" }
```

#### Step 4: VERIFY SKILLS ARE STANDARDS-COMPLIANT
```
Expected:
  ✅ C#
  ✅ ASP.NET Core
  ✅ REST API Design
  ✅ Database Design
  ✅ Authentication

NOT Expected:
  ❌ SecurityEngineering
  ❌ PasswordHashing
  ❌ Persistence Mechanisms
  ❌ Asp Net (should be ASP.NET Core)
  ❌ Any PascalCase names
  ❌ Any vague labels
```

#### Step 5: Complete Challenge Flow
```pwsh
# Approve version
$approveUrl = "$gateway/api/challenges/$challengeId/versions/{versionId}/approve"
Invoke-RestMethod -Uri $approveUrl -Method Post `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }

# Publish version
$publishUrl = "$gateway/api/challenges/$challengeId/versions/{versionId}/publish"
Invoke-RestMethod -Uri $publishUrl -Method Post `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }

# Submit solution (as user)
# -> User submits code
# -> AI grades
# -> Check if skill points are created correctly

Write-Host "✅ Challenge flow complete"
```

#### Step 6: Query Database to Verify Data
```sql
-- Verify new skills exist and are canonical
SELECT TOP 20 Id, Name, Slug, CreatedAt FROM SKILLS ORDER BY CreatedAt DESC;

-- Verify no bad skills created
SELECT * FROM SKILLS WHERE Name LIKE '%[A-Z][a-z]%[A-Z]%' AND Name NOT LIKE '% %';  -- Should return 0 rows

-- Verify challenge criteria were created
SELECT * FROM CHALLENGE_CRITERIA WHERE ChallengeVersionId = '{versionId}';

-- Verify skill mappings exist
SELECT * FROM CRITERIA_SKILL_MAPPINGS WHERE ChallengeVersionId = '{versionId}';
```

---

## Success Criteria

✅ **Build**: Succeeds with no new errors
✅ **Deployment**: New revision is healthy and responding
✅ **Database**: All test data cleaned, tables are empty before new tests
✅ **New Skills**: All generated skills are canonical, reusable, measurable
✅ **No Bad Skills**: Database contains NO PascalCase, no vague labels, no challenge-specific wording
✅ **E2E Flow**: Challenge creation → review → grade → skill points all work correctly

---

## Rollback Plan

If issues occur:

```pwsh
# Revert to previous revision
az containerapp revision deactivate `
  --name challenge-service `
  --resource-group skillsnap-rg-2604282023 `
  --revision challenge-service--{current-bad-revision}

# Activate previous version
az containerapp revision activate `
  --name challenge-service `
  --resource-group skillsnap-rg-2604282023 `
  --revision challenge-service--{previous-good-revision}
```

---

## Deployment Timeline

1. **Build**: 2-3 minutes
2. **Push to ACR**: 1-2 minutes  
3. **Deploy to Container Apps**: 5-10 minutes
4. **Cleanup Database**: 1-2 minutes
5. **E2E Testing**: 5-10 minutes

**Total: ~15-25 minutes**

---

## Support Resources

- Azure CLI docs: `az containerapp update --help`
- Container Apps logs: `az containerapp logs show -g skillsnap-rg-2604282023 -n challenge-service`
- SQL connection string: From Azure Portal > ChallengeServiceDb > Connection Strings
- Skill standards reference: `D:\Capstone\request` file
