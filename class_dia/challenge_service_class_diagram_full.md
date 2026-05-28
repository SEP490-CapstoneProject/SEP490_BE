```mermaid
classDiagram
direction LR

%% Full Challenge Service Class Diagram (extracted from codebase)

%% Controllers
class ChallengeControllerV2 {
  -IChallengeService _challengeService
  -ILogger~ChallengeControllerV2~ _logger
  +CreateChallenge(request CreateChallengeDto) IActionResult
  +GetChallenge(id Guid) IActionResult
  +ListChallenges(skip int, take int, status string?, userId int?) IActionResult
  +UpdateChallenge(id Guid, request UpdateChallengeDto) IActionResult
  +DeleteChallenge(id Guid) IActionResult
  +SubmitForReview(id Guid) IActionResult
  +ApproveChallenge(id Guid) IActionResult
  +RejectChallenge(id Guid, request RejectChallengeDto) IActionResult
  -GetCurrentUserId() int?
  -IsAdmin() bool
}

class ChallengeCreatorController {
  -IChallengeService _challengeService
  -ISubmissionService _submissionService
  -ILogger~ChallengeCreatorController~ _logger
  +GetMyChallenges(skip int, take int) IActionResult
  +GetChallengeVersions(challengeId Guid) IActionResult
  +SetActiveVersion(challengeId Guid, versionId Guid) IActionResult
  +ApproveAndPublish(challengeId Guid) IActionResult
  +GetChallengeSubmissions(challengeId Guid, skip int, take int) IActionResult
}

class ChallengeDiscoveryController {
  -IChallengeService _challengeService
  -ILogger~ChallengeDiscoveryController~ _logger
  +GetPublishedChallenges(skip int, take int, search string?, skillFilter string?) IActionResult
  +GetPublicChallenge(id Guid) IActionResult
}

class ChallengeSubmissionController {
  -ISubmissionService _submissionService
  -ILogger~ChallengeSubmissionController~ _logger
  +GetMySubmissions(challengeId Guid, skip int, take int) IActionResult
}

%% Application Services & Helpers
class IChallengeService {
  <<interface>>
  +CreateChallengeAsync(request CreateChallengeDto, userId int) Task~ChallengeDto~
  +GetChallengeByIdAsync(id Guid, currentUserId int?) Task~ChallengeDto?~
  +ListChallengesAsync(pageSize int, cursor int?, currentUserId int?) Task~List~ChallengeDto~~
  +UpdateChallengeAsync(id Guid, request UpdateChallengeDto, userId int) Task~ChallengeDto~
  +DeleteChallengeAsync(id Guid, userId int) Task
  +SubmitForReviewAsync(id Guid, userId int) Task~ChallengeDto~
  +ApproveChallengeAsync(id Guid, adminId int) Task~ChallengeDto~
  +RejectChallengeAsync(id Guid, reason string, adminId int) Task~ChallengeDto~
  +GetChallengesPagedAsync(skip int, take int, status string?, userId int?) Task~Tuple~List~ChallengeDto~, int~~
  +GetCreatorChallengesAsync(userId int, skip int, take int) Task~Tuple~List~CreatorChallengeDto~, int~~
  +GetChallengeVersionsAsync(challengeId Guid, creatorUserId int) Task~List~ChallengeVersionDto~~
  +SetActiveVersionAsync(challengeId Guid, versionId Guid, creatorUserId int) Task~ChallengeVersionDto~
  +ApproveAndPublishAsync(challengeId Guid, creatorUserId int) Task~CreatorChallengeDto~
  +GetPublishedChallengesAsync(skip int, take int, searchTerm string?, skillFilter string?) Task~Tuple~List~PublicChallengeDto~, int~~
  +GetPublicChallengeByIdAsync(id Guid) Task~PublicChallengeDto?~
}

class ChallengeService {
  -IChallengeRepository _challengeRepository
  -IChallengeVersionRepository _versionRepository
  -ISkillRepository _skillRepository
  -IEvaluationCriteriaRepository _evaluationCriteriaRepository
  -IChallengeCriteriaRepository _challengeCriteriaRepository
  -ICriteriaSkillMappingRepository _criteriaSkillMappingRepository
  -IGeminiAIService _geminiAIService
  -IEventPublisher _eventPublisher
  -IActorResolverClient _actorResolverClient
  -ILogger~ChallengeService~ _logger
  +CreateChallengeAsync(request CreateChallengeDto, userId int) Task~ChallengeDto~
  +GetChallengeByIdAsync(id Guid, currentUserId int?) Task~ChallengeDto?~
  +ListChallengesAsync(pageSize int, cursor int?, currentUserId int?) Task~List~ChallengeDto~~
  +UpdateChallengeAsync(id Guid, request UpdateChallengeDto, userId int) Task~ChallengeDto~
  +DeleteChallengeAsync(id Guid, userId int) Task
  +SubmitForReviewAsync(id Guid, userId int) Task~ChallengeDto~
  +ApproveChallengeAsync(id Guid, adminId int) Task~ChallengeDto~
  +RejectChallengeAsync(id Guid, reason string, adminId int) Task~ChallengeDto~
  +GetChallengesPagedAsync(skip int, take int, status string?, userId int?) Task~Tuple~List~ChallengeDto~, int~~
  +GetCreatorChallengesAsync(userId int, skip int, take int) Task~Tuple~List~CreatorChallengeDto~, int~~
  +GetChallengeVersionsAsync(challengeId Guid, creatorUserId int) Task~List~ChallengeVersionDto~~
  +SetActiveVersionAsync(challengeId Guid, versionId Guid, creatorUserId int) Task~ChallengeVersionDto~
  +ApproveAndPublishAsync(challengeId Guid, creatorUserId int) Task~CreatorChallengeDto~
  +GetPublishedChallengesAsync(skip int, take int, searchTerm string?, skillFilter string?) Task~Tuple~List~PublicChallengeDto~, int~~
  +GetPublicChallengeByIdAsync(id Guid) Task~PublicChallengeDto?~
  -EnsureOwner(challenge Challenge, userId int) void
  -MapToDto(challenge Challenge) ChallengeDto
  -MapToPublicVersionDtoAsync(version ChallengeVersion) Task~PublicVersionDto~
  -ResolveOrCreateCriteriaAsync(criteriaName string) Task~EvaluationCriteria~
  -PersistCriteriaModelAsync(version ChallengeVersion, analysis ChallengeAnalysisResult) Task
}

class ISubmissionService {
  <<interface>>
  +SubmitSolutionAsync(challengeId Guid, request SubmitSolutionDto, userId int) Task~SubmissionDto~
  +GetSubmissionByIdAsync(id Guid, currentUserId int?) Task~SubmissionDto?~
  +GetUserSubmissionsAsync(userId int, challengeId Guid?) Task~List~SubmissionDto~~
  +GetChallengeSubmissionsAsync(challengeId Guid) Task~List~SubmissionDto~~
  +GradeSubmissionAsync(id Guid) Task~SubmissionDto~
  +GetSubmissionsPagedAsync(skip int, take int, status string?, userId int?) Task~Tuple~List~SubmissionDto~, int~~
  +GetChallengeSubmissionsWithUserInfoAsync(challengeId Guid, skip int, take int) Task~SubmissionListResponseDto~
  +GetUserSubmissionsForChallengeAsync(challengeId Guid, userId int, skip int, take int) Task~ParticipantSubmissionListResponseDto~
}

%% Repositories
class IChallengeRepository {
  <<interface>>
  +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~Challenge~
  +GetAllAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +GetByStatusAsync(status ChallengeStatus, cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +GetPublishedAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +GetExpiredAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +AddAsync(challenge Challenge, cancellationToken CancellationToken) Task
  +UpdateAsync(challenge Challenge, cancellationToken CancellationToken) Task
  +DeleteAsync(id Guid, cancellationToken CancellationToken) Task
  +ExistsAsync(id Guid, cancellationToken CancellationToken) Task~bool~
}

class ChallengeRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~Challenge~
  +GetAllAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +GetByStatusAsync(status ChallengeStatus, cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +GetPublishedAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +GetExpiredAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
  +AddAsync(challenge Challenge, cancellationToken CancellationToken) Task
  +UpdateAsync(challenge Challenge, cancellationToken CancellationToken) Task
  +DeleteAsync(id Guid, cancellationToken CancellationToken) Task
  +ExistsAsync(id Guid, cancellationToken CancellationToken) Task~bool~
}

class IChallengeVersionRepository {
  <<interface>>
  +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~ChallengeVersion~
  +GetActiveVersionAsync(challengeId Guid, cancellationToken CancellationToken) Task~ChallengeVersion~
  +GetVersionsByChallengeAsync(challengeId Guid, cancellationToken CancellationToken) Task~IEnumerable~ChallengeVersion~~
  +AddAsync(version ChallengeVersion, cancellationToken CancellationToken) Task
  +UpdateAsync(version ChallengeVersion, cancellationToken CancellationToken) Task
}

class ChallengeVersionRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~ChallengeVersion~
  +GetActiveVersionAsync(challengeId Guid, cancellationToken CancellationToken) Task~ChallengeVersion~
  +GetVersionsByChallengeAsync(challengeId Guid, cancellationToken CancellationToken) Task~IEnumerable~ChallengeVersion~~
  +AddAsync(version ChallengeVersion, cancellationToken CancellationToken) Task
  +UpdateAsync(version ChallengeVersion, cancellationToken CancellationToken) Task
}

class IChallengeCriteriaRepository {
  <<interface>>
  +GetByVersionAsync(challengeVersionId Guid, cancellationToken CancellationToken) Task~List~ChallengeCriteria~~
  +AddRangeAsync(challengeCriteria IEnumerable~ChallengeCriteria~, cancellationToken CancellationToken) Task
}

class ChallengeCriteriaRepository {
  -ChallengeDbContext _context
  +GetByVersionAsync(challengeVersionId Guid, cancellationToken CancellationToken) Task~List~ChallengeCriteria~~
  +AddRangeAsync(challengeCriteria IEnumerable~ChallengeCriteria~, cancellationToken CancellationToken) Task
}

%% Additional Repos (skills, mappings)
class ISkillRepository {
  <<interface>>
  +GetByIdAsync(id Guid) Task~Skill?
  +AddAsync(skill Skill) Task
}
class IEvaluationCriteriaRepository {
  <<interface>>
  +GetByNameAsync(name string) Task~EvaluationCriteria?
  +AddAsync(criteria EvaluationCriteria) Task
}
class ICriteriaSkillMappingRepository {
  <<interface>>
  +GetByCriteriaAsync(criteriaId Guid) Task~List~CriteriaSkillMapping~~
  +AddAsync(mapping CriteriaSkillMapping) Task
}

%% Domain Entities
class Challenge {
  +Guid Id
  +string Title
  +string Description
  +string ExpectedSolution
  +decimal DifficultyScore
  +string DifficultyLabel
  +ChallengeStatus Status
  +Guid? CurrentVersionId
  +int CreatedById
  +int? ReviewedById
  +string RejectionReason
  +DateTime Deadline
  +DateTime? PublishedAt
  +DateTime CreatedAt
  +DateTime UpdatedAt
}

class ChallengeVersion {
  +Guid Id
  +Guid ChallengeId
  +int VersionNumber
  +string Title
  +string Description
  +string ExpectedSolution
  +decimal DifficultyScore
  +string DifficultyLabel
  +string SkillWeightMapping
  +string ModelName
  +string PromptVersion
  +DateTime EvaluatedAt
  +bool IsActive
  +DateTime CreatedAt
}

class ChallengeCriteria {
  +Guid Id
  +Guid ChallengeVersionId
  +Guid CriteriaId
  +decimal Weight
  +DateTime VersionedAt
}

class ChallengeSubmission {
  +Guid Id
  +Guid ChallengeId
  +int UserId
  +string SubmissionContent
  +string GithubUrl
  +decimal OverallScore
  +string AiFeedback
  +SubmissionStatus Status
  +Guid VersionSnapshotId
  +Guid VersionId
  +int AttemptCount
  +DateTime CreatedAt
  +DateTime? GradedAt
  +DateTime UpdatedAt
}

class SubmissionCriteriaScore {
  +Guid Id
  +Guid SubmissionId
  +Guid CriteriaId
  +decimal Score
  +string Feedback
  +DateTime CreatedAt
}

class EvaluationCriteria {
  +Guid Id
  +string Name
  +string Description
  +DateTime CreatedAt
  +DateTime UpdatedAt
}

class Skill {
  +Guid Id
  +string Name
  +string? Description
  +Guid? CategoryId
}

class SkillAlias {
  +Guid Id
  +Guid SkillId
  +string Alias
}

class SkillCategory {
  +Guid Id
  +string Name
}

class PendingSkill {
  +Guid Id
  +string Name
  +SkillApprovalStatus Status
}

class CriteriaSkillMapping {
  +Guid Id
  +Guid CriteriaId
  +Guid SkillId
  +decimal Weight
}

class SkillPointTransaction {
  +Guid Id
  +int UserId
  +Guid SkillId
  +decimal Points
  +string SourceType
  +Guid SourceId
  +DateTime CreatedAt
}

class UserSkill {
  +Guid Id
  +int UserId
  +Guid SkillId
  +decimal TotalPoints
  +decimal MasteryScore
  +VerificationLevel VerificationLevel
  +int ChallengeCount
  +DateTime? LastVerifiedAt
}

class SkillRelationship {
  +Guid Id
  +Guid SourceSkillId
  +Guid TargetSkillId
  +SkillRelationType RelationType
}

class PromptSanitizationLog {
  +Guid Id
  +string OriginalPrompt
  +string SanitizedPrompt
  +DateTime CreatedAt
}

%% DTOs (summary)
class CreateChallengeDto {
  +string Title
  +string Description
  +string ExpectedSolution
  +DateTime Deadline
}
class UpdateChallengeDto { +string Title +string Description +string ExpectedSolution +DateTime Deadline }
class RejectChallengeDto { +string Reason }
class ChallengeDto { +Guid Id +string Title +string Description +string Status +DateTime CreatedAt +int CreatedById +int? ReviewedById +DateTime Deadline +DateTime? PublishedAt }
class CreatorChallengeDto { +Guid Id +string Title +string Description +string Status +Guid? CurrentVersionId +DateTime CreatedAt +DateTime UpdatedAt +DateTime Deadline }
class ChallengeVersionDto { +Guid Id +Guid ChallengeId +int VersionNumber +string Title +string Description +string ExpectedSolution +decimal DifficultyScore +string DifficultyLabel +List~ChallengeVersionCriteriaDto~ Criteria }
class ChallengeVersionCriteriaDto { +Guid Id +Guid VersionId +string Name +string Description +decimal Weight +decimal MaxScore +int DisplayOrder }
class PublicChallengeDto { +Guid Id +string Title +string Description +decimal DifficultyScore +string DifficultyLabel +DateTime Deadline +DateTime? PublishedAt +DateTime CreatedAt +Guid? CurrentVersionId +PublicVersionDto? ActiveVersion }
class PublicVersionDto { +Guid Id +int VersionNumber +decimal DifficultyScore +string DifficultyLabel +List~PublicCriteriaDto~ Criteria +DateTime CreatedAt }
class PublicCriteriaDto { +Guid Id +string Name +string Description +decimal MaxScore +int DisplayOrder }
class SubmissionDto { +Guid Id +Guid ChallengeId +int UserId +string Status +decimal OverallScore +string AiFeedback +DateTime CreatedAt +DateTime? GradedAt }
class SubmitSolutionDto { +string Content +string? GithubUrl }

%% Relations
ChallengeControllerV2 --> IChallengeService
ChallengeCreatorController --> IChallengeService
ChallengeCreatorController --> ISubmissionService
ChallengeDiscoveryController --> IChallengeService
ChallengeSubmissionController --> ISubmissionService

IChallengeService <|.. ChallengeService
IChallengeRepository <|.. ChallengeRepository
IChallengeVersionRepository <|.. ChallengeVersionRepository
IChallengeCriteriaRepository <|.. ChallengeCriteriaRepository

ChallengeService --> IChallengeRepository
ChallengeService --> IChallengeVersionRepository
ChallengeService --> IChallengeCriteriaRepository
ChallengeService --> ISkillRepository
ChallengeService --> IEvaluationCriteriaRepository
ChallengeService --> ICriteriaSkillMappingRepository
ChallengeService --> IGeminiAIService
ChallengeService --> IEventPublisher
ChallengeService --> IActorResolverClient

ChallengeRepository --> ChallengeDbContext
ChallengeVersionRepository --> ChallengeDbContext
ChallengeCriteriaRepository --> ChallengeDbContext

ChallengeDbContext --> Challenge
ChallengeDbContext --> ChallengeVersion
ChallengeDbContext --> ChallengeCriteria
ChallengeDbContext --> ChallengeSubmission
ChallengeDbContext --> SubmissionCriteriaScore
ChallengeDbContext --> EvaluationCriteria
ChallengeDbContext --> Skill
ChallengeDbContext --> SkillAlias
ChallengeDbContext --> SkillCategory
ChallengeDbContext --> PendingSkill
ChallengeDbContext --> CriteriaSkillMapping
ChallengeDbContext --> SkillPointTransaction
ChallengeDbContext --> UserSkill
ChallengeDbContext --> SkillRelationship
ChallengeDbContext --> PromptSanitizationLog

Challenge "1" o-- "*" ChallengeVersion
ChallengeVersion "1" o-- "*" ChallengeCriteria
ChallengeVersion "1" o-- "*" ChallengeSubmission
ChallengeCriteria "*" --> "1" EvaluationCriteria
ChallengeSubmission "1" o-- "*" SubmissionCriteriaScore

ChallengeControllerV2 ..> CreateChallengeDto
ChallengeControllerV2 ..> UpdateChallengeDto
ChallengeControllerV2 ..> RejectChallengeDto
ChallengeCreatorController ..> CreatorChallengeDto
ChallengeCreatorController ..> ChallengeVersionDto
ChallengeCreatorController ..> ChallengeDto
ChallengeDiscoveryController ..> PublicChallengeDto
ChallengeSubmissionController ..> SubmissionDto

ChallengeService ..> CreateChallengeDto
ChallengeService ..> UpdateChallengeDto
ChallengeService ..> ChallengeDto
ChallengeService ..> CreatorChallengeDto
ChallengeService ..> ChallengeVersionDto
ChallengeService ..> PublicChallengeDto
ChallengeService ..> PublicVersionDto
ChallengeService ..> PublicCriteriaDto
ChallengeService ..> ChallengeVersionCriteriaDto

%% Background Job service
class IChallengeExpirationJob { <<interface>> +ExecuteAsync() Task }
class ChallengeExpirationJob { -IChallengeRepository _challengeRepository -ILogger~ChallengeExpirationJob~ _logger +ExecuteAsync() Task }
ChallengeExpirationJob ..|> IChallengeExpirationJob
ChallengeExpirationJob --> IChallengeRepository

```