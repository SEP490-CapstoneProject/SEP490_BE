# PORTFOLIO & JOB MATCHING TESTING & LOGIC GUIDE

## 1. API ENDPOINT SPECIFICATIONS

### 1.1 Portfolio Matching API
**Endpoint:** GET /api/portfolio/{portfolioId}/match-jobs

**Path Parameters:**
- portfolioId: int (required) - The portfolio ID to find matching jobs for

**Query Parameters:**
- page: int (default: 1) - Page number for pagination
- pageSize: int (default: 20) - Number of results per page

**Authentication:** [Authorize] - Requires JWT Bearer token

**cURL Example:**
\\\ash
curl -X GET "https://portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/portfolio/123/match-jobs?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
\\\

**Postman Setup:**
1. New Request → GET
2. URL: {{portfolio-service-url}}/api/portfolio/{{portfolioId}}/match-jobs
3. Params tab:
   - Key: page, Value: 1
   - Key: pageSize, Value: 20
4. Headers tab:
   - Authorization: Bearer {{jwt-token}}

**Success Response (200 OK):**
\\\json
{
  "items": [
    {
      "jobPostId": 456,
      "title": "Senior C# Developer",
      "companyName": "Tech Corp",
      "position": "Senior Developer",
      "employmentType": "Full-time",
      "location": "Ho Chi Minh City",
      "cosineScore": 0.876,
      "skillScore": 0.92,
      "categoryScore": 1.0,
      "finalScore": 0.8692,
      "matchReasons": [
        "Strong C# expertise match",
        "Leadership experience matches requirements",
        "Location aligned"
      ]
    },
    {
      "jobPostId": 457,
      "title": "Full-stack Developer",
      "companyName": "StartUp Inc",
      "position": "Developer",
      "employmentType": "Full-time",
      "location": "Hanoi",
      "cosineScore": 0.654,
      "skillScore": 0.78,
      "categoryScore": 0.5,
      "finalScore": 0.6452,
      "matchReasons": [
        "JavaScript experience matches",
        "Database knowledge aligned"
      ]
    }
  ],
  "total": 156,
  "page": 1,
  "pageSize": 20
}
\\\

### 1.2 Company Post Matching API
**Endpoint:** GET /api/company-posts/{jobPostId}/match-portfolios

**Path Parameters:**
- jobPostId: int (required) - The job post ID to find matching portfolios for

**Query Parameters:**
- page: int (default: 1) - Page number for pagination
- pageSize: int (default: 20) - Number of results per page

**Authentication:** [Authorize] - Requires JWT Bearer token

**cURL Example:**
\\\ash
curl -X GET "https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/company-posts/456/match-portfolios?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
\\\

**Success Response (200 OK):**
\\\json
{
  "items": [
    {
      "portfolioId": 123,
      "firstName": "John",
      "lastName": "Doe",
      "title": "Senior Software Engineer",
      "description": "Experienced full-stack developer with 8 years of experience...",
      "skills": ["C#", "ASP.NET Core", "SQL Server", "Azure", "React"],
      "cosineScore": 0.912,
      "skillScore": 0.95,
      "categoryScore": 1.0,
      "finalScore": 0.9052,
      "matchReasons": [
        "Excellent match on required C# skills",
        "Azure experience aligns with tech stack",
        "Leadership background matches role requirements"
      ]
    }
  ],
  "total": 89,
  "page": 1,
  "pageSize": 20
}
\\\

---

## 2. EMBEDDING IMPLEMENTATION DETAILS

### 2.1 How Embeddings Work

**Purpose:** Convert text (portfolio/job description) into numerical vectors for semantic matching

**Model Used:** Google AI Embedding (embedding-001)
- Output: 768-dimensional float vector
- Represents semantic meaning of text
- Similar concepts have similar vectors

### 2.2 Portfolio Embedding Generation

**Fields Combined:**
1. **Title** - portfolio.Name (e.g., "Senior Full-Stack Developer")
2. **Description** - All portfolio blocks concatenated
3. **Skills** - From BlockTypeId == 2 (skill blocks)
4. **Projects** - From BlockTypeId == 6 (project descriptions)
5. **CustomFields** - Top 5 blocks data

**Process:**
\\\
Portfolio Created/Updated Event
    ↓
RabbitMQ: "portfolio.changed"
    ↓
PortfolioEmbeddingConsumer.Handle()
    ↓
TextNormalizer.BuildPortfolioText(portfolio)
    ↓
Combined Text: "Senior Full-Stack Developer. 8 years experience in...skills...projects...education..."
    ↓
GoogleAiEmbeddingService.CreateEmbeddingAsync(combinedText)
    ↓
