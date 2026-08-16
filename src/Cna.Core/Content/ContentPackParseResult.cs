namespace Cna.Core.Content;

public sealed class ContentPackParseResult
{
    private ContentPackParseResult(
        ContentPackDefinition? definition,
        string? errorCode,
        string? message)
    {
        Definition = definition;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool IsSuccess => Definition is not null;

    public ContentPackDefinition? Definition { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    internal static ContentPackParseResult Success(ContentPackDefinition definition) =>
        new(definition, null, null);

    internal static ContentPackParseResult Failure(string errorCode, string message) =>
        new(null, errorCode, message);
}
