using Cna.Core.Exercises;
using Cna.Core.Exercises.Sequencing;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Xunit;
using Spike = Cna.Core.Tests.Exercises.PendingFromCursorSpikeTests;

namespace Cna.Core.Tests.Exercises.Sequencing;

// S1: the promoted src derivation (Cna.Core.Exercises.Sequencing.PendingFromCursor) is checked against the
// spike's test-local reference over the whole catalog, and its table coverage is pinned by reflection.
// Real-session equivalence with ExerciseSession.Pending is the S2 oracle's job.
public sealed class PendingFromCursorShadowTests
{
    private static string[] Subtypes() => typeof(PendingDecision).Assembly.GetTypes()
        .Where(t => t.BaseType == typeof(PendingDecision)).Select(t => t.Name).Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public void ShadowDerivationEqualsTheSpikeReferenceAtEveryCatalogPositionForBothSpikeLastEvents()
    {
        foreach (var gameTurn in new[] { 1, 2 })
        {
            var turn = Cna1979LandSequenceV4.CreateTurn(gameTurn);
            foreach (var position in turn)
            {
                Assert.Equal(
                    Spike.PendingFromCursor.Derive(turn, position.PositionId, Spike.LastEvent.CompletedPosition, gameTurn == 1),
                    PendingFromCursor.Derive(turn, position.PositionId, PendingLastEvent.CompletedPosition, gameTurn == 1));
                Assert.Equal(
                    Spike.PendingFromCursor.Derive(turn, position.PositionId, Spike.LastEvent.MoveWithinPosition, gameTurn == 1),
                    PendingFromCursor.Derive(turn, position.PositionId, PendingLastEvent.MoveWithinPosition, gameTurn == 1));
            }
        }
    }

    [Fact]
    public void BothLaterStageOccurrencesUseTheSameHostStagesAtMatchingPositions()
    {
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        foreach (var first in turn.Where(p => p.OperationStage is 2 or 3 && p.ActorRole == LandActorRole.FirstActingSide))
        {
            var second = turn.Single(p => p.OperationStage == first.OperationStage && p.ActorRole == LandActorRole.SecondActingSide
                && p.PhaseId == first.PhaseId && p.SegmentId == first.SegmentId && p.StepId == first.StepId);
            Assert.Equal(CampaignOpenSeatCatalog.StagesAt(first, true), CampaignOpenSeatCatalog.StagesAt(second, true));
        }
    }

    [Fact]
    public void EverySubtypeIsDerivableOrOnTheNamedExceptionListAndNothingIsInvented()
    {
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        var later = Cna1979LandSequenceV4.CreateTurn(2);
        var derived = turn.SelectMany(p => Enum.GetValues<PendingLastEvent>()
                .Select(e => PendingFromCursor.Derive(turn, p.PositionId, e)))
            .Concat(later.Select(p => PendingFromCursor.Derive(later, p.PositionId, PendingLastEvent.CompletedPosition, false)))
            .Where(x => x is not null).Select(x => x!).ToHashSet(StringComparer.Ordinal);
        var subtypes = Subtypes();
        Assert.Empty(derived.Except(subtypes));
        var underived = subtypes.Except(derived).Order(StringComparer.Ordinal).ToArray();
        Assert.True(underived.SequenceEqual(PendingExceptionTable.Unobservable.Order(StringComparer.Ordinal)),
            "Subtype(s) with no derivation row and no exception entry: " + string.Join(", ",
                underived.Except(PendingExceptionTable.Unobservable)));
    }

    [Fact]
    public void CycleRepeatReturnsTheOccurrenceAssaultArmOrNullForTheLegacyOccurrence()
    {
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        string At(string suffix) => turn.Single(p => p.PositionId.EndsWith(suffix, StringComparison.Ordinal)).PositionId;
        Assert.Null(PendingFromCursor.Derive(turn, At("operation-1.first-player.movement-and-combat.movement"), PendingLastEvent.CycleRepeat));
        Assert.Equal("SecondAssaultPending",
            PendingFromCursor.Derive(turn, At("operation-1.second-player.movement-and-combat.movement"), PendingLastEvent.CycleRepeat));
        Assert.Equal("StageEntryAssaultPending",
            PendingFromCursor.Derive(turn, At("operation-2.first-player.movement-and-combat.movement"), PendingLastEvent.CycleRepeat));
    }

    [Fact]
    public void ReserveEventStaysOnReserveDesignationOnlyWhileDesignatingAndAdvancesOnCompletion()
    {
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        var reserve = turn.Single(p => p.PositionId.EndsWith("operation-2.second-player.reserve-designation", StringComparison.Ordinal));
        Assert.Equal("StageEntrySecondReservePending",
            PendingFromCursor.Derive(turn, reserve.PositionId, PendingLastEvent.ReserveWithinPosition));
        // Completion advanced the cursor to the next position: generic completed-position path.
        var next = turn[turn.ToList().IndexOf(reserve) + 1];
        Assert.Equal(PendingFromCursor.Derive(turn, next.PositionId, PendingLastEvent.CompletedPosition),
            PendingFromCursor.Derive(turn, next.PositionId, PendingLastEvent.ReserveWithinPosition));
    }

    [Fact]
    public void UnknownPositionIsRejected()
    {
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        Assert.Throws<ArgumentException>(() => PendingFromCursor.Derive(turn, "no.such.position", PendingLastEvent.CompletedPosition));
    }
}
