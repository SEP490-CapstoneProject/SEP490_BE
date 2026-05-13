using FluentAssertions;
using RecruitmentPlatform.AI.Services;
using Xunit;

namespace RecruitmentPlatform.AI.Tests;

/// <summary>
/// Unit tests for CosineSimilarityService
/// </summary>
public class CosineSimilarityServiceTests
{
    private readonly CosineSimilarityService _sut = new();

    // ─── Null / Empty guards ───────────────────────────────────────────────────

    [Fact]
    public void CosineSimilaritySafe_WhenBothNull_ReturnsZero()
    {
        _sut.CosineSimilaritySafe(null, null).Should().Be(0);
    }

    [Fact]
    public void CosineSimilaritySafe_WhenFirstNull_ReturnsZero()
    {
        _sut.CosineSimilaritySafe(null, [1f, 2f]).Should().Be(0);
    }

    [Fact]
    public void CosineSimilaritySafe_WhenSecondNull_ReturnsZero()
    {
        _sut.CosineSimilaritySafe([1f, 2f], null).Should().Be(0);
    }

    [Fact]
    public void CosineSimilaritySafe_WhenBothEmpty_ReturnsZero()
    {
        _sut.CosineSimilaritySafe([], []).Should().Be(0);
    }

    [Fact]
    public void CosineSimilaritySafe_WhenDifferentLengths_ReturnsZero()
    {
        _sut.CosineSimilaritySafe([1f, 2f], [1f, 2f, 3f]).Should().Be(0);
    }

    // ─── Mathematical correctness ──────────────────────────────────────────────

    [Fact]
    public void CosineSimilaritySafe_IdenticalVectors_ReturnsOne()
    {
        // cos(A, A) == 1
        float[] v = [1f, 2f, 3f];
        _sut.CosineSimilaritySafe(v, v).Should().BeApproximately(1.0, precision: 0.0001);
    }

    [Fact]
    public void CosineSimilaritySafe_OrthogonalVectors_ReturnsZero()
    {
        // [1,0] ⊥ [0,1]  → cosine = 0
        _sut.CosineSimilaritySafe([1f, 0f], [0f, 1f]).Should().BeApproximately(0.0, precision: 0.0001);
    }

    [Fact]
    public void CosineSimilaritySafe_OppositeVectors_ReturnsZeroClamped()
    {
        // [-1,0] vs [1,0] → cosine = -1, but clamped to 0
        _sut.CosineSimilaritySafe([-1f, 0f], [1f, 0f]).Should().Be(0);
    }

    [Fact]
    public void CosineSimilaritySafe_KnownAngle45Deg_ReturnsApprox07071()
    {
        // [1,0] vs [1,1]/sqrt2 → cos(45°) ≈ 0.7071
        float[] a = [1f, 0f];
        float[] b = [1f, 1f]; // not normalised – formula handles it
        var result = _sut.CosineSimilaritySafe(a, b);
        result.Should().BeApproximately(0.7071, precision: 0.001);
    }

    [Fact]
    public void CosineSimilaritySafe_AllZeroVector_ReturnsZero()
    {
        // normA == 0 → divide-by-zero guard
        _sut.CosineSimilaritySafe([0f, 0f, 0f], [1f, 2f, 3f]).Should().Be(0);
    }

    [Fact]
    public void CosineSimilaritySafe_ResultAlwaysBetween0And1()
    {
        var rng = new Random(42);
        for (var i = 0; i < 200; i++)
        {
            var a = Enumerable.Range(0, 10).Select(_ => (float)(rng.NextDouble() * 2 - 1)).ToArray();
            var b = Enumerable.Range(0, 10).Select(_ => (float)(rng.NextDouble() * 2 - 1)).ToArray();
            var score = _sut.CosineSimilaritySafe(a, b);
            score.Should().BeInRange(0, 1, because: "cosine similarity must be clamped to [0,1]");
        }
    }
}

/// <summary>
/// Unit tests for ScoringHelper
/// </summary>
public class ScoringHelperTests
{
    private readonly ScoringHelper _sut = new();

    // ─── ComputeSkillScore ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeSkillScore_WhenRequiredIsEmpty_ReturnsZero()
    {
        _sut.ComputeSkillScore([], ["C#", "SQL"]).Should().Be(0);
    }

    [Fact]
    public void ComputeSkillScore_WhenCandidateIsEmpty_ReturnsZero()
    {
        _sut.ComputeSkillScore(["C#", "SQL"], []).Should().Be(0);
    }

