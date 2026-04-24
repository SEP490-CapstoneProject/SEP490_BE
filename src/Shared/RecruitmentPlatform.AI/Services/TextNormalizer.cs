using System.Text;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;

namespace RecruitmentPlatform.AI.Services;

public sealed class TextNormalizer : ITextNormalizer
{
    public string BuildPortfolioText(EmbeddingTextInput input)
    {
        return BuildText("Portfolio", input);
    }

    public string BuildJobText(EmbeddingTextInput input)
    {
        return BuildText("Job", input);
    }

    private static string BuildText(string prefix, EmbeddingTextInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{prefix}Title: {NormalizeField(input.Title)}");
        sb.AppendLine($"{prefix}Description: {NormalizeField(input.Description)}");
        sb.AppendLine("Skills: " + NormalizeCollection(input.Skills));
        sb.AppendLine("Categories: " + NormalizeCollection(input.Categories));
        sb.AppendLine("Projects: " + NormalizeCollection(input.Projects));
        sb.AppendLine("Custom: " + NormalizeCollection(input.CustomFields));
        return sb.ToString();
    }

    private static string NormalizeField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeCollection(IReadOnlyCollection<string>? values)
    {
        if (values == null || values.Count == 0)
        {
            return "N/A";
        }

        return string.Join(", ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
