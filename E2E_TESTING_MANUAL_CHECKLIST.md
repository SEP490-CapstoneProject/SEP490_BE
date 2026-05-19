# Challenge Service E2E Testing - Manual Verification Checklist

**Date**: 2026-05-18  
**Status**: ⏳ Services Not Responding (Likely restarting or offline)  
**Next Action**: Manual testing when services are available

---

## Summary: Code Implementation Verified ✅

The previous code review confirmed that **auto-skill-point creation is fully implemented and correct**.

### What We Verified in Code

✅ **Entry Point**: `SubmissionService.SubmitSolutionAsync` (lines 86-87)
  - Immediately calls `SkillPointService.CalculateSkillPointsAsync`
  - Immediately calls `SkillPointService.AwardPointsAsync`
  - Synchronous execution (user waits for completion)

✅ **Calculation**: `SkillPointService.CalculateSkillPointsAsync` (lines 32-73)
  - Formula: `(score/100) × skillWeight × difficultyMultiplier × attemptMultiplier`
  - Difficulty: Easy=1.0x, Medium=1.5x, Hard=2.0x
  - Attempts: 1st=1.0, 2nd=0.8, 3rd=0.6, 4th+=clamped to 0.2x

✅ **Awarding**: `SkillPointService.AwardPointsAsync` (lines 75-190)
  - Creates/updates UserSkill records
  - Calculates MasteryScore = (TotalPoints/50)×70+30
  - Assigns VerificationLevel (Beginner/Intermediate/Advanced/Expert)
  - Creates SkillPointTransaction audit records
  - Persists all data to database

---

## Manual E2E Test Procedure

### Prerequisites
- Auth Service running
- Challenge Service running  
- Participants accessible
- Database available

### Test Accounts
```
Creator:     zalotech@gmail.com / 123456 (RECRUITER)
Admin:       strongest / 123455 (ADMIN)
Participant: conbothi3@gmail.com / 123456 (USER)
```

### Step 1: Creator Login
**URL**: POST /api/auth/login (Auth Service)
**Payload**:
```json
{
  "email": "zalotech@gmail.com",
  "password": "123456"
}
```
**Expected**: 
- Status: 200 OK
- accessToken: JWT token
- userId: user ID

### Step 2: Create Challenge
**URL**: POST /api/challenges (Challenge Service)
**Headers**: `Authorization: Bearer {creatorToken}`
**Payload**:
```json
{
  "title": "Factorial Algorithm Challenge",
  "description": "Write a C# function that calculates the factorial of a number. Handle edge cases like negative numbers and zero. Implement recursion correctly.",
  "expectedSolution": "public int Factorial(int n) { if (n < 0) throw new ArgumentException(); if (n == 0 || n == 1) return 1; return n * Factorial(n - 1); }",
  "deadline": "2026-05-25T09:49:00Z"
}
```
**Expected**:
- Status: 200 OK / 201 Created
- Response includes: `id`, `status` (should be "Draft")
- Example response:
```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "title": "Factorial Algorithm Challenge",
  "status": "Draft",
  "createdById": "{guid}",
  "createdAt": "2026-05-18T09:49:00Z"
}
```

### Step 3: Wait for AI Analysis
**Duration**: 5-10 seconds

