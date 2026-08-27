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
        StableIdValidation.Require(kind, nameof(kind));
        if (string.Equals(kind, "designate-reserve", StringComparison.Ordinal))
        {
            StableIdValidation.Require(elementId, nameof(elementId));
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
}

public sealed class ExerciseControllerActionSet
{
    public ExerciseControllerActionSet(
        CampaignActionAudience audience,
        IEnumerable<ExerciseControllerCandidate> candidates,
        int priorReserveDesignationCount = 0)
    {
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfNegative(priorReserveDesignationCount);
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
        PriorReserveDesignationCount = priorReserveDesignationCount;
        Candidates = Array.AsReadOnly(copy
            .OrderBy(value => value.ActionId, StringComparer.Ordinal)
            .ToArray());
    }

    public CampaignActionAudience Audience { get; }
    public int PriorReserveDesignationCount { get; }
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

        if (policy == ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId)
            return SelectDesignateAllReservesThenFirstByActionId(selected);

        var matrixPolicy = MatrixPolicy(policy);
        return matrixPolicy.HasValue
            ? SelectMatrixPolicy(selected, matrixPolicy.Value)
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

    private static ExerciseControllerSelection SelectMatrixPolicy(
        ExerciseControllerActionSet selected,
        MatrixControllerPolicy policy)
    {
        var initiativeCandidates = selected.Candidates.Where(candidate => candidate.Kind is
            "act-first" or "act-last").ToArray();
        if (initiativeCandidates.Length > 0)
        {
            var actFirst = initiativeCandidates.Where(candidate => string.Equals(
                candidate.Kind,
                "act-first",
                StringComparison.Ordinal)).ToArray();
            var actLast = initiativeCandidates.Where(candidate => string.Equals(
                candidate.Kind,
                "act-last",
                StringComparison.Ordinal)).ToArray();
            if (initiativeCandidates.Length != selected.Candidates.Count
                || actFirst.Length != 1
                || actLast.Length != 1)
            {
                return ExerciseControllerSelection.Failed(
                    ExerciseControllerSelectionFailure.PolicyFailed);
            }

            return ExerciseControllerSelection.Selected(
                selected.Audience,
                policy.ActFirst ? actFirst[0].ActionId : actLast[0].ActionId);
        }

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
        return policy.ReserveSelection switch
        {
            MatrixReserveSelection.None => ExerciseControllerSelection.Selected(
                selected.Audience,
                completions[0].ActionId),
            MatrixReserveSelection.One when selected.PriorReserveDesignationCount > 1 =>
                ExerciseControllerSelection.Failed(
                    ExerciseControllerSelectionFailure.PolicyFailed),
            MatrixReserveSelection.One => ExerciseControllerSelection.Selected(
                selected.Audience,
                selected.PriorReserveDesignationCount == 0 && designation is not null
                    ? designation.ActionId
                    : completions[0].ActionId),
            MatrixReserveSelection.All => ExerciseControllerSelection.Selected(
                selected.Audience,
                designation?.ActionId ?? completions[0].ActionId),
            _ => throw new ArgumentOutOfRangeException(nameof(policy)),
        };
    }

    private static MatrixControllerPolicy? MatrixPolicy(ExerciseControllerPolicy policy) =>
        policy switch
        {
            ExerciseControllerPolicy.ActFirstReserveNoneThenFirstByActionId =>
                new(true, MatrixReserveSelection.None),
            ExerciseControllerPolicy.ActFirstReserveOneThenFirstByActionId =>
                new(true, MatrixReserveSelection.One),
            ExerciseControllerPolicy.ActFirstReserveAllThenFirstByActionId =>
                new(true, MatrixReserveSelection.All),
            ExerciseControllerPolicy.ActLastReserveNoneThenFirstByActionId =>
                new(false, MatrixReserveSelection.None),
            ExerciseControllerPolicy.ActLastReserveOneThenFirstByActionId =>
                new(false, MatrixReserveSelection.One),
            ExerciseControllerPolicy.ActLastReserveAllThenFirstByActionId =>
                new(false, MatrixReserveSelection.All),
            _ => null,
        };

    private enum MatrixReserveSelection
    {
        None,
        One,
        All,
    }

    private readonly record struct MatrixControllerPolicy(
        bool ActFirst,
        MatrixReserveSelection ReserveSelection);
}
