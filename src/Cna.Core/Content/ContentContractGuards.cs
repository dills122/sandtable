namespace Cna.Core.Content;

internal static class ContentContractGuards
{
    public static string RequireStableId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (!IsAsciiLetterOrDigit(value[0])
            || !IsAsciiLetterOrDigit(value[^1]))
        {
            throw new ArgumentException(
                "A stable ID must begin and end with a lowercase ASCII letter or digit.",
                parameterName);
        }

        var previousWasSeparator = false;

        foreach (var character in value)
        {
            if (IsAsciiLetterOrDigit(character))
            {
                previousWasSeparator = false;
                continue;
            }

            if ((character is '-' or '.') && !previousWasSeparator)
            {
                previousWasSeparator = true;
                continue;
            }

            throw new ArgumentException(
                "A stable ID must use lowercase ASCII letters, digits, and nonadjacent hyphen or dot separators.",
                parameterName);
        }

        return value;
    }

    public static string RequireSourceAtom(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > 128 || !IsSourceStart(value[0]))
        {
            throw new ArgumentException(
                "A source atom must be 1 through 128 safe ASCII characters and begin with a letter or digit.",
                parameterName);
        }

        if (value.Any(character => !IsSourceCharacter(character)))
        {
            throw new ArgumentException(
                "A source atom may contain only ASCII letters, digits, dot, underscore, colon, or hyphen.",
                parameterName);
        }

        return value;
    }

    public static string RequirePresentationText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    public static string RequireSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length != 71
            || !value.StartsWith("sha256:", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A content hash must be 'sha256:' followed by 64 lowercase hexadecimal digits.",
                parameterName);
        }

        foreach (var character in value.AsSpan(7))
        {
            if (character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f'))
            {
                throw new ArgumentException(
                    "A content hash must be 'sha256:' followed by 64 lowercase hexadecimal digits.",
                    parameterName);
            }
        }

        return value;
    }

    public static T[] CopyValues<T>(IEnumerable<T> values, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copy = values.ToArray();

        if (copy.Any(value => value is null))
        {
            throw new ArgumentException("Null collection entries are not allowed.", parameterName);
        }

        return copy;
    }

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';

    private static bool IsSourceStart(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';

    private static bool IsSourceCharacter(char value) =>
        IsSourceStart(value) || value is '.' or '_' or ':' or '-';
}
