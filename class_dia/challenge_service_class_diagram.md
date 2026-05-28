classDiagram
direction LR

class ChallengeControllerV2 {
    -IChallengeService _challengeService
    -ILogger~ChallengeControllerV2~ _logger
    +CreateChallenge(request CreateChallengeDto) IActionResult
    +GetChallenge(id Guid) IActionResult
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
    -ILogger~ChallengeCreatorController~ _logger
    +GetMyChallenges(skip int, take int) IActionResult
    +GetChallengeVersions(challengeId Guid) IActionResult
    +SetActiveVersion(challengeId Guid, versionId Guid) IActionResult
    +ApproveAndPublish(challengeId Guid) IActionResult
    -GetCurrentUserId() int?
}

class ChallengeDiscoveryController {
    -IChallengeService _challengeService
    -ILogger~ChallengeDiscoveryController~ _logger
    +GetPublishedChallenges(skip int, take int, search string?, skillFilter string?) IActionResult
    +GetPublicChallenge(id Guid) IActionResult
}

class IChallengeService {
    <<interface>>
    +CreateChallengeAsync(request CreateChallengeDto, userId int) Task~ChallengeDto~
    +GetChallengeByIdAsync(id Guid, currentUserId int?) Task~ChallengeDto?~
    +SubmitForReviewAsync(id Guid, userId int) Task~ChallengeDto~
    +ApproveChallengeAsync(id Guid, adminId int) Task~ChallengeDto~
    +RejectChallengeAsync(id Guid, reason string, adminId int) Task~ChallengeDto~
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
    -IChallengeCriteriaRepository _challengeCriteriaRepository
    -IGeminiAIService _geminiAIService
    -ILogger~ChallengeService~ _logger
    +CreateChallengeAsync(request CreateChallengeDto, userId int) Task~ChallengeDto~
    +GetChallengeByIdAsync(id Guid, currentUserId int?) Task~ChallengeDto?~
    +SubmitForReviewAsync(id Guid, userId int) Task~ChallengeDto~
    +ApproveChallengeAsync(id Guid, adminId int) Task~ChallengeDto~
    +RejectChallengeAsync(id Guid, reason string, adminId int) Task~ChallengeDto~
    +GetCreatorChallengesAsync(userId int, skip int, take int) Task~Tuple~List~CreatorChallengeDto~, int~~
    +GetChallengeVersionsAsync(challengeId Guid, creatorUserId int) Task~List~ChallengeVersionDto~~
    +SetActiveVersionAsync(challengeId Guid, versionId Guid, creatorUserId int) Task~ChallengeVersionDto~
    +ApproveAndPublishAsync(challengeId Guid, creatorUserId int) Task~CreatorChallengeDto~
    +GetPublishedChallengesAsync(skip int, take int, searchTerm string?, skillFilter string?) Task~Tuple~List~PublicChallengeDto~, int~~
    +GetPublicChallengeByIdAsync(id Guid) Task~PublicChallengeDto?~
    -EnsureOwner(challenge Challenge, userId int) void
    -MapToDto(challenge Challenge) ChallengeDto
    -MapToPublicVersionDtoAsync(version ChallengeVersion) Task~PublicVersionDto~
}

class IChallengeRepository {
    <<interface>>
    +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~Challenge~
    +GetAllAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
    +GetPublishedAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
    +AddAsync(challenge Challenge, cancellationToken CancellationToken) Task
    +UpdateAsync(challenge Challenge, cancellationToken CancellationToken) Task
    +DeleteAsync(id Guid, cancellationToken CancellationToken) Task
}

class ChallengeRepository {
    -ChallengeDbContext _context
    +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~Challenge~
    +GetAllAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
    +GetPublishedAsync(cancellationToken CancellationToken) Task~IEnumerable~Challenge~~
    +AddAsync(challenge Challenge, cancellationToken CancellationToken) Task
    +UpdateAsync(challenge Challenge, cancellationToken CancellationToken) Task
    +DeleteAsync(id Guid, cancellationToken CancellationToken) Task
}

class IChallengeVersionRepository {
    <<interface>>
    +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~ChallengeVersion~
    +GetVersionsByChallengeAsync(challengeId Guid, cancellationToken CancellationToken) Task~IEnumerable~ChallengeVersion~~
    +AddAsync(version ChallengeVersion, cancellationToken CancellationToken) Task
    +UpdateAsync(version ChallengeVersion, cancellationToken CancellationToken) Task
}

