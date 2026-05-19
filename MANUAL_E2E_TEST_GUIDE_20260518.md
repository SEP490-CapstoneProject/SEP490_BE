# 🧪 MANUAL E2E TEST GUIDE - Step by Step

**Hướng dẫn kiểm tra toàn bộ hoạt động của Challenge Service**  
(Complete testing guide for Challenge Service)

**Ngày**: 2026-05-18  
**Trạng thái**: Ready to test

---

## 🚀 QUICK START - Copy/Paste Ready

### Test Credentials

```
Creator Account:
  Email: zalotech@gmail.com
  Password: 123456

Participant Account:
  Email: conbothi3@gmail.com
  Password: 123456
```

### Service URLs

```
Auth Service: https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
Challenge Service: https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
```

---

## STEP 1: Test Login (Creator)

### Method: POST
```
URL: https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth/login
```

### Headers
```
Content-Type: application/json
```

### Body (Raw JSON)
```json
{
  "email": "zalotech@gmail.com",
  "password": "123456"
}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 12,
      "email": "zalotech@gmail.com",
      "name": "Creator User"
    }
  }
}
```

### Save This Token
```
CREATOR_TOKEN = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
CREATOR_ID = 12
```

---

## STEP 2: Test Login (Participant)

### Method: POST
```
URL: https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth/login
```

### Headers
```
Content-Type: application/json
```

### Body (Raw JSON)
```json
{
  "email": "conbothi3@gmail.com",
  "password": "123456"
}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 2,
      "email": "conbothi3@gmail.com",
      "name": "Participant User"
    }
  }
}
```

### Save This Token
```
PARTICIPANT_TOKEN = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
PARTICIPANT_ID = 2
```

---

## STEP 3: Create Challenge

### Method: POST
```
URL: https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/challenges
```

### Headers
```
Authorization: Bearer {CREATOR_TOKEN}
Content-Type: application/json
```

### Body (Raw JSON)
```json
{
  "title": "Build Authentication System",
  "description": "Create a secure authentication system with JWT tokens and password hashing",
  "expectedSolution": "public class AuthService {\n  public string GenerateToken(User user) {\n    var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };\n    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));\n    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);\n    var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);\n    return new JwtSecurityTokenHandler().WriteToken(token);\n  }\n}",
  "deadline": "2026-05-25T23:59:59Z"
}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Build Authentication System",
    "createdById": 12,
    "status": "Draft",
    "createdAt": "2026-05-18T10:40:00Z"
  }
}
```

### Save Challenge ID
```
CHALLENGE_ID = 550e8400-e29b-41d4-a716-446655440000
```

---

## STEP 4: Trigger AI Analysis

### What This Does
- AI analyzes the challenge
- Extracts skills required
- Determines difficulty
- Creates evaluation criteria

### Method: POST
```
URL: https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/challenges/{CHALLENGE_ID}/submit-review
```

Replace `{CHALLENGE_ID}` with actual ID from STEP 3

### Headers
```
Authorization: Bearer {CREATOR_TOKEN}
Content-Type: application/json
```

### Body (Empty JSON)
```json
{}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "challengeId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "AnalyzedByAI",
    "difficulty": "Medium",
    "skills": [
      {
        "name": "JWT",
        "weight": 4.5
      },
      {
        "name": "C#",
        "weight": 3.0
      },
      {
        "name": "Security",
        "weight": 4.0
      }
    ],
    "criteria": [
      "Code Quality",
      "Security Best Practices",
      "Performance",
      "Error Handling",
      "Documentation"
    ]
  }
}
```

### What To Check
- ✅ Status changed to "AnalyzedByAI"
- ✅ Skills extracted (should be 3-5 skills)
- ✅ Difficulty set (Easy/Medium/Hard)
- ✅ Criteria identified (should be 5 criteria)

**Wait**: AI processing takes ~3 seconds. Be patient!

---

## STEP 5: Approve Challenge

