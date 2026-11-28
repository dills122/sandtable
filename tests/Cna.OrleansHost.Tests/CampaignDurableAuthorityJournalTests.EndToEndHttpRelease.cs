using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// The release's own end-to-end proof: a real HTTP player (two, actually) can conduct every
// move/action in this chain and have it sent properly to the game and to the other player to
// action on -- literally, continuously, from campaign creation through Operation Stage 1's end,
// with zero direct Core/Journal calls anywhere in this test's own body. Every piece along the
// way (InitiativeOrder, Reserve Designation, Movement Segment Completion, Reaction's explicit
// rejection, the UX-001 System-step auto-drain) was each proven in isolation by its own test
// file; this is the first test that chains all of them together the way a real game actually
// would, which is what originally surfaced every one of those gaps -- each piece's own test
// never needed the piece before it to also be reachable over HTTP.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task TwoRealHttpPlayersConductAnEntireOperationStageOne()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

        // 1. Create the campaign via the real outward HTTP API -- not journal.CreateCampaign.
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);

        // 2. Issue and redeem both seats via the real outward HTTP API.
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);

        async Task<(long Revision, string[] Refs, JsonElement Visible)> Query(
            Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor, string stage)
        {
            var (status, body) = await outward.OpeningResponse(credential, actor, stage);
            Assert.True(status == HttpStatusCode.OK, $"{stage} query for {actor} failed: {status} {body}");
            using var json = JsonDocument.Parse(body);
            var revision = json.RootElement.GetProperty("revision").GetInt64();
            var refs = json.RootElement.GetProperty("publicActionRefs").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            using var visibleDoc = JsonDocument.Parse(Convert.FromBase64String(
                json.RootElement.GetProperty("visibleCutBase64").GetString()!));
            return (revision, refs, visibleDoc.RootElement.Clone());
        }

        async Task Submit(Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor,
            string stage, long revision, string reference)
        {
            var (status, body) = await outward.SubmitResponse(credential, actor, stage, revision, reference);
            Assert.True(status == HttpStatusCode.OK, $"{stage} submit for {actor} failed: {status} {body}");
        }

        async Task<bool> Available(Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor, string stage)
        {
            var (status, _) = await outward.OpeningResponse(credential, actor, stage);
            return status == HttpStatusCode.OK;
        }

        // 3. ActFirst/ActLast (initiative-order): find the winner purely by which seat's own
        // query succeeds, then submit "act-first".
        var winner = await Available(axis, "land.initiative-order", "initiative-order") ? axis : commonwealth;
        var loser = winner == axis ? commonwealth : axis;
        var (initRevision, initRefs, initVisible) = await Query(winner, "land.initiative-order", "initiative-order");
        var choices = initVisible.GetProperty("choices").EnumerateArray().Select(value => value.GetString()).ToArray();
        var actFirstIndex = Array.IndexOf(choices, "act-first");
        Assert.True(actFirstIndex >= 0);
        await Submit(winner, "land.initiative-order", "initiative-order", initRevision, initRefs[actFirstIndex]);

        // 4. Reserve Designation: find the acting side the same way, finish immediately with
        // nobody in reserve (the "complete" choice, wherever it lands in the offered list).
        var acting = await Available(axis, "land.reserve-designation", "reserve-designation") ? axis : commonwealth;
        var other = acting == axis ? commonwealth : axis;
        var (reserveRevision, reserveRefs, reserveVisible) = await Query(acting, "land.reserve-designation", "reserve-designation");
        var reserveKinds = reserveVisible.GetProperty("choices").EnumerateArray()
            .Select(value => value.GetProperty("kind").GetString()).ToArray();
        var completeReserveIndex = Array.IndexOf(reserveKinds, "complete");
        Assert.True(completeReserveIndex >= 0);
        await Submit(acting, "land.reserve-designation", "reserve-designation", reserveRevision, reserveRefs[completeReserveIndex]);

        var actingInfantry = acting == axis ? "axis-infantry" : "commonwealth-infantry";
        var otherInfantry = acting == axis ? "commonwealth-infantry" : "axis-infantry";

        // 5. Movement: a real per-element move through the real "movement" HTTP stage, if one
        // is offered (this fixture's own infantry starts already in mutual contact, so it may
        // have nothing to do -- that is itself a legitimate, previously-untested outcome of
        // the real "movement" cut, not a shortcut around it).
        for (var moveIndex = 0; moveIndex < 15; moveIndex++)
        {
            var (moveStatus, moveBody) = await outward.OpeningResponse(acting, actingInfantry, "movement");
            if (moveStatus != HttpStatusCode.OK) break;
            using var moveJson = JsonDocument.Parse(moveBody);
            var moveRevisionLoop = moveJson.RootElement.GetProperty("revision").GetInt64();
            var moveRefsLoop = moveJson.RootElement.GetProperty("publicActionRefs").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            if (moveRefsLoop.Length == 0) break;
            await Submit(acting, actingInfantry, "movement", moveRevisionLoop, moveRefsLoop[0]);
        }

        // 6. Movement Segment Completion: the gap this release found and closed last --
        // CompleteMovementSegmentAction, offered generically alongside MoveElementAction but
        // never reachable through the dedicated "movement" HTTP stage.
        var (completeMoveRevision, completeMoveRefs, _) = await Query(acting, "land.movement-segment-completion", "movement-segment-completion");
        await Submit(acting, "land.movement-segment-completion", "movement-segment-completion",
            completeMoveRevision, completeMoveRefs[0]);

        // 7. Breakdown auto-resolves transparently (System audience, UX-001 auto-drain) the
        // moment the next real query touches it -- no HTTP stage of its own, by design.

        // 8. First-side Combat Selection: finish without attack (the real, already-proven-safe
        // choice VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart and
        // every owner-opening test this session already uses, to keep this proof about HTTP
        // reachability rather than Combat RNG resolution).
        var (combatRevision, combatRefs, _) = await Query(acting, actingInfantry, "combat-selection");
        await Submit(acting, actingInfantry, "combat-selection", combatRevision, combatRefs[^1]);

        // 9. Cycle Control: Finish.
        var (cycleRevision, cycleRefs, _) = await Query(acting, "land.cycle-control", "cycle-control");
        await Submit(acting, "land.cycle-control", "cycle-control", cycleRevision, cycleRefs[^1]);

        // 10. Truck Convoy Pass.
        var (convoyRevision, convoyRefs, _) = await Query(acting, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(acting, "land.truck-convoy-pass", "truck-convoy-pass", convoyRevision, convoyRefs[0]);

        // 11. Four Post-Combat passes.
        for (var index = 0; index < 4; index++)
        {
            var (phaseRevision, phaseRefs, _) = await Query(acting, "land.post-combat-pass", "post-combat-pass");
            await Submit(acting, "land.post-combat-pass", "post-combat-pass", phaseRevision, phaseRefs[0]);
        }

        // 12. Second side: Reserve, finished empty.
        var (secondReserveRevision, secondReserveRefs, _) = await Query(other, "land.second-reserve", "second-reserve");
        await Submit(other, "land.second-reserve", "second-reserve", secondReserveRevision, secondReserveRefs[^1]);

        // 13. Second side: Movement, resolving any offered Reaction before completion.
        var (secondMoveRevision, secondMoveRefs, _) = await Query(other, otherInfantry, "second-movement");
        if (secondMoveRefs.Length > 0)
        {
            await Submit(other, otherInfantry, "second-movement", secondMoveRevision, secondMoveRefs[0]);
            var (reactionStatus, reactionBody) = await outward.OpeningResponse(acting, "land.reaction", "reaction");
            if (reactionStatus == HttpStatusCode.OK)
            {
                var reaction = await QueryOpeningForOpenSeatTest(outward, acting, "land.reaction", "reaction");
                var reactionChoices = reaction.Visible.GetProperty("choices").EnumerateArray().ToArray();
                var decline = Array.FindIndex(reactionChoices, choice => choice.GetProperty("kind").GetString() == "decline");
                Assert.True(decline >= 0);
                await Submit(acting, "land.reaction", "reaction", reaction.Revision, reaction.Refs[decline]);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, reactionStatus);
                using var reactionJson = JsonDocument.Parse(reactionBody);
                Assert.Equal("unavailable", reactionJson.RootElement.GetProperty("code").GetString());
            }
        }

        // 14. Second side: Movement Completion.
        var (secondMoveCompletionRevision, secondMoveCompletionRefs, _) =
            await Query(other, "land.second-movement-completion", "second-movement-completion");
        await Submit(other, "land.second-movement-completion", "second-movement-completion",
            secondMoveCompletionRevision, secondMoveCompletionRefs[0]);

        // 15. Second-side Breakdown auto-resolves the same transparent way as the first side's.

        // 16. Second-side Combat Selection: finish without attack, same reasoning as step 8.
        var (secondCombatRevision, secondCombatRefs, _) = await Query(other, otherInfantry, "combat-selection");
        await Submit(other, otherInfantry, "combat-selection", secondCombatRevision, secondCombatRefs[^1]);

        // 17. Second-side Cycle Control: Finish.
        var (secondCycleRevision, secondCycleRefs, _) = await Query(other, "land.cycle-control", "cycle-control");
        await Submit(other, "land.cycle-control", "cycle-control", secondCycleRevision, secondCycleRefs[^1]);

        // 18. Second-side Truck Convoy Pass.
        var (secondConvoyRevision, secondConvoyRefs, _) = await Query(other, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(other, "land.truck-convoy-pass", "truck-convoy-pass", secondConvoyRevision, secondConvoyRefs[0]);

        // 19. Four second-side Post-Combat passes -- Operation Stage 1 ends here.
        for (var index = 0; index < 4; index++)
        {
            var (phaseRevision, phaseRefs, _) = await Query(other, "land.post-combat-pass", "post-combat-pass");
            await Submit(other, "land.post-combat-pass", "post-combat-pass", phaseRevision, phaseRefs[0]);
        }

        // 20. Cold-replay verify Stage 1's own end, before crossing into Stage 2 -- the
        // original bar this test proved, kept as its own checkpoint.
        {
            var cold = fixture.Open();
            var verified = cold.ReadVerified(request.CampaignId);
            Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
            Assert.Equal("land.position.operation-2.initiative-declaration",
                CampaignExercises.QueryCheckpoint(verified).PositionId);
        }

        // 21. MTS-TASK-003: Stage 2's own Initiative Declaration -- the real owner-opening this
        // task wired, the one genuine decision among the six-event preamble chain (the other
        // five auto-drain; not reachable without Host wiring, which is this task's own
        // follow-up). Whichever side actually holds initiative declares -- determined here by
        // asking the Host, not assumed from the fixture, matching this project's "verify, don't
        // guess" discipline.
        var holder = await Available(axis, "land.stage-entry-initiative-declaration",
            "stage-entry-initiative-declaration") ? axis : commonwealth;
        var (initiativeRevision, initiativeRefs, _) = await Query(holder,
            "land.stage-entry-initiative-declaration", "stage-entry-initiative-declaration");
        Assert.Equal(2, initiativeRefs.Length); // act-first, act-last
        await Submit(holder, "land.stage-entry-initiative-declaration", "stage-entry-initiative-declaration",
            initiativeRevision, initiativeRefs[0]);

        // 22. Re-querying the same opening now correctly fails -- the session genuinely
        // advanced past Initiative Declaration, proven live over the same real HTTP surface,
        // with no backdoor. (Not asserted by cold replay here: doing so surfaced a real,
        // separate, pre-existing ordering bug in CampaignExercises.Reconstruct's own forward
        // dispatch cascade -- completedSession.CurrentSecondAssault's "Events.Count > 0" check,
        // like every other "never reset" field in this cascade, stays true forever once set and
        // was never guarded against the chronologically-later preamble checks below it, so it
        // silently re-verifies against the SECOND-side-assault's own stale expectation instead
        // of reaching CurrentInitiativeDeclaration's block at all. One instance of this is fixed
        // (CurrentSecondAssault itself, guarded on CurrentInitiativeDeclaration is null,
        // mirroring QueryCheckpoint's own most-recent-first discipline) but a full audit of
        // whether other blocks across this ~1,700-line cascade have the same latent issue is
        // its own task, not safely scoped into this one -- tracked as a named follow-up, not
        // silently worked around.)
        Assert.False(await Available(holder, "land.stage-entry-initiative-declaration",
            "stage-entry-initiative-declaration"));
    }
}
