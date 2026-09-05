using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignV10LegacyBridge
{
    public static CampaignSnapshot ToLegacy(
        CampaignSnapshotV10 snapshot,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV5
            ?? throw new InvalidOperationException("A current campaign requires Content Pack v5.");
        if (!CampaignSnapshotV10Validator.IsValid(snapshot, artifact, context.Scenario)
            || snapshot.ReactionWindow is not null
            || snapshot.CurrentPosition.Kind != CampaignPositionV10Kind.Sequence
            || !Cna1979SetupCatalog.TryGet(snapshot.Setup.SetupId, out var definition))
        {
            throw new InvalidOperationException(
                "Only valid non-Reaction Snapshot 10 authority can enter predecessor mechanics.");
        }

        return new CampaignSnapshot(
            CampaignSnapshot.CurrentContractVersion,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            CampaignSetupSnapshot.FromDefinition(definition),
            ToLegacyWorld(snapshot.World),
            snapshot.InitiativeHolder,
            snapshot.OperationStageOrders,
            snapshot.OperationStageWeather,
            snapshot.RandomState,
            DematerializeMovement(snapshot.CurrentPosition.SequencePosition!));
    }

    public static CampaignSnapshotV10 FromLegacy(
        CampaignSnapshotV10 prior,
        CampaignSnapshot successor,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV5
            ?? throw new InvalidOperationException("A current campaign requires Content Pack v5.");
        if (prior.ReactionWindow is not null
            || successor.ContractVersion != CampaignSnapshot.CurrentContractVersion
            || !string.Equals(successor.CampaignId, prior.CampaignId, StringComparison.Ordinal)
            || !string.Equals(successor.RulesetHash, prior.RulesetHash, StringComparison.Ordinal)
            || successor.StateVersion != checked(prior.StateVersion + 1))
        {
            throw new InvalidOperationException(
                "Predecessor mechanics produced an incompatible successor transition.");
        }

        var projected = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            prior.CampaignId,
            successor.StateVersion,
            prior.RulesetHash,
            prior.Setup,
            LiftWorld(prior.World, successor.World),
            successor.InitiativeHolder,
            successor.OperationStageOrders,
            successor.OperationStageWeather,
            successor.RandomState,
            CampaignPositionV10.FromSequence(MaterializeMovement(successor)),
            null);
        if (!CampaignSnapshotV10Validator.IsValid(projected, artifact, context.Scenario))
        {
            throw new InvalidOperationException(
                "Predecessor mechanics did not produce valid Snapshot 10 state.");
        }

        return projected;
    }

    public static CampaignSnapshotV10 FromLegacySnapshot(
        CampaignSnapshot snapshot,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV5
            ?? throw new InvalidOperationException("A current campaign requires Content Pack v5.");
        if (!Cna1979SetupCatalog.TryGet(snapshot.Setup.SetupId, out var definition))
        {
            throw new InvalidOperationException("The predecessor setup is not admitted.");
        }

        var setup = CampaignSetupSnapshotV5.FromPredecessor(
            CampaignSetupSnapshot.FromDefinition(definition),
            new CampaignContentV5Selection(artifact.Identity, context.Scenario.ScenarioId));
        var seeded = CampaignWorldV5Factory.CreateInitial(artifact, context.Scenario);
        var projected = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            setup,
            LiftWorld(seeded, snapshot.World),
            snapshot.InitiativeHolder,
            snapshot.OperationStageOrders,
            snapshot.OperationStageWeather,
            snapshot.RandomState,
            CampaignPositionV10.FromSequence(snapshot.SequencePosition),
            null);
        if (!CampaignSnapshotV10Validator.IsValid(projected, artifact, context.Scenario))
        {
            throw new InvalidOperationException(
                "The predecessor snapshot cannot be represented as current authority.");
        }

        return projected;
    }

    private static CampaignWorldSnapshot ToLegacyWorld(CampaignWorldSnapshotV5 world) => new(
        CampaignWorldSnapshot.CurrentContractVersion,
        world.Elements.Select(element => new CampaignElementState(
            element.ElementId,
            element.CurrentLocationId,
            element.ReserveStatus,
            new CampaignElementOperationalState(
                element.OperationalState.LedgerGameTurn,
                element.OperationalState.LedgerOperationStage,
                element.OperationalState.CapabilityPointsExpended,
                element.OperationalState.CohesionLevel,
                element.OperationalState.VehicleBreakdownState))).ToArray(),
            world.Representations);

    private static LandSequencePosition MaterializeMovement(CampaignSnapshot snapshot)
    {
        var position = snapshot.SequencePosition;
        return IsFirstSideMovement(position) && position.ActiveSide is null
            ? CopyPosition(position, FirstActingSideResolver.Resolve(snapshot))
            : position;
    }

    private static LandSequencePosition DematerializeMovement(LandSequencePosition position) =>
        IsFirstSideMovement(position) && position.ActiveSide is not null
            ? CopyPosition(position, null)
            : position;

    private static bool IsFirstSideMovement(LandSequencePosition position) =>
        position.ActorRole == LandActorRole.FirstActingSide
        && position.PhaseId == LandPhaseIds.MovementAndCombat
        && position.SegmentId == LandSegmentIds.Movement;

    private static LandSequencePosition CopyPosition(
        LandSequencePosition position,
        LandSide? activeSide) => new(
        position.ContractVersion,
        position.PositionId,
        position.GameTurn,
        position.OperationStage,
        position.StageId,
        position.PhaseId,
        position.SegmentId,
        position.StepId,
        position.ActorRole,
        activeSide,
        position.Sources);

    private static CampaignWorldSnapshotV5 LiftWorld(
        CampaignWorldSnapshotV5 prior,
        CampaignWorldSnapshot successor)
    {
        var priorById = prior.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        return new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            successor.Elements.Select(element =>
            {
                var old = priorById[element.ElementId];
                var preserveMovementEnded = old.OperationalState.LedgerGameTurn
                        == element.OperationalState.LedgerGameTurn
                    && old.OperationalState.LedgerOperationStage
                        == element.OperationalState.LedgerOperationStage;
                return new CampaignElementStateV5(
                    element.ElementId,
                    element.CurrentLocationId,
                    element.ReserveStatus,
                    new CampaignElementOperationalStateV5(
                        element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage,
                        element.OperationalState.CapabilityPointsExpended,
                        element.OperationalState.CohesionLevel,
                        element.OperationalState.VehicleBreakdownState,
                        preserveMovementEnded ? old.OperationalState.MovementEnded : null),
                    old.Components);
            }).ToArray(),
            successor.Representations);
    }
}
