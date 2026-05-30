```mermaid
classDiagram
direction LR

%% Controller-centric flow diagram: start from controllers and draw dependencies backwards

%% Controllers with main endpoints
class ChallengeControllerV2 {
  -IChallengeService _challengeService
  -ILogger~ChallengeControllerV2~ _logger
  +CreateChallenge(request CreateChallengeDto)
  +GetChallenge(id Guid)
  +ListChallenges(skip, take, status?, userId?)
  +UpdateChallenge(id Guid, request UpdateChallengeDto)
  +DeleteChallenge(id Guid)
  +SubmitForReview(id Guid)
  +ApproveChallenge(id Guid)
  +RejectChallenge(id Guid, request RejectChallengeDto)
}

class ChallengeCreatorController {
  -IChallengeService _challengeService
  -ISubmissionService _submissionService
  -ILogger~ChallengeCreatorController~ _logger
  +GetMyChallenges(skip, take)
  +GetChallengeVersions(challengeId Guid)
  +SetActiveVersion(challengeId Guid, versionId Guid)
  +ApproveAndPublish(challengeId Guid)
  +GetChallengeSubmissions(challengeId Guid, skip, take)
}

class ChallengeDiscoveryController {
  -IChallengeService _challengeService
  -ILogger~ChallengeDiscoveryController~ _logger
  +GetPublishedChallenges(skip, take, search?, skillFilter?)
  +GetPublicChallenge(id Guid)
}

class ChallengeSubmissionController {
  -ISubmissionService _submissionService
  -ILogger~ChallengeSubmissionController~ _logger
  +GetMySubmissions(challengeId Guid, skip, take)
}

class SubmissionControllerV2 {
  -ISubmissionService _submissionService
  +SubmitSolution(challengeId Guid, request SubmitSolutionDto)
  +GetSubmission(id Guid)
  +GetChallengeSubmissions(challengeId Guid, skip, take)
}

class SkillControllerV2 {
  -ISkillService _skillService
  +GetUserSkills(userId int)
  +RecalculateSkills() 
}

class PortfolioSkillsControllerV2 {
  -IPortfolioSkillsService _portfolioSkillsService
  +SyncPortfolioSkills(portfolioId int)
}

%% Application services called by controllers
class IChallengeService { <<interface>> }
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
  -ISubmissionGradingOrchestrator _gradingOrchestrator
  -ILogger~ChallengeService~ _logger
  +CreateChallengeAsync(CreateChallengeDto, userId)
  +GetChallengeByIdAsync(id, currentUserId)
  +ListChallengesAsync(pageSize, cursor, currentUserId)
  +UpdateChallengeAsync(id, UpdateChallengeDto, userId)
  +DeleteChallengeAsync(id, userId)
  +SubmitForReviewAsync(id, userId)
  +ApproveChallengeAsync(id, adminId)
  +RejectChallengeAsync(id, reason, adminId)
  +GetChallengesPagedAsync(skip, take, status?, userId?)
  +GetCreatorChallengesAsync(userId, skip, take)
  +GetChallengeVersionsAsync(challengeId, creatorUserId)
  +SetActiveVersionAsync(challengeId, versionId, creatorUserId)
  +ApproveAndPublishAsync(challengeId, creatorUserId)
  +GetPublishedChallengesAsync(skip, take, searchTerm?, skillFilter?)
  +GetPublicChallengeByIdAsync(id)
}

class ISubmissionService { <<interface>> }
class SubmissionServiceImpl {
  -ISubmissionRepository _submissionRepository
  -ISubmissionCriteriaScoreRepository _criteriaScoreRepository
  -ISkillPointTransactionRepository _skillPointTxRepository
  -ISkillPointService _skillPointService
  -IGradingService _gradingService
  -ISubmissionGradingOrchestrator _gradingOrchestrator
  -ILogger~SubmissionServiceImpl~ _logger
  +SubmitSolutionAsync(challengeId, SubmitSolutionDto, userId)
  +GetSubmissionByIdAsync(id, currentUserId)
  +GetUserSubmissionsAsync(userId, challengeId?)
  +GetChallengeSubmissionsAsync(challengeId)
  +GradeSubmissionAsync(id)
}

class ISkillService { <<interface>> }
class SkillServiceImpl {
  -ISkillRepository _skillRepository
  -IUserSkillRepository _userSkillRepository
  -ILogger~SkillServiceImpl~ _logger
  +GetUserSkills(userId)
  +RecalculateUserSkills(userId)
}

class IPortfolioSkillsService { <<interface>> }
class PortfolioSkillsServiceImpl {
  -IActorResolverClient _actorResolverClient
  -ILogger~PortfolioSkillsServiceImpl~ _logger
  +SyncPortfolioSkills(portfolioId)
}

%% Grading & AI services
class IGradingService { <<interface>> }
class GradingServiceImpl {
  -IGeminiAIService _geminiAIService
  -IPromptSanitizationService _promptSanitizationService
  -ILogger~GradingServiceImpl~ _logger
  +GradeSubmissionAsync(submissionId)
}

class IGeminiAIService { <<interface>> }
class GeminiAIServiceReal {
  -IGeminiAIClient _client
  -ILogger~GeminiAIServiceReal~ _logger
  +AnalyzeChallengeAsync(title, description, expectedSolution)
  +AnalyzeSubmissionAsync(submissionContent)
}

class IPromptSanitizationService { <<interface>> }
class PromptSanitizationService {
  +Sanitize(prompt)
}

%% Repositories (interfaces & implementations)
class IChallengeRepository { <<interface>> }
class ChallengeRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id)
  +GetAllAsync()
  +GetByStatusAsync(status)
  +GetPublishedAsync()
  +AddAsync(challenge)
  +UpdateAsync(challenge)
  +DeleteAsync(id)
  +ExistsAsync(id)
}

class IChallengeVersionRepository { <<interface>> }
class ChallengeVersionRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id)
  +GetActiveVersionAsync(challengeId)
  +GetVersionsByChallengeAsync(challengeId)
  +AddAsync(version)
  +UpdateAsync(version)
}

class IChallengeCriteriaRepository { <<interface>> }
class ChallengeCriteriaRepository {
  -ChallengeDbContext _context
  +GetByVersionAsync(challengeVersionId)
  +AddRangeAsync(challengeCriteria)
}

class ISubmissionRepository { <<interface>> }
class SubmissionRepository {
  -ChallengeDbContext _context
  +AddAsync(submission)
  +GetByIdAsync(id)
  +GetByChallengeAsync(challengeId)
}

class ISubmissionCriteriaScoreRepository { <<interface>> }
class SubmissionCriteriaScoreRepository {
  -ChallengeDbContext _context
  +AddAsync(score)
  +GetBySubmissionAsync(submissionId)
}

class ISkillRepository { <<interface>> }
class SkillRepository {
  -ChallengeDbContext _context
  +GetByIdAsync(id)
  +AddAsync(skill)
  +FindByNameAsync(name)
}

class ISkillPointTransactionRepository { <<interface>> }
class SkillPointTransactionRepository {
  -ChallengeDbContext _context
  +AddAsync(tx)
}

class IUserSkillRepository { <<interface>> }
class UserSkillRepository {
  -ChallengeDbContext _context
  +GetByUserAsync(userId)
  +UpdateAsync(userSkill)
}

class IEvaluationCriteriaRepository { <<interface>> }
class EvaluationCriteriaRepository {
  -ChallengeDbContext _context
  +GetByNameAsync(name)
  +AddAsync(criteria)
}

class ICriteriaSkillMappingRepository { <<interface>> }
class CriteriaSkillMappingRepository {
  -ChallengeDbContext _context
  +GetByCriteriaAsync(criteriaId)
  +AddAsync(mapping)
}

%% Infrastructure clients
class IGeminiAIClient { <<interface>> }
class GeminiAIClient {
  -HttpClient _http
  +CallAnalyzeEndpoint(payload)
}

class IEventPublisher { <<interface>> }
class EventPublisher {
  -HttpClient _http
  +Publish(event)
}

class IActorResolverClient { <<interface>> }
class ActorResolverClient {
  -HttpClient _http
  +ResolveUserById(userId)
}

%% DbContext and Entities
class ChallengeDbContext {
  +DbSet~Challenge~ Challenges
  +DbSet~ChallengeVersion~ ChallengeVersions
  +DbSet~ChallengeCriteria~ ChallengeCriteria
  +DbSet~ChallengeSubmission~ ChallengeSubmissions
  +DbSet~SubmissionCriteriaScore~ SubmissionCriteriaScores
  +DbSet~EvaluationCriteria~ EvaluationCriteria
  +DbSet~Skill~ Skills
  +DbSet~UserSkill~ UserSkills
}

class Challenge {
  +Id Guid
  +Title string
  +Description string
  +ExpectedSolution string
  +DifficultyScore decimal
  +DifficultyLabel string
  +Status ChallengeStatus
  +CurrentVersionId Guid?
  +CreatedById int
  +ReviewedById int?
  +Deadline DateTime
  +PublishedAt DateTime?
}

class ChallengeVersion {
  +Id Guid
  +ChallengeId Guid
  +VersionNumber int
  +Title string
  +Description string
  +SkillWeightMapping string
  +ModelName string
  +PromptVersion string
}

class ChallengeCriteria {
  +Id Guid
  +ChallengeVersionId Guid
  +CriteriaId Guid
  +Weight decimal
}

class ChallengeSubmission {
  +Id Guid
  +ChallengeId Guid
  +UserId int
  +SubmissionContent string
  +GithubUrl string
  +OverallScore decimal
  +AiFeedback string
  +Status SubmissionStatus
  +AttemptCount int
}

class SubmissionCriteriaScore {
  +Id Guid
  +SubmissionId Guid
  +CriteriaId Guid
  +Score decimal
  +Feedback string
}

class EvaluationCriteria {
  +Id Guid
  +Name string
  +Description string
}

class Skill {
  +Id Guid
  +Name string
  +Description string
}

class UserSkill {
  +Id Guid
  +UserId int
  +SkillId Guid
  +TotalPoints decimal
}

%% Controller -> Service -> Repo chains (explicit flows)
ChallengeControllerV2 --> ChallengeService : CreateChallengeAsync
ChallengeService --> ChallengeRepository : AddAsync
ChallengeRepository --> ChallengeDbContext : SaveChanges

ChallengeControllerV2 --> ChallengeService : SubmitForReviewAsync
ChallengeService --> ChallengeVersionRepository : AddAsync
ChallengeService --> EvaluationCriteriaRepository : AddAsync
ChallengeService --> CriteriaSkillMappingRepository : AddAsync
ChallengeService --> EventPublisher : Publish(ChallengeAnalyzed)

ChallengeControllerV2 --> ChallengeService : ApproveChallengeAsync
ChallengeService --> ChallengeRepository : UpdateAsync
ChallengeService --> EventPublisher : Publish(ChallengePublished)

ChallengeCreatorController --> SubmissionServiceImpl : GetChallengeSubmissionsWithUserInfoAsync
SubmissionServiceImpl --> SubmissionRepository : GetByChallengeAsync
SubmissionServiceImpl --> SubmissionCriteriaScoreRepository : GetBySubmissionAsync
SubmissionServiceImpl --> SkillPointTransactionRepository : AddAsync

SubmissionControllerV2 --> SubmissionServiceImpl : SubmitSolutionAsync
SubmissionServiceImpl --> SubmissionRepository : AddAsync
SubmissionServiceImpl --> GradingServiceImpl : GradeSubmissionAsync
GradingServiceImpl --> GeminiAIServiceReal : AnalyzeSubmissionAsync
GradingServiceImpl --> PromptSanitizationService : Sanitize
GradingServiceImpl --> SubmissionCriteriaScoreRepository : AddAsync
GradingServiceImpl --> SkillPointTransactionRepository : AddAsync

SkillControllerV2 --> SkillServiceImpl : RecalculateUserSkills
SkillServiceImpl --> SkillRepository : FindByNameAsync
SkillServiceImpl --> UserSkillRepository : UpdateAsync

PortfolioSkillsControllerV2 --> PortfolioSkillsServiceImpl : SyncPortfolioSkills
PortfolioSkillsServiceImpl --> ActorResolverClient : ResolveUser
PortfolioSkillsServiceImpl --> SkillRepository : AddAsync

%% Jobs
ChallengeExpirationJob --> ChallengeRepository : GetPublishedAsync / UpdateAsync
SkillRecalculationJobService --> UserSkillRepository : GetByUserAsync / UpdateAsync

%% Notes
note left of SubmissionServiceImpl : Handles submission lifecycle: persist -> grade -> record scores -> award skill points
note right of ChallengeService : Handles creator-facing flows and AI analysis integration

```