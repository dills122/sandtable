using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;

namespace Cna.OrleansHost.Tests;

/// <summary>
/// Proves Phase C3a of issue #439's new round-open/round-close instrumentation
/// (<c>CampaignDurableAuthorityJournal.RecordRoundOpened</c>/<c>RecordRoundClosed</c>, drained via
/// <c>DrainPendingRoundTimerActions</c>) fires correctly, for all seven round kinds, from the
/// exact production call sites the phase's task named: the seven <c>InitializeXRound</c> methods
/// and <c>Submissions.cs</c>'s <c>UpdateXRound(... Status = "completed")</c> sites. This is the
/// layer <c>CampaignGrain</c> drains to arm/disarm a real <c>CampaignRoundTimeoutGrain</c> (see
/// <c>CampaignDurableAuthorityJournalTests.GrainEquivalence.cs</c> for the real-silo proof of that
/// next layer); this file tests the instrumentation itself against the real SQLite journal, no
/// Orleans involved, reusing
/// this test class's own existing round-preparation fixtures (the same ones
/// *Timeout.cs/Rba.cs already use) as "what correct round state looks like."
/// </summary>
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public void SelectionRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, owner) = PrepareSelectionRound(journal);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Selection);
        Assert.Equal(journal.GetSelectionRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Selection, (long?)null), closed);
    }

    [Fact]
    public void RbaRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Rba);
        Assert.Equal(journal.GetRbaRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        var opening = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId, opening.Revision,
            opening.PublicActionRefs[^1]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Rba, (long?)null), closed);
    }

    [Fact]
    public void ForceRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 7);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Force);
        Assert.Equal(journal.GetForceRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Force, (long?)null), closed);
    }

    [Fact]
    public void RetreatRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, attacker, defender, defenderId) = PrepareRetreatRound(journal);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Retreat);
        Assert.Equal(journal.GetRetreatRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        var (captor, element, opening) = RetreatOpening(journal, attacker, defender, defenderId);
        Assert.True(journal.SubmitOwnerPostAssaultRetreat(captor, element,
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Retreat, (long?)null), closed);
    }

    [Fact]
    public void CustodyRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, attacker, defender, defenderId) = PrepareCustodyRound(journal);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Custody);
        Assert.Equal(journal.GetCustodyRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        var (captor, element, opening) = CustodyOpening(journal, attacker, defender, defenderId);
        Assert.True(journal.SubmitOwnerCustody(captor, element,
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Custody, (long?)null), closed);
    }

    [Fact]
    public void ReserveRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, owner) = PrepareReserveRound(journal);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Reserve);
        Assert.Equal(journal.GetReserveRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        var opening = journal.QueryReserveReleaseOwnerOpening(owner, "axis-infantry");
        Assert.True(journal.SubmitOwnerReserveRelease(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Reserve, (long?)null), closed);
    }

    [Fact]
    public void CycleRoundOpenAndCloseAreRecordedForTimerArming()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, owner) = PrepareCycleRound(journal);
        var opened = journal.DrainPendingRoundTimerActions();
        var armed = Assert.Single(opened, a => a.RoundKind == CampaignRoundKind.Cycle);
        Assert.Equal(journal.GetCycleRoundDeadlineMs(request.CampaignId), armed.DeadlineMs);
        Assert.NotNull(armed.DeadlineMs);

        var opening = journal.QueryCycleControlOwnerOpening(owner);
        Assert.True(journal.SubmitOwnerCycleControl(owner, opening.Revision,
            opening.PublicActionRefs[^1]).IsAccepted);
        var closed = journal.DrainPendingRoundTimerActions();
        Assert.Contains((CampaignRoundKind.Cycle, (long?)null), closed);
    }

    /// <summary>
    /// A caller that never drains (e.g. every pre-existing test in this class, none of which know
    /// about this phase's new instrumentation) must see no behavior change: <see
    /// cref="CampaignDurableAuthorityJournal.DrainPendingRoundTimerActions"/> is new, additive,
    /// in-memory-only bookkeeping that does not alter any SQL this journal executes.
    /// </summary>
    [Fact]
    public void UndrainedRoundTimerActionsDoNotAffectOrdinaryJournalBehavior()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, owner) = PrepareSelectionRound(journal);
        // Deliberately never drains -- matches how every *Timeout.cs test in this class already
        // exercises the journal without knowing this list exists.
        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        var accepted = journal.SubmitOwnerCombatSelection(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[^1]);
        Assert.True(accepted.IsAccepted);
        Assert.True(Cna.Core.Exercises.CampaignExercises.Reconstruct(
            journal.ReadVerified(request.CampaignId)).IsVerified);
    }
}