Returns: float[768] vector
    ↓
Stored in Database with embedding_status = "Ready"
\\\

**SQL to Check Portfolio Embedding Status:**
\\\sql
SELECT TOP 10 
    p.Id,
    p.Name,
    p.Description,
    e.Embedding_Status,
    e.CreatedAt
FROM PORTFOLIO p
LEFT JOIN PORTFOLIO_EMBEDDINGS e ON p.Id = e.PortfolioId
WHERE e.Embedding_Status = 'Ready'
ORDER BY e.CreatedAt DESC;
\\\

### 2.3 Company Post Embedding Generation

**Fields Combined:**
1. **Title** - JobDescription.Position (e.g., "Senior C# Developer")
2. **Description** - JobDescription text
3. **Skills** - RequirementsMandatory + RequirementsPreferred
4. **Categories** - EmploymentType + Address/Location
5. **CustomFields** - Benefits + Salary range

**Process:** Similar to portfolio, triggered by "company.post.changed" event

**SQL to Check Job Post Embedding Status:**
\\\sql
SELECT TOP 10
    j.Id,
    j.Position,
    e.Embedding_Status,
    e.CreatedAt,
    e.Error_Message
FROM JOB_POST j
LEFT JOIN JOB_EMBEDDINGS e ON j.Id = e.JobPostId
WHERE e.Embedding_Status IN ('Ready', 'Failed', 'Pending')
ORDER BY e.CreatedAt DESC;
\\\

### 2.4 Embedding Status States

| Status | Meaning | Usable for Matching |
|--------|---------|-------------------|
| **Ready** | Successfully created and stored | ✅ YES |
| **Pending** | Waiting to be created (queued) | ❌ NO |
| **Failed** | Creation failed (API error, quota) | ❌ NO |

**Debug Pending Embeddings:**
\\\sql
-- Count portfolios not ready for matching
SELECT 
    embedding_status,
    COUNT(*) as count
FROM PORTFOLIO_EMBEDDINGS
GROUP BY embedding_status;

-- Find portfolios that failed
SELECT 
    p.Id,
    p.Name,
    e.Embedding_Status,
    e.Error_Message,
    e.Retry_Count
FROM PORTFOLIO p
LEFT JOIN PORTFOLIO_EMBEDDINGS e ON p.Id = e.PortfolioId
WHERE e.Embedding_Status = 'Failed'
ORDER BY e.UpdatedAt DESC;
\\\

---

## 3. MATCHING ALGORITHM DEEP DIVE

### 3.1 Step-by-Step Algorithm (MatchingEngine.cs)

**Input:**
- Source portfolio/job post with embedding_status = "Ready"
- Target list of job posts/portfolios to match against
- MatchingOptions (PreFilterTake, MinimumFinalScore, MinimumDescriptionLength)

