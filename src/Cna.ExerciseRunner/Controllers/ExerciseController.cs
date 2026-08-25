using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Controllers;

public sealed class ExerciseControllerCandidate
{
    public const int CurrentContractVersion = 1;

    public ExerciseControllerCandidate(
        int contractVersion,
        string actionId,
        string kind,
        string? elementId)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        RequireStableId(kind, nameof(kind));
        if (string.Equals(kind, "designate-reserve", StringComparison.Ordinal))
        {
            RequireStableId(elementId, nameof(elementId));
        }
        else if (elementId is not null)
        {
            throw new ArgumentException(
                "Only Reserve designation candidates may carry an element ID.",
                nameof(elementId));
        }

        ContractVersion = contractVersion;
        ActionId = actionId;
        Kind = kind;
        ElementId = elementId;
    }

    public int ContractVersion { get; }
    public string ActionId { get; }
    public string Kind { get; }
    public string? ElementId { get; }

    private static void RequireStableId(string? value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!IsAsciiLowerOrDigit(value[0]) || !IsAsciiLowerOrDigit(value[^1]))
        {
            throw new ArgumentException(
                "A stable ID must begin and end with a lowercase ASCII letter or digit.",
                parameterName);
        }

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

public sealed class ExerciseControllerActionSet
{
    public ExerciseControllerActionSet(
        CampaignActionAudience audience,
        IEnumerable<ExerciseControllerCandidate> candidates)
    {
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ArgumentNullException.ThrowIfNull(candidates);
        var copy = candidates.ToArray();
        if (copy.Any(value => value is null)
            || copy.Select(value => value.ActionId)
                .Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(
                "Controller candidates must be nonnull with unique action IDs.",
                nameof(candidates));
        }
        Audience = audience;
        Candidates = Array.AsReadOnly(copy
            .OrderBy(value => value.ActionId, StringComparer.Ordinal)
            .ToArray());
    }

    public CampaignActionAudience Audience { get; }
    public IReadOnlyList<ExerciseControllerCandidate> Candidates { get; }
}

public enum ExerciseControllerSelectionFailure
{
    None,
    NoActiveAudience,
    MultipleActiveAudiences,
    PolicyFailed,
}

public sealed class ExerciseControllerSelection
{
    private ExerciseControllerSelection(
        CampaignActionAudience? audience,
        string? actionId,
        ExerciseControllerSelectionFailure failureReason)
    {
        Audience = audience;
        ActionId = actionId;
        FailureReason = failureReason;
    }

    public bool IsSelected => ActionId is not null;
    public CampaignActionAudience? Audience { get; }
    public string? ActionId { get; }
    public ExerciseControllerSelectionFailure FailureReason { get; }

    internal static ExerciseControllerSelection Selected(
        CampaignActionAudience audience,
        string actionId) =>
        new(audience, actionId, ExerciseControllerSelectionFailure.None);

    internal static ExerciseControllerSelection Failed(
        ExerciseControllerSelectionFailure reason)
    {
        if (reason == ExerciseControllerSelectionFailure.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
        return new ExerciseControllerSelection(null, null, reason);
    }
}

public static class ExerciseController
{
    private static readonly CampaignActionAudience[] AudienceOrder =
    [
        CampaignActionAudience.System,
        CampaignActionAudience.Axis,
        CampaignActionAudience.Commonwealth,
    ];

    public static ExerciseControllerSelection Select(
        ExerciseControllerManifest policies,
        IReadOnlyList<ExerciseControllerActionSet> actionSets)
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(actionSets);
        if (actionSets.Count != AudienceOrder.Length
            || !actionSets.Select(value => value.Audience).SequenceEqual(AudienceOrder))
            throw new ArgumentException(
                "Controller action sets must use fixed audience order.",
                nameof(actionSets));
        var active = actionSets.Where(value => value.Candidates.Count > 0).ToArray();
        if (active.Length == 0)
            return ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.NoActiveAudience);
        if (active.Length != 1)
            return ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.MultipleActiveAudiences);

        var selected = active[0];
        var policy = selected.Audience switch
        {
            CampaignActionAudience.System => policies.System,
            CampaignActionAudience.Axis => policies.Axis,
            CampaignActionAudience.Commonwealth => policies.Commonwealth,
            _ => throw new ArgumentOutOfRangeException(nameof(actionSets)),
        };
        if (policy == ExerciseControllerPolicy.FirstByActionId)
        {
            return ExerciseControllerSelection.Selected(
                selected.Audience,
                selected.Candidates[0].ActionId);
        }

        return policy == ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId
            ? SelectDesignateAllReservesThenFirstByActionId(selected)
            : ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.PolicyFailed);
    }

    private static ExerciseControllerSelection
        SelectDesignateAllReservesThenFirstByActionId(
            ExerciseControllerActionSet selected)
    {
        var reserveCandidates = selected.Candidates.Where(candidate => candidate.Kind is
            "designate-reserve" or "complete-reserve-designation").ToArray();
        if (reserveCandidates.Length == 0)
        {
            return ExerciseControllerSelection.Selected(
                selected.Audience,
                selected.Candidates[0].ActionId);
        }

        var completions = reserveCandidates.Where(candidate => string.Equals(
            candidate.Kind,
            "complete-reserve-designation",
            StringComparison.Ordinal)).ToArray();
        if (reserveCandidates.Length != selected.Candidates.Count
            || completions.Length != 1)
        {
            return ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.PolicyFailed);
        }

        var designation = reserveCandidates
            .Where(candidate => string.Equals(
                candidate.Kind,
                "designate-reserve",
                StringComparison.Ordinal))
            .OrderBy(candidate => candidate.ElementId, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal)
            .FirstOrDefault();
        return ExerciseControllerSelection.Selected(
            selected.Audience,
            designation?.ActionId ?? completions[0].ActionId);
    }
}
