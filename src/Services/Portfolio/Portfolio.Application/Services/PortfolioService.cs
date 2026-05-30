using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portfolio.Application.BlockHandlers;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models.Events;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;
using RecruitmentPlatform.AI.Services;
using RecruitmentPlatform.Contracts.Time;
using System.Text.Json;
using System.Transactions;

namespace Portfolio.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _repo;
    private readonly IPortfolioFollowRepository _followRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmployeeServiceClient _employeeClient;
    private readonly IAuthServiceClient _authServiceClient;
    private readonly IReviewerProfileClient _reviewerProfileClient;
    private readonly ICompanyMatchingClient _companyMatchingClient;
    private readonly IPortfolioEmbeddingEventPublisher _embeddingEventPublisher;
    private readonly IPortfolioModerationEventPublisher _moderationEventPublisher;
    private readonly IPortfolioNotificationEventPublisher _notificationEventPublisher;
    private readonly IMatchingEngine _matchingEngine;
    private readonly ITextNormalizer _textNormalizer;
    private readonly IEmbeddingService _embeddingService;
    private readonly ModerationService _moderationService;
    private readonly IFeatureVerificationService _featureVerificationService;
    private readonly IMemoryCache _cache;
    private readonly IEnumerable<IBlockHandler> _handlers;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPortfolioRepository repo,
        IPortfolioFollowRepository followRepository,
        ICurrentUserService currentUser,
        IEmployeeServiceClient employeeClient,
        IAuthServiceClient authServiceClient,
        IReviewerProfileClient reviewerProfileClient,
        ICompanyMatchingClient companyMatchingClient,
        IPortfolioEmbeddingEventPublisher embeddingEventPublisher,
        IPortfolioModerationEventPublisher moderationEventPublisher,
        IPortfolioNotificationEventPublisher notificationEventPublisher,
        IMatchingEngine matchingEngine,
        ITextNormalizer textNormalizer,
        IEmbeddingService embeddingService,
        ModerationService moderationService,
        IFeatureVerificationService featureVerificationService,
        IMemoryCache cache,
        IEnumerable<IBlockHandler> handlers,
        ILogger<PortfolioService> logger)
    {
        _repo = repo;
        _followRepository = followRepository;
        _currentUser = currentUser;
        _employeeClient = employeeClient;
        _authServiceClient = authServiceClient;
        _reviewerProfileClient = reviewerProfileClient;
        _companyMatchingClient = companyMatchingClient;
        _embeddingEventPublisher = embeddingEventPublisher;
        _moderationEventPublisher = moderationEventPublisher;
        _notificationEventPublisher = notificationEventPublisher;
        _matchingEngine = matchingEngine;
        _textNormalizer = textNormalizer;
        _embeddingService = embeddingService;
        _moderationService = moderationService;
        _featureVerificationService = featureVerificationService;
        _cache = cache;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<PortfolioDto?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        return p == null ? null : MapToDto(p);
    }

    public async Task<IEnumerable<PortfolioDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var list = await _repo.GetByEmployeeIdAsync(employeeId);
        return list.Select(MapToDto);
    }

    public async Task<PortfolioDto?> GetMainByEmployeeIdAsync(int employeeId)
    {
        var portfolio = await _repo.GetMainByEmployeeIdAsync(employeeId);
        return portfolio == null ? null : MapToDto(portfolio);
    }

    public async Task<PortfolioDto> CreateAsync(int employeeId, CreatePortfolioRequest request)
    {
        var valid = await _employeeClient.ValidateEmployeeAsync(employeeId);
        if (!valid)
            throw new KeyNotFoundException($"Employee {employeeId} not found");

        var currentCount = await _repo.CountByEmployeeIdAsync(employeeId);
        var userId = _currentUser.UserId;
        var canCreate = await _featureVerificationService.CanPerformActionAsync(userId, "MAX_PORTFOLIOS", currentCount);
        if (!canCreate)
        {
            _logger.LogWarning("User {UserId} exceeded MAX_PORTFOLIOS quota", userId);
            throw new InvalidOperationException("You have reached your portfolio limit. Upgrade your subscription to create more portfolios.");
        }

        var portfolio = new Domain.Entities.Portfolio
        {
            EmployeeId = employeeId,
            Name = request.Name,
            Status = "active",
            IsMain = request.IsMain,
            IsPublic = request.IsPublic,
            CreatedAt = VietnamTime.Now()
        };
        await ApplyModerationAndEmbeddingAsync(portfolio);

        var created = await _repo.CreateAsync(portfolio);
        await TryPublishEmbeddingEventAsync(created.Id);
        if (request.IsMain)
        {
            await _repo.SetMainPortfolioAsync(employeeId, created.Id);
            created.IsMain = true;
        }
        _logger.LogInformation("Portfolio {Id} created for employee {EmpId}", created.Id, employeeId);
        return MapToDto(created);
    }

    public async Task<PortfolioDto> UpdateAsync(int id, int employeeId, UpdatePortfolioRequest request)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        portfolio.Name = request.Name;
        portfolio.Status = request.Status;
        if (request.IsPublic.HasValue)
            portfolio.IsPublic = request.IsPublic.Value;
        if (request.IsMain.HasValue)
            portfolio.IsMain = request.IsMain.Value;
        portfolio.UpdatedAt = VietnamTime.Now();
        await ApplyModerationAndEmbeddingAsync(portfolio);

        if (request.IsMain == true)
            await _repo.SetMainPortfolioAsync(employeeId, portfolio.Id);

        var updated = await _repo.UpdateAsync(portfolio);
        await TryPublishEmbeddingEventAsync(updated.Id);
        return MapToDto(updated);
    }

    public async Task<PortfolioDto> ToggleMainAsync(int id, int employeeId)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        if (portfolio.IsMain)
        {
            portfolio.IsMain = false;
            portfolio.UpdatedAt = VietnamTime.Now();
            var updated = await _repo.UpdateAsync(portfolio);
            return MapToDto(updated);
        }

        await _repo.SetMainPortfolioAsync(employeeId, portfolio.Id);
        portfolio.IsMain = true;
        portfolio.UpdatedAt = VietnamTime.Now();
        var saved = await _repo.UpdateAsync(portfolio);
        return MapToDto(saved);
    }

    public async Task<PortfolioDto> TogglePublicAsync(int id, int employeeId)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        portfolio.IsPublic = !portfolio.IsPublic;
        portfolio.UpdatedAt = VietnamTime.Now();

        var updated = await _repo.UpdateAsync(portfolio);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id, int employeeId)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        return await _repo.DeleteAsync(id);
    }

    public async Task<PagedResult<PortfolioDto>> GetAllAsync(int page, int pageSize, string? status, string? searchTerm, string? blockType, PortfolioSortMode sort, PortfolioRankBy rankBy)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var normalizedBlockType = await ValidateAndNormalizeBlockTypeAsync(blockType);
        var (items, total, rankingMap) = await _repo.GetAllAsync(page, pageSize, status, searchTerm, normalizedBlockType, sort, rankBy);
        var mappedItems = items.Select(MapToDto).ToList();
        foreach (var item in mappedItems)
        {
            var hasRanking = rankingMap.TryGetValue(item.PortfolioId, out var ranking);
            item.Ranking = new RankingDto
            {
                TotalScore = hasRanking ? ranking.TotalScore : 0m,
                AverageScore = hasRanking ? ranking.AverageScore : 0m,
                RankPosition = hasRanking ? ranking.RankPosition : 0
            };
        }

        await PopulateReviewersAsync(mappedItems, mappedItems.Select(x => x.PortfolioId));
        await PopulateFollowStateAsync(mappedItems, mappedItems.Select(x => x.PortfolioId));

        return new PagedResult<PortfolioDto>
        {
            Items = mappedItems,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<PortfolioDto>> GetPendingPortfoliosAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _repo.GetPendingForModerationAsync(page, pageSize);
        var mappedItems = items.Select(MapToDto).ToList();
        await PopulateReviewersAsync(mappedItems, mappedItems.Select(x => x.PortfolioId));

        return new PagedResult<PortfolioDto>
        {
            Items = mappedItems,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PortfolioDto> ApprovePortfolioAsync(int portfolioId, int reviewerId, string actorRole, string? notes)
    {
        var portfolio = await _repo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        portfolio.ModerationStatus = "Approved";
        portfolio.ModerationReason = string.IsNullOrWhiteSpace(notes) ? "Approved by moderator" : notes.Trim();
        portfolio.ModeratedAt = VietnamTime.Now();
        portfolio.Status = "active";
        portfolio.IsPublic = true;
        portfolio.UpdatedAt = VietnamTime.Now();

        var updated = await _repo.UpdateAsync(portfolio);
        await TryPublishEmbeddingEventAsync(updated.Id);

        var normalizedActorRole = NormalizeActorRole(actorRole);
        var userId = updated.EmployeeId.ToString();

        var approveEvt = new Models.Events.PortfolioApprovedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "portfolio.approved",
            Version = 1,
            UserId = userId,
            ActorId = reviewerId.ToString(),
            ActorType = normalizedActorRole,
            ObjectId = updated.Id.ToString(),
            Title = "Your portfolio has been approved",
            Content = string.IsNullOrWhiteSpace(notes)
                ? "Your portfolio has been approved and is now live."
                : $"Your portfolio has been approved. Notes: {notes}",
            Type = "PORTFOLIO_APPROVED",
            CreatedAt = VietnamTime.Now()
        };
        await _moderationEventPublisher.PublishPortfolioApprovedNotificationAsync(approveEvt);

        var realtimeEvt = new RecruitmentPlatform.Contracts.Realtime.PostModerationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "portfolio.moderation",
            Version = 1,
            PostId = updated.Id,
            UserId = userId,
            Status = "APPROVED",
            Reason = portfolio.ModerationReason ?? "Approved by moderator",
            PostType = "Portfolio",
            Title = "Your portfolio has been approved",
            Content = approveEvt.Content,
            ActorId = reviewerId.ToString(),
            ActorType = normalizedActorRole,
            CreatedAt = VietnamTime.Now()
        };
        await _moderationEventPublisher.PublishPortfolioModerationEventAsync(realtimeEvt);

        return MapToDto(updated);
    }

    public async Task<PortfolioDto> RejectPortfolioAsync(int portfolioId, int reviewerId, string actorRole, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required");

        var portfolio = await _repo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        portfolio.ModerationStatus = "Rejected";
        portfolio.ModerationReason = reason.Trim();
        portfolio.ModeratedAt = VietnamTime.Now();
        portfolio.Status = "inactive";
        portfolio.IsPublic = false;
        portfolio.UpdatedAt = VietnamTime.Now();

        var updated = await _repo.UpdateAsync(portfolio);
        var normalizedActorRole = NormalizeActorRole(actorRole);
        var userId = updated.EmployeeId.ToString();

        var rejectEvt = new Models.Events.PortfolioRejectedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "portfolio.rejected",
            Version = 1,
            UserId = userId,
            ActorId = reviewerId.ToString(),
            ActorType = normalizedActorRole,
            ObjectId = updated.Id.ToString(),
            Title = "Your portfolio was rejected",
            Content = $"Your portfolio was rejected. Reason: {reason}",
            Type = "PORTFOLIO_REJECTED",
            CreatedAt = VietnamTime.Now()
        };
        await _moderationEventPublisher.PublishPortfolioRejectedNotificationAsync(rejectEvt);

        var realtimeEvt = new RecruitmentPlatform.Contracts.Realtime.PostModerationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "portfolio.moderation",
            Version = 1,
            PostId = updated.Id,
            UserId = userId,
            Status = "REJECTED",
            Reason = reason,
            PostType = "Portfolio",
            Title = "Your portfolio was rejected",
            Content = rejectEvt.Content,
            ActorId = reviewerId.ToString(),
            ActorType = normalizedActorRole,
            CreatedAt = VietnamTime.Now()
        };
        await _moderationEventPublisher.PublishPortfolioModerationEventAsync(realtimeEvt);

        return MapToDto(updated);
    }

    public async Task<CreatePortfolioResponse> UpdateFullPortfolioAsync(
        int id,
        int employeeId,
        UpdateFullPortfolioRequest request,
        Dictionary<string, IFormFile> fileMap)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var blockTypes = await _repo.GetBlockTypesAsync();
        var handlerMap = _handlers.ToDictionary(h => h.BlockType, StringComparer.OrdinalIgnoreCase);

        ValidateBlockMultiplicity(
            new CreatePortfolioRequest { Blocks = request.Blocks },
            blockTypes);

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        portfolio.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Status))
            portfolio.Status = request.Status;
        if (request.IsPublic.HasValue)
            portfolio.IsPublic = request.IsPublic.Value;
        if (request.IsMain.HasValue)
            portfolio.IsMain = request.IsMain.Value;
        portfolio.UpdatedAt = VietnamTime.Now();

        if (request.IsMain == true)
            await _repo.SetMainPortfolioAsync(employeeId, portfolio.Id);

        // Remove all existing blocks
        portfolio.Blocks.Clear();

        foreach (var blockRequest in request.Blocks.OrderBy(b => b.Order))
        {
            if (!blockTypes.TryGetValue(blockRequest.Type.ToUpperInvariant(), out var blockType))
                throw new ArgumentException($"Unknown block type: {blockRequest.Type}");

            if (!handlerMap.TryGetValue(blockRequest.Type, out var handler))
                throw new InvalidOperationException($"No handler registered for block type: {blockRequest.Type}");

            var block = new PortfolioBlock
            {
                Portfolio = portfolio,
                BlockTypeId = blockType.Id,
                Variant = blockRequest.Variant,
                DisplayOrder = blockRequest.Order,
                IsVisible = true
            };

            await handler.HandleAsync(block, blockRequest.Data, fileMap);
            portfolio.Blocks.Add(block);
        }

        // Entity already tracked by EF Core, just commit changes
        await ApplyModerationAndEmbeddingAsync(portfolio);
        await _repo.CommitAsync();
        await TryPublishEmbeddingEventAsync(portfolio.Id);

        scope.Complete();

        _logger.LogInformation("Portfolio {Id} fully updated with {BlockCount} blocks for employee {EmployeeId}",
            portfolio.Id, portfolio.Blocks.Count, employeeId);

        return new CreatePortfolioResponse { PortfolioId = portfolio.Id, Message = "Portfolio updated successfully" };
    }

    public async Task<CreatePortfolioResponse> CreatePortfolioAsync(
        CreatePortfolioRequest request,
        Dictionary<string, IFormFile> fileMap)
    {
        if (request.EmployeeId <= 0)
            throw new ArgumentException("EmployeeId is required");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        var blockTypes = await _repo.GetBlockTypesAsync();

        var handlerMap = _handlers.ToDictionary(h => h.BlockType, StringComparer.OrdinalIgnoreCase);

        ValidateBlockMultiplicity(request, blockTypes);

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var portfolio = new Domain.Entities.Portfolio
        {
            EmployeeId = request.EmployeeId,
            Name = request.Name,
            Status = "active",
            IsMain = request.IsMain,
            IsPublic = request.IsPublic,
            CreatedAt = VietnamTime.Now()
        };

        foreach (var blockRequest in request.Blocks.OrderBy(b => b.Order))
        {
            if (!blockTypes.TryGetValue(blockRequest.Type.ToUpperInvariant(), out var blockType))
                throw new ArgumentException($"Unknown block type: {blockRequest.Type}");

            if (!handlerMap.TryGetValue(blockRequest.Type, out var handler))
                throw new InvalidOperationException($"No handler registered for block type: {blockRequest.Type}");

            var block = new PortfolioBlock
            {
                Portfolio = portfolio,
                BlockTypeId = blockType.Id,
                Variant = blockRequest.Variant,
                DisplayOrder = blockRequest.Order,
                IsVisible = true
            };

            await handler.HandleAsync(block, blockRequest.Data, fileMap);

            portfolio.Blocks.Add(block);
        }

        await ApplyModerationAndEmbeddingAsync(portfolio);

        _repo.AddAsync(portfolio);
        await _repo.CommitAsync();
        await TryPublishEmbeddingEventAsync(portfolio.Id);
        
        // Publish moderation notifications
        var moderationStatus = portfolio.ModerationStatus ?? "Approved";
        var userId = request.EmployeeId.ToString();

        if (string.Equals(moderationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            var rejectEvt = new Models.Events.PortfolioRejectedNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.rejected",
                Version = 1,
                UserId = userId,
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                ObjectId = portfolio.Id.ToString(),
                Title = "Your portfolio was rejected",
                Content = $"Your portfolio was rejected. Reason: {portfolio.ModerationReason}",
                Type = "PORTFOLIO_REJECTED",
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioRejectedNotificationAsync(rejectEvt);

            // Publish realtime event
            var realtimeEvt = new RecruitmentPlatform.Contracts.Realtime.PostModerationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.moderation",
                Version = 1,
                PostId = portfolio.Id,
                UserId = userId,
                Status = "REJECTED",
                Reason = portfolio.ModerationReason ?? "Portfolio does not meet quality standards",
                PostType = "Portfolio",
                Title = "Your portfolio was rejected",
                Content = $"Your portfolio was rejected. Reason: {portfolio.ModerationReason}",
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioModerationEventAsync(realtimeEvt);
        }
        else if (string.Equals(moderationStatus, "PendingReview", StringComparison.OrdinalIgnoreCase))
        {
            var pendingEvt = new Models.Events.PortfolioPendingReviewNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.pending.review",
                Version = 1,
                UserId = userId,
                ActorId = null,
                ActorType = "SYSTEM",
                ObjectId = portfolio.Id.ToString(),
                Title = "Your portfolio is under review",
                Content = $"Your portfolio is pending manual review. Reason: {portfolio.ModerationReason}",
                Type = "PORTFOLIO_PENDING_REVIEW",
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioPendingReviewNotificationAsync(pendingEvt);

            var triageEvt = new Models.Events.PortfolioPendingReviewNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.pending.review",
                Version = 1,
                UserId = string.Empty,
                ActorId = null,
                ActorType = "SYSTEM",
                ObjectId = portfolio.Id.ToString(),
                Title = "Portfolio chờ duyệt thủ công",
                Content = $"Portfolio #{portfolio.Id} cần admin/moderator xem xét. Lý do: {portfolio.ModerationReason}",
                Type = "PORTFOLIO_PENDING_REVIEW",
                TargetRoles = new[] { "ADMIN", "MODERATOR" },
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioPendingReviewNotificationAsync(triageEvt);

            // Publish realtime event
            var realtimeEvt = new RecruitmentPlatform.Contracts.Realtime.PostModerationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.moderation",
                Version = 1,
                PostId = portfolio.Id,
                UserId = userId,
                Status = "PENDING_REVIEW",
                Reason = portfolio.ModerationReason ?? "Portfolio pending manual review",
                PostType = "Portfolio",
                Title = "Your portfolio is under review",
                Content = $"Your portfolio is pending manual review. Reason: {portfolio.ModerationReason}",
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioModerationEventAsync(realtimeEvt);
        }
        else
        {
            var approveEvt = new Models.Events.PortfolioApprovedNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.approved",
                Version = 1,
                UserId = userId,
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                ObjectId = portfolio.Id.ToString(),
                Title = "Your portfolio has been approved",
                Content = "Your portfolio has been approved and is now live.",
                Type = "PORTFOLIO_APPROVED",
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioApprovedNotificationAsync(approveEvt);

            // Publish realtime event
            var realtimeEvt = new RecruitmentPlatform.Contracts.Realtime.PostModerationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "portfolio.moderation",
                Version = 1,
                PostId = portfolio.Id,
                UserId = userId,
                Status = "APPROVED",
                Reason = "Portfolio approved",
                PostType = "Portfolio",
                Title = "Your portfolio has been approved",
                Content = "Your portfolio has been approved and is now live.",
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                CreatedAt = VietnamTime.Now()
            };

            await _moderationEventPublisher.PublishPortfolioModerationEventAsync(realtimeEvt);
        }

        if (request.IsMain)
        {
            await _repo.SetMainPortfolioAsync(request.EmployeeId, portfolio.Id);
            portfolio.IsMain = true;
        }

        scope.Complete();

        _logger.LogInformation("Portfolio {Id} created with {BlockCount} blocks for employee {EmployeeId}",
            portfolio.Id, portfolio.Blocks.Count, request.EmployeeId);

        return new CreatePortfolioResponse 
        { 
            PortfolioId = portfolio.Id, 
            Message = "Portfolio created successfully",
            ModerationStatus = portfolio.ModerationStatus,
            ModerationReason = portfolio.ModerationReason
        };
    }

    private static void ValidateBlockMultiplicity(
        CreatePortfolioRequest request,
        Dictionary<string, BlockType> blockTypes)
    {
        var typeCounts = request.Blocks
            .GroupBy(b => b.Type.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (typeCode, count) in typeCounts)
        {
            if (blockTypes.TryGetValue(typeCode, out var bt) && !bt.IsMultiple && count > 1)
                throw new ArgumentException($"Block type '{typeCode}' does not allow multiple blocks (IsMultiple=false).");
        }
    }

    private static PortfolioDto MapToDto(Domain.Entities.Portfolio p) => new()
    {
        PortfolioId = p.Id,
        EmployeeId = p.EmployeeId,
        PortfolioName = p.Name,
        Status = p.Status,
        ModerationStatus = p.ModerationStatus,
        ModerationReason = p.ModerationReason,
        ModeratedAt = p.ModeratedAt,
        IsMain = p.IsMain,
        IsPublic = p.IsPublic,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Blocks = new List<BlockDto>()
    };

    private async Task PopulateReviewersAsync(List<PortfolioDto> items, IEnumerable<int> portfolioIds)
    {
        if (items.Count == 0) return;

        var reviewerUserIdsByPortfolio = await _repo.GetReviewerUserIdsByPortfolioIdsAsync(portfolioIds);
        var allUserIds = reviewerUserIdsByPortfolio.Values.SelectMany(x => x).Distinct().ToList();
        if (allUserIds.Count == 0) return;

        var authUsers = await _authServiceClient.GetUsersByIdsAsync(allUserIds);
        var recruiterUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "RECRUITER", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();
        var expertUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "EXPERT", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();

        var recruiterProfiles = await _reviewerProfileClient.GetCompanyProfilesByUserIdsAsync(recruiterUserIds);
        var expertProfiles = await _reviewerProfileClient.GetExpertProfilesByUserIdsAsync(expertUserIds);

        foreach (var item in items)
        {
            if (!reviewerUserIdsByPortfolio.TryGetValue(item.PortfolioId, out var userIds))
            {
                item.Reviewers = new List<PortfolioReviewerDto>();
                continue;
            }

            item.Reviewers = BuildReviewers(userIds, authUsers, recruiterProfiles, expertProfiles);
        }
    }

    private async Task PopulateReviewersAsync(List<PortfolioWithComplimentDto> items, IEnumerable<int> portfolioIds)
    {
        if (items.Count == 0) return;

        var reviewerUserIdsByPortfolio = await _repo.GetReviewerUserIdsByPortfolioIdsAsync(portfolioIds);
        var allUserIds = reviewerUserIdsByPortfolio.Values.SelectMany(x => x).Distinct().ToList();
        if (allUserIds.Count == 0) return;

        var authUsers = await _authServiceClient.GetUsersByIdsAsync(allUserIds);
        var recruiterUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "RECRUITER", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();
        var expertUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "EXPERT", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();

        var recruiterProfiles = await _reviewerProfileClient.GetCompanyProfilesByUserIdsAsync(recruiterUserIds);
        var expertProfiles = await _reviewerProfileClient.GetExpertProfilesByUserIdsAsync(expertUserIds);

        foreach (var item in items)
        {
            if (!reviewerUserIdsByPortfolio.TryGetValue(item.Id, out var userIds))
            {
                item.Reviewers = new List<PortfolioReviewerDto>();
                continue;
            }

            item.Reviewers = BuildReviewers(userIds, authUsers, recruiterProfiles, expertProfiles);
        }
    }

    private static List<PortfolioReviewerDto> BuildReviewers(
        IEnumerable<int> userIds,
        Dictionary<int, AuthInternalUserDto> authUsers,
        Dictionary<int, ReviewerProfileDto> recruiterProfiles,
        Dictionary<int, ReviewerProfileDto> expertProfiles)
    {
        return userIds
            .Select(userId =>
            {
                if (!authUsers.TryGetValue(userId, out var authUser))
                {
                    return null;
                }

                var role = authUser.Role;
                if (!string.Equals(role, "RECRUITER", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(role, "EXPERT", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                ReviewerProfileDto? profile = null;
                if (string.Equals(role, "RECRUITER", StringComparison.OrdinalIgnoreCase))
                {
                    recruiterProfiles.TryGetValue(userId, out profile);
                }
                else
                {
                    expertProfiles.TryGetValue(userId, out profile);
                }

                return new PortfolioReviewerDto
                {
                    UserId = userId,
                    Role = role,
                    Name = profile?.Name,
                    Avatar = profile?.Avatar
                };
            })
            .Where(x => x != null)
            .Cast<PortfolioReviewerDto>()
            .ToList();
    }

    public async Task<PagedResult<PortfolioWithComplimentDto>> GetAllWithComplimentFilterAsync(PortfolioQueryParams queryParams)
    {
        if (queryParams.Page < 1) queryParams.Page = 1;
        if (queryParams.PageSize < 1) queryParams.PageSize = 10;
        if (queryParams.PageSize > 100) queryParams.PageSize = 100;
        queryParams.BlockType = await ValidateAndNormalizeBlockTypeAsync(queryParams.BlockType);

        var (items, total) = await _repo.GetAllWithComplimentFilterAsync(queryParams);
        await PopulateReviewersAsync(items, items.Select(x => x.Id));
        await PopulateFollowStateAsync(items, items.Select(x => x.Id));
        return new PagedResult<PortfolioWithComplimentDto>
        {
            Items = items,
            Total = total,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    private async Task<string?> ValidateAndNormalizeBlockTypeAsync(string? blockType)
    {
        if (string.IsNullOrWhiteSpace(blockType))
        {
            return null;
        }

        var normalized = blockType.Trim().ToUpperInvariant();
        var blockTypes = await _repo.GetBlockTypesAsync();
        if (!blockTypes.ContainsKey(normalized))
        {
            throw new ArgumentException($"BlockType '{blockType}' is invalid.");
        }

        return normalized;
    }

    private async Task PopulateFollowStateAsync(List<PortfolioDto> items, IEnumerable<int> portfolioIds)
    {
        if (items.Count == 0)
        {
            return;
        }

        if (!_currentUser.HasCompany || _currentUser.CompanyId <= 0)
        {
            foreach (var item in items)
            {
                item.IsFollowed = false;
            }

            return;
        }

        var followedIds = await _followRepository.GetFollowedPortfolioIdsAsync(_currentUser.CompanyId, portfolioIds);
        foreach (var item in items)
        {
            item.IsFollowed = followedIds.Contains(item.PortfolioId);
        }
    }

    private async Task PopulateFollowStateAsync(List<PortfolioWithComplimentDto> items, IEnumerable<int> portfolioIds)
    {
        if (items.Count == 0)
        {
            return;
        }

        if (!_currentUser.HasCompany || _currentUser.CompanyId <= 0)
        {
            foreach (var item in items)
            {
                item.IsFollowed = false;
            }

            return;
        }

        var followedIds = await _followRepository.GetFollowedPortfolioIdsAsync(_currentUser.CompanyId, portfolioIds);
        foreach (var item in items)
        {
            item.IsFollowed = followedIds.Contains(item.Id);
        }
    }

    public async Task<JobMatchPagedResult> MatchJobsForPortfolioAsync(int portfolioId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (safePage, safePageSize) = NormalizeMatchPaging(page, pageSize);
        var hasAiMatching = await _featureVerificationService.HasFeatureAccessAsync(_currentUser.UserId, "AI_MATCHING");
        if (!hasAiMatching)
        {
            throw new UnauthorizedAccessException("gói hiện tại không được sử dụng AI");
        }

        var portfolio = await _repo.GetByIdAsync(portfolioId);
        if (portfolio == null)
        {
            return new JobMatchPagedResult { Page = safePage, PageSize = safePageSize };
        }

        var sourceEmbedding = ParseEmbedding(portfolio.Embedding);
        if (!EmbeddingReadinessPolicy.IsReady(portfolio.EmbeddingStatus, sourceEmbedding))
        {
            return new JobMatchPagedResult { Page = safePage, PageSize = safePageSize };
        }

        var cacheKey = $"portfolio:{portfolio.Id}:{portfolio.EmbeddingVersion}:matched-jobs:{safePage}:{safePageSize}";
        if (_cache.TryGetValue(cacheKey, out JobMatchPagedResult? cached) && cached != null)
        {
            return cached;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var candidates = await _companyMatchingClient.GetJobCandidatesAsync(cts.Token);

        var sourceSkills = ExtractPortfolioSkills(portfolio);
        var request = new MatchingRequest
        {
            SourceId = portfolio.Id,
            SourceTitle = portfolio.Name,
            SourceDescription = BuildPortfolioDescription(portfolio),
            SourceSkills = sourceSkills,
            SourceCategories = Array.Empty<string>().ToList(),
            SourceEmbedding = sourceEmbedding,
            SourceEmbeddingVersion = portfolio.EmbeddingVersion
        };

        var matches = _matchingEngine.Match(request, candidates, safePage, safePageSize);

        // Enrich with job post details
        var matchedIds = matches.Items.Select(x => x.Id).ToList();
        var postDetails = await _companyMatchingClient.GetPostsByIdsAsync(matchedIds, cancellationToken);
        var postLookup = postDetails.ToDictionary(p => p.PostId);

        var result = new JobMatchPagedResult
        {
            Total = matches.Total,
            Page = matches.Page,
            PageSize = matches.PageSize,
            Items = matches.Items.Select(x =>
            {
                postLookup.TryGetValue(x.Id, out var detail);
                return new JobMatchResultDto
                {
                    // Matching scores
                    PostId = x.Id,
                    Title = detail?.Position ?? x.Title,
                    Cosine = x.Cosine,
                    SkillScore = x.SkillScore,
                    CategoryScore = x.CategoryScore,
                    FinalScore = x.FinalScore,
                    // Post detail fields
                    CompanyName = detail?.CompanyName,
                    CompanyAvatar = detail?.CompanyAvatar,
                    CoverImageUrl = detail?.CoverImageUrl,
                    MediaType = detail?.MediaType,
                    MediaUrl = detail?.MediaUrl,
                    Address = detail?.Address,
                    Salary = detail?.Salary,
                    EmploymentType = detail?.EmploymentType,
                    ExperienceYear = detail?.ExperienceYear,
                    Quantity = detail?.Quantity,
                    JobDescription = detail?.JobDescription,
                    RequirementsMandatory = detail?.RequirementsMandatory,
                    RequirementsPreferred = detail?.RequirementsPreferred,
                    Benefits = detail?.Benefits,
                    Status = detail?.Status ?? 0,
                    CreatedAt = detail?.CreatedAt ?? default,
                    IsSaved = detail?.IsSaved ?? false,
                    ReviewStatus = detail?.ReviewStatus,
                    ReviewReason = detail?.ReviewReason
                };
            }).ToList()
        };

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<MatchingCandidateFeed> GetMatchingCandidatesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var portfolios = await _repo.GetPublicPortfoliosForMatchingAsync(limit);
        var items = portfolios
            .Select(portfolio =>
            {
                var embedding = ParseEmbedding(portfolio.Embedding);
                return new MatchingCandidate
                {
                    Id = portfolio.Id,
                    Title = portfolio.Name,
                    Description = BuildPortfolioDescription(portfolio),
                    Skills = ExtractPortfolioSkills(portfolio),
                    Categories = new List<string>(),
                    Embedding = embedding,
                    EmbeddingVersion = portfolio.EmbeddingVersion,
                    EmbeddingStatus = portfolio.EmbeddingStatus,
                    UpdatedAt = portfolio.EmbeddingUpdatedAt ?? portfolio.UpdatedAt ?? portfolio.CreatedAt
                };
            })
            .Where(candidate => candidate.Embedding.Length > 0)
            .ToList();

        return new MatchingCandidateFeed { Items = items };
    }

    private async Task ApplyModerationAndEmbeddingAsync(Domain.Entities.Portfolio portfolio)
    {
        var description = BuildPortfolioDescription(portfolio);
        var hasProject = portfolio.Blocks.Any(x => x.BlockTypeId == 6);
        var moderation = _moderationService.Check(description, hasProject);
        portfolio.ModerationStatus = moderation.Status;
        portfolio.ModerationReason = moderation.Reason;
        portfolio.ModeratedAt = VietnamTime.Now();
        var isApproved = string.Equals(moderation.Status, "Approved", StringComparison.OrdinalIgnoreCase);
        portfolio.Status = isApproved ? "active" : "inactive";

        if (!isApproved)
        {
            portfolio.IsPublic = false;
            portfolio.EmbeddingStatus = EmbeddingReadinessPolicy.Failed;
            return;
        }

        portfolio.EmbeddingStatus = EmbeddingReadinessPolicy.Pending;
        var input = new EmbeddingTextInput
        {
            Title = portfolio.Name,
            Description = description,
            Skills = ExtractPortfolioSkills(portfolio),
            Categories = Array.Empty<string>(),
            Projects = portfolio.Blocks.Where(x => x.BlockTypeId == 6).Select(x => x.DataJson).ToList(),
            CustomFields = portfolio.Blocks.Select(x => x.DataJson).Take(5).ToList()
        };
        var text = _textNormalizer.BuildPortfolioText(input);

        try
        {
            var embedding = await _embeddingService.CreateEmbeddingAsync(text);
            portfolio.Embedding = JsonSerializer.Serialize(embedding);
            portfolio.EmbeddingVersion = Math.Max(1, portfolio.EmbeddingVersion + 1);
            portfolio.EmbeddingUpdatedAt = VietnamTime.Now();
            portfolio.EmbeddingStatus = EmbeddingReadinessPolicy.ResolveStatus(embedding);
        }
        catch (Exception ex)
        {
            portfolio.EmbeddingStatus = EmbeddingReadinessPolicy.Failed;
            _logger.LogWarning(ex, "Failed to generate embedding for portfolio {PortfolioId}", portfolio.Id);
        }
    }

    private static string BuildPortfolioDescription(Domain.Entities.Portfolio portfolio)
    {
        var pieces = portfolio.Blocks
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.DataJson)
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(" ", pieces);
    }

    private static List<string> ExtractPortfolioSkills(Domain.Entities.Portfolio portfolio)
    {
        return portfolio.Blocks
            .Where(x => x.BlockTypeId == 2)
            .SelectMany(x => x.DataJson.Split([',', ';', '\n', '\r', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static float[] ParseEmbedding(string? embeddingJson)
    {
        if (string.IsNullOrWhiteSpace(embeddingJson))
        {
            return Array.Empty<float>();
        }

        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? Array.Empty<float>();
        }
        catch
        {
            return Array.Empty<float>();
        }
    }

    private static (int Page, int PageSize) NormalizeMatchPaging(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 50);
        return (safePage, safePageSize);
    }

    private static string NormalizeActorRole(string actorRole)
    {
        if (string.Equals(actorRole, "MODERATOR", StringComparison.OrdinalIgnoreCase))
        {
            return "MODERATOR";
        }

        return "ADMIN";
    }

    private async Task TryPublishEmbeddingEventAsync(int portfolioId)
    {
        try
        {
            await _embeddingEventPublisher.PublishPortfolioChangedAsync(portfolioId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish embedding event for portfolio {PortfolioId}", portfolioId);
        }
    }

    public async Task<PortfolioReportDto> ReportPortfolioAsync(int portfolioId, int reporterUserId, CreatePortfolioReportRequest request)
    {
        if (reporterUserId <= 0) throw new ArgumentException("Invalid reporter user ID");
        
        var portfolio = await _repo.GetByIdAsync(portfolioId);
        if (portfolio == null) throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 100)
            throw new ArgumentException("Reason is required and must not exceed 100 characters");

        if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
            throw new ArgumentException("Description must not exceed 1000 characters");

        var existingReport = await _repo.GetPortfolioReportByIdAndReporterAsync(portfolioId, reporterUserId);
        if (existingReport != null)
            throw new InvalidOperationException("You have already reported this portfolio");

        var report = new PortfolioReport
        {
            PortfolioId = portfolioId,
            ReporterUserId = reporterUserId,
            Reason = request.Reason,
            Description = request.Description,
            Status = PortfolioReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreatePortfolioReportAsync(report);

        var @event = new PortfolioReportCreatedNotificationEvent
        {
            PortfolioId = portfolioId,
            ReporterUserId = reporterUserId,
            Reason = request.Reason,
            Description = request.Description,
            Title = $"Portfolio {portfolioId} has been reported",
            Content = $"Portfolio has been reported for: {request.Reason}"
        };

        try
        {
            await _notificationEventPublisher.PublishReportCreatedAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish report created event for portfolio {PortfolioId}", portfolioId);
        }

        return MapToDto(created);
    }

    public async Task<PagedResult<PortfolioReportDto>> GetPortfolioReportsAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _repo.GetPortfolioReportsAsync(page, pageSize);
        return new PagedResult<PortfolioReportDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private PortfolioReportDto MapToDto(PortfolioReport report)
    {
        return new PortfolioReportDto
        {
            Id = report.Id,
            PortfolioId = report.PortfolioId,
            ReporterUserId = report.ReporterUserId,
            Reason = report.Reason,
            Description = report.Description,
            Status = (int)report.Status,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedAt = report.ReviewedAt,
            ReviewNote = report.ReviewNote,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    public async Task<List<PortfolioSummaryDto>> GetPortfoliosByIdsAsync(IEnumerable<int> ids)
    {
        var portfolios = await _repo.GetPortfoliosByIdsAsync(ids);
        return portfolios.Select(p => new PortfolioSummaryDto
        {
            PortfolioId = p.Id,
            EmployeeId = p.EmployeeId,
            Name = p.Name,
            Status = p.Status,
            ModerationStatus = p.ModerationStatus ?? string.Empty,
            IsMain = p.IsMain,
            IsPublic = p.IsPublic,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }

    public async Task<PortfolioReportDto> ReviewPortfolioReportAsync(int reportId, int reviewerUserId, ReviewPortfolioReportRequest request)
    {
        var report = await _repo.GetPortfolioReportByIdAsync(reportId)
            ?? throw new KeyNotFoundException($"Report {reportId} not found");

        if (report.Status != PortfolioReportStatus.Pending)
        {
            throw new InvalidOperationException("This report has already been reviewed.");
        }

        var action = request.Action?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.");
        }

        var now = VietnamTime.Now();
        report.ReviewedByUserId = reviewerUserId;
        report.ReviewedAt = now;
        report.ReviewNote = request.ReviewNote?.Trim();
        report.UpdatedAt = now;

        if (action == "approve_violation")
        {
            report.Status = PortfolioReportStatus.Approved;

            if (report.Portfolio != null && string.Equals(report.Portfolio.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                report.Portfolio.Status = "inactive";
                report.Portfolio.IsPublic = false;
                report.Portfolio.ModerationStatus = "RemovedByModeration";
                report.Portfolio.ModeratedAt = now;
                report.Portfolio.UpdatedAt = now;
                await _repo.UpdateAsync(report.Portfolio);
                try
                {
                    await _embeddingEventPublisher.PublishPortfolioChangedAsync(report.Portfolio.Id);
                }
                catch { /* swallow embedding publish errors */ }

                var realtimeEvt = new RecruitmentPlatform.Contracts.Realtime.PostModerationEvent
                {
                    EventId = Guid.NewGuid().ToString("N"),
                    EventType = "portfolio.moderation",
                    Version = 1,
                    PostId = report.PortfolioId,
                    UserId = report.Portfolio.EmployeeId.ToString(),
                    Status = "REMOVED",
                    Reason = report.ReviewNote ?? "Removed by moderation",
                    PostType = "Portfolio",
                    Title = "Your portfolio was removed due to violation",
                    Content = "Your portfolio has been removed due to violation of policies.",
                    ActorId = reviewerUserId.ToString(),
                    ActorType = "ADMIN",
                    CreatedAt = now
                };

                await _moderationEventPublisher.PublishPortfolioModerationEventAsync(realtimeEvt);
            }
        }
        else if (action == "reject")
        {
            report.Status = PortfolioReportStatus.Rejected;
        }
        else
        {
            throw new ArgumentException("Action must be one of: approve_violation, reject.");
        }

        await _repo.UpdatePortfolioReportAsync(report);

        return MapToDto(report);
    }
}
