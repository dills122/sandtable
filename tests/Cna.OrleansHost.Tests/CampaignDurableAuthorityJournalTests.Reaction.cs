using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Owner ruling (release-blocking, same audit as #497/#499): Reaction window resolution
// (MoveReactingElementAction/DeclineReactionWindowAction/CompleteReactionParticipantAction,
// opened whenever a Movement or second-side Movement crosses an eligible opponent's reaction
// opportunity) had no owner opening or HTTP stage. Confirmed reachable with the standard
// admitted content (not just theoretically possible) by the pre-existing
// VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart test, which already
// finds and submits a real reaction-triggering second-side move and was, until now, unable to
// resolve what it opened through any owner opening. Built the same way as
// InitiativeOrder/ReserveDesignation -- no new Core logic: CampaignLegalActions.Query already
// routes to CampaignObservationV7ActionDerivation.DerivePlayer/DeriveSystem whenever
// snapshot.ReactionWindow is not null, and already replay-verifies it.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Reuses VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart's own,
    // already-proven setup (Request() fixture; first side voluntarily finishes Combat with
    // nothing adjacent; Cycle Control Finish; Truck Convoy; four Post-Combat passes; second
    // side's Reserve designates its one eligible element then completes) to reach the second
    // side's own Movement, then searches its real move offers -- on disposable copies of the
    // same base authority, the identical search pattern that test already uses -- for one that
    // opens a real reaction window for the first side. InitiativeOrder/ReserveDesignation for
    // the FIRST side are exercised through the ordinary AdvanceToContactCombat helper here
    // (already proven correct in their own dedicated test files); this helper exists only to
    // reach a genuine open reaction window for Reaction's own tests.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Acting,
        CampaignSeatCredential Other, JournalFixture Fixture, ManualClock Clock)
        AdvanceToOpenReactionWindow()
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
        // Finish Second Reserve designation immediately with nobody designated -- the
        // simplest closed path to a completed Reserve stage, with every element still free
        // to move (same closed path AdvanceToSecondCombatSelectionWithAdjacentEnemy's own
        // remarks use for the same reason).
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
                    try
                    {
                        attemptJournal.QueryReactionOwnerOpening(axis);
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
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
    public async Task ReactionIsOfferedOnlyToTheReactingSideAndAcceptedThroughHttp()
    {
        var (request, acting, other, fixture, clock) = AdvanceToOpenReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);

            // Side-safe: the mover has no reaction opening at all while the window is open --
            // same invariant every other asymmetric owner opening holds (and the same thing
            // VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart already
            // proved for the plain "movement" stage during an open window).
            Assert.Throws<InvalidOperationException>(() => journal.QueryReactionOwnerOpening(acting));

            var opening = journal.QueryReactionOwnerOpening(other);
            Assert.NotEmpty(opening.PublicActionRefs);
            string? chosenRef = null;
            string? windowId = null;
            using (var visible = JsonDocument.Parse(opening.VisibleBytes))
            {
                var choices = visible.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.Contains(choices, value => value.GetProperty("kind").GetString() == "move");
                var moveIndex = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "move");
                windowId = choices[moveIndex].GetProperty("windowId").GetString();
                Assert.NotNull(windowId);
                chosenRef = opening.PublicActionRefs[moveIndex];
            }

            // The real outward HTTP API, not CampaignDurableAuthorityJournal.Append.
            await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
            await outward.Submit(other, opening.ActingElementId, "reaction", "reaction", opening, chosenRef!);

            var cold = fixture.Open(clock);
            var afterHttp = cold.ReadVerified(request.CampaignId);
            Assert.True(CampaignExercises.Reconstruct(afterHttp).IsVerified);

            // Retry through the normal durable Host: idempotent, same canonical receipt.
            var retried = cold.SubmitOwnerReaction(other, opening.Revision, chosenRef!);
            Assert.True(retried.IsAccepted);

            // Having committed to an opportunity, the window is now in its second phase: the
            // same "reaction" opening, re-queried, narrows to that opportunity's own remaining
            // options plus completing it -- not the first-phase decline/other-opportunity set.
            var narrowed = cold.QueryReactionOwnerOpening(other);
            using (var visible = JsonDocument.Parse(narrowed.VisibleBytes))
            {
                var choices = visible.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.DoesNotContain(choices, value => value.GetProperty("kind").GetString() == "decline");
                Assert.All(choices, value => Assert.Equal(windowId, value.GetProperty("windowId").GetString()));
            }
        }
    }

    [Fact]
    public void ReactionRejectsStaleForgedAndWrongAudienceSubmissions()
    {
        var (request, acting, other, fixture, clock) = AdvanceToOpenReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);
            var opening = journal.QueryReactionOwnerOpening(other);
            var legalRef = opening.PublicActionRefs[0];
            var before = StoredAuthority(fixture.Path, request.CampaignId);

            // Wrong audience: the mover cannot submit even a well-formed reference.
            Assert.False(journal.SubmitOwnerReaction(acting, opening.Revision, legalRef).IsAccepted);
            // Stale revision.
            Assert.False(journal.SubmitOwnerReaction(other, opening.Revision + 1, legalRef).IsAccepted);
            // Forged reference: well-formed ("act." + 64 hex) but never issued.
            var forged = "act." + new string(legalRef[4] == '0' ? '1' : '0', 64);
            Assert.NotEqual(legalRef, forged);
            Assert.False(journal.SubmitOwnerReaction(other, opening.Revision, forged).IsAccepted);
            // Oversized reference.
            Assert.False(journal.SubmitOwnerReaction(other, opening.Revision,
                "act." + new string('0', 1_000_000)).IsAccepted);

            Assert.Equal(before, StoredAuthority(fixture.Path, request.CampaignId));
            Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);

            // The genuine offer still works after every rejected attempt above.
            var accepted = journal.SubmitOwnerReaction(other, opening.Revision, legalRef);
            Assert.True(accepted.IsAccepted);
            var cold = fixture.Open(clock);
            Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
        }
    }

    [Fact]
    public void DecliningTheReactionWindowClosesItAndSurvivesColdReplay()
    {
        var (request, acting, other, fixture, clock) = AdvanceToOpenReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);
            var opening = journal.QueryReactionOwnerOpening(other);
            using var visible = JsonDocument.Parse(opening.VisibleBytes);
            var choices = visible.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            var declineIndex = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "decline");
            Assert.True(declineIndex >= 0, "This fixture's opportunity has no decline offer.");
            Assert.True(journal.SubmitOwnerReaction(other, opening.Revision,
                opening.PublicActionRefs[declineIndex]).IsAccepted);

            var cold = fixture.Open(clock);
            var verified = cold.ReadVerified(request.CampaignId);
            Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);

            // The window is closed: the reacting side has nothing left to resolve.
            Assert.Throws<InvalidOperationException>(() => cold.QueryReactionOwnerOpening(other));
        }
    }
}
