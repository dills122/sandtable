using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CycleMoveCommand(int ContractVersion, string Kind, string CycleId, string PositionId,
    long ExpectedPriorVersion, CampaignCombatUnitKey Unit, string OriginLocationId, string DestinationLocationId);
internal sealed record CycleMoveInput(CycleMoveCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CycleMoveTrack(CampaignCombatUnitKey Unit, IReadOnlyList<string> Route);
internal sealed record CycleMoveBasis(CombatResultReleaseSource Source, CombatResolutionState Result,
    CampaignCombatCycleAuthority Cycle, LandSequencePosition Position);
internal sealed record CycleMoveState(CycleMoveBasis Basis, string BaseHash, long StateVersion, string Prefix,
    CampaignWorldSnapshotV7 World, IReadOnlyList<CycleMoveTrack> Tracks, IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts);
internal sealed class CycleMoveResult(CycleMoveState state, byte[] bytes, bool duplicate)
{
    private readonly byte[] bytes = bytes.ToArray();
    public CycleMoveState State { get; } = state;
    public byte[] EventBytes => bytes.ToArray();
    public bool Duplicate { get; } = duplicate;
}

/// <summary>Isolated Result2-to-Movement mechanism. No emitted release/repeat or live campaign admission.</summary>
internal static class CampaignCombatCycleMovement
{
    public static CycleMoveState ReplayIsolatedBoundary(CombatResultReleaseSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        var (trusted, retained) = Capture(inputs, events);
        var basis = Derive(source);
        var state = new CycleMoveState(basis, CampaignCombatCycleMovementCodec.Hash(CampaignCombatCycleMovementCodec.SerializeBase(basis)),
            basis.Cycle.OpenedAuthorityVersion, basis.Cycle.OpeningPrefix, basis.Result.World, [], []);
        for (var i = 0; i < retained.Length; i++)
        {
            var next = Advance(state, trusted[i]);
            if (!retained[i].AsSpan().SequenceEqual(next.EventBytes)) throw new JsonException("Movement event differs from replay.");
            state = next.State;
        }
        _ = CampaignCombatCycleMovementCodec.SerializeState(state);
        return state;
    }
    public static CycleMoveInput Command(CycleMoveState state, string destination)
    {
        var unit = state.Basis.Result.Context.Committed.Base.Steps.Selection!.Attacker.Unit;
        return new(new(1, "move", CampaignCombatReserveCompletionCodec.CycleId(state.Basis.Cycle), state.Basis.Position.PositionId,
            state.StateVersion, unit, state.World.Elements.Single(e => e.ElementId == unit.ElementId).CurrentLocationId, destination),
            unit.OriginalSide == "axis" ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);
    }
    public static CycleMoveResult ApplyIsolatedBoundary(CombatResultReleaseSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events, CycleMoveInput input, byte[]? cachedState = null)
    {
        var (trusted, retained) = Capture(inputs, events);
        if (cachedState is not null) CampaignCombatCycleMovementCodec.Check(cachedState);
        var cache = cachedState?.ToArray();
        var state = ReplayIsolatedBoundary(source, trusted, retained);
        if (cache is not null && !cache.AsSpan().SequenceEqual(CampaignCombatCycleMovementCodec.SerializeState(state)))
            throw new JsonException("Movement cache differs from replay.");
        Authorize(state, input);
        var hash = CampaignCombatCycleMovementCodec.Hash(CampaignCombatCycleMovementCodec.SerializeCommand(input.Command));
        for (var i = 0; i < state.Receipts.Count; i++)
            if (state.Receipts[i].CommandHash == hash && state.Receipts[i].Actor == input.Actor)
                return new(state, retained[i], true);
        return Advance(state, input);
    }
    private static CycleMoveBasis Derive(CombatResultReleaseSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = CampaignCombatResultRelease.Replay(source, [], []).Result;
        if (source.Boundary.Weather.AttackerKind != WeatherKind.Normal || source.Boundary.Weather.DefenderKind != WeatherKind.Normal)
            throw new JsonException("Movement requires Normal Weather.");
        // D2a labels this gap as synthetic; authentic second-cycle admission belongs to Task019.
        var prefix = CampaignOpeningPreambleCodec.HashWithDomain("isolated-unimplemented-release-repeat", CampaignCombatResolutionCodec.SerializeState(result));
        var c = result.Context.Committed.Base.Boundary.Cycle with { Ordinal = 2, OpenedAuthorityVersion = checked(result.StateVersion + 10), OpeningPrefix = prefix };
        var role = c.PlayerPhaseSlot == "first-acting-side" ? LandActorRole.FirstActingSide : LandActorRole.SecondActingSide;
        var p = Cna1979LandSequence.CreateTurn(c.GameTurn).Single(p => p.OperationStage == c.OperationStage && p.ActorRole == role && p.SegmentId == LandSegmentIds.Movement);
        var position = new LandSequencePosition(p.ContractVersion, p.PositionId, p.GameTurn, p.OperationStage, p.StageId, p.PhaseId,
            p.SegmentId, p.StepId, p.ActorRole, c.ActingSide, p.Sources);
        return new(source, result, c, position);
    }
    private static void Authorize(CycleMoveState state, CycleMoveInput input)
    {
        _ = CampaignCombatCycleMovementCodec.SerializeInput(input);
        var cmd = input.Command; var c = state.Basis.Cycle;
        var selected = state.Basis.Result.Context.Committed.Base.Steps.Selection!.Attacker.Unit;
        if (cmd.ContractVersion != 1 || cmd.Kind != "move" || cmd.Unit != selected ||
            CampaignCombatCycleMovementCodec.Actor(input.Actor) != selected.OriginalSide ||
            selected.OriginalSide != CampaignSnapshotSerializer.FormatSide(c.ActingSide) ||
            cmd.CycleId != CampaignCombatReserveCompletionCodec.CycleId(c) || cmd.PositionId != state.Basis.Position.PositionId)
            throw new JsonException("Foreign Movement actor, unit or cycle.");
    }
    private static CycleMoveResult Advance(CycleMoveState prior, CycleMoveInput input)
    {
        Authorize(prior, input); var cmd = input.Command; var basis = prior.Basis;
        if (cmd.ExpectedPriorVersion != prior.StateVersion || prior.Receipts.Count >= 32 || prior.StateVersion == long.MaxValue)
            throw new JsonException("Stale Movement or exhausted capacity.");
        var pack = basis.Source.Request.Context.Setup.Artifact.Definition;
        var world = prior.World; var element = world.Elements.Single(e => e.ElementId == cmd.Unit.ElementId);
        var facts = pack.Elements.Single(e => e.ElementId == element.ElementId);
        var edge = pack.Edges.SingleOrDefault(e => (e.FirstLocationId == cmd.OriginLocationId && e.SecondLocationId == cmd.DestinationLocationId) ||
            (e.SecondLocationId == cmd.OriginLocationId && e.FirstLocationId == cmd.DestinationLocationId));
        var destination = pack.Locations.SingleOrDefault(l => l.LocationId == cmd.DestinationLocationId);
        if (cmd.OriginLocationId != element.CurrentLocationId || edge is null || destination is null || edge.Features.Count != 0 ||
            destination.TerrainId != "land.terrain.clear" || facts.MobilityId != Cna1979Movement.NonMotorizedMobilityId ||
            facts.PlacementMode != ContentPlacementMode.Independent || facts.BaseCapabilityPointAllowance != 10 ||
            facts.CombatClassificationId != "land.combat-classification.combat-unit" ||
            facts.Components.Any(c => c.ComponentClassId != "land.combat-component.infantry") ||
            element.OperationalState.LedgerGameTurn != basis.Cycle.GameTurn || element.OperationalState.LedgerOperationStage != basis.Cycle.OperationStage)
            throw new JsonException("Outside adjacent Clear ordinary-infantry Movement profile.");
        var terrain = Cna1979Movement.LookupTerrain(destination.TerrainId, facts.MobilityId);
        if (!terrain.IsSupported || terrain.Value.Cost != new CapabilityPointAmount(2, 1))
            throw new JsonException("Clear terrain cost differs from admitted Movement artifact.");
        // Accepted two-counter Result2 source proves singleton infantry opposition, not positive ZOC.
        if (world.Elements.Any(e => e.Components.Count != 1 || e.Components[0].CurrentToe > 10 ||
                pack.Elements.Single(f => f.ElementId == e.ElementId).Components.Any(c => c.ComponentClassId != "land.combat-component.infantry")))
            throw new JsonException("Unsupported opposing control profile.");
        CombatCycleMovementCharge charge; CampaignWorldSnapshotV7 projected;
        try
        {
            charge = CampaignCombatCycleMovementRules.AssessAndCharge(cmd.Unit, element.OperationalState, 10,
                CampaignCombatSpendCeiling.Ordinary, 2, world.Relationships, "mov.preview", world.CohesionCauses);
            _ = world.ProjectOrdinaryMove(cmd.Unit, cmd.DestinationLocationId, "mov.preview");
        }
        catch (Exception error) when (error is ArgumentException or OverflowException) { throw new JsonException("Movement projection rejected.", error); }
        var rep = world.Representations.Single(r => r.BoundElementIds.SequenceEqual([element.ElementId]));
        var after = charge.Spending.State;
        var effect = CampaignCombatCycleMovementCodec.Value(new
        {
            kind = "ordinary-move",
            unit = cmd.Unit,
            representationId = rep.RepresentationId,
            originLocationId = cmd.OriginLocationId,
            destinationLocationId = cmd.DestinationLocationId,
            costs = new
            {
                terrainCost = 2,
                breakOffCost = charge.Assessment.BreakOffCost,
                totalCost = charge.Assessment.TotalCost.Numerator,
                baseCpa = 10,
                voluntaryCeiling = 15,
                beforeCp = element.OperationalState.CapabilityPointsExpended.Numerator,
                afterCp = after.CapabilityPointsExpended.Numerator,
                excessCpDp = charge.Spending.Cause?.Points ?? 0,
                beforeCohesion = element.OperationalState.CohesionLevel,
                afterCohesion = after.CohesionLevel
            },
            endedMemberships = charge.Assessment.Affected.Select(r => new
            {
                relationId = r.RelationId,
                creationReceiptId = r.CreationReceiptId,
                kind = r.Kind,
                attacker = r.Attacker,
                defender = r.Defender,
                endingCause = "ordinary-break-off"
            }).ToArray()
        });
        var unsigned = CampaignCombatCycleMovementCodec.Event(prior, input, effect, null);
        var receiptId = "mov." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.ordinary-movement-receipt.v1", unsigned)[7..];
        var bytes = CampaignCombatCycleMovementCodec.Event(prior, input, effect, receiptId);
        try { projected = world.ProjectOrdinaryMove(cmd.Unit, cmd.DestinationLocationId, receiptId); }
        catch (Exception error) when (error is ArgumentException or OverflowException) { throw new JsonException("Movement projection rejected.", error); }
        var track = prior.Tracks.SingleOrDefault(t => t.Unit == cmd.Unit);
        if (track is not null && track.Route[^1] != cmd.OriginLocationId) throw new JsonException("Movement route differs from origin.");
        var route = Array.AsReadOnly(track is null ? new[] { cmd.OriginLocationId, cmd.DestinationLocationId } : track.Route.Append(cmd.DestinationLocationId).ToArray());
        var tracks = Array.AsReadOnly(new[] { new CycleMoveTrack(cmd.Unit, route) });
        var receipt = new CampaignOpeningPreambleReceipt(CampaignCombatCycleMovementCodec.Hash(CampaignCombatCycleMovementCodec.SerializeCommand(cmd)),
            CampaignCombatCycleMovementCodec.Hash(bytes), receiptId, input.Actor, prior.StateVersion + 1);
        var state = new CycleMoveState(basis, prior.BaseHash, prior.StateVersion + 1, CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            projected, tracks, Array.AsReadOnly(prior.Receipts.Append(receipt).ToArray()));
        _ = CampaignCombatCycleMovementCodec.SerializeState(state);
        return new(state, bytes, false);
    }
    private static (CycleMoveInput[] Inputs, byte[][] Events) Capture(IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != events.Count || events.Count > 32) throw new JsonException("Movement history count/capacity mismatch.");
        var trusted = inputs.ToArray(); var retained = new byte[events.Count][];
        for (var i = 0; i < retained.Length; i++)
        {
            _ = CampaignCombatCycleMovementCodec.SerializeInput(trusted[i]);
            ArgumentNullException.ThrowIfNull(events[i]); CampaignCombatCycleMovementCodec.Check(events[i]); retained[i] = events[i].ToArray();
        }
        return (trusted, retained);
    }
}
