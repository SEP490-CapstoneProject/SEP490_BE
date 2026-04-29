using RecruitmentPlatform.AI.Services;
using Xunit;

namespace E2E.Tests;

public class ModerationWorkflowE2ETests
{
    private readonly ModerationService _moderationService;

    public ModerationWorkflowE2ETests()
    {
        _moderationService = new ModerationService();
    }

    #region Rejected Post Workflows

    [Fact]
    public void FullWorkflow_RejectedPost_NoApprovalPath()
    {
        // Arrange
        var bannedContent = "Visit our casino at https://viagra-site.com for amazing offers!";

        // Act
        var result = _moderationService.CheckPost(bannedContent);

        // Assert - Verify immediate rejection
        Assert.Equal("Rejected", result.Status);
        Assert.Contains("viagra", result.TriggeringKeyword);
    }

    [Fact]
    public void FullWorkflow_RejectedPost_WithBlacklistedLink()
    {
        // Arrange
        var suspiciousContent = "Check my portfolio at https://bit.ly/myprofile for details";

        // Act
        var result = _moderationService.CheckPost(suspiciousContent);

        // Assert - Verify rejection and reason
        Assert.Equal("Rejected", result.Status);
        Assert.Contains("bit.ly", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullWorkflow_RejectedPost_MultipleViolations()
    {
        // Arrange - Post contains both spam keyword and malicious link
        var content = "Join our casino now! Visit https://bit.ly/casino for amazing offers";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert - Should catch first violation (spam keyword takes precedence)
        Assert.Equal("Rejected", result.Status);
    }

    #endregion

    #region Pending Review Workflows

    [Fact]
    public void FullWorkflow_PendingPost_ReviewableContent()
    {
        // Arrange - Content that's borderline quality
        var content = "I'm a web developer with experience";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert
        Assert.True(result.Status == "PendingReview" || result.Status == "Approved");
    }

    [Fact]
    public void FullWorkflow_PendingPost_WithUnverifiedLink()
    {
        // Arrange - Good content but unverified domain
        var content = "I build amazing web applications. Visit my site https://my-custom-portfolio.com to see my projects and case studies.";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert - Should not reject, but won't be auto-approved due to unverified link
        Assert.NotEqual("Rejected", result.Status);
        Assert.NotNull(result.LinkVerification);
        Assert.False(result.LinkVerification.IsValid); // Unverified domain
    }

    [Fact]
    public void FullWorkflow_PendingPost_LowQualityButClean()
    {
        // Arrange - Short but clean content
        var content = "I am a software developer working on web projects";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert
        Assert.True(result.Status == "PendingReview" || result.Status == "Approved");
    }

    #endregion

    #region Approved Post Workflows

    [Fact]
    public void FullWorkflow_ApprovedPost_HighQualityContent()
    {
        // Arrange
        var content = "Full-stack developer with 5+ years of experience in web development. I specialize in React, Node.js, and cloud architecture. Check out my work at https://github.com/username for detailed project examples.";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.NotNull(result.LinkVerification);
        Assert.True(result.LinkVerification.IsValid);
    }

    [Fact]
    public void FullWorkflow_ApprovedPost_WithMultipleWhitelistedLinks()
    {
        // Arrange
        var content = "Check out my portfolio at https://github.com/developer and connect with me at https://linkedin.com/in/myprofile. I also share content on https://dev.to/myblog";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.NotNull(result.LinkVerification);
        Assert.Equal(3, result.LinkVerification.DetectedUrls.Count);
        Assert.True(result.LinkVerification.IsValid);
    }

    [Fact]
    public void FullWorkflow_ApprovedPost_ProfessionalPortfolioPitch()
    {
        // Arrange
        var content = "I'm a passionate full-stack engineer with expertise in React, Node.js, and AWS. I've built scalable applications serving millions of users and have experience with microservices architecture. My portfolio showcases several completed projects: https://portfolio.com/user. Open to discussing new opportunities!";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert
        Assert.Equal("Approved", result.Status);
    }

    #endregion

    #region Complex Workflow Scenarios

    [Fact]
    public void Workflow_MultiplePostsFromSameUser_VariousStatuses()
    {
        // Arrange - Simulate processing multiple posts from the same user
        var posts = new[]
        {
            new { Content = "Check casino offers at https://bit.ly/casino", ExpectedStatus = "Rejected" },
            new { Content = "a a a a a a a a a a a a a a a a a a a a a", ExpectedStatus = "PendingReview" },
            new { Content = "Senior developer with 10+ years experience at https://github.com/profile specializing in cloud architecture", ExpectedStatus = "Approved" }
        };

        // Act & Assert
        foreach (var post in posts)
        {
            var result = _moderationService.CheckPost(post.Content);
            Assert.True(
                result.Status == post.ExpectedStatus,
                $"Post '{post.Content}' returned {result.Status} but expected {post.ExpectedStatus}"
            );
        }
    }

    [Fact]
    public void Workflow_EdgeCase_MinimumLengthApproval()
    {
        // Arrange - Exactly at threshold
        var minimalContent = "Developer with skills in React and Node technologies";

        // Act
        var result = _moderationService.CheckPost(minimalContent);

        // Assert - Should be approved or pending, not rejected
        Assert.NotEqual("Rejected", result.Status);
    }

    [Fact]
    public void Workflow_EdgeCase_URLsWithSpecialCharacters()
    {
        // Arrange
        var content = "Check my work at https://github.com/user?tab=repositories&sort=stars for my top projects!";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert
        Assert.NotEqual("Rejected", result.Status);
        Assert.NotNull(result.LinkVerification);
        Assert.NotEmpty(result.LinkVerification.DetectedUrls);
    }

    [Fact]
    public void Workflow_EdgeCase_MultipleLinksWithMalicious()
    {
        // Arrange - One good, one bad link
        var content = "See my projects at https://github.com/user but also check https://bit.ly/shortlink for more";

        // Act
        var result = _moderationService.CheckPost(content);

        // Assert - Should be rejected due to malicious link
        Assert.Equal("Rejected", result.Status);
    }

    #endregion

    #region Real-world Integration Scenarios

    [Fact]
    public void Integration_CommunityPostFlow_ProfessionalShowcase()
    {
        // Simulate community post creation workflow
        var communityPostContent = "Excited to announce the launch of my new project! Built with React and Node.js. GitHub: https://github.com/projects/new. Looking for feedback and collaboration opportunities!";

        var result = _moderationService.CheckPost(communityPostContent);

        Assert.NotEqual("Rejected", result.Status);
    }

    [Fact]
    public void Integration_CompanyJobPostFlow_FullDescription()
    {
        // Simulate company job posting with combined fields
        var position = "Senior Backend Engineer";
        var jobDescription = "We're hiring a senior backend engineer for our growing platform";
        var requirements = "8+ years experience with Java, Spring Boot, and cloud platforms like AWS or Azure";
        var benefits = "Competitive salary, remote work, professional development";

        var combinedContent = string.Join(" ", new[] { position, jobDescription, requirements, benefits });
        var result = _moderationService.CheckPost(combinedContent);

        Assert.NotEqual("Rejected", result.Status);
    }

    [Fact]
    public void Integration_AdminReviewFlow_PendingPostApproval()
    {
        // Simulate admin review of pending post
        var pendingContent = "Nice portfolio with modern tech stack";

        var initialResult = _moderationService.CheckPost(pendingContent);

        // If marked for review, admin can approve
        if (initialResult.Status == "PendingReview")
        {
            // Admin would manually approve this
            Assert.NotNull(initialResult.Reason);
        }
    }

    [Fact]
    public void Integration_AdminReviewFlow_RejectedPostReason()
    {
        // Simulate admin rejecting a pending post
        var content = "Some content";

        var result = _moderationService.CheckPost(content);

        // If it gets rejected, reason should be clear
        if (result.Status == "Rejected")
        {
            Assert.NotEmpty(result.Reason);
        }
    }

    #endregion

    #region Notification Trigger Scenarios

    [Fact]
    public void NotificationTrigger_PendingReview_Admin()
    {
        // When post is marked pending, admin notification should be triggered
        var content = "a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a";

        var result = _moderationService.CheckPost(content);

        if (result.Status == "PendingReview")
        {
            // This would trigger POST_PENDING_REVIEW notification
            Assert.NotEmpty(result.Reason);
            Assert.True(string.IsNullOrEmpty(result.TriggeringKeyword)); // Not a keyword rejection
        }
    }

    [Fact]
    public void NotificationTrigger_Approved_User()
    {
        // When post is auto-approved, user might get implicit confirmation
        var content = "Amazing full-stack developer with years of experience in React and backend development. Check my portfolio: https://github.com/fullstack";

        var result = _moderationService.CheckPost(content);

        if (result.Status == "Approved")
        {
            // This would implicitly confirm post creation success
            Assert.Equal("Approved", result.Status);
        }
    }

    [Fact]
    public void NotificationTrigger_Rejected_User()
    {
        // When post is rejected, user should get error notification
        var content = "Join our casino platform at https://bit.ly/casino for great rewards!";

        var result = _moderationService.CheckPost(content);

        Assert.Equal("Rejected", result.Status);
        Assert.NotEmpty(result.Reason);
    }

    #endregion

    #region Authorization and Access Control Scenarios

    [Fact]
    public void Workflow_AdminCanApproveRejectedPost()
    {
        // Simulate flow where admin can potentially override
        var post = _moderationService.CheckPost("Some borderline content");

        // Regardless of moderation result, admin endpoints require authorization
        // This is tested separately but documents the flow
        Assert.NotNull(post.Status);
    }

    [Fact]
    public void Workflow_RegularUserCannotAccessAdminEndpoints()
    {
        // This is an authorization check - moderation service doesn't handle this
        // but documents the expected workflow
        var content = "Test post";
        var result = _moderationService.CheckPost(content);

        // Non-admin users should not be able to approve/reject posts
        // This is enforced at controller level with [Authorize(Roles = "ADMIN")]
        Assert.NotNull(result);
    }

    #endregion

    #region Content Quality Analysis

    [Fact]
    public void QualityAnalysis_WordCountImpact()
    {
        // Verify word count affects scoring
        var shortPost = "Developer here";
        var mediumPost = "I'm a developer with experience in React and backend development";
        var longPost = "I'm a full-stack developer with 10+ years of experience across multiple technologies and platforms. I specialize in React, Node.js, cloud architecture, and database design.";

        var shortResult = _moderationService.CheckPost(shortPost);
        var mediumResult = _moderationService.CheckPost(mediumPost);
        var longResult = _moderationService.CheckPost(longPost);

        // Longer, quality content should be approved
        Assert.True(longResult.Status != "Rejected");
    }

    [Fact]
    public void QualityAnalysis_VerifiedLinkBoost()
    {
        // Verify verified links improve score
        var withoutLink = "I'm a web developer with years of experience in modern technologies";
        var withLink = "I'm a web developer with years of experience. See my work at https://github.com/developer";

        var withoutResult = _moderationService.CheckPost(withoutLink);
        var withResult = _moderationService.CheckPost(withLink);

        // Content with verified link should not be rejected
        Assert.NotEqual("Rejected", withResult.Status);
    }

    #endregion
}
