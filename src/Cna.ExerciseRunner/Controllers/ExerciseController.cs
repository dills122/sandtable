using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Controllers;

public sealed class ExerciseControllerCandidate
{
    public const int CurrentContractVersion = 3;

    public ExerciseControllerCandidate(
        int contractVersion,
        string actionId,
        string kind,
        string? elementId,
        string? originLocationId = null,
        string? destinationLocationId = null,
        CapabilityPointAmount? movementTotalCost = null)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        StableIdValidation.Require(kind, nameof(kind));
        if (string.Equals(kind, "designate-reserve", StringComparison.Ordinal))
        {
            StableIdValidation.Require(elementId, nameof(elementId));
            if (originLocationId is not null
                || destinationLocationId is not null
                || movementTotalCost is not null)
                throw new ArgumentException(
                    "Reserve designation candidates cannot carry Movement data.",
                    nameof(originLocationId));
        }
        else if (string.Equals(kind, "move-element", StringComparison.Ordinal))
        {
            StableIdValidation.Require(elementId, nameof(elementId));
            StableIdValidation.Require(originLocationId, nameof(originLocationId));
            StableIdValidation.Require(destinationLocationId, nameof(destinationLocationId));
            if (string.Equals(originLocationId, destinationLocationId, StringComparison.Ordinal))
                throw new ArgumentException(
                    "Movement candidates must change location.",
                    nameof(destinationLocationId));
            ArgumentNullException.ThrowIfNull(movementTotalCost);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
                movementTotalCost,
                CapabilityPointAmount.Zero);
        }
        else if (elementId is not null
            || originLocationId is not null
            || destinationLocationId is not null
            || movementTotalCost is not null)
        {
            throw new ArgumentException(
                "Only Reserve designation and Movement candidates may carry semantic data.",
                nameof(elementId));
        }

        ContractVersion = contractVersion;
        ActionId = actionId;
        Kind = kind;
        ElementId = elementId;
        OriginLocationId = originLocationId;
        DestinationLocationId = destinationLocationId;
        MovementTotalCost = movementTotalCost;
    }

    public int ContractVersion { get; }
    public string ActionId { get; }
    public string Kind { get; }
    public string? ElementId { get; }
    public string? OriginLocationId { get; }
    public string? DestinationLocationId { get; }
    public CapabilityPointAmount? MovementTotalCost { get; }
}

