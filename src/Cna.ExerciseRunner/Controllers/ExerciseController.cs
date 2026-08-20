using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Controllers;

public sealed class ExerciseControllerActionSet
{
    public ExerciseControllerActionSet(
        CampaignActionAudience audience,
        IEnumerable<string> actionIds)
    {
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ArgumentNullException.ThrowIfNull(actionIds);
        var copy = actionIds.ToArray();
        if (copy.Any(value => string.IsNullOrWhiteSpace(value))
            || copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("Action IDs must be nonempty and unique.", nameof(actionIds));
        Audience = audience;
        ActionIds = Array.AsReadOnly(copy.Order(StringComparer.Ordinal).ToArray());
    }

    public CampaignActionAudience Audience { get; }
    public IReadOnlyList<string> ActionIds { get; }
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
        var active = actionSets.Where(value => value.ActionIds.Count > 0).ToArray();
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
        return policy switch
        {
            ExerciseControllerPolicy.FirstByActionId => ExerciseControllerSelection.Selected(
                selected.Audience,
                selected.ActionIds[0]),
            _ => ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.PolicyFailed),
        };
    }
}
