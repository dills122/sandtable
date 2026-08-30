namespace Cna.Core.Content;

public sealed class ContentPackV5ParseResult
{
    private ContentPackV5ParseResult(
        ContentPackV5Definition? definition,
        string? errorCode,
        string? message)
    {
        Definition = definition;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool IsSuccess => Definition is not null;

    public ContentPackV5Definition? Definition { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    internal static ContentPackV5ParseResult Success(ContentPackV5Definition definition) =>
        new(definition, null, null);

    internal static ContentPackV5ParseResult Failure(string errorCode, string message) =>
        new(null, errorCode, message);
}
