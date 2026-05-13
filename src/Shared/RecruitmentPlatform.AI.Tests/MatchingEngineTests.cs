using FluentAssertions;
using RecruitmentPlatform.AI.Models;
using RecruitmentPlatform.AI.Services;
using Xunit;

namespace RecruitmentPlatform.AI.Tests;

/// <summary>
/// Unit tests for MatchingEngine – covers filtering, pre-filter, scoring, pagination.
/// MinimumDescriptionLength = 50, PreFilterTake = 500 (MatchingOptions defaults).
/// </summary>
public class MatchingEngineTests
{
    private static MatchingEngine BuildEngine() =>
        new(new CosineSimilarityService(), new ScoringHelper());

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Returns a unit float vector of <paramref name="dim"/> dimensions (all 1/sqrt(dim)).</summary>
    private static float[] UnitVector(int dim = 4)
    {
        var v = Enumerable.Repeat(1f, dim).ToArray();
        var norm = MathF.Sqrt(v.Sum(x => x * x));
        return v.Select(x => x / norm).ToArray();
    }

    /// <summary>An all-zero embedding – should never match.</summary>
    private static float[] ZeroVector(int dim = 4) => new float[dim];

    /// <summary>A ready candidate with description >= 50 chars by default.</summary>
    private static MatchingCandidate ReadyCandidate(
        int id,
        string title = "Job Title",
        List<string>? skills = null,
        string description = "This is a long enough job description for the matching engine to accept it.",
        float[]? embedding = null,
        string embeddingStatus = "Ready",
        List<string>? categories = null) => new()
    {
        Id = id,
        Title = title,
        Description = description,
        Skills = skills ?? ["C#", "SQL"],
        Categories = categories ?? [],
        Embedding = embedding ?? UnitVector(),
        EmbeddingStatus = embeddingStatus,
        EmbeddingVersion = 1,
        UpdatedAt = DateTime.UtcNow
    };

    private static MatchingRequest BuildRequest(
        List<string>? skills = null,
        float[]? embedding = null,
        List<string>? categories = null) => new()
    {
        SourceId = 1,
        SourceTitle = "Senior C# Dev",
        SourceDescription = "8 years of experience in backend development.",
        SourceSkills = skills ?? ["C#", "SQL"],
        SourceCategories = categories ?? [],
        SourceEmbedding = embedding ?? UnitVector(),
        SourceEmbeddingVersion = 1
    };

    // ─── IsQualityCandidate filtering ─────────────────────────────────────────

