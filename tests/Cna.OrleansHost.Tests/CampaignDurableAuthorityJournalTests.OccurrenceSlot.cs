using System.Net;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Spike A W4: the Host continuation chain and AppendSelectionNoAttack are driven by the CURRENT
// occurrence slot, not by retained Stage 1 session fields. Stage 2 first side is the only later
// occurrence reachable through durable Host kinds today (second-side and Stage 3 Breakdown kinds
// arrive in W5/W6), so the other slots are proven at session level in
// Cna.Core.Tests/Hosting/OccurrenceSlotContinuationTests.cs.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private sealed record StageTwoSelection(CampaignDurableAuthorityJournal Journal, TimeProvider Clock,
        CampaignCreationRequest Request, CampaignSeatCredential Holder, string Actor);

    // Drives Stage 1 -> Stage 2 declaration/reserve/movement completion so the Stage 2 first-side
    // Breakdown (and its Selection round) is current.
    private static async Task<StageTwoSelection> AdvanceToStageTwoFirstSideSelection(JournalFixture fixture, TimeProvider? trustedClock = null, Action? beforeBreakdown = null)
    {
        var clock = trustedClock ?? new ManualClock();
        var journal = fixture.Create(clock);
        var (request, axis, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal);
        var selection = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            selection.Revision, selection.PublicActionRefs[^1]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(commonwealth);
        Assert.True(journal.SubmitOwnerCycleControl(commonwealth, control.Revision, control.PublicActionRefs[^1]).IsAccepted);
        var convoy = journal.QueryTruckConvoyPassOwnerOpening(commonwealth);
        Assert.True(journal.SubmitOwnerTruckConvoyPass(commonwealth, convoy.Revision, convoy.PublicActionRefs[0]).IsAccepted);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        for (var index = 0; index < 4; index++)
        {
            var pass = journal.QueryPostCombatPassOwnerOpening(commonwealth);
            Assert.True(journal.SubmitOwnerPostCombatPass(commonwealth, pass.Revision, Assert.Single(pass.PublicActionRefs)).IsAccepted);
        }
        const string declaration = "stage-entry-initiative-declaration";
        const string declarationKey = "land.stage-entry-initiative-declaration";
        async Task<bool> HasOpening(CampaignSeatCredential seat, string stage)
        {
            var (status, body) = await outward.OpenForSeatResponse(seat);
            Assert.True(status == HttpStatusCode.OK, body);
            return ParseOpen(body).Any(entry => entry.OpeningStage == stage);
        }
        var holder = await HasOpening(axis, declaration) ? axis : commonwealth;
        var init = await QueryOpeningForOpenSeatTest(outward, holder, declarationKey, declaration);
        Assert.Equal(HttpStatusCode.OK, (await outward.SubmitResponse(holder, declarationKey, declaration,
            init.Revision, init.Refs[0])).Status);
        const string reserve = "stage-entry-reserve";
        const string reserveKey = "land.stage-entry-reserve";
        var reserveCut = await QueryOpeningForOpenSeatTest(outward, holder, reserveKey, reserve);
        Assert.Equal(HttpStatusCode.OK, (await outward.SubmitResponse(holder, reserveKey, reserve,
            reserveCut.Revision, reserveCut.Refs[^1])).Status);
        const string finish = "stage-entry-movement-completion";
        const string finishKey = "land.stage-entry-movement-completion";
        var completion = await QueryOpeningForOpenSeatTest(outward, holder, finishKey, finish);
        // The drain after this completion runs the Stage 2 Breakdown and tries to arm its round.
        beforeBreakdown?.Invoke();
        var submitted = await outward.SubmitResponse(holder, finishKey, finish,
            completion.Revision, Assert.Single(completion.Refs));
        Assert.Equal(HttpStatusCode.OK, submitted.Status);
        return new StageTwoSelection(journal, clock, request, holder,
            holder.Audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry");
    }

    [Fact]
    public async Task StageTwoFirstSideSelectionArmsDeadlineAndFinishRunsStageTwoChain()
    {
        using var fixture = new JournalFixture();
        var stage = await AdvanceToStageTwoFirstSideSelection(fixture);
        var before = CampaignExercises.QueryCheckpoint(fixture.Open(stage.Clock).ReadVerified(stage.Request.CampaignId));
        Assert.Equal("land.position.operation-2.first-player.movement-and-combat.combat.position-determination",
            before.PositionId);
        // Armed from the Stage 2 Breakdown entry: opened now plus the selection budget.
        var deadline = stage.Journal.GetSelectionRoundDeadlineMs(stage.Request.CampaignId);
        Assert.NotNull(deadline);
        Assert.Contains(stage.Request.CampaignId, stage.Journal.CollectingSelectionCampaigns("", 16));

        var noAttackBefore = CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-no-attack");
        var opening = stage.Journal.QueryCombatOwnerOpening(stage.Holder, stage.Actor);
        var accepted = stage.Journal.SubmitOwnerCombatSelection(stage.Holder, stage.Actor,
            opening.Revision, opening.PublicActionRefs[^1]);
        Assert.True(accepted.IsAccepted);

        var live = CampaignExercises.QueryCheckpoint(fixture.Open(stage.Clock).ReadVerified(stage.Request.CampaignId));
        // Stage 2 no-attack + empty Reserve Release completed (System kinds reused, no new wire name).
        Assert.StartsWith("land.position.operation-2.first-player.", live.PositionId);
        Assert.True(before.PositionId != live.PositionId);
        Assert.Equal(noAttackBefore + 1, CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-no-attack"));
        Assert.Null(stage.Journal.GetSelectionRoundDeadlineMs(stage.Request.CampaignId));
        var cold = fixture.Open(stage.Clock).ReadVerified(stage.Request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        var coldCheckpoint = CampaignExercises.QueryCheckpoint(cold);
        Assert.Equal((live.PositionId, live.StateVersion, live.ContractVersion),
            (coldCheckpoint.PositionId, coldCheckpoint.StateVersion, coldCheckpoint.ContractVersion));
        // Retried receipt is byte-identical and does not run the chain again.
        var retry = stage.Journal.SubmitOwnerCombatSelection(stage.Holder, stage.Actor,
            opening.Revision, opening.PublicActionRefs[^1]);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(noAttackBefore + 1, CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-no-attack"));
    }

    [Fact]
    public async Task StageTwoFirstSideSelectionDeadlineExpiresThroughStageTwoNoAttack()
    {
        using var fixture = new JournalFixture();
        var stage = await AdvanceToStageTwoFirstSideSelection(fixture);
        var before = CampaignExercises.QueryCheckpoint(fixture.Open(stage.Clock).ReadVerified(stage.Request.CampaignId));
        var noAttackBefore = CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-no-attack");
        ((ManualClock)stage.Clock).Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(stage.Journal.TickSelectionRound(stage.Request.CampaignId));
        ((ManualClock)stage.Clock).Advance(TimeSpan.FromMilliseconds(1));
        var cold = fixture.Open(stage.Clock);
        Assert.True(cold.TickSelectionRound(stage.Request.CampaignId));
        Assert.False(cold.TickSelectionRound(stage.Request.CampaignId));
        var session = fixture.Open(stage.Clock).ReadVerified(stage.Request.CampaignId);
        var live = CampaignExercises.QueryCheckpoint(session);
        Assert.True(before.PositionId != live.PositionId, $"{before.PositionId} / {live.PositionId}");
        Assert.StartsWith("land.position.operation-2.first-player.", live.PositionId);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-selection-timeout"));
        Assert.Equal(noAttackBefore + 1, CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-no-attack"));
        Assert.Null(cold.GetSelectionRoundDeadlineMs(stage.Request.CampaignId));
        // A late owner submission after the deadline cannot consume or reopen the closed round.
        var late = stage.Journal.SubmitOwnerCombatSelection(stage.Holder, stage.Actor, 0, "stale");
        Assert.False(late.IsAccepted);
    }

    [Fact]
    public async Task StageTwoBreakdownWithoutTrustedClockClosesSelectionAndDrainsFromTheReloadedHead()
    {
        using var fixture = new JournalFixture();
        var clock = new TrustedNowFailingClock();
        var stage = await AdvanceToStageTwoFirstSideSelection(fixture, clock, () => clock.FailTrustedNow = true);
        // No trusted clock at entry: the round closes as selection-unavailable inside the drain, which
        // persists the timeout, no-attack and empty Reserve Release in one call. The drain must continue
        // from the reloaded head, so nothing is left at Combat Selection and no round is armed.
        var session = fixture.Open(stage.Clock).ReadVerified(stage.Request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.NotEqual("land.position.operation-2.first-player.movement-and-combat.combat.position-determination",
            CampaignExercises.QueryCheckpoint(session).PositionId);
        Assert.Null(stage.Journal.GetSelectionRoundDeadlineMs(stage.Request.CampaignId));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, stage.Request.CampaignId, "native-combat-selection-timeout"));
        Assert.False(stage.Journal.TickSelectionRound(stage.Request.CampaignId));
    }

    // Fails only the journal's TryTrustedNow read (seat authentication still sees a clock), which is
    // the "no trusted clock at round entry" case.
    private sealed class TrustedNowFailingClock : TimeProvider
    {
        private readonly DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z",
            global::System.Globalization.CultureInfo.InvariantCulture);
        internal bool FailTrustedNow { get; set; }
        public override DateTimeOffset GetUtcNow() =>
            FailTrustedNow && new global::System.Diagnostics.StackTrace().ToString().Contains("TryTrustedNow")
                ? throw new InvalidOperationException("Trusted clock unavailable.") : now;
    }
}
