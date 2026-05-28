using System.Security.Cryptography;
using System.Text;

namespace Challenge.Application.Services;

public interface IPromptSanitizationService
{
    Task<string> SanitizePromptAsync(
        string rawPrompt,
        Guid versionId,
        CancellationToken cancellationToken = default);
}

public class PromptSanitizationService : IPromptSanitizationService
{
    private readonly ILogger<PromptSanitizationService> _logger;

    public PromptSanitizationService(ILogger<PromptSanitizationService> logger)
    {
        _logger = logger;
    }

    public Task<string> SanitizePromptAsync(
        string rawPrompt,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var sanitized = rawPrompt ?? string.Empty;
        var riskFlags = new List<string>();

        if (sanitized.Contains("{{") || sanitized.Contains("}}"))
        {
            riskFlags.Add("injection_attempt");
            sanitized = sanitized.Replace("{{", string.Empty).Replace("}}", string.Empty);
        }

        var dangerousKeywords = new[] { "ignore", "disregard", "role:", "system:", "simulate", "forget", "pretend" };
        foreach (var keyword in dangerousKeywords)
        {
            if (sanitized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                riskFlags.Add("system_prompt_keyword");
                break;
            }
        }

        if (sanitized.Length > 5000)
        {
            sanitized = sanitized[..5000];
            riskFlags.Add("truncated");
        }

        if (riskFlags.Count > 0)
        {
            _logger.LogInformation(
                "Sanitized prompt for version {VersionId}: {Flags}",
                versionId,
                string.Join(", ", riskFlags));
        }

        return Task.FromResult(sanitized);
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var hashedBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes);
    }
}