    [Fact]
    public void ComputeSkillScore_PerfectMatch_ReturnsOne()
    {
        _sut.ComputeSkillScore(["C#", "SQL", "Azure"], ["C#", "SQL", "Azure"]).Should().Be(1);
    }

    [Fact]
    public void ComputeSkillScore_NoOverlap_ReturnsZero()
    {
        _sut.ComputeSkillScore(["C#", "Azure"], ["Python", "AWS"]).Should().Be(0);
    }

    [Fact]
    public void ComputeSkillScore_HalfMatch_ReturnsHalf()
    {
        // Required: C#, ASP.NET, SQL, Azure → candidate has C#, SQL → 2/4 = 0.5
        var score = _sut.ComputeSkillScore(["C#", "ASP.NET", "SQL", "Azure"], ["C#", "SQL", "React"]);
        score.Should().BeApproximately(0.5, precision: 0.0001);
    }

    [Fact]
    public void ComputeSkillScore_IsCaseInsensitive()
    {
        _sut.ComputeSkillScore(["C#", "sql"], ["c#", "SQL"]).Should().Be(1);
    }

    [Fact]
    public void ComputeSkillScore_DuplicatesInRequired_CountedOnce()
    {
        var score = _sut.ComputeSkillScore(["C#", "C#"], ["C#"]);
        score.Should().Be(1);
    }

    // ─── ComputeCategoryScore ──────────────────────────────────────────────────

    [Fact]
    public void ComputeCategoryScore_WhenRequiredIsEmpty_ReturnsZero()
    {
        _sut.ComputeCategoryScore([], ["Full-time"]).Should().Be(0);
    }

    [Fact]
    public void ComputeCategoryScore_SingleRequiredMatched_ReturnsOne()
    {
        _sut.ComputeCategoryScore(["Full-time"], ["Full-time", "Remote"]).Should().Be(1);
    }

    [Fact]
    public void ComputeCategoryScore_SingleRequiredNotMatched_ReturnsZero()
    {
        _sut.ComputeCategoryScore(["Full-time"], ["Part-time"]).Should().Be(0);
    }

    [Fact]
    public void ComputeCategoryScore_MultiplePartialMatch_ReturnsRatio()
    {
        // Required: Full-time, HCMC → candidate: Full-time, Hanoi → 1/2 = 0.5
        var score = _sut.ComputeCategoryScore(["Full-time", "Ho Chi Minh City"], ["Full-time", "Hanoi"]);
        score.Should().BeApproximately(0.5, precision: 0.0001);
    }

    [Fact]
    public void ComputeCategoryScore_IsCaseInsensitive()
    {
        _sut.ComputeCategoryScore(["FULL-TIME"], ["full-time"]).Should().Be(1);
    }

    // ─── ComputeFinalScore ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeFinalScore_AllPerfect_ReturnsOne()
    {
        _sut.ComputeFinalScore(1.0, 1.0, 1.0).Should().Be(1.0);
    }

    [Fact]
    public void ComputeFinalScore_AllZero_ReturnsZero()
    {
        _sut.ComputeFinalScore(0, 0, 0).Should().Be(0);
    }

    [Fact]
    public void ComputeFinalScore_CorrectWeighting()
    {
        // FinalScore = cosine*0.7 + skill*0.2 + category*0.1
        // = 0.85*0.7 + 0.80*0.2 + 1.0*0.1 = 0.595 + 0.16 + 0.10 = 0.855
        var score = _sut.ComputeFinalScore(0.85, 0.80, 1.0);
        score.Should().BeApproximately(0.855, precision: 0.0001);
    }

    [Fact]
    public void ComputeFinalScore_ClampedToOneWhenInputsExceed1()
    {
        _sut.ComputeFinalScore(2.0, 2.0, 2.0).Should().Be(1.0);
    }

    [Fact]
    public void ComputeFinalScore_NegativeInputsClamped()
    {
        _sut.ComputeFinalScore(-1, -1, -1).Should().Be(0);
    }

    [Fact]
    public void ComputeFinalScore_OnlyCosineContributes()
    {
        // skill=0, category=0 → finalScore = cosine*0.7
        var score = _sut.ComputeFinalScore(1.0, 0, 0);
        score.Should().BeApproximately(0.7, precision: 0.0001);
    }

    [Fact]
    public void ComputeFinalScore_RoundedTo4DecimalPlaces()
    {
        // 0.8 * 0.7 + 0.9 * 0.2 + 0.5 * 0.1 = 0.56 + 0.18 + 0.05 = 0.79
        var score = _sut.ComputeFinalScore(0.8, 0.9, 0.5);
        score.Should().Be(0.79);
    }
}
