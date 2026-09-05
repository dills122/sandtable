namespace Cna.Core.Content;

internal sealed class ContentPackV6ParseResult
{
    private ContentPackV6ParseResult(
        ContentPackV6Definition? definition,
        string? errorCode,
        string? message)
    {
        Definition = definition;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool IsSuccess => Definition is not null;

    public ContentPackV6Definition? Definition { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    internal static ContentPackV6ParseResult Success(ContentPackV6Definition definition) =>
        new(definition, null, null);

    internal static ContentPackV6ParseResult Failure(string errorCode, string message) =>
        new(null, errorCode, message);
}
