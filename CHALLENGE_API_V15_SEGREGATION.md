# Challenge API Segregation - v15 Implementation

## Overview
Challenge Service v15 introduces segregated APIs for two user roles:
1. **Challenge Creators** - Manage challenges they create
2. **Participants** - Discover and solve published challenges

## API Endpoints

### Creator Management APIs
Base URL: `/api/creator/challenges`

#### 1. Get All Challenges Created by User
```
GET /api/creator/challenges?skip=0&take=20
```
**Description**: List all challenges created by the authenticated user (all statuses: Draft, PendingReview, Published)

**Response**:
```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "title": "Backend Challenge",
      "description": "A backend coding challenge",
      "status": "Published",
      "currentVersionId": "00000000-0000-0000-0000-000000000001",
      "createdAt": "2025-02-18T10:00:00Z",
      "updatedAt": "2025-02-18T11:00:00Z",
      "deadline": "2025-03-01T00:00:00Z"
    }
  ],
  "totalCount": 1,
  "skip": 0,
  "take": 20
}
```

#### 2. Get All Versions of a Challenge
```
GET /api/creator/challenges/{challengeId}/versions
```
**Description**: List all versions of a specific challenge with complete details (criteria, skill mappings)

**Response**:
```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "challengeId": "00000000-0000-0000-0000-000000000000",
    "versionNumber": 1,
    "title": "Backend Challenge v1",
    "description": "First version",
    "expectedSolution": "...",
    "difficultyScore": 7.5,
    "difficultyLabel": "Hard",
    "skillWeightMapping": "{\"ASP.NET Core\": 5, \"JWT Authentication\": 3}",
    "modelName": "Gemini 2.0",
    "promptVersion": "v1.0-standardized",
    "evaluatedAt": "2025-02-18T10:15:00Z",
    "isActive": true,
    "createdAt": "2025-02-18T10:15:00Z",
    "criteria": [
      {
        "id": "00000000-0000-0000-0000-000000000002",
        "versionId": "00000000-0000-0000-0000-000000000001",
        "name": "Code Quality",
        "description": "Evaluation of code quality",
        "weight": 0.5,
        "maxScore": 100,
        "displayOrder": 0
      }
    ],
    "skillMappings": [
      {
        "id": "00000000-0000-0000-0000-000000000003",
        "versionId": "00000000-0000-0000-0000-000000000001",
        "skillName": "ASP.NET Core",
        "weight": 5.0,
        "criteriaIds": ["00000000-0000-0000-0000-000000000002"]
      }
    ]
  }
]
```

#### 3. Set Active Version for Challenge
```
PUT /api/creator/challenges/{challengeId}/versions/{versionId}
```
**Description**: Change which version is the active/current version for a challenge. Only the creator can perform this action.

**Request Body**:
```json
{}
```

**Response**: Returns the full `ChallengeVersionDto` for the now-active version

### Participant Discovery APIs
Base URL: `/api/challenges/public`

#### 1. Get Published Challenges
```
GET /api/challenges/public?skip=0&take=20&search=&skillFilter=
```
**Description**: List all published challenges available for participants (only active versions shown)

**Query Parameters**:
- `skip` (optional): Pagination offset (default: 0)
- `take` (optional): Pagination limit (default: 20)
- `search` (optional): Search by title or description
- `skillFilter` (optional): Filter by skill name

**Response**:
```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "title": "Backend Challenge",
      "description": "A backend coding challenge",
      "difficultyScore": 7.5,
      "difficultyLabel": "Hard",
      "deadline": "2025-03-01T00:00:00Z",
      "publishedAt": "2025-02-18T12:00:00Z",
      "createdAt": "2025-02-18T10:00:00Z",
      "currentVersionId": "00000000-0000-0000-0000-000000000001",
      "activeVersion": {
        "id": "00000000-0000-0000-0000-000000000001",
        "versionNumber": 1,
        "difficultyScore": 7.5,
        "difficultyLabel": "Hard",
        "skillWeights": {
          "ASP.NET Core": 5,
          "JWT Authentication": 3
        },
        "criteria": [
          {
            "id": "00000000-0000-0000-0000-000000000002",
            "name": "Code Quality",
            "description": "Evaluation of code quality",
            "maxScore": 100,
            "displayOrder": 0
          }
        ],
        "createdAt": "2025-02-18T10:15:00Z"
      }
    }
  ],
  "totalCount": 5,
  "skip": 0,
  "take": 20
}
```

#### 2. Get Single Published Challenge
```
GET /api/challenges/public/{challengeId}
```
**Description**: Get details of a specific published challenge (active version only)

**Response**: Returns `PublicChallengeDto` (same structure as item in list above)

## Key Design Decisions

### 1. Data Filtering
- **Creator APIs**: Show all versions, all statuses, complete criteria and skill mapping details
- **Participant APIs**: Show only published challenges, only active versions, skill names and weights only

