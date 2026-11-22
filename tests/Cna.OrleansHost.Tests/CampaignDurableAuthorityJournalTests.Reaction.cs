using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Owner ruling history (release-blocking, same audit as #497/#499):
//  1. Reaction window resolution (MoveReactingElementAction/DeclineReactionWindowAction/
//     CompleteReactionParticipantAction) had no owner opening or HTTP stage at all.
//  2. Building it the same way as InitiativeOrder/ReserveDesignation (the generic
//     CampaignExercises.Query/Submit path) hit #394 gap 1: that path's stale
//     CurrentCombatDecision gate unconditionally rejects every second-side-triggered window --
//     the only kind the standard admitted content ever actually opens. Ruled: sidestep gap 1
//     with a dedicated non-generic Core entry point (CampaignExercises.QueryReactionWindow/
//     SubmitReactionWindow in CampaignExercises.cs), not remediate the shared gate.
//  3. That sidestep is confirmed correct (no more gate-related rejection), but a second,
//     independent, already-documented gap (#394 gap 3: ExerciseSession.CurrentSnapshot is
//     never bridged to the second-side native chain's own world/position -- see
//     docs/work/plans/2026-11-18-combat-continuation-second-side-design-proposal.md) still
//     means Core cannot derive legal Reaction actions for a window a second-side Movement
//     opened. Ruled: not fixed this release (tracked separately, PR #501). Instead,
//     CampaignHostAdapter.CreateReactionOwnerDraft now distinguishes that specific case (a
//     real window genuinely open, confirmed via CurrentSecondMove.ReactionWindow) from "no
//     window at all", and throws CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException
//     instead of the generic "Reaction is unavailable" -- which both CampaignDurableAuthorityJournal
//     and the outward HTTP API (CampaignDurableOutwardApi.cs) now propagate as an explicit
//     FID-002 rule-not-implemented response, same pattern as second-side Repeat's own
//     RuleNotImplemented rejection reason, rather than folding a real, open decision into the
//     ordinary "no such opening" case or letting an unhandled exception reach a real player.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Reuses VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart's own,
    // already-proven setup (Request() fixture; first side voluntarily finishes Combat with
    // nothing adjacent; Cycle Control Finish; Truck Convoy; four Post-Combat passes; second
    // side's Reserve finished empty so every element stays free to move) to reach the second
    // side's own Movement, then searches its real move offers -- on disposable copies of the
    // same base authority -- for one that opens a real reaction window. Detection is the thing
    // under test itself: a move opens a real window iff querying Reaction for the non-mover
    // throws CampaignReactionWindowRuleNotImplementedException specifically (confirmed real,
    // currently un-resolvable window), not the generic InvalidOperationException (no window).
    private static (CampaignCreationRequest Request, CampaignSeatCredential Mover,
        CampaignSeatCredential Other, JournalFixture Fixture, ManualClock Clock)
        AdvanceToSecondSideReactionWindow()
    {
        var baseFixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = baseFixture.Create(clock);
        var request = Request();
        AdvanceToContactCombat(journal, request, Timing(request));
        var axis = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var combat = journal.QueryCombatOwnerOpening(axis, "axis-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(axis, "axis-infantry",
            combat.Revision, combat.PublicActionRefs[^1]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(axis);
        Assert.True(journal.SubmitOwnerCycleControl(axis, control.Revision,
            control.PublicActionRefs[^1]).IsAccepted);
        var convoy = journal.QueryTruckConvoyPassOwnerOpening(axis);
        Assert.True(journal.SubmitOwnerTruckConvoyPass(axis, convoy.Revision,
            convoy.PublicActionRefs[0]).IsAccepted);
        for (var index = 0; index < 4; index++)
        {
            var phase = journal.QueryPostCombatPassOwnerOpening(axis);
            Assert.True(journal.SubmitOwnerPostCombatPass(axis, phase.Revision,
                Assert.Single(phase.PublicActionRefs)).IsAccepted);
        }
        var commonwealth = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var secondReserve = journal.QuerySecondReserveOwnerOpening(commonwealth);
        var movementElements = JsonDocument.Parse(secondReserve.VisibleBytes).RootElement
            .GetProperty("choices").EnumerateArray()
            .Select(value => value.GetProperty("elementId"))
            .Where(value => value.ValueKind != JsonValueKind.Null)
            .Select(value => value.GetString()!)
            .Distinct(StringComparer.Ordinal).ToArray();
        Assert.True(journal.SubmitOwnerSecondReserve(commonwealth, secondReserve.Revision,
            secondReserve.PublicActionRefs[^1]).IsAccepted);

        var attempts = new List<JournalFixture>();
        try
        {
            foreach (var movementElement in movementElements)
            {
                var movement = journal.QuerySecondMovementOwnerOpening(commonwealth, movementElement);
                for (var index = 0; index < movement.PublicActionRefs.Count; index++)
                {
                    var attempt = IntegratedCampaignSave(baseFixture.Path, clock, attempts);
                    var attemptJournal = attempt.Open(clock);
                    var accepted = attemptJournal.SubmitOwnerSecondMovement(commonwealth, movementElement,
                        movement.Revision, movement.PublicActionRefs[index]);
                    if (!accepted.IsAccepted) continue;
                    var opensRealReactionWindow = false;
                    try { attemptJournal.QueryReactionOwnerOpening(axis); }
                    catch (CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException)
                    { opensRealReactionWindow = true; }
                    catch (InvalidOperationException) { /* genuinely no window from this move */ }
                    if (!opensRealReactionWindow) continue;
                    attempts.Remove(attempt);
                    baseFixture.Dispose();
                    return (request, commonwealth, axis, attempt, clock);
                }
            }
        }
        finally
        {
            foreach (var attempt in attempts) attempt.Dispose();
        }
        baseFixture.Dispose();
        throw new InvalidOperationException(
            "No second-side move in this fixture opens a real reaction window.");
    }

    [Fact]
    public void ReactionOpenedBySecondSideMovementIsRejectedAsRuleNotImplementedNotUnavailable()
    {
        var (request, mover, other, fixture, clock) = AdvanceToSecondSideReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);
            var before = StoredAuthority(fixture.Path, request.CampaignId);

            // Both sides -- not just the non-mover -- get the distinguishable, FID-002-honest
            // signal: a real window exists that this release cannot resolve, not the generic
            // "nothing to see here" every other not-yet-open stage returns.
            Assert.Throws<CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException>(
                () => journal.QueryReactionOwnerOpening(other));
            Assert.Throws<CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException>(
                () => journal.QueryReactionOwnerOpening(mover));

            // A well-formed-looking submission is rejected the same explicit way, through the
            // normal durable Host entry point -- not a thrown exception reaching the caller.
            var forged = "act." + new string('0', 64);
            var rejected = journal.SubmitOwnerReaction(other, 1, forged);
            Assert.False(rejected.IsAccepted);
            Assert.Equal("rule-not-implemented", rejected.RejectionCode);

            // Querying/submitting never mutated anything: still the exact pre-query authority,
            // and the one real step that did commit (the triggering second-side Movement)
            // still reconstructs and verifies cleanly.
            Assert.Equal(before, StoredAuthority(fixture.Path, request.CampaignId));
            Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);

            // Cold replay: the explicit rejection is not an in-memory-only quirk -- it survives
            // a fresh process-local reopen of the same durable file.
            var cold = fixture.Open(clock);
            Assert.Throws<CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException>(
                () => cold.QueryReactionOwnerOpening(other));
            var coldRejected = cold.SubmitOwnerReaction(other, 1, forged);
            Assert.False(coldRejected.IsAccepted);
            Assert.Equal("rule-not-implemented", coldRejected.RejectionCode);
            Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
        }
    }

    [Fact]
    public async Task ReactionOpenedBySecondSideMovementReturnsHttp409RuleNotImplementedNotACrash()
    {
        var (request, mover, other, fixture, clock) = AdvanceToSecondSideReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);

            // The real outward HTTP API, not CampaignDurableAuthorityJournal.Append directly --
            // this is the actual boundary a real player's request crosses, and the one the
            // owner ruling specifically called out as must-not-crash.
            await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

            var (openingStatus, openingBody) = await outward.OpeningResponse(
                other, "land.reaction", "reaction");
            Assert.Equal(HttpStatusCode.Conflict, openingStatus);
            using (var openingJson = JsonDocument.Parse(openingBody))
                Assert.Equal("rule-not-implemented", openingJson.RootElement.GetProperty("code").GetString());

            var (moverOpeningStatus, moverOpeningBody) = await outward.OpeningResponse(
                mover, "land.reaction", "reaction");
            Assert.Equal(HttpStatusCode.Conflict, moverOpeningStatus);
            using (var moverOpeningJson = JsonDocument.Parse(moverOpeningBody))
                Assert.Equal("rule-not-implemented", moverOpeningJson.RootElement.GetProperty("code").GetString());

            var (actionStatus, actionBody) = await outward.SubmitResponse(
                other, "land.reaction", "reaction", 1, "act." + new string('0', 64));
            Assert.Equal(HttpStatusCode.Conflict, actionStatus);
            using (var actionJson = JsonDocument.Parse(actionBody))
                Assert.Equal("rule-not-implemented", actionJson.RootElement.GetProperty("code").GetString());

            // The HTTP layer never mutated anything either; the whole chain still verifies.
            var cold = fixture.Open(clock);
            Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
        }
    }
}
