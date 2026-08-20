using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

internal static class ExerciseContractText
{
    internal static string FormatFailure(ExerciseFailureCategory value) => value switch
    {
        ExerciseFailureCategory.ManifestInvalid => "manifest-invalid",
        ExerciseFailureCategory.BuildIdentityUnavailable => "build-identity-unavailable",
        ExerciseFailureCategory.ControllerFailed => "controller-failed",
        ExerciseFailureCategory.NoUniqueLegalAction => "no-unique-legal-action",
        ExerciseFailureCategory.IllegalAction => "illegal-action",
        ExerciseFailureCategory.InvariantFailed => "invariant-failed",
        ExerciseFailureCategory.ReconstructionMismatch => "reconstruction-mismatch",
        ExerciseFailureCategory.ReadjudicationMismatch => "readjudication-mismatch",
        ExerciseFailureCategory.StepLimitExceeded => "step-limit-exceeded",
        ExerciseFailureCategory.Cancelled => "cancelled",
        ExerciseFailureCategory.ArtifactFailed => "artifact-failed",
        ExerciseFailureCategory.UnexpectedFailure => "unexpected-failure",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    internal static ExerciseFailureCategory ParseFailure(string? value)
    {
        foreach (var candidate in Enum.GetValues<ExerciseFailureCategory>())
        {
            if (string.Equals(FormatFailure(candidate), value, StringComparison.Ordinal))
                return candidate;
        }
        throw new JsonException("Unknown failure category.");
    }
}

internal static class StrictJson
{
    internal static void RequireExactProperties(
        JsonElement element,
        IReadOnlyList<string> expected)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.EnumerateObject().Select(value => value.Name)
                .SequenceEqual(expected, StringComparer.Ordinal))
            throw new JsonException("Properties are missing, extra, duplicated, or out of order.");
    }
}
