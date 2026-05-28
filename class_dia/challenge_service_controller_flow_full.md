```mermaid
classDiagram
direction LR

%% Controller-centric comprehensive flow (merged + expanded)
%% Start from controllers -> services -> repositories -> DbContext -> entities -> infra

%% -------------------- CONTROLLERS --------------------
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

class SubmissionControllerV2 {
  -ISubmissionService _submissionService
  -ILogger~SubmissionControllerV2~ _logger
  +SubmitSolution(challengeId Guid, request SubmitSolutionDto) IActionResult
  +GetSubmission(id Guid) IActionResult
  +GetChallengeSubmissions(challengeId Guid, skip int, take int) IActionResult
}

class SkillControllerV2 {
  -ISkillService _skillService
  -ILogger~SkillControllerV2~ _logger
  +GetUserSkills(userId int) IActionResult
  +RecalculateSkills(userId int) IActionResult
}

class PortfolioSkillsControllerV2 {
  -IPortfolioSkillsService _portfolioSkillsService
  -ILogger~PortfolioSkillsControllerV2~ _logger
  +SyncPortfolioSkills(portfolioId int) IActionResult
}

%% -------------------- SERVICES / INTERFACES --------------------
class IChallengeService {<<interface>>}
class ChallengeService {
  -IChallengeRepository _challengeRepository
  -IChallengeVersionRepository _versionRepository
  -ISkillRepository _skillRepository
  -IEvaluationCriteriaRepository _evaluationCriteriaRepository
  -IChallengeCriteriaRepository _challengeCriteriaRepository
  -ICriteriaSkillMappingRepository _criteriaSkillMapping_repository
  -IGeminiAIService _geminiAIService
  -IEventPublisher _eventPublisher
  -IActorResolverClient _actorResolverClient
  -ISubmissionGradingOrchestrator _gradingOrchestrator
  -ILogger~ChallengeService~ _logger
  +CreateChallengeAsync(CreateChallengeDto, userId int) Task~ChallengeDto~
  +GetChallengeByIdAsync(id Guid, currentUserId int?) Task~ChallengeDto?~
  +ListChallengesAsync(pageSize int, cursor int?, currentUserId int?) Task~List~ChallengeDto~~
  +UpdateChallengeAsync(id Guid, UpdateChallengeDto, userId int) Task~ChallengeDto~
  +DeleteChallengeAsync(id Guid, userId int) Task
  +SubmitForReviewAsync(id Guid, userId int) Task~ChallengeDto~
  +ApproveChallengeAsync(id Guid, adminId int) Task~ChallengeDto~
  +RejectChallengeAsync(id Guid, reason string, adminId int) Task~ChallengeDto~
  +GetChallengesPagedAsync(skip int, take int, status string?, userId int?) Task~(List~ChallengeDto~, int)~
  +GetCreatorChallengesAsync(userId int, skip int, take int) Task~(List~CreatorChallengeDto~, int)~
  +GetChallengeVersionsAsync(challengeId Guid, creatorUserId int) Task~List~ChallengeVersionDto~~
  +SetActiveVersionAsync(challengeId Guid, versionId Guid, creatorUserId int) Task~ChallengeVersionDto~
  +ApproveAndPublishAsync(challengeId Guid, creatorUserId int) Task~CreatorChallengeDto~
  +GetPublishedChallengesAsync(skip int, take int, searchTerm string?, skillFilter string?) Task~(List~PublicChallengeDto~, int)~
  +GetPublicChallengeByIdAsync(id Guid) Task~PublicChallengeDto?~
}

class ISubmissionService {<<interface>>}
class SubmissionServiceImpl {
  -ISubmissionRepository _submissionRepository
  -ISubmissionCriteriaScoreRepository _criteriaScoreRepository
  -ISkillPointTransactionRepository _skillPointTxRepository
  -ISkillPointService _skillPointService
  -IGradingService _gradingService
  -ISubmissionGradingOrchestrator _gradingOrchestrator
  -ILogger~SubmissionServiceImpl~ _logger
  +SubmitSolutionAsync(challengeId Guid, SubmitSolutionDto, userId int) Task~SubmissionDto~
  +GetSubmissionByIdAsync(id Guid, currentUserId int?) Task~SubmissionDto?~
  +GetUserSubmissionsAsync(userId int, challengeId Guid?) Task~List~SubmissionDto~~
  +GetChallengeSubmissionsAsync(challengeId Guid) Task~List~SubmissionDto~~
  +GradeSubmissionAsync(id Guid) Task~SubmissionDto~
}

class ISkillService {<<interface>>}
class SkillServiceImpl {
  -ISkillRepository _skillRepository
  -IUserSkillRepository _userSkillRepository
  -ILogger~SkillServiceImpl~ _logger
  +GetUserSkills(userId int) Task~List~SkillDto~~
  +RecalculateUserSkills(userId int) Task
}

class IPortfolioSkillsService {<<interface>>}
class PortfolioSkillsServiceImpl {
  -IActorResolverClient _actorResolverClient
  -ILogger~PortfolioSkillsServiceImpl~ _logger
  +SyncPortfolioSkills(portfolioId int) Task
}

class IGradingService {<<interface>>}
class GradingServiceImpl {
  -IGeminiAIService _geminiAIService
  -IPromptSanitizationService _promptSanitizationService
  -ILogger~GradingServiceImpl~ _logger
  +GradeSubmissionAsync(submissionId Guid) Task
}

class IGeminiAIService {<<interface>>}
class GeminiAIServiceReal {
  -IGeminiAIClient _client
  -ILogger~GeminiAIServiceReal~ _logger
  +AnalyzeChallengeAsync(title string, description string, expectedSolution string) Task~ChallengeAnalysisResult~
  +AnalyzeSubmissionAsync(submissionContent string) Task~SubmissionAnalysisResult~
}

class IPromptSanitizationService {<<interface>>}
class PromptSanitizationService { +Sanitize(prompt string) string }

class ISkillPointService {<<interface>>}
class SkillPointServiceImpl { -ISkillPointTransactionRepository _txRepo -IUserSkillRepository _userSkillRepo +AwardPoints(userId int, skillId Guid, points decimal) Task }

%% -------------------- REPOSITORIES --------------------
class IChallengeRepository {<<interface>>}
class ChallengeRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id Guid) Task~Challenge~
  +GetAllAsync() Task~IEnumerable~Challenge~
  +GetByStatusAsync(status ChallengeStatus) Task~IEnumerable~Challenge~
  +GetPublishedAsync() Task~IEnumerable~Challenge~
  +AddAsync(challenge Challenge) Task
  +UpdateAsync(challenge Challenge) Task
  +DeleteAsync(id Guid) Task
  +ExistsAsync(id Guid) Task~bool~
}

class IChallengeVersionRepository {<<interface>>}
class ChallengeVersionRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id Guid) Task~ChallengeVersion~
  +GetActiveVersionAsync(challengeId Guid) Task~ChallengeVersion~
  +GetVersionsByChallengeAsync(challengeId Guid) Task~IEnumerable~ChallengeVersion~
  +AddAsync(version ChallengeVersion) Task
  +UpdateAsync(version ChallengeVersion) Task
}

class IChallengeCriteriaRepository {<<interface>>}
class ChallengeCriteriaRepository {
  -ChallengeDbContext _context
  +GetByVersionAsync(challengeVersionId Guid) Task~List~ChallengeCriteria~
  +AddRangeAsync(challengeCriteria IEnumerable~ChallengeCriteria~) Task
}

class ISubmissionRepository {<<interface>>}
class SubmissionRepository {
  -ChallengeDbContext _context
  +AddAsync(submission ChallengeSubmission) Task
  +GetByIdAsync(id Guid) Task~ChallengeSubmission~
  +GetByChallengeAsync(challengeId Guid) Task~List~ChallengeSubmission~
}

class ISubmissionCriteriaScoreRepository {<<interface>>}
class SubmissionCriteriaScoreRepository {
  -ChallengeDbContext _context
  +AddAsync(score SubmissionCriteriaScore) Task
  +GetBySubmissionAsync(submissionId Guid) Task~List~SubmissionCriteriaScore~
}

class ISkillRepository {<<interface>>}
class SkillRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id Guid) Task~Skill~
  +AddAsync(skill Skill) Task
  +FindByNameAsync(name string) Task~Skill?~
}

class ISkillPointTransactionRepository {<<interface>>}
class SkillPointTransactionRepository { -ChallengeDbContext _context +AddAsync(tx SkillPointTransaction) Task }

class IUserSkillRepository {<<interface>>}
class UserSkillRepository { -ChallengeDbContext _context +GetByUserAsync(userId int) Task~List~UserSkill~ +UpdateAsync(userSkill UserSkill) Task }

class IEvaluationCriteriaRepository {<<interface>>}
class EvaluationCriteriaRepository { -ChallengeDbContext _context +GetByNameAsync(name string) Task~EvaluationCriteria?~ +AddAsync(criteria EvaluationCriteria) Task }

class ICriteriaSkillMappingRepository {<<interface>>}
class CriteriaSkillMappingRepository { -ChallengeDbContext _context +GetByCriteriaAsync(criteriaId Guid) Task~List~CriteriaSkillMapping~ +AddAsync(mapping CriteriaSkillMapping) Task }

%% -------------------- INFRA CLIENTS / JOBS --------------------
class IGeminiAIClient {<<interface>>}
class GeminiAIClient { -HttpClient _http +CallAnalyzeEndpoint(payload) Task }
class IEventPublisher {<<interface>>}
class EventPublisher { -HttpClient _http +Publish(event) Task }
class IActorResolverClient {<<interface>>}
class ActorResolverClient { -HttpClient _http +ResolveUserById(userId int) Task~UserDto~ }

class ChallengeExpirationJobService {
  -IChallengeRepository _challengeRepository
  -ILogger~ChallengeExpirationJobService~ _logger
  +RunExpirationJobAsync() Task
}
class SkillRecalculationJobService {
  -IUserSkillRepository _userSkillRepository
  -ILogger~SkillRecalculationJobService~ _logger
  +RunRecalculationAsync() Task
}
class BackgroundJobScheduler { +ScheduleJobs() }

%% -------------------- DB / ENTITIES --------------------
class ChallengeDbContext {
  +DbSet~Challenge~ Challenges
  +DbSet~ChallengeVersion~ ChallengeVersions
  +DbSet~ChallengeCriteria~ ChallengeCriteria
  +DbSet~ChallengeSubmission~ ChallengeSubmissions
  +DbSet~SubmissionCriteriaScore~ SubmissionCriteriaScores
  +DbSet~EvaluationCriteria~ EvaluationCriteria
  +DbSet~Skill~ Skills
  +DbSet~UserSkill~ UserSkills
  +DbSet~SkillPointTransaction~ SkillPointTransactions
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

class EvaluationCriteria { +Guid Id +string Name +string Description +DateTime CreatedAt +DateTime UpdatedAt }
class Skill { +Guid Id +string Name +string? Description +Guid? CategoryId }
class UserSkill { +Guid Id +int UserId +Guid SkillId +decimal TotalPoints }
class SkillPointTransaction { +Guid Id +int UserId +Guid SkillId +decimal Points +string SourceType +Guid SourceId +DateTime CreatedAt }

%% -------------------- EXPLICIT FLOWS --------------------
%% Create challenge
ChallengeControllerV2 --> ChallengeService : CreateChallengeAsync(request)
ChallengeService --> ChallengeRepository : AddAsync(challenge)
ChallengeRepository --> ChallengeDbContext : Add(Entity) / SaveChanges

%% Submit for review (AI analysis -> version & criteria persisted)
ChallengeControllerV2 --> ChallengeService : SubmitForReviewAsync(challengeId)
ChallengeService --> GeminiAIServiceReal : AnalyzeChallengeAsync(title, description, expectedSolution)
GeminiAIServiceReal --> GeminiAIClient : CallAnalyzeEndpoint(payload)
ChallengeService --> ChallengeVersionRepository : AddAsync(newVersion)
ChallengeService --> EvaluationCriteriaRepository : AddAsync(if missing)
ChallengeService --> ChallengeCriteriaRepository : AddRangeAsync(criteria)
ChallengeService --> EventPublisher : Publish(ChallengeAnalyzed)

%% Approve/publish
ChallengeControllerV2 --> ChallengeService : ApproveChallengeAsync(challengeId)
ChallengeService --> ChallengeRepository : UpdateAsync(challenge.Status=Published)
ChallengeService --> EventPublisher : Publish(ChallengePublished)

%% Submit solution (participant flow)
SubmissionControllerV2 --> SubmissionServiceImpl : SubmitSolutionAsync(challengeId, dto)
SubmissionServiceImpl --> SubmissionRepository : AddAsync(submission)
SubmissionServiceImpl --> GradingServiceImpl : GradeSubmissionAsync(submissionId)
GradingServiceImpl --> PromptSanitizationService : Sanitize(submissionContent)
GradingServiceImpl --> GeminiAIServiceReal : AnalyzeSubmissionAsync(sanitizedContent)
GradingServiceImpl --> SubmissionCriteriaScoreRepository : AddAsync(scores)
GradingServiceImpl --> SkillPointTransactionRepository : AddAsync(tx)
GradingServiceImpl --> SubmissionRepository : UpdateAsync(submission.OverallScore, AiFeedback)

%% Get challenge submissions (creator)
ChallengeCreatorController --> SubmissionServiceImpl : GetChallengeSubmissionsWithUserInfoAsync(challengeId)
SubmissionServiceImpl --> SubmissionRepository : GetByChallengeAsync(challengeId)
SubmissionServiceImpl --> UserSkillRepository : GetByUserAsync(for stats)

%% Background jobs
BackgroundJobScheduler --> ChallengeExpirationJobService : RunExpirationJobAsync()
ChallengeExpirationJobService --> ChallengeRepository : GetPublishedAsync()
ChallengeExpirationJobService --> ChallengeRepository : UpdateAsync(challenge.Status=Expired)

BackgroundJobScheduler --> SkillRecalculationJobService : RunRecalculationAsync()
SkillRecalculationJobService --> UserSkillRepository : GetByUserAsync()
SkillRecalculationJobService --> UserSkillRepository : UpdateAsync(userSkill)

%% -------------------- NOTES --------------------
note left of ChallengeService : Key responsibilities:
- Business validation (ownership, status)
- Coordinating AI analysis and versioning
- Persisting criteria and mappings
- Publishing events

note right of SubmissionServiceImpl : Key responsibilities:
- Persisting submissions
- Invoking grading orchestration
- Recording per-criteria scores and awarding skill points
- Handling attempt limits and anti-farming

```