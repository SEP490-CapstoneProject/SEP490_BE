# Skill Standardization - Complete Reset Implementation Guide

## Current Status
✅ Plan created (CLEAN SLATE approach)
✅ AI prompts enhanced with skill generation rules (`GeminiAIClient.cs`)
✅ Complete data wipe script created (`SKILL_CLEANUP_COMPLETE.sql`)

## What Changed

### 1. Enhanced AI Prompts
**File**: `D:\Capstone\src\Services\Challenge\Challenge.Infrastructure\Clients\GeminiAIClient.cs`

The `AnalyzeChallengeAsync` prompt now includes:
- **CRITICAL SKILL GENERATION RULES** section
- Clear distinction between GOOD and BAD skills
- Examples aligned with @request standards
- Instructions to use canonical naming (e.g., "ASP.NET Core" not "Asp Net")
- Warnings against challenge-specific skills

**Examples enforced**:
- ✅ GOOD: "C#", "ASP.NET Core", "SignalR", "Database Design", "REST API Design"
- ❌ BAD: "SecurityEngineering", "PasswordHashing", "Chat App Logic", "Persistence Mechanisms"

### 2. Complete Data Wipe Strategy
**Script**: `SKILL_CLEANUP_COMPLETE.sql`

Deletes ALL data from Challenge Service (respecting foreign keys):
1. SUBMISSION_CRITERIA_SCORES
2. SKILL_POINT_TRANSACTIONS
3. USER_SKILLS
4. CRITERIA_SKILL_MAPPINGS
5. CHALLENGE_CRITERIA
6. EVALUATION_CRITERIA
7. SKILL_ALIASES
8. PENDING_SKILLS
9. SKILLS
10. SUBMISSIONS
11. CHALLENGE_VERSIONS
12. CHALLENGES
13. PROMPT_SANITIZATION_LOGS

**Result**: Empty database, ready for fresh start

## Implementation Steps (To Execute)

### Step 1: Build challenge-service v14 with Enhanced Prompts
```pwsh
cd D:\Capstone\src\Services\Challenge\Challenge.API
dotnet build -c Release

# Verify build succeeds
# If successful, proceed to Step 2
```

### Step 2: Wipe Challenge Service Database
```powershell
# Connect to SQL Server and execute cleanup script
# Method 1: Via SQL Server Management Studio
# - Open SKILL_CLEANUP_COMPLETE.sql
# - Connect to server with SA credentials
# - Select ChallengeServiceDb database
# - Execute

# Method 2: Via sqlcmd (command line)
sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -d ChallengeServiceDb -i D:\Capstone\SKILL_CLEANUP_COMPLETE.sql -o D:\Capstone\cleanup_result.log
```

### Step 3: Deploy challenge-service v14
```pwsh
# Build and push Docker image
cd D:\Capstone\src\Services\Challenge\Challenge.API
docker build -t challenge-service:v14 .
docker tag challenge-service:v14 [your-registry]/challenge-service:v14
docker push [your-registry]/challenge-service:v14

# Update deployment to use v14
# (via your deployment pipeline)
```

### Step 4: E2E Test with New Standards
1. **Create Challenge** - New challenges will use v14 with standardized skill generation
2. **Submit for Review** - Verify skills generated are canonical (no PascalCase, no vague labels)
3. **Approve & Publish** - Challenge version becomes active
4. **Submit Solution** - User submits code
5. **Grade Submission** - AI grades using version criteria
6. **Verify Results** - Check all data is standards-compliant

### Step 5: Verify Database
```sql
-- Query to verify all new skills are canonical
SELECT Id, Name, Slug, CreatedAt FROM SKILLS ORDER BY CreatedAt DESC;

-- Examples of VALID names you should see:
-- - C#
-- - ASP.NET Core
-- - REST API Design
-- - Database Design
-- - etc.

-- If you see PascalCase or vague names = PROBLEM, investigate AI response
```

## Success Criteria
- ✅ Database completely wiped before deployment
- ✅ challenge-service v14 deployed with enhanced prompts
- ✅ E2E test creates new skills matching @request standards
- ✅ All skills are: reusable, canonical, measurable, mid-level granularity
- ✅ No PascalCase names
- ✅ No vague/abstract labels
- ✅ No challenge-specific wording

## Files
- `GeminiAIClient.cs` - Enhanced AI prompts (MODIFIED)
- `SKILL_CLEANUP_COMPLETE.sql` - Full database wipe (CREATED)
- `SKILL_STANDARDIZATION_GUIDE.md` - This guide (CREATED)

## Rollback Plan
If issues arise after deployment:
1. Restore database backup (if available)
2. Revert to challenge-service v13
3. Investigate AI response format issues
4. Create new version with fixes

## Important Notes
- **NO DATA PRESERVATION**: This approach deletes all existing test data
- **FRESH START**: All new data created will be standards-compliant
- **IRREVERSIBLE**: Ensure you have backups if you need to preserve any test data