class ChallengeVersionRepository {
    -ChallengeDbContext _context
    +GetByIdAsync(id Guid, cancellationToken CancellationToken) Task~ChallengeVersion~
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

class ChallengeDbContext {
    +DbSet~Challenge~ Challenges
    +DbSet~ChallengeVersion~ ChallengeVersions
    +DbSet~ChallengeCriteria~ ChallengeCriteria
    +DbSet~ChallengeSubmission~ ChallengeSubmissions
    +DbSet~SubmissionCriteriaScore~ SubmissionCriteriaScores
    +DbSet~EvaluationCriteria~ EvaluationCriteria
}

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

class CreateChallengeDto {
    +string Title
    +string Description
    +string ExpectedSolution
    +DateTime Deadline
}

class UpdateChallengeDto {
    +string Title
    +string Description
    +string ExpectedSolution
    +DateTime Deadline
}

class RejectChallengeDto {
    +string Reason
}

class ChallengeDto {
    +Guid Id
    +string Title
    +string Description
    +string Status
    +DateTime CreatedAt
    +int CreatedById
    +int? ReviewedById
    +DateTime Deadline
    +DateTime? PublishedAt
}

class CreatorChallengeDto {
    +Guid Id
    +string Title
    +string Description
    +string Status
    +Guid? CurrentVersionId
    +DateTime CreatedAt
    +DateTime UpdatedAt
    +DateTime Deadline
}

class ChallengeVersionDto {
    +Guid Id
    +Guid ChallengeId
    +int VersionNumber
    +string Title
    +string Description
    +string ExpectedSolution
    +decimal DifficultyScore
    +string DifficultyLabel
    +List~ChallengeVersionCriteriaDto~ Criteria
}

class PublicChallengeDto {
    +Guid Id
    +string Title
    +string Description
    +decimal DifficultyScore
    +string DifficultyLabel
    +DateTime Deadline
    +DateTime? PublishedAt
    +DateTime CreatedAt
    +Guid? CurrentVersionId
    +PublicVersionDto? ActiveVersion
}

class PublicVersionDto {
    +Guid Id
    +int VersionNumber
    +decimal DifficultyScore
    +string DifficultyLabel
    +List~PublicCriteriaDto~ Criteria
    +DateTime CreatedAt
}

class PublicCriteriaDto {
    +Guid Id
    +string Name
    +string Description
    +decimal MaxScore
    +int DisplayOrder
}

class ChallengeVersionCriteriaDto {
    +Guid Id
    +Guid VersionId
    +string Name
    +string Description
    +decimal Weight
    +decimal MaxScore
    +int DisplayOrder
}

ChallengeControllerV2 --> IChallengeService
ChallengeCreatorController --> IChallengeService
ChallengeDiscoveryController --> IChallengeService

IChallengeService <|.. ChallengeService
IChallengeRepository <|.. ChallengeRepository
IChallengeVersionRepository <|.. ChallengeVersionRepository
IChallengeCriteriaRepository <|.. ChallengeCriteriaRepository

ChallengeService --> IChallengeRepository
ChallengeService --> IChallengeVersionRepository
ChallengeService --> IChallengeCriteriaRepository

ChallengeRepository --> ChallengeDbContext
ChallengeVersionRepository --> ChallengeDbContext
ChallengeCriteriaRepository --> ChallengeDbContext

ChallengeDbContext --> Challenge
ChallengeDbContext --> ChallengeVersion
ChallengeDbContext --> ChallengeCriteria
ChallengeDbContext --> ChallengeSubmission
ChallengeDbContext --> SubmissionCriteriaScore
ChallengeDbContext --> EvaluationCriteria

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
ChallengeDiscoveryController ..> PublicChallengeDto

ChallengeService ..> CreateChallengeDto
ChallengeService ..> UpdateChallengeDto
ChallengeService ..> ChallengeDto
ChallengeService ..> CreatorChallengeDto
ChallengeService ..> ChallengeVersionDto
ChallengeService ..> PublicChallengeDto
ChallengeService ..> PublicVersionDto
ChallengeService ..> PublicCriteriaDto
ChallengeService ..> ChallengeVersionCriteriaDto