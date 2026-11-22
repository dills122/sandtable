using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

// Post-E2E adversarial scenario testing (2026-11-22, round 2), concurrency probe: "Axis and
// Commonwealth both trying to act when only one should be able to" was confirmed generically
// safe via the shared revision-check infrastructure before this release's two newest pieces of
// code landed -- the UX-001 System-audience auto-drain (PR #508) and the
// CompleteMovementSegmentAction/StopElementMovementAction owner openings (PR #511). Reuses the
// exact race idiom ArrivalAdmissionBoundary.cs's own ConcurrentScheduledArrivalRetriesCommitOnce
// already established (two independent fixture.Open() handles, Task.WhenAll, no shared
// in-process state).
//
// A third probe in this family -- the acting side's own real submission racing the other side's
// replay of that exact (revision, reference) under its own credential -- surfaced a real,
// separately-reportable, pre-existing finding in the shared SubmitOwnerActionCore idempotency-
// retry lookup (not specific to Movement Segment Completion/Stop Element Movement, and not
// requiring genuine concurrency to reproduce -- a plain sequential replay with a coincidentally-
// matching seat epoch, which both sides' first-ever action always has, triggers it 100% of the
// time). Reported separately rather than committed here as a flaking regression test; see the
// session report for the precise repro and why it isn't a fix this pass should make
// unilaterally (there is no `side`/audience column on accepted_steps to add to the retry
// predicate without a schema change).
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task ConcurrentIdenticalMovementSegmentCompletionSubmissionsCommitExactlyOnce()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        var reference = Assert.Single(opening.PublicActionRefs);

        // The acting side's own two concurrent clients both racing to submit the identical
        // accepted decision (the double-request-during-a-slow-response scenario a real HTTP
        // retry would produce).
        var results = await Task.WhenAll(
            Task.Run(() => fixture.Open().SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference)),
            Task.Run(() => fixture.Open().SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference)));

        Assert.True(results[0].IsAccepted);
        Assert.True(results[1].IsAccepted);
        Assert.Equal(results[0].CanonicalReceipt, results[1].CanonicalReceipt);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open().ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public async Task ConcurrentCombatSelectionQueriesTriggerTheSystemAutoDrainExactlyOnce()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.True(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);
        var combatElementId = acting.Audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
        var beforeDrainCount = TotalAcceptedSteps(fixture.Path, request.CampaignId);

        // Two genuinely concurrent threads both performing the one query that drains the
        // System-audience Breakdown step as a side effect (CampaignDurableAuthorityJournal.
        // Openings.cs's own C3b remarks) at the same time.
        var openings = await Task.WhenAll(
            Task.Run(() => fixture.Open().QueryCombatOwnerOpening(acting, combatElementId)),
            Task.Run(() => fixture.Open().QueryCombatOwnerOpening(acting, combatElementId)));

        Assert.Equal(openings[0].Revision, openings[1].Revision);
        Assert.Equal(openings[0].VisibleBytes, openings[1].VisibleBytes);
        Assert.Equal(openings[0].PublicActionRefs, openings[1].PublicActionRefs);
        Assert.NotEmpty(openings[0].PublicActionRefs);

        // Whatever the drain committed, it committed it exactly once: a single query's own
        // delta (from a fresh campaign built the same way) is the same as two racing queries'
        // combined delta.
        var afterDrainCount = TotalAcceptedSteps(fixture.Path, request.CampaignId);
        using var controlFixture = new JournalFixture();
        var controlJournal = controlFixture.Create();
        var (controlRequest, controlActing, _) = AdvanceToMovementSegmentCompletion(controlJournal);
        var controlOpening = controlJournal.QueryMovementSegmentCompletionOwnerOpening(controlActing);
        Assert.True(controlJournal.SubmitOwnerMovementSegmentCompletion(controlActing, controlOpening.Revision,
            controlOpening.PublicActionRefs[0]).IsAccepted);
        var controlBefore = TotalAcceptedSteps(controlFixture.Path, controlRequest.CampaignId);
        var controlCombatElementId = controlActing.Audience == CampaignActionAudience.Axis
            ? "axis-infantry" : "commonwealth-infantry";
        _ = controlJournal.QueryCombatOwnerOpening(controlActing, controlCombatElementId);
        var controlAfter = TotalAcceptedSteps(controlFixture.Path, controlRequest.CampaignId);

        Assert.Equal(controlAfter - controlBefore, afterDrainCount - beforeDrainCount);
        Assert.True(CampaignExercises.Reconstruct(fixture.Open().ReadVerified(request.CampaignId)).IsVerified);
    }

    private static long TotalAcceptedSteps(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", campaignId);
        return (long)query.ExecuteScalar()!;
    }
}