### What This Does
- Changes challenge status to Published
- Makes it visible to participants
- Allows submissions

### Method: POST
```
URL: https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/challenges/{CHALLENGE_ID}/approve
```

### Headers
```
Authorization: Bearer {CREATOR_TOKEN}
Content-Type: application/json
```

### Body
```json
{}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "status": "Published",
    "publishedAt": "2026-05-18T10:42:00Z"
  }
}
```

### What To Check
- ✅ Status changed to "Published"
- ✅ Challenge now visible to participants

---

## STEP 6: Submit Solution (Participant)

### What This Does
- Participant submits their solution
- Triggers AI grading
- AI evaluates based on criteria

### Method: POST
```
URL: https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/submissions?challengeId={CHALLENGE_ID}
```

### Headers
```
Authorization: Bearer {PARTICIPANT_TOKEN}
Content-Type: application/json
```

### Body (Raw JSON)
```json
{
  "submissionContent": "public class AuthService {\n  private readonly string _secret = \"your-super-secret-key\";\n  \n  public string GenerateToken(User user) {\n    var claims = new List<Claim> \n    { \n      new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),\n      new Claim(ClaimTypes.Email, user.Email)\n    };\n    \n    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));\n    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);\n    \n    var token = new JwtSecurityToken(\n      issuer: \"YourAppName\",\n      audience: \"YourAppUsers\",\n      claims: claims,\n      expires: DateTime.UtcNow.AddHours(1),\n      signingCredentials: creds\n    );\n    \n    return new JwtSecurityTokenHandler().WriteToken(token);\n  }\n  \n  public bool ValidateToken(string token) {\n    try {\n      var handler = new JwtSecurityTokenHandler();\n      var principal = handler.ValidateToken(token, new TokenValidationParameters {\n        ValidateIssuerSigningKey = true,\n        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),\n        ValidateIssuer = true,\n        ValidIssuer = \"YourAppName\",\n        ValidateAudience = true,\n        ValidAudience = \"YourAppUsers\",\n        ValidateLifetime = true\n      }, out SecurityToken validatedToken);\n      \n      return true;\n    } catch {\n      return false;\n    }\n  }\n}"
}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "challengeId": "550e8400-e29b-41d4-a716-446655440000",
    "userId": 2,
    "status": "Submitted",
    "submittedAt": "2026-05-18T10:43:00Z"
  }
}
```

### Save Submission ID
```
SUBMISSION_ID = 660e8400-e29b-41d4-a716-446655440001
```

### What To Check
- ✅ Submission created successfully
- ✅ Status is "Submitted"
- ✅ Received submission ID

**Wait**: AI grading takes ~10 seconds. Be patient!

---

## STEP 7: Check Submission Status & Grade

### What This Does
- Shows submission grade
- Shows criteria scores
- Shows AI feedback
- Shows skill points awarded

### Method: GET
```
URL: https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/submissions/{SUBMISSION_ID}
```

### Headers
```
Authorization: Bearer {PARTICIPANT_TOKEN}
```

### Expected Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "challengeId": "550e8400-e29b-41d4-a716-446655440000",
    "userId": 2,
    "status": "Graded",
    "overallScore": 82,
    "aiFeedback": "Good implementation of JWT authentication. Code quality is solid. Consider adding token refresh logic and rate limiting.",
    "criteriaScores": [
      {
        "criteria": "Code Quality",
        "score": 85
      },
      {
        "criteria": "Security Best Practices",
        "score": 80
      },
      {
        "criteria": "Performance",
        "score": 78
      },
      {
        "criteria": "Error Handling",
        "score": 88
      },
      {
        "criteria": "Documentation",
        "score": 80
      }
    ],
    "skillPointsAwarded": [
      {
        "skillName": "JWT",
        "points": 3.7
      },
      {
        "skillName": "C#",
        "points": 2.5
      },
      {
        "skillName": "Security",
        "points": 3.2
      }
    ],
    "gradedAt": "2026-05-18T10:43:15Z"
  }
}
```

### What To Check
- ✅ Status changed to "Graded"
- ✅ Overall score between 0-100
- ✅ 5 criteria scores present
- ✅ Skill points awarded
- ✅ AI feedback provided

---

## STEP 8: Verify Database Tables Populated

### After Step 7 completes, verify these tables have data:

```sql
-- Query 1: Check USER_SKILLS table
SELECT TOP 10 * FROM USER_SKILLS 
WHERE UserId IN (2, 12)
ORDER BY CreatedAt DESC;

