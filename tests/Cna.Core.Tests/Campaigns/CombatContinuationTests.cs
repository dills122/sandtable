using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatContinuationTests
{
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void ExhaustedAmmunitionStillAllowsEngagedBreakOffWithoutSpending(string side)
    {
        var p = Probe($"zero-engaged.{side}.attacker");
        var before = JsonSerializer.SerializeToUtf8Bytes(p.World);
        var result = Assess(p);
        var witness = Assert.Single(result.Witnesses);
        Assert.Equal(p.Release.Members[0].Unit, witness.Unit);
        Assert.Equal(2, witness.TerrainCost); Assert.Equal(4, witness.BreakOffCost);
        Assert.Equal(11, witness.AfterCp); Assert.Equal(1, witness.ExcessCpDp);
        Assert.False(witness.UsesReleaseException);
        Assert.Equal("no-own-ammunition", result.CombatAssessment);
        Assert.Equal(p.Release.CompletionReceiptId, result.ReleaseCompletionReceiptId);
        Assert.Equal(p.End.CompletionReceiptId, result.MovementCompletionReceiptId);
        Assert.Equal(before, JsonSerializer.SerializeToUtf8Bytes(p.World));
        Assert.Throws<NotSupportedException>(() => ((IList<CombatContinuationWitness>)result.Witnesses).Clear());
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void SettledCatalogueHasCanonicalWitnessesAndPreservesWorldAndRelease(string side)
    {
        foreach (var kind in new[] { "ordinary", "zero-retreat", "refusal-loss-dp", "zero-engaged", "defender-capture-guard", "defender-capture-escape", "attacker-capture-guard-cp-limit", "attacker-capture-escape" })
            foreach (var seal in new[] { "attacker", "defender" })
            {
                var p = Probe($"{kind}.{side}.{seal}");
                var before = JsonSerializer.SerializeToUtf8Bytes(new { p.World, p.Release, p.End });
                var result = Assess(p); var member = p.Release.Members[0];
                var origin = p.World.Elements.Single(e => e.ElementId == member.Unit.ElementId).CurrentLocationId;
                var expected = p.Pack.Edges.Where(e => e.FirstLocationId == origin || e.SecondLocationId == origin)
                    .Select(e => e.FirstLocationId == origin ? e.SecondLocationId : e.FirstLocationId)
                    .Where(d => p.World.Elements.All(e => e.CurrentLocationId != d) && p.World.Guards.All(g => g.CurrentLocationId != d))
                    .Order(StringComparer.Ordinal).ToArray();
                Assert.Equal(expected, result.Witnesses.Select(w => w.DestinationLocationId));
                var breakOff = kind == "zero-engaged" ? 4 : kind is "ordinary" or "refusal-loss-dp" or "defender-capture-guard" or "defender-capture-escape" ? 2 : 0;
                Assert.All(result.Witnesses, w =>
                {
                    Assert.Equal(breakOff, w.BreakOffCost);
                    Assert.Equal(member.SpentCp.Numerator + breakOff + 2, w.AfterCp);
                    Assert.Equal(Math.Max(0, w.AfterCp - 10) - Math.Max(0, member.SpentCp.Numerator - 10), w.ExcessCpDp);
                });
                Assert.Equal(before, JsonSerializer.SerializeToUtf8Bytes(new { p.World, p.Release, p.End }));
            }
    }

    [Theory]
    [InlineData(9, 15, 5)]
    [InlineData(10, 0, 0)]
    public void EngagedCeilingUsesCumulativeCost(int spent, int expected, int dp)
    {
        var p = Change(Probe("zero-engaged.axis.attacker"), spent: spent);
        var witnesses = Assess(p).Witnesses;
        if (expected == 0) Assert.Empty(witnesses);
        else { Assert.Equal(expected, Assert.Single(witnesses).AfterCp); Assert.Equal(dp, witnesses[0].ExcessCpDp); }
    }

    [Theory]
    [InlineData(11, 13, 2)]
    [InlineData(13, 15, 2)]
    [InlineData(16, 0, 0)]
    public void PreviousOverspendIsNeverRefundedOrChargedTwice(int spent, int expected, int dp)
    {
        var p = Change(Probe("zero-retreat.axis.attacker"), spent: spent);
        var witnesses = Assess(p).Witnesses;
        if (expected == 0) Assert.Empty(witnesses);
        else Assert.All(witnesses, w => { Assert.Equal(expected, w.AfterCp); Assert.Equal(dp, w.ExcessCpDp); });
    }

    [Theory]
    [InlineData("axis", "first-acting-side")]
    [InlineData("axis", "second-acting-side")]
    [InlineData("commonwealth", "first-acting-side")]
    [InlineData("commonwealth", "second-acting-side")]
    public void RetainedDistanceAndEarlierExclusionSurviveCurrentProximity(string side, string slot)
    {
        var p = Scope(Probe($"zero-engaged.{side}.attacker"), 1, slot);
        Assert.Single(Assess(p).Witnesses);
        var own = p.Release.Members[0].Unit;
        var far = new CombatContinuationMovementEnd(p.End.Scope, p.End.Ordinal, p.End.CompletionReceiptId,
            p.End.Locations.Select(l => l with { LocationId = l.Unit.OriginalSide + "-supply" }), []);
        Assert.Empty(Assess(p with { End = far }).Witnesses); // Current World still adjacent.
        var excluded = new CombatContinuationMovementEnd(p.End.Scope, p.End.Ordinal, p.End.CompletionReceiptId, p.End.Locations, [own]);
        Assert.Empty(Assess(p with { End = excluded }).Witnesses);
        var distanceTwo = new CombatContinuationMovementEnd(p.End.Scope, p.End.Ordinal, p.End.CompletionReceiptId,
            p.End.Locations.Select(l => l.Unit == own ? l with { LocationId = side + "-rear" } : l), []);
        Assert.Single(Assess(p with { End = distanceTwo }).Witnesses);
    }

    [Theory]
    [InlineData("I", 1, 8, true)]
    [InlineData("I", 1, 9, false)]
    [InlineData("II", 2, 3, true)]
    [InlineData("II", 2, 4, false)]
    [InlineData("II", 2, 7, false)]
    public void PendingExceptionWaivesOnlyProximityAndUsesStricterCumulativeCeiling(string type, int ordinal, int spent, bool legal)
    {
        var p = Released(Change(Scope(Probe("zero-retreat.axis.attacker"), ordinal), spent: spent), type);
        p = p with
        {
            End = new(p.End.Scope, ordinal, p.End.CompletionReceiptId,
            p.End.Locations.Select(l => l with { LocationId = l.Unit.OriginalSide + "-supply" }), [p.Release.Members[0].Unit])
        };
        var before = JsonSerializer.SerializeToUtf8Bytes(p.Release);
        var result = Assess(p);
        Assert.Equal(legal, result.Witnesses.Count > 0);
        Assert.All(result.Witnesses, w => { Assert.True(w.UsesReleaseException); Assert.Equal(spent + 2, w.AfterCp); Assert.Equal(0, w.ExcessCpDp); });
        Assert.Equal(before, JsonSerializer.SerializeToUtf8Bytes(p.Release));
        var member = p.Release.Members[0];
        var expired = member with { History = member.History with { NextMovement = member.History.NextMovement! with { Status = "expired", CompletionReceiptId = "probe.expired" } } };
        Assert.Empty(Assess(p with { Release = p.Release with { Members = [expired] } }).Witnesses);
    }

    [Fact]
    public void RetainedSecondReserveCannotMove()
    {
        var p = Change(Scope(Probe("zero-retreat.axis.attacker"), 2), spent: 0, status: CampaignElementReserveStatus.ReserveII);
        var m = p.Release.Members[0] with { History = new(p.End.Scope, "probe.designation", "probe.conversion") };
        Assert.Empty(Assess(p with { Release = p.Release with { Members = [m] } }).Witnesses);
    }

    [Fact]
    public void UnsupportedCapabilityCannotMasqueradeAsNoWitness()
    {
        var p = Change(Probe("zero-engaged.axis.attacker"), spent: 15, ammunition: 1);
        p = p with { End = new(p.End.Scope, 1, p.End.CompletionReceiptId, p.End.Locations, [p.Release.Members[0].Unit]) };
        Assert.Throws<JsonException>(() => Assess(p));
        Assert.Throws<JsonException>(() => Assess(Change(p, ammunition: 0), (WeatherKind)999));
        var terrain = p.Pack.Locations.Select((l, i) => i == 0 ? new ContentHex(l.LocationId, "land.terrain.mountain", l.SourceCoordinate, l.Origin) : l).ToArray();
        var pack = p.Pack.WithCollections(p.Pack.SourceIndex, terrain, p.Pack.WeatherAreaAssignments, p.Pack.Edges, p.Pack.Formations, p.Pack.Elements, p.Pack.Scenarios);
        Assert.Throws<JsonException>(() => Assess(Change(p, ammunition: 0) with { Pack = pack }));
    }

    [Fact]
    public void IncompleteReleaseForeignProofAndLedgerMismatchReject()
    {
        var p = Probe("zero-engaged.axis.attacker"); var m = p.Release.Members[0];
        foreach (var bad in new[] { p.Release with { Status = "open" }, p.Release with { Pending = [m.Unit] },
            p.Release with { CompletionReceiptId = null }, p.Release with { Members = [] }, p.Release with { Members = [m, m] },
            p.Release with { Members = [m with { SpentCp = new(9, 1) }] },
            p.Release with { Members = [m with { Unit = new("foreign.creation", "axis", m.Unit.ElementId) }] },
            p.Release with { Members = [m with { History = m.History with { Scope = m.History.Scope with { OperationStage = 2 } } }] } })
            Assert.Throws<JsonException>(() => Assess(p with { Release = bad }));
        foreach (var bad in new[] { new CombatContinuationMovementEnd(p.End.Scope, 2, "probe.end", p.End.Locations, []),
            new(p.End.Scope with { ActingSide = LandSide.Commonwealth }, 1, "probe.end", p.End.Locations, []),
            new(p.End.Scope, 1, "probe.end", p.End.Locations.Take(1), []),
            new(p.End.Scope, 1, "probe.end", p.End.Locations.Reverse(), []),
            new(p.End.Scope, 1, "probe.end", p.End.Locations.Select(l => l with { LocationId = "unknown" }), []),
            new(p.End.Scope, 1, "probe.end", p.End.Locations, [m.Unit, m.Unit]),
            new(p.End.Scope, 1, "probe.end", p.End.Locations, [p.End.Locations.Single(l => l.Unit != m.Unit).Unit]) })
            Assert.Throws<JsonException>(() => Assess(p with { End = bad }));
    }

    [Fact]
    public void ForgedExceptionAndOverflowRejectRatherThanReportNoContinuation()
    {
        var p = Released(Change(Probe("zero-retreat.axis.attacker"), spent: 0), "I");
        var m = p.Release.Members[0]; var h = m.History; var ex = h.NextMovement!;
        foreach (var bad in new[] { h with { NextMovement = null }, h with { VoluntaryCeiling = 15 },
            h with { NextMovement = ex with { Scope = ex.Scope with { OperationStage = 2 } } },
            h with { NextMovement = ex with { Ordinal = 3 } }, h with { NextMovement = ex with { CompletionReceiptId = "fake" } },
            h with { NextMovement = ex with { Status = "expired" } }, h with { OffensiveCommitmentId = "fake.commitment" } })
            Assert.Throws<JsonException>(() => Assess(p with { Release = p.Release with { Members = [m with { History = bad }] } }));
        Assert.Throws<OverflowException>(() => Assess(Change(Probe("zero-engaged.axis.attacker"), spent: long.MaxValue)));
        Assert.Throws<OverflowException>(() => Assess(Change(Probe("zero-engaged.axis.attacker"), cohesion: int.MinValue)));
    }

    private static Boundary Scope(Boundary p, int ordinal, string slot = "first-acting-side")
    {
        var cycle = p.Cycle with { Ordinal = ordinal, PlayerPhaseSlot = slot };
        var scope = p.End.Scope with { PlayerPhaseSlot = slot };
        return p with
        {
            Cycle = cycle,
            End = new(scope, ordinal, p.End.CompletionReceiptId, p.End.Locations, []),
            Release = p.Release with { Members = p.Release.Members.Select(m => m with { History = m.History with { Scope = scope } }).ToArray() }
        };
    }
    private static Boundary Released(Boundary p, string type)
    {
        var m = p.Release.Members[0];
        var history = new CombatReleaseHistory(p.End.Scope, "probe.designation", type == "II" ? "probe.conversion" : null,
            type, "probe.release", p.Cycle.Ordinal, 10, type == "I" ? 10 : 5, null, new(p.End.Scope, p.Cycle.Ordinal + 1, "pending", null));
        return p with { Release = p.Release with { Members = [m with { History = history }] } };
    }
    private static Boundary Change(Boundary p, long? spent = null, int? ammunition = null, int? cohesion = null,
        CampaignElementReserveStatus? status = null)
    {
        var m = p.Release.Members[0]; var e = p.World.Elements.Single(e => e.ElementId == m.Unit.ElementId); var op = e.OperationalState;
        var next = new CampaignElementStateV6(e.ElementId, e.CurrentLocationId, status ?? e.ReserveStatus,
            new(op.LedgerGameTurn, op.LedgerOperationStage, new(spent ?? op.CapabilityPointsExpended.Numerator, 1), cohesion ?? op.CohesionLevel,
                op.VehicleBreakdownState, op.MovementEnded, op.InitialLedgerOrigin), e.Components, e.SourceParentFormationId, e.CurrentParentFormationId,
            new(ammunition ?? e.Ammunition.Points, e.Ammunition.InitialAmmunitionOrigin), e.Readiness);
        var w = p.World;
        var world = new CampaignWorldSnapshotV7(7, w.CreationBinding, w.Elements.Select(x => x.ElementId == e.ElementId ? next : x),
            w.Representations, w.BrokenVehicleLots, w.CohesionCauses, w.Relationships, w.CustodyLots, w.Guards, w.ReplacementEntitlements, w.FutureObligations, w.Settlements);
        return p with { World = world, Release = p.Release with { Members = [m with { SpentCp = next.OperationalState.CapabilityPointsExpended, Status = next.ReserveStatus }] } };
    }

    private sealed record Boundary(ContentPackV7Definition Pack, CampaignCombatCycleAuthority Cycle,
        CampaignWorldSnapshotV7 World, CombatReleaseState Release, CombatContinuationMovementEnd End);
    private static CombatContinuationAssessment Assess(Boundary p, WeatherKind weather = WeatherKind.Normal) =>
        CampaignCombatContinuation.AssessTrustedBoundary(p.Pack, p.Cycle, p.World, p.Release, p.End, weather);
    private static Boundary Probe(string name)
    {
        var source = CombatCycleMovementTests.Source(name);
        var initial = CampaignCombatResultRelease.Replay(source, [], []);
        var open = new CombatReleaseInput(new(1, "open", initial.Release.ReleaseId, initial.Release.StateVersion), CampaignOpeningPreambleActor.System);
        var opened = CampaignCombatResultRelease.Apply(source, [], [], open);
        var complete = new CombatReleaseInput(new(1, "complete", opened.State.ReleaseId, opened.State.StateVersion), CampaignOpeningPreambleActor.System);
        var closed = CampaignCombatResultRelease.Apply(source, [open], [opened.EventBytes!], complete);
        var pack = source.Request.Context.Setup.Artifact.Definition;
        var cycle = initial.Basis.Cycle; var world = initial.Result.World;
        // Isolated retained Movement-end probe, not an assertion of actual campaign history.
        var locations = world.Elements.Select(e => new CombatMovementEndLocation(new(world.CreationBinding,
            pack.Elements.Single(f => f.ElementId == e.ElementId).SideId, e.ElementId),
            source.Boundary.World.Elements.Single(x => x.ElementId == e.ElementId).CurrentLocationId))
            .OrderBy(l => l.Unit.OriginalSide, StringComparer.Ordinal).ThenBy(l => l.Unit.ElementId, StringComparer.Ordinal).ToArray();
        var end = new CombatContinuationMovementEnd(new(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide),
            cycle.Ordinal, "probe.movement-completed", locations, []);
        return new(pack, cycle, world, closed.State, end);
    }
}
