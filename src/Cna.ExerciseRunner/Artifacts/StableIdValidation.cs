namespace Cna.ExerciseRunner.Artifacts;

/// <summary>
/// Shared "stable ID" grammar validation: lowercase ASCII letters, digits, and nonadjacent
/// '-' or '.' separators, beginning and ending with a letter or digit. Used by manifest and
/// controller-candidate contracts that identify entities with a stable, portable ID.
/// </summary>
internal static class StableIdValidation
{
    internal static void Require(string? value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!IsAsciiLowerOrDigit(value[0]) || !IsAsciiLowerOrDigit(value[^1]))
            throw new ArgumentException(
                "A stable ID must begin and end with a lowercase ASCII letter or digit.",
                parameterName);

        var previousWasSeparator = false;
        foreach (var character in value)
        {
            if (IsAsciiLowerOrDigit(character))
            {
                previousWasSeparator = false;
                continue;
            }
            if (character is '-' or '.' && !previousWasSeparator)
            {
                previousWasSeparator = true;
                continue;
            }
            throw new ArgumentException(
                "A stable ID must use lowercase ASCII letters, digits, and nonadjacent separators.",
                parameterName);
        }
    }

    private static bool IsAsciiLowerOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';
}
