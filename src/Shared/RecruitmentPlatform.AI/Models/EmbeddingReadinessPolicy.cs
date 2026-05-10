namespace RecruitmentPlatform.AI.Models;

public static class EmbeddingReadinessPolicy
{
    public const string Pending = "Pending";
    public const string Ready = "Ready";
    public const string Failed = "Failed";

    public static bool IsReady(string? status, float[] embedding)
    {
        return string.Equals(status, Ready, StringComparison.OrdinalIgnoreCase) && embedding.Length > 0;
    }

    public static string ResolveStatus(float[] embedding)
    {
        return embedding.Length > 0 ? Ready : Failed;
    }
}

public sealed class EmbeddingBackfillOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 120;
    public int BatchSize { get; set; } = 50;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 500;
}
