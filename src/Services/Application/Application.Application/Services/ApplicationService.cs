using Application.Application.DTOs;
using Application.Application.Interfaces;
using Application.Domain.Entities;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Contracts.Time;

namespace Application.Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _repo;
    private readonly IUserProfileClient _userProfileClient;
    private readonly ICurrentUserService _currentUser;
    private readonly IFeatureVerificationService _featureVerificationService;
    private readonly IApplicationNotificationEventPublisher _notificationEventPublisher;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        IApplicationRepository repo,
        IUserProfileClient userProfileClient,
        ICurrentUserService currentUser,
        IFeatureVerificationService featureVerificationService,
        IApplicationNotificationEventPublisher notificationEventPublisher,
        ILogger<ApplicationService> logger)
    {
        _repo = repo;
        _userProfileClient = userProfileClient;
        _currentUser = currentUser;
        _featureVerificationService = featureVerificationService;
        _notificationEventPublisher = notificationEventPublisher;
        _logger = logger;
    }

    public async Task<ApplicationDto> CreateApplicationAsync(CreateApplicationRequest request)
    {
        var employeeId = _currentUser.GetEmployeeId();
        var userId = _currentUser.GetUserId();

        try
        {
            // Validate employee exists
            var employee = await _userProfileClient.GetEmployeeByIdAsync(employeeId);
            if (employee == null)
            {
                throw new KeyNotFoundException($"Employee {employeeId} not found");
            }

            // Validate post exists and get CompanyId
            var post = await _userProfileClient.GetCompanyPostByIdAsync(request.CompanyPostId);
            if (post == null)
            {
                throw new KeyNotFoundException($"Company post {request.CompanyPostId} not found");
            }

            // Validate portfolio ownership
            var ownsPortfolio = await _userProfileClient.ValidatePortfolioOwnershipAsync(employeeId, request.PortfolioId);
            if (!ownsPortfolio)
            {
                throw new UnauthorizedAccessException($"Portfolio {request.PortfolioId} does not belong to employee {employeeId}");
            }

            // Check duplicate
            var exists = await _repo.ExistsByEmployeeAndPostAsync(employeeId, request.CompanyPostId);
            if (exists)
            {
                throw new InvalidOperationException("You have already applied to this position");
            }

            var currentCount = await _repo.CountByEmployeeIdAsync(employeeId);
            var canApply = await _featureVerificationService.CanPerformActionAsync(userId, "MAX_APPLY", currentCount);
            if (!canApply)
            {
                _logger.LogWarning("User {UserId} exceeded MAX_APPLY quota", userId);
                throw new InvalidOperationException("You have reached your application limit. Upgrade your subscription to apply to more jobs.");
            }

            var application = new Domain.Entities.Application
            {
                EmployeeId = employeeId,
                CompanyId = post.CompanyId,
                CompanyPostId = request.CompanyPostId,
                PortfolioId = request.PortfolioId,
                Status = ApplicationStatus.WAITING,
                AppliedAt = VietnamTime.Now(),
                CreatedAt = VietnamTime.Now()
            };

            var created = await _repo.CreateAsync(application);
            _logger.LogInformation("Application {Id} created by employee {EmployeeId} for post {PostId}",
                created.ApplicationId, employeeId, request.CompanyPostId);

            await TryPublishCreatedNotificationAsync(created, employee, post);

            // Map to DTO
            var company = await _userProfileClient.GetCompanyByIdAsync(post.CompanyId);
            return new ApplicationDto
            {
                ApplicationId = created.ApplicationId,
                Status = created.Status.ToString(),
                AppliedAt = created.AppliedAt.ToString("MM/yyyy"),
                Post = MapPostDto(post),
                Company = MapCompanyDto(company)
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<PagedResult<ApplicationDto>> GetMyApplicationsAsync(int page, int pageSize)
    {
        var employeeId = _currentUser.GetEmployeeId();
        var (applications, total) = await _repo.GetByEmployeeIdPagedAsync(employeeId, page, pageSize);

        if (!applications.Any()) 
            return new PagedResult<ApplicationDto> { Items = new(), Total = 0, Page = page, PageSize = pageSize };

        // Batch fetch
        var companyIds = applications.Select(a => a.CompanyId).Distinct().ToList();
        var postIds = applications.Select(a => a.CompanyPostId).Distinct().ToList();

        var companies = await _userProfileClient.GetCompaniesByIdsAsync(companyIds);
        var posts = await _userProfileClient.GetPostsByIdsAsync(postIds);

        var items = applications.Select(a => new ApplicationDto
        {
            ApplicationId = a.ApplicationId,
            Status = a.Status.ToString(),
            AppliedAt = a.AppliedAt.ToString("MM/yyyy"),
            Post = posts.TryGetValue(a.CompanyPostId, out var post) ? MapPostDto(post) : new PostDto(),
            Company = companies.TryGetValue(a.CompanyId, out var company) ? MapCompanyDto(company) : new CompanyDto()
        }).ToList();

        return new PagedResult<ApplicationDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<ApplicationManagerDto>> GetCompanyApplicationsAsync(int page, int pageSize)
    {
        var companyId = _currentUser.GetCompanyId();
        var (applications, total) = await _repo.GetByCompanyIdPagedAsync(companyId, page, pageSize);

        if (!applications.Any()) 
            return new PagedResult<ApplicationManagerDto> { Items = new(), Total = 0, Page = page, PageSize = pageSize };

        // Batch fetch
        var employeeIds = applications.Select(a => a.EmployeeId).Distinct().ToList();
        var postIds = applications.Select(a => a.CompanyPostId).Distinct().ToList();

        var employees = await _userProfileClient.GetEmployeesByIdsAsync(employeeIds);
        var posts = await _userProfileClient.GetPostsByIdsAsync(postIds);

        var items = applications.Select(a => new ApplicationManagerDto
        {
            ApplicationId = a.ApplicationId,
            Status = a.Status == ApplicationStatus.WAITING ? "NEW" : a.Status.ToString(),
            AppliedAt = a.AppliedAt,
            PortfolioId = a.PortfolioId,
            RoomId = a.RoomId,
            Candidate = employees.TryGetValue(a.EmployeeId, out var emp) ? MapCandidateDto(emp) : new CandidateDto(),
            Post = posts.TryGetValue(a.CompanyPostId, out var post) ? MapPostDto(post) : new PostDto()
        }).ToList();

        return new PagedResult<ApplicationManagerDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ApplicationDto> UpdateApplicationStatusAsync(int id, UpdateApplicationStatusRequest request)
    {
        var companyId = _currentUser.GetCompanyId();
        var application = await _repo.GetByIdAsync(id);

        if (application == null)
            throw new KeyNotFoundException($"Application {id} not found");

        if (application.CompanyId != companyId)
            throw new UnauthorizedAccessException("You do not own this application");

        // Validate status transition
        if (application.Status == ApplicationStatus.ACCEPTED || application.Status == ApplicationStatus.REJECTED)
            throw new InvalidOperationException("Cannot change status of accepted/rejected application");

        if (request.Status == ApplicationStatus.WAITING)
            throw new InvalidOperationException("Cannot revert to WAITING status");

        application.Status = request.Status;
        application.UpdatedAt = VietnamTime.Now();

        var updated = await _repo.UpdateAsync(application);

        // Enrich
        var candidate = await _userProfileClient.GetEmployeeByIdAsync(updated.EmployeeId);
        var post = await _userProfileClient.GetCompanyPostByIdAsync(updated.CompanyPostId);
        var company = await _userProfileClient.GetCompanyByIdAsync(updated.CompanyId);

        await TryPublishStatusUpdatedNotificationAsync(updated, candidate, post);

        return new ApplicationDto
        {
            ApplicationId = updated.ApplicationId,
            Status = updated.Status.ToString(),
            AppliedAt = updated.AppliedAt.ToString("MM/yyyy"),
            Post = MapPostDto(post),
            Company = MapCompanyDto(company)
        };
    }

    public async Task<ApplicationDto> GetApplicationByIdAsync(int id)
    {
        var application = await _repo.GetByIdAsync(id);
        if (application == null)
            throw new KeyNotFoundException($"Application {id} not found");

        // Ownership check
        var isEmployee = _currentUser.IsEmployee();
        var isCompany = _currentUser.IsCompany();

        if (isEmployee && application.EmployeeId != _currentUser.GetEmployeeId())
            throw new UnauthorizedAccessException("You do not have access to this application");

        if (isCompany && application.CompanyId != _currentUser.GetCompanyId())
            throw new UnauthorizedAccessException("You do not have access to this application");

        // Enrich
        var post = await _userProfileClient.GetCompanyPostByIdAsync(application.CompanyPostId);
        var company = await _userProfileClient.GetCompanyByIdAsync(application.CompanyId);

        return new ApplicationDto
        {
            ApplicationId = application.ApplicationId,
            Status = application.Status.ToString(),
            AppliedAt = application.AppliedAt.ToString("MM/yyyy"),
            Post = MapPostDto(post),
            Company = MapCompanyDto(company)
        };
    }

    private static PostDto MapPostDto(CompanyPostDto? post) => post == null ? new PostDto() : new PostDto
    {
        PostId = post.PostId,
        Position = post.Position,
        Salary = post.Salary,
        Address = post.Address,
        Media = post.Media
    };

    private static CompanyDto MapCompanyDto(CompanyExternalDto? company) => company == null ? new CompanyDto() : new CompanyDto
    {
        CompanyId = company.CompanyId,
        CompanyName = !string.IsNullOrWhiteSpace(company.CompanyName) ? company.CompanyName : company.Name,
        Logo = company.Logo
    };

    private static CandidateDto MapCandidateDto(EmployeeDto? employee) => employee == null ? new CandidateDto() : new CandidateDto
    {
        UserId = employee.UserId,
        Name = employee.Name,
        Avatar = employee.Avatar
    };

    private async Task TryPublishCreatedNotificationAsync(
        Domain.Entities.Application created,
        EmployeeDto employee,
        CompanyPostDto post)
    {
        // EVENT 1: Notify employee of their application submission
        var employeeEvent = new ApplicationNotificationEventPayload
        {
            EventType = "job.application.created",
            UserId = employee.UserId.ToString(),
            ActorId = _currentUser.GetUserId().ToString(),
            ActorType = "USER",
            ObjectId = created.ApplicationId.ToString(),
            Title = "Ứng tuyển thành công",
            Content = $"Bạn đã ứng tuyển thành công vị trí {post.Position}.",
            Type = "APPLICATION_SUBMITTED",
            Author = new NotificationActorDto { Name = employee.Name, Avatar = employee.Avatar },
            CreatedAt = VietnamTime.Now()
        };

        try
        {
            await _notificationEventPublisher.PublishAsync(employeeEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish application created notification to employee. ApplicationId={ApplicationId}", created.ApplicationId);
        }

        // EVENT 2: Notify company of new application (NEW)
        var company = await _userProfileClient.GetCompanyByIdAsync(post.CompanyId);
        if (company != null)
        {
            var companyEvent = new ApplicationNotificationEventPayload
            {
                EventType = "job.application.received",
                UserId = company.CompanyId.ToString(),
                ActorId = employee.UserId.ToString(),
                ActorType = "USER",
                ObjectId = created.ApplicationId.ToString(),
                Title = "Ứng tuyển mới",
                Content = $"{employee.Name} đã ứng tuyển vị trí {post.Position}.",
                Type = "APPLICATION_RECEIVED",
                Author = new NotificationActorDto { Name = employee.Name, Avatar = employee.Avatar },
                CreatedAt = VietnamTime.Now()
            };

            try
            {
                await _notificationEventPublisher.PublishAsync(companyEvent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish application received notification to company. ApplicationId={ApplicationId}, CompanyId={CompanyId}", created.ApplicationId, company.CompanyId);
            }
        }
    }

    private async Task TryPublishStatusUpdatedNotificationAsync(
        Domain.Entities.Application updated,
        EmployeeDto? candidate,
        CompanyPostDto? post)
    {
        if (updated.Status != ApplicationStatus.ACCEPTED && updated.Status != ApplicationStatus.REJECTED)
        {
            return;
        }

        if (candidate is null || candidate.UserId <= 0)
        {
            return;
        }

        var statusType = updated.Status == ApplicationStatus.ACCEPTED
            ? "APPLICATION_APPROVED"
            : "APPLICATION_REJECTED";
        var title = updated.Status == ApplicationStatus.ACCEPTED
            ? "Đơn ứng tuyển đã được duyệt"
            : "Đơn ứng tuyển đã bị từ chối";
        var content = updated.Status == ApplicationStatus.ACCEPTED
            ? $"Đơn ứng tuyển vị trí {post?.Position ?? "đã ứng tuyển"} của bạn đã được duyệt."
            : $"Đơn ứng tuyển vị trí {post?.Position ?? "đã ứng tuyển"} của bạn đã bị từ chối.";

        var eventPayload = new ApplicationNotificationEventPayload
        {
            EventType = "job.application.status.updated",
            UserId = candidate.UserId.ToString(),
            ActorId = _currentUser.GetUserId().ToString(),
            ActorType = "RECRUITER",
            ObjectId = updated.ApplicationId.ToString(),
            Title = title,
            Content = content,
            Type = statusType,
            Author = new NotificationActorDto { Name = _currentUser.GetUserId().ToString() },
            CreatedAt = VietnamTime.Now()
        };

        try
        {
            await _notificationEventPublisher.PublishAsync(eventPayload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish application status notification. ApplicationId={ApplicationId}", updated.ApplicationId);
        }
    }
}