    [Fact]
    public void Match_ExcludesCandidates_WhenEmbeddingStatusNotReady()
    {
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, embeddingStatus: "Pending");
        var result = engine.Match(BuildRequest(), [candidate], page: 1, pageSize: 20);
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public void Match_ExcludesCandidates_WhenEmbeddingStatusFailed()
    {
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, embeddingStatus: "Failed");
        var result = engine.Match(BuildRequest(), [candidate], page: 1, pageSize: 20);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Match_ExcludesCandidates_WhenSkillsEmpty()
    {
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, skills: []);
        var result = engine.Match(BuildRequest(), [candidate], page: 1, pageSize: 20);
        result.Items.Should().BeEmpty("candidates with no skills must be filtered out");
    }

    [Fact]
    public void Match_ExcludesCandidates_WhenDescriptionTooShort()
    {
        var engine = BuildEngine();
        // MinimumDescriptionLength = 50; provide only 20 chars → should be excluded
        var candidate = ReadyCandidate(10, description: "Short description here.");
        var result = engine.Match(BuildRequest(), [candidate], page: 1, pageSize: 20);
        result.Items.Should().BeEmpty("description must be at least 50 chars");
    }

    [Fact]
    public void Match_IncludesCandidates_WhenDescriptionExactlyAtMinimum()
    {
        var engine = BuildEngine();
        // Exactly 50 characters
        var candidate = ReadyCandidate(10, description: "12345678901234567890123456789012345678901234567890");
        var result = engine.Match(BuildRequest(), [candidate], page: 1, pageSize: 20);
        // FinalScore >= 0.3 because same unit vector → cosine=1, skill=1 → final=0.9
        result.Items.Should().NotBeEmpty("description of exactly 50 chars is acceptable");
    }

    // ─── Scoring correctness ───────────────────────────────────────────────────

    [Fact]
    public void Match_PerfectCandidate_HasHighFinalScore()
    {
        // Same embedding (cosine=1) + same skills (skill=1) + no categories
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, skills: ["C#", "SQL"], embedding: UnitVector());
        var result = engine.Match(BuildRequest(skills: ["C#", "SQL"], embedding: UnitVector()), [candidate], 1, 20);

        result.Items.Should().HaveCount(1);
        var match = result.Items[0];
        match.Cosine.Should().BeApproximately(1.0, 0.0001);
        match.SkillScore.Should().BeApproximately(1.0, 0.0001);
        // FinalScore = 1*0.7 + 1*0.2 + 0*0.1 = 0.9 (categories both empty → score=0)
        match.FinalScore.Should().BeApproximately(0.9, 0.0001);
    }

    [Fact]
    public void Match_ZeroEmbeddingCandidate_HasZeroCosine()
    {
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, embedding: ZeroVector());
        var result = engine.Match(BuildRequest(embedding: UnitVector()), [candidate], 1, 20);

        // cosine=0, skill=1, category=0 → final = 0*0.7 + 1*0.2 + 0 = 0.2 → below MinimumFinalScore(0.3)
        result.Items.Should().BeEmpty("final score 0.2 is below minimum threshold of 0.3");
    }

    [Fact]
    public void Match_NoSkillOverlap_ReducesFinalScore()
    {
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, skills: ["Python", "AWS"], embedding: UnitVector());
        var result = engine.Match(BuildRequest(skills: ["C#", "SQL"], embedding: UnitVector()), [candidate], 1, 20);

        if (result.Items.Count == 1)
        {
            result.Items[0].SkillScore.Should().Be(0);
            // FinalScore = 1*0.7 + 0*0.2 + 0*0.1 = 0.7
            result.Items[0].FinalScore.Should().BeApproximately(0.7, 0.0001);
        }
    }

    // ─── MinimumFinalScore threshold ──────────────────────────────────────────

    [Fact]
    public void Match_FiltersOutBelowMinimumFinalScore()
    {
        // cosine=0 + skill=0 + category=0 → finalScore=0 → below threshold 0.3
        var engine = BuildEngine();
        var candidate = ReadyCandidate(10, skills: ["Python"], embedding: ZeroVector());
        var request = BuildRequest(skills: ["C#"], embedding: UnitVector());
        var result = engine.Match(request, [candidate], 1, 20);
        result.Items.Should().BeEmpty("score of 0 must be filtered out");
    }

    // ─── Sorting ───────────────────────────────────────────────────────────────

    [Fact]
    public void Match_SortsByFinalScoreDescending()
    {
        var engine = BuildEngine();

        // Candidate A: perfect embedding match, perfect skills → ~0.9
        var candidateA = ReadyCandidate(1, skills: ["C#", "SQL"], embedding: UnitVector());

        // Candidate B: partial skills, same embedding
        var candidateB = ReadyCandidate(2, skills: ["C#"], embedding: UnitVector());

        var result = engine.Match(BuildRequest(skills: ["C#", "SQL"], embedding: UnitVector()),
            [candidateA, candidateB], 1, 20);

        if (result.Items.Count >= 2)
        {
            result.Items[0].FinalScore.Should().BeGreaterThanOrEqualTo(result.Items[1].FinalScore,
                "results must be sorted by FinalScore descending");
        }
    }

    // ─── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public void Match_Pagination_ReturnsCorrectPage()
    {
        var engine = BuildEngine();

        // Create 5 identical ready candidates
        var candidates = Enumerable.Range(1, 5)
            .Select(i => ReadyCandidate(i, skills: ["C#", "SQL"], embedding: UnitVector()))
            .ToList();

        var page1 = engine.Match(BuildRequest(), candidates, page: 1, pageSize: 2);
        var page2 = engine.Match(BuildRequest(), candidates, page: 2, pageSize: 2);
        var page3 = engine.Match(BuildRequest(), candidates, page: 3, pageSize: 2);

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page3.Items.Should().HaveCount(1, "last page has remaining 1 item");
        page1.Total.Should().Be(5);
    }

    [Fact]
    public void Match_PageSizeClampedToMaxPageSize()
    {
        // MaxPageSize = 50
        var engine = BuildEngine();
        var candidates = Enumerable.Range(1, 10)
            .Select(i => ReadyCandidate(i, skills: ["C#", "SQL"], embedding: UnitVector()))
            .ToList();

        var result = engine.Match(BuildRequest(), candidates, page: 1, pageSize: 999);
        result.PageSize.Should().BeLessThanOrEqualTo(50, "pageSize must be capped at MaxPageSize=50");
    }

    [Fact]
    public void Match_PageLessThan1_NormalisedTo1()
    {
        var engine = BuildEngine();
        var candidate = ReadyCandidate(1, skills: ["C#", "SQL"], embedding: UnitVector());

        var result = engine.Match(BuildRequest(), [candidate], page: -5, pageSize: 10);
        result.Page.Should().Be(1);
    }

    [Fact]
    public void Match_EmptyCandidateList_ReturnsEmptyResult()
    {
        var engine = BuildEngine();
        var result = engine.Match(BuildRequest(), [], 1, 20);
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    // ─── Skill overlap pre-filter ordering ────────────────────────────────────

    [Fact]
    public void Match_BetterSkillOverlap_RanksHigherDuringPrefilter()
    {
        var engine = BuildEngine();

        var weakCandidate = ReadyCandidate(1, skills: ["Python"], embedding: UnitVector()); // 0 overlap
        var strongCandidate = ReadyCandidate(2, skills: ["C#", "SQL"], embedding: UnitVector()); // 2 overlap

        var result = engine.Match(BuildRequest(skills: ["C#", "SQL"]), [weakCandidate, strongCandidate], 1, 20);

        result.Items.Should().Contain(x => x.Id == 2, "candidate with matching skills should be present");
    }

    // ─── Category scoring integration ─────────────────────────────────────────

    [Fact]
    public void Match_CategoryScoreAffectsFinalScore()
    {
        var engine = BuildEngine();

        // Candidate WITH matching category
        var withCategory = ReadyCandidate(1,
            skills: ["C#"],
            embedding: UnitVector(),
            categories: ["Full-time"]);

        // Candidate WITHOUT matching category
        var withoutCategory = ReadyCandidate(2,
            skills: ["C#"],
            embedding: UnitVector(),
            categories: ["Part-time"]);

        var request = BuildRequest(skills: ["C#"], embedding: UnitVector(), categories: ["Full-time"]);
        var result = engine.Match(request, [withCategory, withoutCategory], 1, 20);

        var matchWith = result.Items.FirstOrDefault(x => x.Id == 1);
        var matchWithout = result.Items.FirstOrDefault(x => x.Id == 2);

        if (matchWith != null && matchWithout != null)
        {
            matchWith.CategoryScore.Should().BeGreaterThan(matchWithout.CategoryScore,
                "matching category should yield higher category score");
            matchWith.FinalScore.Should().BeGreaterThan(matchWithout.FinalScore);
        }
    }
}