public sealed class ExerciseControllerActionSet
{
    public ExerciseControllerActionSet(
        CampaignActionAudience audience,
        IEnumerable<ExerciseControllerCandidate> candidates,
        int priorReserveDesignationCount = 0,
        IEnumerable<string>? priorMovedElementIds = null)
    {
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfNegative(priorReserveDesignationCount);
        var copy = candidates.ToArray();
        var moved = priorMovedElementIds?.ToArray() ?? [];
        if (copy.Any(value => value is null)
            || copy.Select(value => value.ActionId)
                .Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(
                "Controller candidates must be nonnull with unique action IDs.",
                nameof(candidates));
        }
        foreach (var elementId in moved)
            StableIdValidation.Require(elementId, nameof(priorMovedElementIds));
        if (moved.Distinct(StringComparer.Ordinal).Count() != moved.Length)
            throw new ArgumentException(
                "Accepted Movement history must contain unique stable element IDs.",
                nameof(priorMovedElementIds));
        Audience = audience;
        PriorReserveDesignationCount = priorReserveDesignationCount;
        PriorMovedElementIds = Array.AsReadOnly(
            moved.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Candidates = Array.AsReadOnly(copy
            .OrderBy(value => value.ActionId, StringComparer.Ordinal)
            .ToArray());
    }

    public CampaignActionAudience Audience { get; }
    public int PriorReserveDesignationCount { get; }
    public IReadOnlyList<string> PriorMovedElementIds { get; }
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

        if (policy.BoundedMovement)
        {
            var movement = SelectBoundedMovement(selected, policy.MovementSelection);
            if (movement is not null) return movement;
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

    private static ExerciseControllerSelection? SelectBoundedMovement(
        ExerciseControllerActionSet selected,
        MatrixMovementSelection selection)
    {
        var movementCandidates = selected.Candidates.Where(candidate => candidate.Kind is
            "move-element" or "complete-movement-segment").ToArray();
        if (movementCandidates.Length == 0) return null;

        var completions = movementCandidates.Where(candidate => string.Equals(
            candidate.Kind,
            "complete-movement-segment",
            StringComparison.Ordinal)).ToArray();
        var moves = movementCandidates.Where(candidate => string.Equals(
            candidate.Kind,
            "move-element",
            StringComparison.Ordinal)).ToArray();
        if (movementCandidates.Length != selected.Candidates.Count
            || completions.Length != 1
            || moves.Any(candidate => candidate.ElementId is null
                || candidate.OriginLocationId is null
                || candidate.DestinationLocationId is null
                || candidate.MovementTotalCost is null))
            return ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.PolicyFailed);

        var prior = selected.PriorMovedElementIds.ToHashSet(StringComparer.Ordinal);
        var eligible = moves.Where(candidate => !prior.Contains(candidate.ElementId!));
        var next = selection switch
        {
            MatrixMovementSelection.StableRoute => eligible
                .OrderBy(candidate => candidate.ElementId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.DestinationLocationId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.OriginLocationId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal)
                .FirstOrDefault(),
            MatrixMovementSelection.LowestCost => eligible
                .OrderBy(candidate => candidate.ElementId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.MovementTotalCost)
                .ThenBy(candidate => candidate.DestinationLocationId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.OriginLocationId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal)
                .FirstOrDefault(),
            _ => throw new ArgumentOutOfRangeException(nameof(selection)),
        };
        return ExerciseControllerSelection.Selected(
            selected.Audience,
            next?.ActionId ?? completions[0].ActionId);
    }

    private static MatrixControllerPolicy? MatrixPolicy(ExerciseControllerPolicy policy) =>
        policy switch
        {
            ExerciseControllerPolicy.ActFirstReserveNoneThenFirstByActionId =>
                new(true, MatrixReserveSelection.None, MatrixMovementSelection.None),
            ExerciseControllerPolicy.ActFirstReserveOneThenFirstByActionId =>
                new(true, MatrixReserveSelection.One, MatrixMovementSelection.None),
            ExerciseControllerPolicy.ActFirstReserveAllThenFirstByActionId =>
                new(true, MatrixReserveSelection.All, MatrixMovementSelection.None),
            ExerciseControllerPolicy.ActLastReserveNoneThenFirstByActionId =>
                new(false, MatrixReserveSelection.None, MatrixMovementSelection.None),
            ExerciseControllerPolicy.ActLastReserveOneThenFirstByActionId =>
                new(false, MatrixReserveSelection.One, MatrixMovementSelection.None),
            ExerciseControllerPolicy.ActLastReserveAllThenFirstByActionId =>
                new(false, MatrixReserveSelection.All, MatrixMovementSelection.None),
            ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete =>
                new(true, MatrixReserveSelection.None, MatrixMovementSelection.StableRoute),
            ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete =>
                new(true, MatrixReserveSelection.One, MatrixMovementSelection.StableRoute),
            ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceThenComplete =>
                new(true, MatrixReserveSelection.All, MatrixMovementSelection.StableRoute),
            ExerciseControllerPolicy.ActLastReserveNoneMoveEachOnceThenComplete =>
                new(false, MatrixReserveSelection.None, MatrixMovementSelection.StableRoute),
            ExerciseControllerPolicy.ActLastReserveOneMoveEachOnceThenComplete =>
                new(false, MatrixReserveSelection.One, MatrixMovementSelection.StableRoute),
            ExerciseControllerPolicy.ActLastReserveAllMoveEachOnceThenComplete =>
                new(false, MatrixReserveSelection.All, MatrixMovementSelection.StableRoute),
            ExerciseControllerPolicy
                .ActFirstReserveNoneMoveEachOnceByLowestCostThenComplete =>
                new(true, MatrixReserveSelection.None, MatrixMovementSelection.LowestCost),
            _ => null,
        };

    private enum MatrixReserveSelection
    {
        None,
        One,
        All,
    }

    private enum MatrixMovementSelection
    {
        None,
        StableRoute,
        LowestCost,
    }

    private readonly record struct MatrixControllerPolicy(
        bool ActFirst,
        MatrixReserveSelection ReserveSelection,
        MatrixMovementSelection MovementSelection)
    {
        public bool BoundedMovement => MovementSelection != MatrixMovementSelection.None;
    }
}