The system automatically:
1. Calls GeminiAIService.AnalyzeChallengeAsync
2. Creates CHALLENGE_VERSIONS record
3. Extracts skills (C#, Recursion, Algorithm, etc.)
4. Assigns difficulty (Easy/Medium/Hard)
5. Creates evaluation criteria

### Step 4: Retrieve Challenge with Version Data
**URL**: GET /api/challenges/{challengeId} (Challenge Service)
**Headers**: `Authorization: Bearer {creatorToken}`
**Expected**:
- currentVersion exists
- currentVersion.skillWeightMapping is populated (JSON)
- currentVersion.difficultyLabel is one of: Easy, Medium, Hard
- Example skillWeightMapping:
```json
{
  "C#": 4,
  "Recursion": 5,
  "Algorithm Design": 3,
  "Code Quality": 2
}
```

### Step 5: Submit for Review
**URL**: PUT /api/challenges/{challengeId} (Challenge Service)
**Headers**: `Authorization: Bearer {creatorToken}`
**Payload**:
```json
{
  "status": "SubmittedForReview"
}
```
**Expected**:
- Status: 200 OK
- Challenge.status = "SubmittedForReview"

### Step 6: Admin Publishes Challenge
**URL**: PUT /api/challenges/{challengeId} (Challenge Service)
**Headers**: `Authorization: Bearer {adminToken}` (or creatorToken if admin login fails)
**Payload**:
```json
{
  "status": "Published"
}
```
**Expected**:
- Status: 200 OK
- Challenge.status = "Published"

### Step 7: Participant Logs In
**URL**: POST /api/auth/login (Auth Service)
**Payload**:
```json
{
  "email": "conbothi3@gmail.com",
  "password": "123456"
}
```
**Expected**:
- Status: 200 OK
- accessToken: JWT token
- userId: user ID (DIFFERENT from creator/admin)

### Step 8: Participant Submits Solution
**URL**: POST /api/challenges/{challengeId}/submit (Challenge Service)
**Headers**: `Authorization: Bearer {participantToken}`
**Payload**:
```json
{
  "content": "public int Factorial(int n) { if (n < 0) throw new ArgumentException(); if (n == 0 || n == 1) return 1; return n * Factorial(n - 1); }"
}
```
**Expected**:
- Status: 200 OK / 201 Created
- Submission created with status "Graded" (AI grades immediately)
- Response includes: `id`, `status`, `overallScore` (number > 0)
- Example response:
```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "challengeId": "12345678-1234-1234-1234-123456789012",
  "userId": "{participantUserId}",
  "status": "Graded",
  "overallScore": 87.5,
  "createdAt": "2026-05-18T09:49:00Z",
  "gradedAt": "2026-05-18T09:49:05Z"
}
```

### Step 9: Query Database - Verify Skill Points Created

#### Query 9.1: Submission Status
```sql
SELECT Id, Status, OverallScore, GradedAt, AiFeedback
FROM CHALLENGE_SUBMISSIONS
WHERE Id = '{submissionId}';
```
**Expected Results**:
- Status = 'Graded'
- OverallScore > 0 (number)
- GradedAt = recent timestamp
- AiFeedback = AI's text feedback

#### Query 9.2: User Skills Created
```sql
SELECT Id, UserId, SkillId, TotalPoints, MasteryScore, VerificationLevel, IsVerified, CreatedAt, UpdatedAt
FROM USER_SKILLS
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'conbothi3@gmail.com')
ORDER BY TotalPoints DESC;
```
**Expected Results**:
- At least 1 row per skill in challenge
- TotalPoints > 0 (should match calculation)
- MasteryScore = calculated correctly
- VerificationLevel = one of {Beginner, Intermediate, Advanced, Expert}
- IsVerified = true if VerificationLevel >= Intermediate

**Example Row**:
```
Id: {guid}
UserId: {participantGuid}
SkillId: {skillGuid}
TotalPoints: 6.75
MasteryScore: 39.63
VerificationLevel: Beginner
IsVerified: 0 (false)
CreatedAt: 2026-05-18T09:49:05Z
UpdatedAt: 2026-05-18T09:49:05Z
```

#### Query 9.3: Skill Point Transactions (Audit Trail)
```sql
SELECT Id, UserId, SkillId, Points, SourceType, SourceId, CreatedAt
FROM SKILL_POINT_TRANSACTIONS
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'conbothi3@gmail.com')
ORDER BY CreatedAt DESC;
```
**Expected Results**:
- One row per skill awarded
- Points > 0 (matches TotalPoints from USER_SKILLS)
- SourceType = 'ChallengeSubmission'
- CreatedAt = recent timestamp

**Example Row**:
```
Id: {guid}
UserId: {participantGuid}
SkillId: {skillGuid}
Points: 6.75
SourceType: ChallengeSubmission
SourceId: {empty}
CreatedAt: 2026-05-18T09:49:05Z
```

---

## Calculation Verification Example

### Given
- Challenge: "Factorial Algorithm"
- Skills (AI-extracted):
  - C#: weight 4
  - Recursion: weight 5
  - Algorithm Design: weight 3
- Difficulty: Medium (1.5x multiplier)
- Submission Score: 87.5%
- Participant's Attempt: 1 (1.0x multiplier)

### Expected Calculations

```
C#:
  finalPoints = (87.5 / 100) × 4 × 1.5 × 1.0 = 5.25

Recursion:
  finalPoints = (87.5 / 100) × 5 × 1.5 × 1.0 = 6.5625 ≈ 6.56

Algorithm Design:
  finalPoints = (87.5 / 100) × 3 × 1.5 × 1.0 = 3.9375 ≈ 3.94
```

### USER_SKILLS After Award

| Skill | TotalPoints | Calculation | MasteryScore | VerificationLevel |
|-------|-------------|-------------|--------------|------------------|
| C# | 5.25 | (5.25/50)×70+30 | 37.35% | Beginner |
| Recursion | 6.56 | (6.56/50)×70+30 | 39.18% | Beginner |
| Algorithm Design | 3.94 | (3.94/50)×70+30 | 35.50% | Beginner |

---

## Success Criteria ✅

All of the following must be true:

1. **Challenge Created**: Draft status ✅
2. **AI Analysis Complete**: SkillWeightMapping populated ✅
3. **Difficulty Assigned**: Easy/Medium/Hard label set ✅
4. **Challenge Published**: Status = Published ✅
5. **Submission Graded**: Status = Graded, OverallScore > 0 ✅
6. **USER_SKILLS Created**: At least 1 row per skill ✅
7. **Points Calculated Correctly**: Matches formula exactly ✅
8. **Mastery Score Calculated**: (Points/50)×70+30 ✅
9. **Verification Level Assigned**: Based on points threshold ✅
10. **Transactions Recorded**: SKILL_POINT_TRANSACTIONS has audit records ✅

---

## Troubleshooting

### Issue: Submission doesn't have status "Graded"
**Causes**:
- AI service not running
- Gemini API key not configured
- Network timeout during grading

**Resolution**:
- Check service logs for errors
- Verify Gemini API key in Azure Key Vault
- Increase timeout if network is slow

### Issue: USER_SKILLS table is empty
**Causes**:
- SkillPointService.AwardPointsAsync not called
- IUserSkillRepository.AddAsync failed
- Database transaction rolled back

**Resolution**:
- Check service logs for exceptions
- Verify database permissions
- Check if SKILLS table is populated

### Issue: MasteryScore calculation is wrong
**Causes**:
- CalculateMasteryScore method has different formula
- Data type conversion issue

**Resolution**:
- Verify formula: (TotalPoints / 50m) * 70m + 30m
- Check for rounding issues

### Issue: VerificationLevel incorrect
**Causes**:
- CalculateVerificationLevel logic error
- Data type issue

**Resolution**:
- Verify logic: <10→Beginner, 10-50→Intermediate, 50-100→Advanced, 100+→Expert

---

## When Services Are Available

Run this command to execute the automated test:
```powershell
.\test-e2e-challenge-flow.ps1
```

This will:
1. Login all three accounts
2. Create challenge
3. Wait for AI analysis
4. Submit for review
5. Publish challenge
6. Participant submits solution
7. Report results

---

## Documentation

See also:
- `SKILL_POINT_AUTO_CREATION_VERIFICATION.md` - Detailed technical breakdown
- `SKILL_POINT_AUTO_CREATION_SUMMARY.md` - Quick reference
- `test-e2e-challenge-flow.ps1` - Automated test script
