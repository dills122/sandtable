using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// E4b R3: retained native windows use the existing authenticated owner route.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Preserve both infantry counters through the first side, then use the same
    // north offer as the Core native-pending fixture. No SQLite-specific copying.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Mover,
        CampaignSeatCredential Other, JournalFixture Fixture, ManualClock Clock)
        AdvanceToSecondSideReactionWindow()
    {
        var baseFixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = baseFixture.Create(clock);
        var request = ContactRequest();
        AdvanceToContactCombat(journal, request, Timing(request), preserveAdjacentInfantry: true);
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
        Assert.True(journal.SubmitOwnerSecondReserve(commonwealth, secondReserve.Revision,
            secondReserve.PublicActionRefs[^1]).IsAccepted);
        var movement = journal.QuerySecondMovementOwnerOpening(commonwealth, "commonwealth-infantry");
        using var visible = JsonDocument.Parse(movement.VisibleBytes);
        var offers = visible.RootElement.GetProperty("movementOffers").EnumerateArray().ToArray();
        var north = Array.FindIndex(offers, value => value.GetProperty("destinationLocationId").GetString() == "north");
        Assert.True(north >= 0);
        Assert.True(journal.SubmitOwnerSecondMovement(commonwealth, "commonwealth-infantry",
            movement.Revision, movement.PublicActionRefs[north]).IsAccepted);
        Assert.True(CampaignHostAdapter.IsNativeReactionPending(journal.ReadVerified(request.CampaignId)));
        Assert.Contains(journal.QueryOpenForSeat(axis).Open, entry => entry.OpeningStage == "reaction" && !entry.RuleNotImplemented);
        return (request, commonwealth, axis, baseFixture, clock);
    }

    [Fact]
    public async Task NativeReactionOwnerSubmissionAndRetrySurviveColdReplay()
    {
        var (request, mover, other, fixture, clock) = AdvanceToSecondSideReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);
            const string actor = "land.reaction";
            long revision;
            string reference;
            (HttpStatusCode Status, string Body) accepted;
            CampaignVerification? liveFingerprint;
            await using (var outward = await OwnerHttpHarness.Start(journal, request.CampaignId))
            {
                var discovery = await outward.OpenForSeatResponse(other);
                Assert.Equal(HttpStatusCode.OK, discovery.Status);
                var entry = Assert.Single(ParseOpen(discovery.Body));
                Assert.Equal("reaction", entry.OpeningStage);
                Assert.Equal("reaction", entry.ActionStage);
                Assert.False(entry.RuleNotImplemented);
                var waiting = await outward.OpenForSeatResponse(mover);
                Assert.Equal(HttpStatusCode.OK, waiting.Status);
                Assert.Empty(ParseOpen(waiting.Body));
                var opening = await QueryOpeningForOpenSeatTest(outward, other, actor, "reaction");
                revision = opening.Revision;
                var choices = opening.Visible.GetProperty("choices").EnumerateArray().ToArray();
                var decline = Array.FindIndex(choices, choice => choice.GetProperty("kind").GetString() == "decline");
                Assert.True(decline >= 0);
                reference = opening.Refs[decline];
                Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(mover, actor, "reaction")).Status);
                Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(mover, actor, "reaction", revision, reference)).Status);
                Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(other, actor, "reaction", revision + 1, reference)).Status);
                accepted = await outward.SubmitResponse(other, actor, "reaction", revision, reference);
                Assert.True(accepted.Status == HttpStatusCode.OK, accepted.Body);
                Assert.Equal(accepted, await outward.SubmitResponse(other, actor, "reaction", revision, reference));
                liveFingerprint = await VerifyReactionHttp(outward, request.CampaignId);
                Assert.NotNull(liveFingerprint);
                Assert.True(liveFingerprint.IsVerified);
            }
            var verification = journal.VerifyReadOnly(request.CampaignId);
            Assert.NotNull(verification);
            Assert.True(verification.IsVerified);
            var cold = fixture.Open(clock);
            Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
            Assert.Equal(verification, cold.VerifyReadOnly(request.CampaignId));
            Assert.Equal(liveFingerprint, verification);
            await using var reopened = await OwnerHttpHarness.Start(cold, request.CampaignId);
            Assert.Equal(accepted, await reopened.SubmitResponse(other, actor, "reaction", revision, reference));
            Assert.Equal(HttpStatusCode.Conflict, (await reopened.SubmitResponse(mover, actor, "reaction", revision, reference)).Status);
            Assert.Equal(liveFingerprint, await VerifyReactionHttp(reopened, request.CampaignId));

            // An eligible Stage 1 second-side window resumes its suspended Movement
            // after the explicit decline, just as the seed-6 empty window does after auto-close.
            var resumed = await reopened.OpenForSeatResponse(mover);
            Assert.Equal(HttpStatusCode.OK, resumed.Status);
            var completion = Assert.Single(ParseOpen(resumed.Body), entry => entry.OpeningStage == "second-movement-completion");
            Assert.False(completion.RuleNotImplemented);
            var completionActor = Assert.Single(completion.Elements);
            var completionOpening = await QueryOpeningForOpenSeatTest(reopened, mover, completionActor, completion.OpeningStage);
            var completed = await reopened.SubmitResponse(mover, completionActor, completion.ActionStage,
                completionOpening.Revision, Assert.Single(completionOpening.Refs));
            Assert.True(completed.Status == HttpStatusCode.OK, completed.Body);
            var finalFingerprint = await VerifyReactionHttp(reopened, request.CampaignId);
            Assert.NotNull(finalFingerprint);
            Assert.True(finalFingerprint.IsVerified);
            Assert.Equal(finalFingerprint, fixture.Open(clock).VerifyReadOnly(request.CampaignId));
        }
    }

    private static async Task<CampaignVerification?> VerifyReactionHttp(OwnerHttpHarness harness, string campaignId)
    {
        using var client = new HttpClient { BaseAddress = harness.Address };
        client.DefaultRequestHeaders.Add("X-Sandtable-Operator", OperatorSecret);
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        using var response = await client.GetAsync($"/api/v2/campaigns/{campaignId}/verification",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonSerializer.Deserialize<CampaignVerification>(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken), JsonSerializerOptions.Web);
    }

    [Fact]
    public async Task NativeReactionMoveThenCompleteRetainsBothEventVersions()
    {
        var (request, mover, other, fixture, clock) = AdvanceToSecondSideReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);
            var opening = journal.QueryReactionOwnerOpening(other);
            using var first = JsonDocument.Parse(opening.VisibleBytes);
            var move = Array.FindIndex(first.RootElement.GetProperty("choices").EnumerateArray().ToArray(),
                choice => choice.GetProperty("kind").GetString() == "move");
            Assert.True(move >= 0);
            var accepted = journal.SubmitOwnerReaction(other, opening.Revision, opening.PublicActionRefs[move]);
            Assert.True(accepted.IsAccepted);
            var cold = fixture.Open(clock);
            var retry = cold.SubmitOwnerReaction(other, opening.Revision, opening.PublicActionRefs[move]);
            Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
            Assert.Equal(journal.VerifyReadOnly(request.CampaignId), cold.VerifyReadOnly(request.CampaignId));
            Assert.Empty(cold.QueryOpenForSeat(mover).Open);
            var next = cold.QueryReactionOwnerOpening(other);
            Assert.NotEqual(opening.Revision, next.Revision);
            var unused = opening.PublicActionRefs.First(reference => reference != opening.PublicActionRefs[move]);
            var stale = cold.SubmitOwnerReaction(other, opening.Revision, unused);
            Assert.False(stale.IsAccepted);
            Assert.Equal(CampaignOwnerSubmissionResult.StaleOpeningCode, stale.RejectionCode);
            using var second = JsonDocument.Parse(next.VisibleBytes);
            var complete = Array.FindIndex(second.RootElement.GetProperty("choices").EnumerateArray().ToArray(),
                choice => choice.GetProperty("kind").GetString() == "complete");
            Assert.True(complete >= 0);
            Assert.True(cold.SubmitOwnerReaction(other, next.Revision, next.PublicActionRefs[complete]).IsAccepted);
            var events = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
            using var moved = JsonDocument.Parse(events[^2]);
            using var closed = JsonDocument.Parse(events[^1]);
            Assert.Equal(59, moved.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(60, closed.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("participantCompleted", closed.RootElement.GetProperty("closeKind").GetString());
            Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        }
    }

    [Fact]
    public async Task NativeReactionExplicitSystemTimeoutReplays()
    {
        var (request, _, _, fixture, clock) = AdvanceToSecondSideReactionWindow();
        using (fixture)
        {
            var journal = fixture.Open(clock);
            var session = journal.ReadVerified(request.CampaignId);
            var system = CampaignHostAdapter.QuerySystem(session).ActionSet!;
            var timeout = Assert.Single(system.Candidates, candidate => candidate.Kind == "close-reaction-window-timeout");
            var command = new CampaignActionSubmission(1, request.CampaignId, system.StateVersion,
                system.PositionId, CampaignActionAudience.System, timeout.ActionId);
            Assert.True(journal.Append(request.CampaignId, command).IsAccepted);
            var events = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
            using var closed = JsonDocument.Parse(events[^1]);
            Assert.Equal(60, closed.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("timeout", closed.RootElement.GetProperty("closeKind").GetString());
            var cold = fixture.Open(clock);
            Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
            Assert.Equal(journal.VerifyReadOnly(request.CampaignId), cold.VerifyReadOnly(request.CampaignId));
        }
    }
}
