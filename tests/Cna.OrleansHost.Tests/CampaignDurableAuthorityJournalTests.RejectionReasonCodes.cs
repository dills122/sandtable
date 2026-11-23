using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Outward API shape-review (2026-11-23), Recommendation 1: SubmitOwnerActionCore's 20+
// Rejected() call sites previously all collapsed to the same indistinguishable null
// RejectionCode. These tests prove a couple of the now-distinguished call sites actually
// surface their specific reason code, not just that the submission still fails.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public void StaleOpeningRevisionIsRejectedWithStaleOpeningCode()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (_, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        var reference = Assert.Single(opening.PublicActionRefs);

        // A revision past the one this opening was actually offered at -- no owner_openings
        // row exists for (campaignId, side, actingElementId, epoch, revision), so this falls
        // through the same "row not found" path a genuinely stale/replayed revision would.
        var result = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision + 1, reference);

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignOwnerSubmissionResult.StaleOpeningCode, result.RejectionCode);
    }

    [Fact]
    public void UnknownPublicActionReferenceIsRejectedWithUnknownActionReferenceCode()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (_, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);

        // Syntactically a valid public reference ("act." + 64 hex-shaped characters), but
        // never actually issued for this opening -- no owner_actions row matches it.
        var unknownReference = "act." + new string('0', 64);

        var result = journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, unknownReference);

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignOwnerSubmissionResult.UnknownActionReferenceCode, result.RejectionCode);
    }
}
