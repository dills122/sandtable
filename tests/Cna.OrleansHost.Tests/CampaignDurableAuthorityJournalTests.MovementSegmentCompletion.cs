using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Owner ruling (release-blocking): the fourth and fifth missing owner openings of this exact
// shape, both found auditing the entire real-HTTP-player chain from campaign creation through
// Operation Stage 1 end to end (scope docs, PR #507 and #509). CompleteMovementSegmentAction
// and StopElementMovementAction (land.position...movement-and-combat.movement) are real,
// mutually exclusive legal candidates CampaignObservationV7ActionDerivation.DeriveOrdinaryMovement
// already produces generically alongside each element's own MoveElementAction options, but the
// dedicated "movement" HTTP stage's own draft type (CampaignContactOwnerMovementCut's
// MovementOffers field) is typed IReadOnlyList<MoveElementAction> -- a hard constraint that can
// never carry either of them, no matter what the shared generic derivation produces. Confirmed
// both fire before any Combat decision has ever been made in the session, so (unlike Reaction)
// neither needs a #394 gap 1 sidestep: built the exact same way as InitiativeOrder/
// ReserveDesignation, a redacted Host wrapper over the generic CampaignExercises.Query/Submit
// path, no new Core logic. Both share one owner opening/HTTP stage ("movement-segment-completion")
// rather than two, since Core's own dispatch ("route is null ? Complete : Stop") makes them
// strictly either-or: a real player querying this one stage always gets whichever of the two
// is actually the live choice, never both, never neither.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Drains every System-audience preamble step and submits whichever player-audience
    // candidate Core lists first at each point (ActFirst, then empty Reserve Designation)
    // -- the exact convention AdvanceToContactCombat's own generic loop already uses -- until
    // the Movement Segment Completion owner opening itself becomes queryable for one side.
    // Stops there without submitting it, leaving the real decision under test still pending.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Acting,
        CampaignSeatCredential Other) AdvanceToMovementSegmentCompletion(CampaignDurableAuthorityJournal journal)
    {
        var request = CombatInfantryRequest();
        var session = journal.CreateCampaign(request, Timing(request));
        var axis = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var commonwealth = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        for (var index = 0; index < 20; index++)
        {
            CampaignSeatCredential? acting = null;
            try { journal.QueryMovementSegmentCompletionOwnerOpening(axis); acting = axis; }
            catch (InvalidOperationException) { }
            if (acting is null)
            {
                try { journal.QueryMovementSegmentCompletionOwnerOpening(commonwealth); acting = commonwealth; }
                catch (InvalidOperationException) { }
            }
            if (acting is not null)
                return (request, acting, acting == axis ? commonwealth : axis);

            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            if (set is null) throw new InvalidOperationException("Nothing pending for any audience.");
            var next = set.Candidates[0];
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, next.ActionId);
            var accepted = journal.Append(request.CampaignId, command);
            if (!accepted.IsAccepted)
                throw new InvalidOperationException("Preamble/movement step was not accepted.");
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("Movement Segment Completion was not reached.");
    }

    [Fact]
    public async Task MovementSegmentCompletionIsOfferedOnlyToTheActingSideAndAcceptedThroughHttp()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToMovementSegmentCompletion(journal);

        // Side-safe: the non-acting side has no opening here at all, same invariant every
        // other asymmetric owner opening already holds.
        Assert.Throws<InvalidOperationException>(() => journal.QueryMovementSegmentCompletionOwnerOpening(other));

        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.Single(opening.PublicActionRefs);
        using (var visible = JsonDocument.Parse(opening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            var choices = visible.RootElement.GetProperty("choices").EnumerateArray()
                .Select(value => value.GetProperty("kind").GetString()).ToArray();
            Assert.Equal(["complete-movement-segment"], choices);
        }

        // The real outward HTTP API, not CampaignDurableAuthorityJournal.Append.
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.Submit(acting, opening.ActingElementId, "movement-segment-completion",
            "movement-segment-completion", opening, opening.PublicActionRefs[0]);

        var cold = fixture.Open();
        var afterHttp = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(afterHttp).IsVerified);

        // Retry through the normal durable Host: idempotent, same canonical receipt.
        var retried = cold.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retried.IsAccepted);

        // The decision is gone: a second query for this (now-resolved) step throws.
        Assert.Throws<InvalidOperationException>(() => cold.QueryMovementSegmentCompletionOwnerOpening(acting));
    }

    [Fact]
    public void MovementSegmentCompletionRejectsStaleForgedAndWrongAudienceSubmissions()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        var before = StoredAuthority(fixture.Path, request.CampaignId);

        // Wrong audience: the other side cannot submit even a well-formed reference.
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(other, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);
        // Stale revision.
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision + 1,
            opening.PublicActionRefs[0]).IsAccepted);
        // Forged reference: well-formed ("act." + 64 hex) but never issued.
        var forged = "act." + new string(opening.PublicActionRefs[0][4] == '0' ? '1' : '0', 64);
        Assert.NotEqual(opening.PublicActionRefs[0], forged);
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, forged).IsAccepted);
        // Oversized reference.
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision,
            "act." + new string('0', 1_000_000)).IsAccepted);

        Assert.Equal(before, StoredAuthority(fixture.Path, request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);

        // The genuine offer still works after every rejected attempt above.
        var accepted = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        var cold = fixture.Open();
        Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void MovementSegmentCompletionAcceptanceSurvivesColdRestartAndDrivesTheGameForward()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.True(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);

        // Cold replay from durable storage alone, fresh journal handle.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified);

        // The game actually moved forward: Breakdown auto-resolves (System audience, already
        // covered by the UX-001 auto-drain -- not a player gap) as soon as a real owner
        // opening query touches it, and first-side Combat Selection becomes reachable next --
        // the real, end-to-end proof, not just the next raw Core candidate. This also proves
        // Movement Segment Completion's own Selection-round initialization (added alongside
        // Movement's and SecondMovementCompletion's own, same #394 gap 4 precedent) actually
        // works, not just that the opening/HTTP wiring exists.
        var combatElementId = acting.Audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
        var combat = cold.QueryCombatOwnerOpening(acting, combatElementId);
        Assert.NotEmpty(combat.PublicActionRefs);
    }

    // Extends AdvanceToMovementSegmentCompletion by one real move: submits the acting side's
    // own infantry's first offered move through the already-proven "movement" owner opening
    // (QueryOwnerOpening/SubmitOwnerMovement), which leaves CampaignObservationV7's own
    // ActiveMovement/route non-null for that element -- the exact condition that replaces
    // CompleteMovementSegmentAction with StopElementMovementAction in the generic legal set.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Acting,
        CampaignSeatCredential Other) AdvanceToActiveMovementRoute(CampaignDurableAuthorityJournal journal)
    {
        var (request, acting, other) = AdvanceToMovementSegmentCompletion(journal);
        var elementId = CombatInfantryElementId(acting);
        var movement = journal.QueryOwnerOpening(acting, elementId);
        Assert.NotEmpty(movement.PublicActionRefs);
        Assert.True(journal.SubmitOwnerMovement(acting, elementId, movement.Revision,
            movement.PublicActionRefs[0]).IsAccepted);
        return (request, acting, other);
    }

    [Fact]
    public async Task StopElementMovementIsOfferedInPlaceOfCompleteOnceARouteIsActiveAndAcceptedThroughHttp()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToActiveMovementRoute(journal);

        Assert.Throws<InvalidOperationException>(() => journal.QueryMovementSegmentCompletionOwnerOpening(other));

        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.Single(opening.PublicActionRefs);
        using (var visible = JsonDocument.Parse(opening.VisibleBytes))
        {
            var choices = visible.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            Assert.Single(choices);
            Assert.Equal("stop-element-movement", choices[0].GetProperty("kind").GetString());
            Assert.True(choices[0].TryGetProperty("routeId", out _));
        }

        // The real outward HTTP API, not CampaignDurableAuthorityJournal.Append.
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.Submit(acting, opening.ActingElementId, "movement-segment-completion",
            "movement-segment-completion", opening, opening.PublicActionRefs[0]);

        var cold = fixture.Open();
        var afterHttp = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(afterHttp).IsVerified);

        // Retry through the normal durable Host: idempotent, same canonical receipt.
        var retried = cold.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retried.IsAccepted);

        // The route is closed: re-querying this same opening either offers a fresh Complete
        // (route cleared) or throws (resolved past Movement entirely) -- never the exact same
        // stale Stop reference again.
        try
        {
            var again = cold.QueryMovementSegmentCompletionOwnerOpening(acting);
            Assert.DoesNotContain(opening.PublicActionRefs[0], again.PublicActionRefs);
        }
        catch (InvalidOperationException) { }
    }

    [Fact]
    public void StopElementMovementRejectsStaleForgedAndWrongAudienceSubmissions()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToActiveMovementRoute(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        var before = StoredAuthority(fixture.Path, request.CampaignId);

        // Wrong audience: the other side cannot submit even a well-formed reference.
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(other, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);
        // Stale revision.
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision + 1,
            opening.PublicActionRefs[0]).IsAccepted);
        // Forged reference: well-formed ("act." + 64 hex) but never issued.
        var forged = "act." + new string(opening.PublicActionRefs[0][4] == '0' ? '1' : '0', 64);
        Assert.NotEqual(opening.PublicActionRefs[0], forged);
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, forged).IsAccepted);
        // Oversized reference.
        Assert.False(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision,
            "act." + new string('0', 1_000_000)).IsAccepted);

        Assert.Equal(before, StoredAuthority(fixture.Path, request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);

        // The genuine offer still works after every rejected attempt above.
        var accepted = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        var cold = fixture.Open();
        Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void StopElementMovementAcceptanceSurvivesColdRestartAndDrivesTheGameForward()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToActiveMovementRoute(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.True(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);

        // Cold replay from durable storage alone, fresh journal handle.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);

        // The game actually moved forward: querying this same opening again either offers
        // Complete now (route cleared) or throws (resolved past Movement entirely) -- never
        // the exact same Stop reference again.
        try
        {
            var again = cold.QueryMovementSegmentCompletionOwnerOpening(acting);
            Assert.DoesNotContain(opening.PublicActionRefs[0], again.PublicActionRefs);
        }
        catch (InvalidOperationException)
        {
            // Resolved past Movement Segment Completion entirely -- also a legitimate forward
            // outcome (e.g. the element had no more legal moves, so the segment auto-completed).
        }
    }
}
