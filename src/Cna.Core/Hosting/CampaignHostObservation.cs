using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Hosting;

// These are display facts, not action capabilities. In particular, contact handles
// are not private representation IDs and the read never derives movement offers.
public sealed record CampaignHostObservationPosition(int GameTurn, int OperationStage,
    string PositionId, string? ActiveSide);
public sealed record CampaignHostObservationLocation(string LocationId, string Label);
public sealed record CampaignHostObservationEdge(string FirstLocationId, string SecondLocationId);
public sealed record CampaignHostObservationElement(string ElementId, string ParentFormationId,
    string CurrentLocationId, string DisplayLabel);
public sealed record CampaignHostObservationContact(string Handle, string ObserverFormationId,
    string ApparentLocationId, string Identification, string Access, bool IsCurrent);

public sealed record CampaignHostObservation(CampaignHostObservationPosition? Position,
    bool WorldAvailable, IReadOnlyList<CampaignHostObservationLocation> VisibleLocations,
    IReadOnlyList<CampaignHostObservationEdge> VisibleEdges,
    IReadOnlyList<CampaignHostObservationElement> OwnElements,
    IReadOnlyList<CampaignHostObservationContact> ApparentContacts)
{
    public static CampaignHostObservation Unavailable(CampaignHostObservationPosition? position = null) =>
        new(position, false, [], [], [], []);

    /// <summary>Side-only display cut from a retained, verified exercise session.</summary>
    public static CampaignHostObservation Query(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new ArgumentOutOfRangeException(nameof(audience)),
        };
        if (session.SourceTurn is { } source)
            return Unavailable(new(source.Position.GameTurn, source.Position.OperationStage,
                source.Position.PositionId, Side(source.Position.ActiveSide)));
        if (session.Pregame is not null || session.Context.ArtifactV6 is not { } artifact)
            return Unavailable();

        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (checkpoint.StateVersion != session.CurrentContacts.AuthorityVersion)
            throw new InvalidOperationException("Observation requires one coherent retained contact cut.");
        var snapshot = session.CurrentSnapshot;
        var turn = CampaignGameTurn.CurrentGameTurn(session);
        var position = Cna1979LandSequence.CreateTurn(turn)
            .SingleOrDefault(value => value.PositionId == checkpoint.PositionId);
        var sequence = snapshot.CurrentPosition.SequenceContext;
        var stage = position?.OperationStage ?? sequence.OperationStage;
        var order = session.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>()
            .LastOrDefault(value => value.GameTurn == turn && value.OperationStage == stage)?.ToOrder()
            ?? snapshot.OperationStageOrders.SingleOrDefault(value =>
                value.GameTurn == turn && value.OperationStage == stage);
        var holder = session.CurrentHistory.OfType<CampaignNativeTurnInitiativeV56>()
            .LastOrDefault()?.Holder ?? snapshot.InitiativeHolder;
        var active = position?.ActorRole switch
        {
            LandActorRole.InitiativeHolder => holder,
            LandActorRole.FirstActingSide => order?.FirstSide,
            LandActorRole.SecondActingSide => order?.SecondSide,
            LandActorRole.Commonwealth => LandSide.Commonwealth,
            _ => null,
        };
        var cursor = new CampaignHostObservationPosition(turn, stage, checkpoint.PositionId, Side(active));

        // The V11 snapshot is frozen at the Stage 1 cut once native continuation
        // begins. Select the currently retained world, never construct a new V11.
        var world = CurrentWorld(session);
        if (world is null || !MatchesRetainedWorld(session, world)) return Unavailable(cursor);
        var definition = artifact.Definition.LegacyDefinition;
        var own = CampaignOwnElementProjection.ProjectPlaced(definition, world, side)
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        // Same formation-scoped contact knowledge consumed by the per-element
        // owner cuts. Union only views accessible to this seat's placed formations.
        var contacts = own.Select(value => value.ParentFormationId).Distinct(StringComparer.Ordinal)
            .SelectMany(formation => session.CurrentContacts.ObserveFor(side, formation).Contacts)
            .Distinct().OrderBy(value => value.Handle, StringComparer.Ordinal)
            .ThenBy(value => value.ObserverFormationId, StringComparer.Ordinal)
            .Select(value => new CampaignHostObservationContact(value.Handle, value.ObserverFormationId,
                value.ApparentLocationId, CampaignContactTransitionCodec.Identification(value.Identification),
                value.Access == CampaignContactAccess.Side ? "side" : "observer", value.IsCurrent))
            .ToArray();
        return new CampaignHostObservation(cursor, true,
            definition.Locations.OrderBy(value => value.LocationId, StringComparer.Ordinal)
                .Select(value => new CampaignHostObservationLocation(value.LocationId, value.LocationId)).ToArray(),
            definition.Edges.Select(value => new CampaignObservationEdge(value.FirstLocationId,
                    value.SecondLocationId, []))
                .OrderBy(value => value.FirstLocationId, StringComparer.Ordinal)
                .ThenBy(value => value.SecondLocationId, StringComparer.Ordinal)
                .Select(value => new CampaignHostObservationEdge(value.FirstLocationId, value.SecondLocationId)).ToArray(),
            own.Select(value => new CampaignHostObservationElement(value.ElementId, value.ParentFormationId,
                value.CurrentLocationId, value.ElementId)).ToArray(), contacts);
    }

    private static string? Side(LandSide? side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => null,
    };

    private static CampaignWorldSnapshotV6? AssaultWorld(CampaignNativeRepeatAssaultRuntime? assault) =>
        assault?.Release?.World ?? assault?.CustodyWorld ?? assault?.SettledWorld
        ?? assault?.PaidWorld ?? assault?.RetreatWorld ?? assault?.Selection.World;

    // Native events with an explicit result-world hash bind the view to the
    // last accepted event, not merely to a non-null older occurrence slot.
    private static bool MatchesRetainedWorld(ExerciseSession session, CampaignWorldSnapshotV6 world)
    {
        var expected = session.CurrentHistory.LastOrDefault() switch
        {
            CampaignNativeSecondReserveV43 reserve => reserve.ResultWorldHash,
            CampaignNativeSecondMoveV44 move => move.ResultWorldHash,
            CampaignNativeSecondMovementCompletedV45 completed => completed.WorldHash,
            CampaignNativeSecondBreakdownV46 breakdown => breakdown.WorldHash,
            CampaignNativeReactionMoveV59 reaction => reaction.ResultWorldHash,
            CampaignNativeReactionClosedV60 closed => closed.ResultWorldHash,
            CampaignNativeReserveReleaseEventV27 release => release.ResultWorldHash,
            CampaignNativeCycleControlV28 control => control.ResultWorldHash,
            CampaignNativeRepeatMovementV29 repeat => repeat.ResultWorldHash,
            CampaignNativeRepeatBreakdownV30 repeatBreakdown => repeatBreakdown.WorldHash,
            CampaignNativeTruckConvoyPassV41 convoy => convoy.WorldHash,
            CampaignNativePostCombatPassV42 pass => pass.WorldHash,
            CampaignNativeInitiativeDeclarationV49 or CampaignNativeWeatherDeterminationV50
                or CampaignNativeNoObligationOrganizationV51 or CampaignNativeNoObligationNavalConvoyArrivalV52
                or CampaignNativeNoObligationFleetAssignmentV53 or CampaignNativeNoObligationFleetRepairV54
                or CampaignNativeEndOfTurnV55 or CampaignNativeTurnInitiativeV56
                or CampaignNativeNoObligationTurnPhaseV57 =>
                CampaignGameTurn.BoundaryInfo(session.CurrentHistory[^1]).WorldHash,
            _ => null,
        };
        return expected is null || expected == CampaignNativeCombatPaidWorldV22.Hash(world);
    }

    // Each native occurrence has its own retained slots; later occurrences never
    // reset the earlier ones. Select in chronological occurrence order, as the
    // checkpoint does. The later-turn prelude carries the settled prior world.
    private static CampaignWorldSnapshotV6? CurrentWorld(ExerciseSession session)
    {
        if (session.NativeReaction is { } reaction) return reaction.World;
        if (session.Pending is TurnBoundaryPending)
        {
            return session.CurrentThirdStageSecondAssault?.Release?.World;
        }
        return AssaultWorld(session.CurrentThirdStageSecondAssault)
            ?? session.CurrentThirdStageSecondCombatDecision?.World
            ?? session.CurrentThirdStageSecondMoveWorld ?? session.CurrentThirdStageSecondReserveWorld
            ?? AssaultWorld(session.CurrentThirdStageAssault) ?? session.CurrentThirdStageCombatDecision?.World
            ?? session.CurrentThirdStageMoveWorld ?? session.CurrentThirdStageReserveWorld
            ?? AssaultWorld(session.CurrentStageEntrySecondAssault)
            ?? session.CurrentStageEntrySecondCombatDecision?.World
            ?? session.CurrentStageEntrySecondMoveWorld ?? session.CurrentStageEntrySecondReserveWorld
            ?? AssaultWorld(session.CurrentStageEntryAssault) ?? session.CurrentStageEntryCombatDecision?.World
            ?? session.CurrentStageEntryMoveWorld ?? session.CurrentStageEntryReserveWorld
            ?? AssaultWorld(session.CurrentSecondAssault) ?? session.CurrentSecondCombatDecision?.World
            ?? session.CurrentSecondMoveWorld ?? session.CurrentSecondReserveWorld
            ?? AssaultWorld(session.CurrentPostRepeat?.ActiveAssault) ?? session.CurrentPostRepeat?.World
            ?? AssaultWorld(session.CurrentFirstRepeatAssault) ?? session.CurrentRepeatMoveWorld
            ?? session.CurrentReserveRelease?.World ?? session.CurrentCombatCustodyWorld
            ?? session.CurrentCombatSettledWorld ?? session.CurrentCombatPaidWorld
            ?? session.CurrentCombatRetreatWorld ?? session.CurrentCombatDecision?.PriorSnapshot.World
            ?? session.CurrentSnapshot.World;
    }
}
