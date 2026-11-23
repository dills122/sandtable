using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Owner ruling (2026-11-22): the cross-audience idempotency-retry finding reported alongside
// PR #514's concurrency probes (SubmitOwnerActionCore's retry lookup matched on
// (campaign_id, action_kind, acting_element_id, owner_public_ref, seat_epoch, owner_revision)
// with no audience predicate at all, so a wrong-side credential resubmitting the correct side's
// own already-accepted (revision, reference) -- which always coincides on seat_epoch for both
// sides' very first action, since epoch is a per-side counter starting at 1 -- was handed that
// side's own cached receipt instead of a rejection) gets the real fix: a new owner_side column
// on accepted_steps (and the matching nullable Side field on the Marten mirror,
// CampaignAcceptedStepEvent), bound into the retry lookup's WHERE clause. See
// CampaignDurableAuthorityJournal.cs's PersistAccepted and
// CampaignDurableAuthorityJournal.Submissions.cs's SubmitOwnerActionCore for the exact change and
// why it is deliberately NOT a StepHash input (no change to the canonical hash chain format).
//
// No migration is needed for either backend: the SQLite journal's own OpenExisting requires an
// exact CurrentSchemaVersion match and already drops every older on-disk format outright ("no
// real data exists" for that format -- its own doc comment), and the new Marten/Postgres field is
// a plain optional JSON property that already-persisted events simply deserialize as null,
// falling back to the pre-fix (audience-blind) retry match for that one historical row only --
// consistent with how every other schema addition in this codebase has worked (there is no
// ALTER TABLE/migration mechanism here at all, issue #330).
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // The exact repro from the original finding: fully sequential, no concurrency needed at all.
    [Fact]
    public void SequentialWrongAudienceResubmissionOfTheOtherSidesExactReferenceIsRejected()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        var reference = Assert.Single(opening.PublicActionRefs);

        var first = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference);
        Assert.True(first.IsAccepted);

        // Other now replays the exact (revision, reference) acting just used, under its own
        // credential -- well after acting's own commit, no race involved.
        var second = journal.SubmitOwnerMovementSegmentCompletion(other, opening.Revision, reference);
        Assert.False(second.IsAccepted);

        Assert.True(Cna.Core.Exercises.CampaignExercises.Reconstruct(
            fixture.Open().ReadVerified(request.CampaignId)).IsVerified);
        // Exactly one accepted step: the fix rejects the replay outright, it does not also
        // let it through as a second, separately-accepted step.
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            Cna.Core.Hosting.CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind));
    }

    // The genuinely concurrent version of the same repro -- previously flaky (roughly 3 of 8
    // runs let the wrong side through), now deterministic across repeated runs.
    [Fact]
    public async Task ConcurrentWrongAudienceAndActingMovementSegmentCompletionSubmissionsAgreeOnOneWinner()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var fixture = new JournalFixture();
            var journal = fixture.Create();
            var (request, acting, other) = AdvanceToMovementSegmentCompletion(journal);
            var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
            var reference = Assert.Single(opening.PublicActionRefs);

            var results = await Task.WhenAll(
                Task.Run(() => fixture.Open().SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference)),
                Task.Run(() => fixture.Open().SubmitOwnerMovementSegmentCompletion(other, opening.Revision, reference)));

            Assert.True(results[0].IsAccepted);
            Assert.False(results[1].IsAccepted);
            Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
                Cna.Core.Hosting.CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind));
            Assert.True(Cna.Core.Exercises.CampaignExercises.Reconstruct(
                fixture.Open().ReadVerified(request.CampaignId)).IsVerified);
        }
    }

    // A second, genuinely different owner-action kind (InitiativeOrder, not Movement Segment
    // Completion), to confirm the fix is in the shared SubmitOwnerActionCore chokepoint and not
    // narrowly specific to one stage.
    [Fact]
    public void SequentialWrongAudienceResubmissionOfInitiativeOrderIsRejected()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, winner, loser) = AdvanceToInitiativeDeclaration(journal);
        var opening = journal.QueryInitiativeOrderOwnerOpening(winner);
        var reference = opening.PublicActionRefs[0];

        var first = journal.SubmitOwnerInitiativeOrder(winner, opening.Revision, reference);
        Assert.True(first.IsAccepted);

        var second = journal.SubmitOwnerInitiativeOrder(loser, opening.Revision, reference);
        Assert.False(second.IsAccepted);

        Assert.True(Cna.Core.Exercises.CampaignExercises.Reconstruct(
            fixture.Open().ReadVerified(request.CampaignId)).IsVerified);
    }

    // The fix must not regress the legitimate case: the SAME side resubmitting its own already-
    // accepted (revision, reference) -- e.g. a real HTTP client retrying after a dropped
    // response -- still gets the identical cached receipt back, not a second accepted step.
    [Fact]
    public void SameAudienceRetryStillCommitsExactlyOnceAfterTheFix()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        var reference = Assert.Single(opening.PublicActionRefs);

        var first = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference);
        Assert.True(first.IsAccepted);
        var retry = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference);
        Assert.True(retry.IsAccepted);
        Assert.Equal(first.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            Cna.Core.Hosting.CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind));
    }
}
