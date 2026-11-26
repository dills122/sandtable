using Xunit;

namespace Cna.Core.Tests.Exercises.Sequencing;

// S0: pins what the real-session drivers actually reach, so the S2 oracle's coverage claims are
// checked rather than asserted in prose. Observations are printed with SEQUENCING_DUMP=<path>.
public sealed class SequencingScenarioCoverageTests
{
    private static SequencingRecorder Scenario(string name) => SequencingScenarios.All.Single(s => s.Samples[0].Scenario == name);

    [Fact]
    public void PregameChainReachesEveryNoAttackSubtypeAndRecordsEveryStep()
    {
        var r = Scenario("pregame-no-attack");
        Assert.True(r.Samples.Count > 70);
        Assert.All(r.Samples, s => Assert.DoesNotContain("<throws", s.PositionId));
        var pending = r.Samples.Select(s => s.PendingName).Where(x => x is not null).ToHashSet();
        foreach (var expected in new[] { "SecondReservePending", "StageEntryBreakdownPending", "StageEntrySecondAssaultPending",
                     "ThirdStageReservePending", "ThirdStageMovementCompletionPending", "ThirdStageBreakdownPending",
                     "ThirdStageNoObligationFleetRepairPending", "TruckConvoyPassPending", "PostCombatPassPending" })
            Assert.Contains(expected, pending);
    }

    [Fact]
    public void TurnBoundaryScenarioStopsAtTheReadOnlyReserveOpening()
    {
        var boundary = SequencingScenarios.All.Single(s => s.Samples[0].Scenario.StartsWith("turn-boundary-", StringComparison.Ordinal));
        var turnSteps = boundary.Samples.Where(s => s.Label.StartsWith("turn.", StringComparison.Ordinal)).ToArray();
        Assert.Equal(10, turnSteps.Length);
        Assert.All(turnSteps, sample => Assert.Equal("TurnBoundaryPending", sample.PendingName));
        Assert.Equal("land.position.operation-1.first-player.reserve-designation", turnSteps[^1].PositionId);
    }

    [Fact]
    public void MoveSamplesAreTheOnlyMoveWithinPositionSamplesAndKeepTheCursorOnMovement()
    {
        var all = SequencingScenarios.All.SelectMany(s => s.Samples).ToList();
        var moves = all.Where(s => s.LastEvent == SampleLastEvent.MoveWithinPosition).ToList();
        Assert.All(moves, s => Assert.EndsWith("movement-and-combat.movement", s.PositionId));
        // A real Move was driven for at least the Stage 1 second side (the harness can offer one there).
        Assert.Contains(moves, s => s.Label == "op1.second.move" && s.PendingName == "SecondMovePending");
        Assert.All(moves, s => Assert.EndsWith("MovePending", s.PendingName ?? "<null>"));
    }

    [Fact]
    public void RealAttackAndRepeatCycleAreDrivenThroughTheCombatHarness()
    {
        var attack = Scenario("real-attack");
        Assert.Contains(attack.Samples, s => s.Label == "resolve-combat-assault" || s.Label == "complete-combat-cancelled-assault");
        Assert.Contains(attack.Samples, s => s.Label == "decline-retreat-before-assault"); // RBA decline is as far as the harness goes
        Assert.Contains(attack.Samples, s => s.Label == "combat-cycle-control-finish");
        var repeat = Scenario("real-attack-repeat");
        Assert.Contains(repeat.Samples, s => s.Label == "combat-cycle-control-repeat");
        // Every sample, attack or not, has a resolvable cursor position.
        Assert.All(attack.Samples.Concat(repeat.Samples), s => Assert.DoesNotContain("<throws", s.PositionId));
    }

    [Fact]
    public void RepeatCycleMovesTheCursorBackwardsSoTwoLastEventValuesAreNotEnough()
    {
        // Spike section 3 guessed this; S0 observes it. After Cycle Control Repeat the checkpoint points at the
        // Movement position again, but the last event is CampaignNativeCycleControlV28, not a Move and not the
        // event that completed the previous catalog position. A cursor+two-value flag would read the
        // preceding position (Reserve Designation) and be wrong for any non-legacy occurrence.
        var samples = Scenario("real-attack-repeat").Samples;
        var repeat = samples.Single(s => s.Label == "combat-cycle-control-repeat");
        Assert.Equal("CampaignNativeCycleControlV28", repeat.LastHistoryType);
        Assert.Equal(SampleLastEvent.CompletedPosition, repeat.LastEvent); // the 2-value classification cannot see it
        Assert.EndsWith("first-player.movement-and-combat.movement", repeat.PositionId);
        var before = samples.TakeWhile(s => s != repeat).Last();
        Assert.EndsWith("reserve-release", before.PositionId);
        Assert.Null(repeat.PendingName); // legacy occurrence: no PendingDecision, so the old derivation is still right
    }

    [Fact]
    public void DumpObservationsWhenAsked()
    {
        if (Environment.GetEnvironmentVariable("SEQUENCING_DUMP") is not { Length: > 0 } path) return;
        File.WriteAllLines(path, SequencingScenarios.All.SelectMany(s => s.Samples.Select(x =>
                $"{x.Scenario} | {x.Label} | pending={x.PendingName ?? "<null>"} | last={x.LastEvent}/{x.LastHistoryType} | nonNull={x.NonNullCurrentFields.Count} | {x.PositionId}")
            .Concat(s.Notes.Select(n => "NOTE " + n))));
    }
}
