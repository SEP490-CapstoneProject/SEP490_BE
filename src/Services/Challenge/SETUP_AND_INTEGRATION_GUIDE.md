# AI Challenge System - Setup & Integration Guide

## Quick Start for Development

### 1. Database Setup

Add connection string to `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SkillSnapChallenge;Trusted_Connection=true;MultipleActiveResultSets=true;"
  },
  "Gemini": {
    "ApiKey": "your-key-here"
  }
}
```

### 1.1 Azure Key Vault for Production Secrets

For production, keep auth and AI secrets in Azure Key Vault and bootstrap it the same way as the other services:

```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://<your-keyvault-name>.vault.azure.net/"
    }
  }
}
```

Recommended secret names:
- `JwtSettings--SecretKey`
- `JwtSettings--Issuer`
- `JwtSettings--Audience`
- `Gemini--ApiKey`
- `Gemini--Model`

### 2. Create Database

```bash
cd src/Services/Challenge

# Create migration
dotnet ef migrations add InitialSchema --startup-project Challenge.API

# Apply migration
dotnet ef database update --startup-project Challenge.API
```

### 3. Register Services in Program.cs

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault();

// Add Challenge API
builder.Services.AddChallengeApi(builder.Configuration);

// Add other services
builder.Services.AddControllers();
builder.Services.AddAuthentication();

var app = builder.Build();

// Migrate database
await app.MigrateDatabaseAsync();

// Configure middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseChallengeApi();

app.Run();
```

### 4. Testing Endpoints

**Create a Challenge:**
```bash
curl -X POST http://localhost:5000/api/challenges \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "title": "Build SignalR Chat App",
    "description": "Create a real-time chat application using SignalR",
    "expectedSolution": "GitHub repo link...",
    "difficultyScore": 8,
    "deadline": "2026-12-31T23:59:59Z"
  }'
```

**Submit for Review:**
```bash
curl -X POST http://localhost:5000/api/challenges/{id}/submit-review \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Approve Challenge (Admin):**
```bash
curl -X POST http://localhost:5000/api/challenges/{id}/approve \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -d '{}' # Will create immutable ChallengeVersion snapshot
```

## Integration with Existing Services

### 1. Portfolio Service Integration

The Challenge Service exposes verified skills via the Portfolio API:

```csharp
// In Portfolio Service code
using (var client = new HttpClient())
{
    var response = await client.GetAsync(
        "https://challenge-service/api/portfolio/verified-skills?userId={userId}");
    
    var verifiedSkills = await response.Content.ReadAsAsync<List<VerifiedSkillDto>>();
    
    // Display in portfolio alongside manual skills
}
```

### 2. Event Bus Integration

When a challenge is approved or graded, publish events:

```csharp
// In ChallengeController.ApproveChallenge
var @event = new ChallengeApprovedEvent
{
    ChallengeId = challenge.Id,
    VersionId = version.Id,
    Title = challenge.Title,
    PublishedAt = DateTime.UtcNow
};

await _eventBus.PublishAsync(@event);
```

### 3. Real-time Notifications

When submission is graded, notify user:

```csharp
// In SubmissionGradingOrchestrator
var notification = new NotificationEvent
{
    UserId = submission.UserId,
    Type = "SubmissionGraded",
    Title = "Your challenge was graded!",
    Content = $"You earned {totalPoints} points"
};

await _notificationService.SendAsync(notification);
```

## Architecture Decisions

### Why Immutable Snapshots?
- **Consistency**: All submissions for a challenge grade against same criteria
- **Auditability**: Can see exactly what was asked when submission was made
- **Flexibility**: Challenge can be edited for future submissions
- **Version Control**: Multiple versions if AI model upgrades

### Why Four-Phase Skill Normalization?
- **Exact Match**: Most common case (fast)
- **Alias Lookup**: Handle "JavaScript" → "JS", "TypeScript" → "TS"
- **Fuzzy Match**: Catch typos and variations
- **Pending Skill**: Discover new skills through AI analysis

### Why Anti-Farming Multipliers?
```
1st attempt:  100% points (encouraged exploration)
2nd attempt:  50% points  (discourage grinding)
3rd attempt:  25% points
4th+ attempt: 10% points  (minimal reward)
```

Also enforces category diversity:
- Same category 3+ times this month: -20% penalty
- New category bonus: +10% bonus

## Database Schema

### Key Relationships

```
Challenge (1) ---> (N) ChallengeVersion
    ↓
    +---> (N) ChallengeSubmission
             ↓
             +---> (1) ChallengeVersion (immutable reference)

User --(many-to-many)--> Skill
         through UserSkill
             ↓
         tracks TotalPoints, VerificationLevel

ChallengeSubmission ----> SubmissionCriteriaScore ----> EvaluationCriteria
```

### Indexing Strategy

```sql
-- Fast challenge queries
CREATE INDEX IX_Challenges_Status ON CHALLENGES(Status)
CREATE INDEX IX_Challenges_CreatedById ON CHALLENGES(CreatedById)
CREATE INDEX IX_Challenges_Deadline ON CHALLENGES(Deadline)

-- Fast user skill queries
CREATE UNIQUE INDEX IX_UserSkills_UserSkill ON USER_SKILLS(UserId, SkillId)
CREATE INDEX IX_UserSkills_VerificationLevel ON USER_SKILLS(UserId, VerificationLevel)

-- Fast submission tracking
CREATE INDEX IX_Submissions_UserChallenge ON CHALLENGE_SUBMISSIONS(UserId, ChallengeId)
CREATE INDEX IX_Submissions_Status ON CHALLENGE_SUBMISSIONS(Status)

-- Fast point audit
CREATE INDEX IX_Transactions_UserSkill ON SKILL_POINT_TRANSACTIONS(UserId, SkillId)
```

