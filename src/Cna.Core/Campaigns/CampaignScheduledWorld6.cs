using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Bounded synthetic successor-world cut. No legacy setup calls this transition.
internal static class CampaignScheduledWorld6
{
    private const string ArtifactHash = "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
    private const string DeferredId = "axis-infantry";
    private const string Destination = "north-west";
    private static readonly CampaignWorldSnapshotV6 CompleteWorld = CreateCompleteWorld();
    private static readonly CampaignWorldSnapshotV6 PendingWorld = CreatePendingWorld();

    public static CampaignWorldSnapshotV6 CreateInitial() => PendingWorld;

    public static CampaignWorldSnapshotV6 Arrive(CampaignWorldSnapshotV6 world, LandSide actor,
        LandSequencePosition position, string destination)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(position);
        if (world != PendingWorld || actor != LandSide.Axis || position != ArrivalPosition()
            || destination != Destination)
            throw new ArgumentException("The synthetic World6 arrival is not eligible.");
        return CompleteWorld;
    }

    internal static LandSequencePosition ArrivalPosition() => Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
        value.OperationStage == 1 && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);

    private static CampaignWorldSnapshotV6 CreateCompleteWorld()
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        if (artifact.Identity.PackId != "rules-lab.content.breakdown-truck.v1" || artifact.Identity.Hash != ArtifactHash)
            throw new InvalidOperationException("The certified World6 fixture no longer matches its pinned identity.");
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single(value => value.ScenarioId == "breakdown-truck-lab");
        var placement = scenario.InitialPlacements.Single(value => value.ElementId == DeferredId);
        if (placement.LocationId != Destination)
            throw new InvalidOperationException("The synthetic arrival template has changed.");
        return CampaignWorldV6Factory.CreateInitial(artifact, scenario);
    }

    private static CampaignWorldSnapshotV6 CreatePendingWorld()
    {
        var representation = CompleteWorld.Representations.Single(value => value.BoundElementIds.Contains(DeferredId, StringComparer.Ordinal));
        return new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            CompleteWorld.Elements.Where(value => value.ElementId != DeferredId),
            CompleteWorld.Representations.Where(value => value.RepresentationId != representation.RepresentationId),
            CompleteWorld.BrokenVehicleLots);
    }
}
