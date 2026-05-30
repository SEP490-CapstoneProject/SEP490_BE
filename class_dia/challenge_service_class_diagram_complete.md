```mermaid
classDiagram
direction LR

%% Comprehensive Challenge Service Class Diagram (auto-extracted)

%% API Controllers
class ChallengeControllerV2
class ChallengeCreatorController
class ChallengeDiscoveryController
class ChallengeSubmissionController
class SubmissionControllerV2
class SkillControllerV2
class PortfolioSkillsControllerV2
class ExceptionHandlingMiddleware
class Program
class ChallengeApiStartup

%% Application Interfaces & Services
class IChallengeService
class ChallengeService
class ISubmissionService
class SubmissionServiceImpl
class ISkillService
class SkillServiceImpl
class ISkillPointService
class SkillPointServiceImpl
class IPortfolioSkillsService
class PortfolioSkillsServiceImpl
class IGradingService
class GradingServiceImpl
class ISubmissionGradingOrchestrator
class ISkillPointEngine
class ISkillNormalizationService
class IPromptSanitizationService
class IGeminiAIService
class GeminiAIServiceReal
class GeminiAIService
class IChallengeExpirationJob
class ChallengeExpirationJob
class ISkillRecalculationJobService
class SkillRecalculationJobService

%% Domain Repositories
class IChallengeRepository
class IChallengeVersionRepository
class IChallengeCriteriaRepository
class ISubmissionRepository
class ISubmissionCriteriaScoreRepository
class ISkillRepository
class ISkillPointTransactionRepository
class IUserSkillRepository
class IEvaluationCriteriaRepository
class ICriteriaSkillMappingRepository

class ChallengeRepository
class ChallengeVersionRepository
class ChallengeCriteriaRepository
class SubmissionRepository
class SubmissionCriteriaScoreRepository
class SkillRepository
class SkillPointTransactionRepository
class UserSkillRepository
class CriteriaSkillMappingRepository
class EvaluationCriteriaRepository

%% Infrastructure Clients / Utilities
class GeminiAIClient
class EventPublisher
class ActorResolverClient
class AzureKeyVaultConfiguration
class BackgroundJobScheduler
class ServiceCollectionExtensions
class DependencyInjectionExtensions
class GlobalUsings

%% Domain Entities
class Challenge
class ChallengeVersion
class ChallengeCriteria
class ChallengeSubmission
class SubmissionCriteriaScore
class EvaluationCriteria
class Skill
class SkillAlias
class SkillCategory
class PendingSkill
class CriteriaSkillMapping
class SkillPointTransaction
class UserSkill
class SkillRelationship
class PromptSanitizationLog

%% DTOs & Models
class CreateChallengeDto
class UpdateChallengeDto
class ModerateChallengeDto
class ChallengeDto
class CreatorChallengeDto
class ChallengeVersionDto
class ChallengeVersionCriteriaDto
class PublicChallengeDto
class PublicVersionDto
class PublicCriteriaDto
class SubmissionDto
class SubmitSolutionDto
class SubmissionResponseDtos
class SkillDto
class PortfolioIntegrationDto
class ResultModels

%% EF Core DbContext
class ChallengeDbContext

%% Relations: API -> Application
ChallengeControllerV2 --> IChallengeService
ChallengeCreatorController --> IChallengeService
ChallengeDiscoveryController --> IChallengeService
ChallengeSubmissionController --> ISubmissionService
SubmissionControllerV2 --> ISubmissionService
SkillControllerV2 --> ISkillService
PortfolioSkillsControllerV2 --> IPortfolioSkillsService

Program --> ChallengeApiStartup
ChallengeApiStartup --> ServiceCollectionExtensions
ExceptionHandlingMiddleware ..> Program

%% Application wiring
IChallengeService <|.. ChallengeService
ISubmissionService <|.. SubmissionServiceImpl
ISkillService <|.. SkillServiceImpl
ISkillPointService <|.. SkillPointServiceImpl
IPortfolioSkillsService <|.. PortfolioSkillsServiceImpl
IGradingService <|.. GradingServiceImpl
IGeminiAIService <|.. GeminiAIServiceReal
IChallengeExpirationJob <|.. ChallengeExpirationJob

ChallengeService --> IChallengeRepository
ChallengeService --> IChallengeVersionRepository
ChallengeService --> IChallengeCriteriaRepository
ChallengeService --> ISkillRepository
ChallengeService --> IEvaluationCriteriaRepository
ChallengeService --> ICriteriaSkillMappingRepository
ChallengeService --> IGeminiAIService
ChallengeService --> IEventPublisher
ChallengeService --> IActorResolverClient
ChallengeService --> ISubmissionGradingOrchestrator

SubmissionServiceImpl --> ISubmissionRepository
SubmissionServiceImpl --> ISubmissionCriteriaScoreRepository
SubmissionServiceImpl --> ISkillPointTransactionRepository
SubmissionServiceImpl --> ISubmissionGradingOrchestrator

SkillServiceImpl --> ISkillRepository
SkillPointServiceImpl --> ISkillPointTransactionRepository
PortfolioSkillsServiceImpl --> IActorResolverClient
GradingServiceImpl --> IGeminiAIService
GradingServiceImpl --> IPromptSanitizationService

%% Repositories implementations
IChallengeRepository <|.. ChallengeRepository
IChallengeVersionRepository <|.. ChallengeVersionRepository
IChallengeCriteriaRepository <|.. ChallengeCriteriaRepository
ISubmissionRepository <|.. SubmissionRepository
ISubmissionCriteriaScoreRepository <|.. SubmissionCriteriaScoreRepository
ISkillRepository <|.. SkillRepository
ISkillPointTransactionRepository <|.. SkillPointTransactionRepository
IUserSkillRepository <|.. UserSkillRepository
ICriteriaSkillMappingRepository <|.. CriteriaSkillMappingRepository
IEvaluationCriteriaRepository <|.. EvaluationCriteriaRepository

ChallengeRepository --> ChallengeDbContext
ChallengeVersionRepository --> ChallengeDbContext
ChallengeCriteriaRepository --> ChallengeDbContext
SubmissionRepository --> ChallengeDbContext
SubmissionCriteriaScoreRepository --> ChallengeDbContext
SkillRepository --> ChallengeDbContext
SkillPointTransactionRepository --> ChallengeDbContext
UserSkillRepository --> ChallengeDbContext
CriteriaSkillMappingRepository --> ChallengeDbContext
EvaluationCriteriaRepository --> ChallengeDbContext

%% Infrastructure clients
GeminiAIClient ..> IGeminiAIService
EventPublisher ..> IEventPublisher
ActorResolverClient ..> IActorResolverClient
ServiceCollectionExtensions ..> DependencyInjectionExtensions
BackgroundJobScheduler --> SkillRecalculationJobService
BackgroundJobScheduler --> ChallengeExpirationJob

%% DbContext -> Entities
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

%% Entity relations
Challenge "1" o-- "*" ChallengeVersion
ChallengeVersion "1" o-- "*" ChallengeCriteria
ChallengeVersion "1" o-- "*" ChallengeSubmission
ChallengeCriteria "*" --> "1" EvaluationCriteria
ChallengeSubmission "1" o-- "*" SubmissionCriteriaScore

%% DTO usage
ChallengeControllerV2 ..> CreateChallengeDto
ChallengeControllerV2 ..> UpdateChallengeDto
ChallengeControllerV2 ..> ModerateChallengeDto
ChallengeCreatorController ..> ChallengeVersionDto
ChallengeCreatorController ..> CreatorChallengeDto
ChallengeDiscoveryController ..> PublicChallengeDto
ChallengeSubmissionController ..> SubmissionDto
SubmissionServiceImpl ..> SubmissionDto
SkillServiceImpl ..> SkillDto

%% Background jobs
ChallengeExpirationJob --> IChallengeRepository
SkillRecalculationJobService --> IUserSkillRepository

%% Notes
note left of ChallengeService : Contains core business flows: create, submit for review,
set active version, publish/approve/reject, mapping AI analysis to criteria/skills

note right of SubmissionServiceImpl : Handles participant submissions, grading orchestration,
skill point transactions and attempts tracking

```