-- Should show:
-- UserId: 2 (participant)
-- SkillName: JWT, C#, Security
-- TotalPoints: 3.7, 2.5, 3.2 (sum of submissions)
-- MasteryScore: (TotalPoints/50)*70+30
```

```sql
-- Query 2: Check SKILL_POINT_TRANSACTIONS table
SELECT TOP 20 * FROM SKILL_POINT_TRANSACTIONS
WHERE UserId = 2
ORDER BY CreatedAt DESC;

-- Should show:
-- UserId: 2
-- SkillName: JWT, C#, Security
-- Points: 3.7, 2.5, 3.2
-- SourceType: Challenge
-- SourceId: Challenge ID
```

```sql
-- Query 3: Check SUBMISSION_CRITERIA_SCORES table
SELECT TOP 25 * FROM SUBMISSION_CRITERIA_SCORES
WHERE SubmissionId = '{SUBMISSION_ID}'
ORDER BY CreatedAt DESC;

-- Should show:
-- SubmissionId: matching submission
-- CriteriaName: Code Quality, Security Best Practices, etc.
-- Score: 85, 80, 78, 88, 80
```

```sql
-- Query 4: Check all 11 key tables are populated
SELECT 'USER_SKILLS', COUNT(*) FROM USER_SKILLS
UNION ALL SELECT 'SKILL_POINT_TRANSACTIONS', COUNT(*) FROM SKILL_POINT_TRANSACTIONS
UNION ALL SELECT 'SUBMISSION_CRITERIA_SCORES', COUNT(*) FROM SUBMISSION_CRITERIA_SCORES
UNION ALL SELECT 'EVALUATION_CRITERIA', COUNT(*) FROM EVALUATION_CRITERIA
UNION ALL SELECT 'CRITERIA_SKILL_MAPPINGS', COUNT(*) FROM CRITERIA_SKILL_MAPPINGS
UNION ALL SELECT 'CHALLENGE_CRITERIA', COUNT(*) FROM CHALLENGE_CRITERIA
UNION ALL SELECT 'SKILL_CATEGORIES', COUNT(*) FROM SKILL_CATEGORIES
UNION ALL SELECT 'SKILL_ALIASES', COUNT(*) FROM SKILL_ALIASES
UNION ALL SELECT 'PROMPT_SANITIZATION_LOGS', COUNT(*) FROM PROMPT_SANITIZATION_LOGS
UNION ALL SELECT 'SKILL_RELATIONSHIPS', COUNT(*) FROM SKILL_RELATIONSHIPS
UNION ALL SELECT 'PENDING_SKILLS', COUNT(*) FROM PENDING_SKILLS;

