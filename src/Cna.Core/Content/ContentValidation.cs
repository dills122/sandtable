namespace Cna.Core.Content;

public sealed record ContentValidationIssue
{
    public ContentValidationIssue(string code, string path, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Path = path;
        Message = message;
    }

    public string Code { get; }

    public string Path { get; }

    public string Message { get; }
}

public sealed class ContentValidationResult
{
    public ContentValidationResult(IEnumerable<ContentValidationIssue> issues)
    {
        var issueCopy = ContentContractGuards.CopyValues(issues, nameof(issues));
        Issues = Array.AsReadOnly(issueCopy
            .Distinct()
            .OrderBy(issue => issue.Path, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray());
    }

    public bool IsValid => Issues.Count == 0;

    public IReadOnlyList<ContentValidationIssue> Issues { get; }
}