**Step 1: Quality Candidate Filtering**
\\\
FOR EACH candidate in candidates:
  IF candidate.embedding_status != "Ready":
    SKIP (can't match without embedding)
  IF candidate.skills.count < 1:
    SKIP (must have at least 1 skill)
  IF candidate.description.length < MinimumDescriptionLength (default: 50):
    SKIP (description too short)
  ADD to qualified_candidates
\\\

**Example:** Portfolio must have embedding ready + at least 1 skill + 50+ char description

**Step 2: Pre-filter by Skill Overlap**
\\\
FOR EACH qualified_candidate:
  matching_skills = COUNT(skills in BOTH source AND candidate)
  candidate.skill_overlap = matching_skills

SORT qualified_candidates BY skill_overlap DESC
TAKE TOP PreFilterTake candidates (default: 1000)
\\\

**Example:** If searching 50,000 portfolios for a job, reduce to top 1000 by skill match first

**Step 3: Compute Scores for Each Candidate**

**3a. Cosine Similarity Score:**
\\\
source_vector = source_portfolio.embedding  // float[768]
candidate_vector = candidate_portfolio.embedding  // float[768]

cosine_score = CosineSimilarity(source_vector, candidate_vector)
// Returns: 0.0 to 1.0 (1.0 = perfect match)
\\\

**Code Reference:** ScoringAndSimilarity.cs, CosineSimilarityService.CosineSimilaritySafe()

**3b. Skill Score:**
\\\
required_skills = source.required_skills
candidate_skills = candidate.skills

matching_skills = COUNT(skill in required_skills AND skill in candidate_skills)
skill_score = matching_skills / required_skills.count
// Returns: 0.0 to 1.0 (case-insensitive match)
\\\

**Example:**
- Required: ["C#", "ASP.NET", "SQL", "Azure"]
- Candidate has: ["C#", "SQL", "React"]
- skill_score = 2/4 = 0.5

**3c. Category Score:**
\\\
required_categories = source.categories
candidate_categories = candidate.categories

IF required_categories.count == 1:
  category_score = (category_value in candidate_categories) ? 1.0 : 0.0
ELSE:
  matching_categories = COUNT(cat in required_categories AND cat in candidate_categories)
  category_score = matching_categories / required_categories.count
// Returns: 0.0 to 1.0
\\\

**Example:**
- Job requires: ["Full-time", "Ho Chi Minh City"]
- Candidate has: ["Full-time", "Hanoi"]
- category_score = 1/2 = 0.5 (matches employment type, not location)

**3d. Final Score Calculation:**
\\\
FinalScore = (Cosine * 0.7) + (SkillScore * 0.2) + (CategoryScore * 0.1)
\\\

**Example Calculation:**
- Cosine: 0.85 × 0.7 = 0.595
- Skill: 0.80 × 0.2 = 0.160
- Category: 1.0 × 0.1 = 0.100
- **FinalScore = 0.855**

**Code Reference:** ScoringHelper.cs, ComputeFinalScore()

**Step 4: Threshold Filtering**
\\\
FILTER candidates WHERE FinalScore >= MinimumFinalScore (configurable)
\\\

**Step 5: Sort Results**
\\\
SORT BY:
  1. FinalScore DESC (highest matches first)
  2. UpdatedAt DESC (most recent)
  3. Id ASC (tie-breaker)
\\\

**Step 6: Apply Pagination**
\\\
RETURN page*pageSize + pageSize results
\\\

### 3.2 Cosine Similarity Math Explained

**Formula:**
\\\
cosine(A, B) = (A · B) / (||A|| × ||B||)

Where:
- A · B = dot product (sum of element-wise multiplication)
- ||A|| = norm/magnitude of vector A
- ||B|| = norm/magnitude of vector B
\\\

**Example with 3D vectors (simplified):**
\\\
Vector A = [1, 0, 1]  (portfolio skills)
Vector B = [1, 1, 0]  (job requirements)

A · B = (1×1) + (0×1) + (1×0) = 1

||A|| = sqrt(1² + 0² + 1²) = sqrt(2) ≈ 1.414
||B|| = sqrt(1² + 1² + 0²) = sqrt(2) ≈ 1.414

cosine(A, B) = 1 / (1.414 × 1.414) ≈ 0.5
\\\

**Interpretation:**
- 1.0 = perfect match (vectors point same direction)
- 0.5 = moderate match (perpendicular-ish)
- 0.0 = no match (orthogonal vectors)

**Code Reference:** ScoringAndSimilarity.cs, CosineSimilarityService.cs

---

## 4. TESTING & DEBUGGING

### 4.1 Local Test Setup

**Create Test Portfolio:**
\\\sql
INSERT INTO PORTFOLIO (UserId, Name, Description, CreatedAt)
VALUES (
  'user-123',
  'Senior C# Developer',
  'Experienced full-stack developer with 8 years in C#, ASP.NET Core, and Azure. Led teams of 5+ developers.',
  GETUTCDATE()
);

-- Get the inserted portfolio ID
SELECT TOP 1 Id FROM PORTFOLIO ORDER BY Id DESC;
\\\

**Create Test Job Post:**
\\\sql
INSERT INTO JOB_POST (CompanyId, Position, JobDescription, CreatedAt)
VALUES (
  'company-456',
  'Senior C# Developer',
  'Looking for experienced C# developer with ASP.NET Core, SQL Server, and Azure knowledge. Must have 5+ years experience.',
  GETUTCDATE()
);

-- Get the inserted job post ID
SELECT TOP 1 Id FROM JOB_POST ORDER BY Id DESC;
\\\

**Wait for Embeddings to Generate:**
\\\
- Embeddings generated asynchronously via RabbitMQ
- Check status with:
\\\

\\\sql
-- Check portfolio embedding
SELECT embedding_status, error_message, retry_count
FROM PORTFOLIO_EMBEDDINGS
WHERE portfolio_id = YOUR_PORTFOLIO_ID;

-- Check job post embedding  
SELECT embedding_status, error_message, retry_count
FROM JOB_EMBEDDINGS
WHERE job_post_id = YOUR_JOB_POST_ID;

-- Wait until both show 'Ready'
\\\

**Call Matching API Once Ready:**
\\\ash
curl -X GET "http://localhost:5001/api/portfolio/YOUR_PORTFOLIO_ID/match-jobs?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
\\\

### 4.2 Common Debugging Scenarios

**Issue: No matching results returned**

**Debugging Steps:**
1. Verify portfolio exists and has ID
2. Check embedding status:
   \\\sql
   SELECT embedding_status FROM PORTFOLIO_EMBEDDINGS 
   WHERE portfolio_id = ?;
   \\\
3. If NOT 'Ready', wait or check error:
   \\\sql
   SELECT error_message FROM PORTFOLIO_EMBEDDINGS 
   WHERE portfolio_id = ?;
   \\\
4. Check if any jobs exist in database:
   \\\sql
   SELECT COUNT(*) FROM JOB_POST WHERE embedding_status = 'Ready';
   \\\
5. Check minimum score threshold isn't too high

**Issue: Low matching scores despite similar content**

**Root Causes:**
1. Embeddings not capturing semantic similarity well
2. Skill scores too low (exact match required, no fuzzy matching)
3. Category score mismatch (location/employment type differ)

**Diagnosis Query:**
\\\sql
-- Get scores breakdown for specific portfolio
EXEC sp_GetPortfolioMatches 
  @PortfolioId = 123,
  @TopN = 10;
  
-- Shows FinalScore breakdown (Cosine, SkillScore, CategoryScore)
\\\

**Issue: Slow query performance**

**Optimization:**
1. Add index on embedding_status:
   \\\sql
   CREATE INDEX IX_PortfolioEmbeddings_Status 
   ON PORTFOLIO_EMBEDDINGS(embedding_status);
   \\\
2. Reduce PreFilterTake limit in MatchingOptions
3. Increase MinimumFinalScore threshold to filter fewer results

### 4.3 Modifying Weights & Thresholds

**Location:** ScoringHelper.cs, ComputeFinalScore() method

**Current Weights:**
\\\csharp
const float CosineWeight = 0.7f;        // 70% semantic similarity
const float SkillWeight = 0.2f;         // 20% exact skill match
const float CategoryWeight = 0.1f;      // 10% category overlap
\\\

**To Increase Emphasis on Skills:**
\\\csharp
// Before:
const float CosineWeight = 0.7f;
const float SkillWeight = 0.2f;
const float CategoryWeight = 0.1f;

// After (more strict on skills):
const float CosineWeight = 0.5f;
const float SkillWeight = 0.35f;
const float CategoryWeight = 0.15f;

// Update the formula:
return (cosine * CosineWeight) + (skillScore * SkillWeight) + (categoryScore * CategoryWeight);
\\\

**Testing Changes:**
1. Update weights locally
2. Run unit tests: ScoringHelperTests.cs
3. Test with sample data
4. Compare old vs new scores
5. Deploy to staging for A/B testing

---

## 5. LOGIC ENHANCEMENT PATTERNS

### 5.1 Safe Enhancement Process

**Step 1: Create Feature Branch**
\\\ash
git checkout -b feature/enhanced-matching-scores
\\\

**Step 2: Modify Scoring Logic**
\\\csharp
// File: ScoringAndSimilarity.cs
public static class ScoringHelper
{
  public static float ComputeFinalScore(
    float cosine,
    float skillScore,
    float categoryScore,
    MatchingContext context = null)
  {
    // NEW: Add configurable weights
    float cosineWeight = context?.CosineWeight ?? 0.7f;
    float skillWeight = context?.SkillWeight ?? 0.2f;
    float categoryWeight = context?.CategoryWeight ?? 0.1f;
    
    return (cosine * cosineWeight) + 
           (skillScore * skillWeight) + 
           (categoryScore * categoryWeight);
  }
}
\\\

**Step 3: Write Unit Tests**
\\\csharp
[Test]
public void ComputeFinalScore_WithCustomWeights_ReturnsCorrectScore()
{
  var context = new MatchingContext 
  {
    CosineWeight = 0.5f,
    SkillWeight = 0.35f,
    CategoryWeight = 0.15f
  };
  
  float score = ScoringHelper.ComputeFinalScore(
    cosine: 0.8f,
    skillScore: 0.9f,
    categoryScore: 1.0f,
    context: context);
  
  float expected = (0.8f * 0.5f) + (0.9f * 0.35f) + (1.0f * 0.15f);
  Assert.AreEqual(expected, score, 0.0001f);
}
\\\

**Step 4: Test with Real Data**
\\\sql
-- Create test portfolios with known scores
DECLARE @OldCosineWeight FLOAT = 0.7;
DECLARE @NewCosineWeight FLOAT = 0.5;

SELECT TOP 100
  p.Id,
  (@OldCosineWeight * 0.8) + (0.2 * 0.9) + (0.1 * 1.0) AS OldScore,
  (@NewCosineWeight * 0.8) + (0.35 * 0.9) + (0.15 * 1.0) AS NewScore
FROM PORTFOLIO p
WHERE EMBEDDING_STATUS = 'Ready'
ORDER BY NewScore DESC;
\\\

**Step 5: Staging Deployment**
1. Deploy to staging environment
2. Run matching queries with new weights
3. Compare results vs production
4. Get stakeholder approval
5. Monitor error rates

**Step 6: Production Deployment**
1. Create feature flag for new weights
2. Deploy with feature flag OFF
3. Gradually enable for 10% → 50% → 100% of requests
4. Monitor scores and error rates
5. Keep rollback ready (disable feature flag)

### 5.2 Example Enhancement: Multi-Factor Scoring

**Current Formula:**
\\\
FinalScore = (Cosine * 0.7) + (Skill * 0.2) + (Category * 0.1)
\\\

**Enhanced Formula (Add Experience Match):**
\\\
ExperienceScore = CalculateExperienceMatch(portfolio.YearsExp, job.RequiredYearsExp)

FinalScore = (Cosine * 0.6) + (Skill * 0.2) + (Category * 0.1) + (Experience * 0.1)
\\\

**Implementation:**
\\\csharp
private static float CalculateExperienceMatch(int portfolioYears, int requiredYears)
{
  if (portfolioYears >= requiredYears)
    return 1.0f;  // Fully qualified
  
  if (portfolioYears >= requiredYears * 0.8f)
    return 0.9f;  // Mostly qualified
    
  if (portfolioYears >= requiredYears * 0.5f)
    return 0.7f;  // Partially qualified
    
  return 0.5f;    // Below target, but considered
}

public static float ComputeFinalScoreWithExperience(
  float cosine,
  float skillScore,
  float categoryScore,
  float experienceScore)
{
  return (cosine * 0.6f) + (skillScore * 0.2f) + 
         (categoryScore * 0.1f) + (experienceScore * 0.1f);
}
\\\

---

## 6. TROUBLESHOOTING REFERENCE

| Problem | Cause | Solution |
|---------|-------|----------|
| All candidates return 0 scores | Embedding missing/invalid | Check embedding_status = 'Ready' |
| Scores very similar across results | Poor embedding quality | Re-generate embeddings, check text normalization |
| Unexpected candidates ranked high | Weight imbalance | Review and adjust scoring weights |
| No results for obvious matches | Threshold too high | Reduce MinimumFinalScore config |
| Query timeout on large datasets | Pre-filter limit too high | Increase PreFilterTake or add database index |
| Skill scores always 0 or 1 | Case sensitivity in matching | Check ScoringHelper skill normalization |

---

## 7. CONFIGURATION REFERENCE

**appsettings.json:**
\\\json
{
  "MatchingOptions": {
    "PreFilterTake": 1000,
    "MinimumFinalScore": 0.5,
    "MinimumDescriptionLength": 50,
    "EnableSemanticMatching": true
  },
  "EmbeddingService": {
    "Model": "embedding-001",
    "MaxRetries": 3,
    "RetryDelayMs": 1000
  }
}
\\\

**Database Indexes (Performance):**
\\\sql
-- Speed up embedding status checks
CREATE INDEX IX_PortfolioEmbeddings_Status 
ON PORTFOLIO_EMBEDDINGS(embedding_status) 
INCLUDE (portfolio_id);

-- Speed up skill lookups
CREATE INDEX IX_Skills_Portfolio 
ON PORTFOLIO_SKILLS(portfolio_id) 
INCLUDE (skill_name);

-- Speed up matching queries
CREATE INDEX IX_JOB_Embedding_Status
ON JOB_POST_EMBEDDINGS(embedding_status)
INCLUDE (job_post_id);
\\\

---

## 8. QUICK START TESTING CHECKLIST

- [ ] Portfolio API endpoint working (GET /api/portfolio/{id}/match-jobs)
- [ ] Job API endpoint working (GET /api/company-posts/{id}/match-portfolios)
- [ ] JWT authentication required and validated
- [ ] Pagination working (page, pageSize parameters)
- [ ] Embedding generation triggers on create/update
- [ ] Embeddings reach 'Ready' status within 30 seconds
- [ ] Matching only includes 'Ready' embeddings
- [ ] Scores between 0.0 and 1.0
- [ ] Results sorted by FinalScore DESC
- [ ] MinimumFinalScore threshold respected
- [ ] No null/invalid values in response
- [ ] Database queries complete < 5 seconds for 1000+ candidates

---

**Last Updated:** Current Session  
**Service:** Portfolio & Company Post Matching with AI Embeddings  
**Status:** Ready for Testing & Enhancement