### 2. Authorization
- **Creator APIs**: Require authentication; only creator of challenge can access
- **Participant APIs**: Public/AllowAnonymous; no authentication required

### 3. Response DTOs
- Created separate DTOs to prevent accidental exposure of internal data:
  - `CreatorChallengeDto` - Challenge metadata for creator
  - `ChallengeVersionDto` - Full version details with all criteria and mappings
  - `PublicChallengeDto` - Safe public challenge view
  - `PublicVersionDto` - Safe public version view (skill names + weights only)

### 4. Version Switching
- Creators can set any version as active via `PUT /api/creator/challenges/{id}/versions/{versionId}`
- This updates the `CHALLENGES.CurrentVersionId` field
- Participants always see the active version

## API Usage Examples

### Example 1: Creator Lists Their Challenges
```bash
curl -X GET "https://api.example.com/api/creator/challenges?skip=0&take=10" \
  -H "Authorization: Bearer {token}"
```

### Example 2: Creator Views All Versions of a Challenge
```bash
curl -X GET "https://api.example.com/api/creator/challenges/{challengeId}/versions" \
  -H "Authorization: Bearer {token}"
```

### Example 3: Creator Activates a Different Version
```bash
curl -X PUT "https://api.example.com/api/creator/challenges/{challengeId}/versions/{versionId}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d "{}"
```

### Example 4: Participant Discovers Published Challenges
```bash
curl -X GET "https://api.example.com/api/challenges/public?skip=0&take=20&search=backend"
```

### Example 5: Participant Views Details of a Challenge
```bash
curl -X GET "https://api.example.com/api/challenges/public/{challengeId}"
```

## Data Persistence

### Database Changes
- No new tables added
- Added navigation property `Criteria` to `ChallengeCriteria` entity for EF Core relationship
- Updated `DbContext` with proper relationship configuration

### Backward Compatibility
- Existing `/api/challenges` endpoints remain unchanged
- New creator endpoints use `/api/creator/challenges` prefix
- New participant endpoints use `/api/challenges/public` prefix

## Deployment Notes

### For v15 Release:
1. Build: `dotnet build src/Services/Application/Application.sln -c Release`
2. Docker build: Include new version tag `v15`
3. Migration: No database migration needed (only EF relationship configuration)
4. Testing: See E2E test examples below

## E2E Testing

### Test 1: Creator Workflow
```powershell
# 1. Creator lists their challenges
$challenges = Invoke-RestMethod -Uri "https://api.example.com/api/creator/challenges" `
  -Headers @{ Authorization = "Bearer $creatorToken" }

# 2. Creator views versions
$versions = Invoke-RestMethod -Uri "https://api.example.com/api/creator/challenges/$challengeId/versions" `
  -Headers @{ Authorization = "Bearer $creatorToken" }

# 3. Creator activates a version
$result = Invoke-RestMethod -Uri "https://api.example.com/api/creator/challenges/$challengeId/versions/$versionId" `
  -Method PUT `
  -Headers @{ Authorization = "Bearer $creatorToken"; "Content-Type" = "application/json" } `
  -Body "{}"
```

### Test 2: Participant Workflow
```powershell
# 1. Participant discovers challenges
$challenges = Invoke-RestMethod -Uri "https://api.example.com/api/challenges/public?skip=0&take=20"

# 2. Participant views challenge details
$challenge = Invoke-RestMethod -Uri "https://api.example.com/api/challenges/public/$challengeId"

# 3. Participant submits solution (existing endpoint)
$submission = Invoke-RestMethod -Uri "https://api.example.com/api/submissions?challengeId=$challengeId" `
  -Method POST `
  -Headers @{ "Content-Type" = "application/json"; Authorization = "Bearer $participantToken" } `
  -Body $submissionBody
```

## Files Modified/Created

### Created:
- `Challenge.Application/DTOs/CreatorChallengeDto.cs` - New DTOs for segregated views
- `Challenge.API/Controllers/ChallengeCreatorController.cs` - Creator management endpoints
- `Challenge.API/Controllers/ChallengeDiscoveryController.cs` - Participant discovery endpoints

### Modified:
- `Challenge.Application/Interfaces/IChallengeService.cs` - Added new service methods
- `Challenge.Application/Services/ChallengeServiceImpl.cs` - Implemented new methods
- `Challenge.Domain/Entities/ChallengeCriteria.cs` - Added navigation property
- `Challenge.Infrastructure/Persistence/Repositories/ChallengeCriteriaRepository.cs` - Added `.Include()` for Criteria
- `Challenge.Infrastructure/Persistence/ChallengeDbContext.cs` - Added relationship configuration

## Summary

Challenge Service v15 successfully segregates APIs into:
- **Creator Management** (`/api/creator/challenges/*`) - Full control, all data visible
- **Participant Discovery** (`/api/challenges/public/*`) - Read-only, published+active only

This ensures clean separation of concerns and prevents accidental data leakage while maintaining backward compatibility.
