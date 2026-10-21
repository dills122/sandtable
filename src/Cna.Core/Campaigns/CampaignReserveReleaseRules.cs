using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignReserveReleaseChoice
{
    ReleaseI,
    ConvertToII,
    ReleaseII,
    RetainII,
}

internal sealed record CampaignReserveReleaseMember(
    string ElementId, CampaignElementReserveStatus Status,
    int BaseCapabilityPointAllowance,
    IReadOnlyList<CampaignReserveReleaseChoice> Choices);

internal sealed record CampaignReserveReleaseMovementAllowance(
    string ElementId, int NextCycleOrdinal, int CapabilityPointCeiling);

internal sealed record CampaignReserveReleaseDisposition(
    CampaignWorldSnapshotV6 World,
    CampaignElementReserveStatus PriorStatus,
    CampaignElementReserveStatus ResultingStatus,
    CampaignReserveReleaseMovementAllowance? NextMovement);

// The normal campaign Reserve Release rule. The accepted event and Host owner
// opening use this same projection; neither may infer an offer from opposing
// World elements that are outside the acting side's own reserve queue.
internal static class CampaignReserveReleaseRules
{
    internal static IReadOnlyList<CampaignReserveReleaseMember> Pending(
        CampaignWorldSnapshotV6 world, CampaignContentContext context,
        LandSide actingSide, int cycleOrdinal)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cycleOrdinal);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Reserve Release needs normal Content 6.");
        if (!CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException("Reserve Release World is invalid.");
        var sideId = CampaignSnapshotSerializer.FormatSide(actingSide);
        var own = artifact.Definition.LegacyDefinition.Elements
            .Where(element => element.SideId == sideId)
            .ToDictionary(element => element.ElementId, StringComparer.Ordinal);
        if (cycleOrdinal > 1 && world.Elements.Any(element =>
                own.ContainsKey(element.ElementId)
                && element.ReserveStatus == CampaignElementReserveStatus.ReserveI))
            throw new InvalidOperationException(
                "An unresolved Reserve I member cannot enter a later cycle.");
        var dueStatus = cycleOrdinal == 1
            ? CampaignElementReserveStatus.ReserveI
            : CampaignElementReserveStatus.ReserveII;
        return world.Elements.Where(element => own.ContainsKey(element.ElementId)
                && element.ReserveStatus == dueStatus)
            .Select(element => new CampaignReserveReleaseMember(
                element.ElementId, element.ReserveStatus,
                own[element.ElementId].BaseCapabilityPointAllowance,
                Choices(element.ReserveStatus, cycleOrdinal)))
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static CampaignReserveReleaseDisposition Apply(
        CampaignWorldSnapshotV6 world, CampaignContentContext context,
        LandSide actingSide, int cycleOrdinal, string elementId,
        CampaignReserveReleaseChoice choice)
    {
        var pending = Pending(world, context, actingSide, cycleOrdinal);
        var current = pending.Count == 0 ? null : pending[0];
        if (current is null || current.ElementId != elementId
            || !current.Choices.Contains(choice))
            throw new InvalidOperationException("Reserve choice is not the current owner offer.");
        var resultingStatus = choice is CampaignReserveReleaseChoice.ConvertToII
            or CampaignReserveReleaseChoice.RetainII
            ? CampaignElementReserveStatus.ReserveII
            : CampaignElementReserveStatus.None;
        var allowance = resultingStatus == CampaignElementReserveStatus.None
            ? new CampaignReserveReleaseMovementAllowance(elementId,
                checked(cycleOrdinal + 1),
                choice == CampaignReserveReleaseChoice.ReleaseI
                    ? current.BaseCapabilityPointAllowance
                    : current.BaseCapabilityPointAllowance / 2)
            : null;
        var after = new CampaignWorldSnapshotV6(
            CampaignWorldSnapshotV6.CurrentContractVersion,
            world.Elements.Select(element => element.ElementId == elementId
                ? new CampaignElementStateV5(element.ElementId,
                    element.CurrentLocationId, resultingStatus,
                    element.OperationalState, element.Components)
                : element), world.Representations, world.BrokenVehicleLots);
        return new(after, current.Status, resultingStatus, allowance);
    }

    private static IReadOnlyList<CampaignReserveReleaseChoice> Choices(
        CampaignElementReserveStatus status, int cycleOrdinal) =>
        (status, cycleOrdinal) switch
        {
            (CampaignElementReserveStatus.ReserveI, 1) =>
                [CampaignReserveReleaseChoice.ReleaseI,
                    CampaignReserveReleaseChoice.ConvertToII],
            (CampaignElementReserveStatus.ReserveII, > 1) =>
                [CampaignReserveReleaseChoice.ReleaseII,
                    CampaignReserveReleaseChoice.RetainII],
            _ => throw new InvalidOperationException(
                "Reserve status is invalid for the current cycle."),
        };
}