-- Should show all with COUNT(*) > 0
```

---

## 🎯 EXPECTED RESULTS SUMMARY

### After Complete Test:

| Table | Before | After | Status |
|-------|--------|-------|--------|
| USER_SKILLS | 0 | 3+ | ✅ POPULATED |
| SKILL_POINT_TRANSACTIONS | 0 | 3+ | ✅ POPULATED |
| SUBMISSION_CRITERIA_SCORES | 0 | 5 | ✅ POPULATED |
| EVALUATION_CRITERIA | 0 | 5+ | ✅ POPULATED |
| CRITERIA_SKILL_MAPPINGS | 0 | 15+ | ✅ POPULATED |
| CHALLENGE_CRITERIA | 0 | 5 | ✅ POPULATED |
| SKILL_CATEGORIES | 0+ | 10+ | ✅ POPULATED |
| SKILL_ALIASES | 0 | 5+ | ✅ POPULATED |
| PROMPT_SANITIZATION_LOGS | 0 | 3+ | ✅ POPULATED |
| SKILL_RELATIONSHIPS | 0 | 0-3 | ⚠️ OK |
| PENDING_SKILLS | 0 | 0-2 | ⚠️ OK |

---

## 🔧 Testing Tools

### Option 1: Postman
1. Create new Collection "Challenge Service Test"
2. Create 8 requests following steps 1-8
3. Set variables for TOKEN and IDs
4. Run requests in sequence

### Option 2: PowerShell
```powershell
# Run test script
cd D:\Capstone
.\test-e2e-fixed.ps1
```

### Option 3: cURL
```bash
# Step 1: Login
curl -X POST https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"zalotech@gmail.com","password":"123456"}'
```

### Option 4: Browser DevTools Console
```javascript
const token = "YOUR_TOKEN_HERE";
const challengeId = "YOUR_CHALLENGE_ID";

// Get submission status
fetch(`https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/submissions/${submissionId}`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
}).then(r => r.json()).then(console.log);
```

---

## ⏱️ TIMING

| Step | Task | Time |
|------|------|------|
| 1 | Login Creator | ~1 sec |
| 2 | Login Participant | ~1 sec |
| 3 | Create Challenge | ~1 sec |
| 4 | AI Analysis | ~3 sec ⏳ |
| 5 | Approve Challenge | ~0.5 sec |
| 6 | Submit Solution | ~0.5 sec |
| 7 | Check Grade | ~10 sec ⏳ (wait for AI) |
| 8 | Query Database | ~1 sec |
| **TOTAL** | **Full Test** | **~18 seconds** |

**⏳ Total wait time: ~13 seconds (AI processing)**

---

## ✅ SUCCESS CHECKLIST

- [ ] Step 1: Creator login successful, token saved
- [ ] Step 2: Participant login successful, token saved
- [ ] Step 3: Challenge created, ID saved
- [ ] Step 4: AI analysis completed, skills extracted
- [ ] Step 5: Challenge approved and published
- [ ] Step 6: Solution submitted successfully
- [ ] Step 7: Submission graded, score 0-100, skill points awarded
- [ ] Step 8: Database queries show all tables populated
- [ ] All 11 tables have data > 0

---

## 🐛 TROUBLESHOOTING

### Issue: "401 Unauthorized"
```
Solution: 
- Check token is valid (not expired)
- Add "Bearer " prefix to token
- Check Authorization header spelling
```

### Issue: "404 Not Found"
```
Solution:
- Check URL spelling
- Make sure to replace {CHALLENGE_ID} with actual ID
- Make sure to replace {SUBMISSION_ID} with actual ID
```

### Issue: "400 Bad Request"
```
Solution:
- Check JSON formatting (use JSON validator)
- Check all required fields present
- Check field names spelling and case
```

### Issue: "AI Analysis taking too long"
```
Solution:
- AI processing takes 3-10 seconds
- Wait patiently, don't send multiple requests
- Check service logs if still not responding
```

### Issue: "Tables still empty after grading"
```
Solution:
- Make sure grading is complete (status = "Graded", not "Submitted")
- Check if skill points were awarded in response
- Wait 5 seconds for database to sync
- Query database again
```

---

## 📞 Support

If you encounter issues:

1. Check service is online:
   ```
   https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/health
   https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/health
   ```

2. Check logs in Azure Container Apps

3. Verify database connectivity

4. Contact: Check deployment status in Azure Portal

---

**Hướng dẫn này sẽ giúp bạn test toàn bộ hoạt động của Challenge Service**  
*This guide will help you test the complete functionality of Challenge Service*

**Ready to test? Start with STEP 1!** ✅