## Authorization Model

### Role-Based Access

```csharp
[Authorize(Roles = "Admin")]
[HttpPost("{id}/approve")]
public async Task<IActionResult> ApproveChallenge(Guid id)
{
    // Only admins can approve challenges
}

[Authorize]
[HttpPost]
public async Task<IActionResult> CreateChallenge(CreateChallengeDto dto)
{
    // Any authenticated user can create challenges
}
```

### Challenge Ownership

```csharp
var userId = Guid.Parse(User.FindFirst("sub").Value);

// Can only edit own drafts
if (challenge.CreatedById != userId && !User.IsInRole("Admin"))
    return Forbid();
```

## Monitoring & Logging

### Key Metrics to Track

```csharp
// Challenge metrics
_logger.LogInformation(
    "Challenge created: {Title}, Difficulty={Difficulty}",
    title, difficultyScore);

// Submission metrics
_logger.LogInformation(
    "Submission graded: UserId={UserId}, Challenge={ChallengeId}, Score={Score}",
    userId, challengeId, overallScore);

// Point metrics
_logger.LogInformation(
    "Points awarded: UserId={UserId}, Skill={SkillName}, Points={Points}",
    userId, skillName, points);
```

### Alert Thresholds

```csharp
// Alert if grading takes too long
if (stopwatch.Elapsed > TimeSpan.FromSeconds(30))
{
    _logger.LogWarning("Slow grading detected: {SubmissionId}, Duration={Duration}ms",
        submissionId, stopwatch.ElapsedMilliseconds);
}

// Alert if many submissions fail
if (failedCount > 10)
{
    _logger.LogError("High submission failure rate: {FailedCount} failures",
        failedCount);
}
```

## Performance Optimization

### Query Optimization

Bad:
```csharp
// N+1 query problem
var challenges = await _challengeRepo.GetAllAsync();
foreach (var challenge in challenges)
{
    var version = await _versionRepo.GetLatestByChallengeAsync(challenge.Id);
}
```

Good:
```csharp
// Include related data
var challenges = await _context.Challenges
    .Include(c => c.CurrentVersion)
    .ToListAsync();
```

### Caching Strategy

```csharp
// Cache skill list (rarely changes)
var skills = await _cache.GetOrCreateAsync("skills:all",
    async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        return await _skillRepo.GetAllAsync();
    });

// Cache user skills (more volatile)
var userSkills = await _cache.GetOrCreateAsync($"user:skills:{userId}",
    async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await _userSkillRepo.GetByUserAsync(userId);
    });
```

## Deployment Checklist

### Pre-Deployment
- [ ] Create database migration
- [ ] Test all API endpoints
- [ ] Verify Gemini API connection
- [ ] Add [Authorize] attributes
- [ ] Set up SSL certificates
- [ ] Configure Azure KeyVault secrets

### Deployment
- [ ] Build Docker image
- [ ] Push to ACR
- [ ] Deploy to Azure Container Apps
- [ ] Run database migrations
- [ ] Verify endpoints are responding
- [ ] Check logs for errors

### Post-Deployment
- [ ] Test production endpoints
- [ ] Monitor error rates
- [ ] Verify database connectivity
- [ ] Check API response times
- [ ] Set up alerts

## Troubleshooting

### Challenge Not Appearing After Approval

Check:
```sql
-- Verify challenge exists
SELECT * FROM CHALLENGES WHERE Id = @id

-- Verify version was created
SELECT * FROM CHALLENGE_VERSIONS WHERE ChallengeId = @id

-- Verify status changed
SELECT Status, CurrentVersionId, PublishedAt FROM CHALLENGES WHERE Id = @id
```

### Submission Not Getting Points

Check:
```sql
-- Verify submission exists
SELECT * FROM CHALLENGE_SUBMISSIONS WHERE Id = @id

-- Verify points were calculated
SELECT * FROM SKILL_POINT_TRANSACTIONS WHERE SourceId = @id

-- Verify user skill was updated
SELECT * FROM USER_SKILLS WHERE UserId = @userId AND SkillId = @skillId
```

### Gemini API Errors

Check:
```csharp
// Verify API key is configured
var apiKey = configuration["Gemini:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
    throw new InvalidOperationException("Gemini API key not configured");

// Check logs for HTTP errors
_logger.LogError("Gemini API error: {StatusCode} {Message}", 
    response.StatusCode, response.Content);
```

## Next Steps

1. **Implement Database Migrations**
   - Generate initial schema migration
   - Add seed data for test skills/categories

2. **Connect to Gemini API**
   - Move from mock responses to real API calls
   - Implement retry logic and circuit breaker

3. **Add Authorization**
   - Wire up JWT validation
   - Implement role-based access
   - Add resource-level ownership checks

4. **Create Test Suite**
   - Unit tests for repositories (in-memory EF)
   - Integration tests for services
   - API contract tests

5. **Set Up CI/CD**
   - GitHub Actions workflow
   - Automated testing on PR
   - Docker build and push
   - Deploy to staging/prod

## Support

For questions or issues:
1. Check the logs in the container
2. Review the API documentation at `/swagger`
3. Verify database connectivity
4. Check Gemini API status
