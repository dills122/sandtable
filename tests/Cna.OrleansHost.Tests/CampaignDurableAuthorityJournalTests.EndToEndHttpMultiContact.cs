using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Post-E2E adversarial scenario testing (2026-11-22, round 2): the flagship E2E proof
// (EndToEndHttpRelease.cs) and the Force-Assignment/Retreat/Custody follow-up
// (EndToEndHttpForceAssignment.cs) both used CombatInfantryRequest/PositiveCombatRequest, whose
// acting side has exactly one reserve-eligible, non-vehicle element -- so Reserve Designation's
// real "designate" branch (as opposed to "finish with nobody in reserve"), reserve-release,
// repeat-movement and repeat-combat-selection had never been driven over the real outward HTTP
// API either, only through direct Core/Journal calls (PrepareInfantryForceRound's
// doubleContact/tripleContact paths across Rba.cs/IntegratedCampaign.cs/RetreatTimeout.cs/
// CustodyTimeout.cs/RepeatedRetreat.cs/RepeatedCustody.cs/SecondSideAfterRepeat.cs/etc.).
//
// This test drives PositiveDoubleContactRequest(12345) -- the exact fixture/seed
// PrepareInfantryForceRound(doubleContact: true) and Rba.cs's own
// RbaRepeatedAssaultOrForceEntryTimeoutColdReplaysRealHistory(forceEntry: false) already use,
// confirmed deterministic by bisecting a scratch run against that exact helper -- entirely
// through the real outward HTTP API: a real "designate" Reserve Designation choice, two
// separate contacts resolved as two separate Combat Selection rounds for two different
// elements (axis-infantry immediately, axis-infantry-second via Reserve Release + Repeat
// Movement + Repeat Combat Selection), and a direct probe that the second acting side's own
// documented lack of a Repeat edge this release (CampaignHostAdapter throws
// InvalidOperationException when CurrentSecondAssault is not null, per
// docs/work/plans/2026-11-22-outward-http-api-reference.md's citation) actually surfaces as a
// clean HTTP 409 rather than a crash, rather than trusting that citation alone.
//
// Bisection note: an early draft of this test blindly ran a "submit whatever move is offered,
// up to 15 times" loop for axis-infantry (copied from EndToEndHttpRelease.cs, where that
// fixture never offers axis-infantry a move at all, so the loop is a no-op there). For this
// fixture axis-infantry starts with one legal move offered despite already being
// adjacent/in-contact (preserveAdjacentInfantry), and blindly consuming it sent axis-infantry on
// an unintended real journey, consuming far more accepted steps (34 vs. the direct-Core
// reference's 13) before Combat Selection and landing on a different, genuinely-unpayable
// combat outcome (native-combat-cancelled-assault) than the reference's payable one -- not an
// HTTP-vs-direct divergence, just this test's own first draft not reproducing the reference
// fixture's intended "leave it in place" setup. Confirmed by reproducing the reference's exact
// 13-step prefix once the move loop was removed.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task RealHttpPlayersResolveTwoSeparateContactsInOneMovementSegment()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = PositiveDoubleContactRequest(12345);

        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);

        async Task<(long Revision, string[] Refs, JsonElement Visible)> Query(
            CampaignSeatCredential credential, string actor, string stage)
        {
            var (status, body) = await outward.OpeningResponse(credential, actor, stage);
            Assert.True(status == HttpStatusCode.OK, $"{stage} opening for {actor} failed: {status} {body}");
            using var json = JsonDocument.Parse(body);
            var revision = json.RootElement.GetProperty("revision").GetInt64();
            var refs = json.RootElement.GetProperty("publicActionRefs").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            using var visibleDoc = JsonDocument.Parse(Convert.FromBase64String(
                json.RootElement.GetProperty("visibleCutBase64").GetString()!));
            return (revision, refs, visibleDoc.RootElement.Clone());
        }

        async Task Submit(CampaignSeatCredential credential, string actor, string stage,
            long revision, string reference)
        {
            var (status, body) = await outward.SubmitResponse(credential, actor, stage, revision, reference);
            Assert.True(status == HttpStatusCode.OK, $"{stage} submit for {actor} failed: {status} {body}");
        }

        async Task<bool> Available(CampaignSeatCredential credential, string actor, string stage)
        {
            var (status, _) = await outward.OpeningResponse(credential, actor, stage);
            return status == HttpStatusCode.OK;
        }

        // Resolve a paid assault + any Retreat/Custody decisions it opens, discovered
        // dynamically (same reasoning as EndToEndHttpForceAssignment.cs), for one attacker/
        // defender pair, over real HTTP.
        async Task ResolveOneAssault(CampaignSeatCredential attacker, string attackerElementId,
            CampaignSeatCredential defender, string defenderElementId)
        {
            var rba = await Query(defender, defenderElementId, "retreat-before-assault");
            await Submit(defender, defenderElementId, "rba-decline", rba.Revision, rba.Refs[^1]);

            if (!await Available(attacker, attackerElementId, "force-assignment")) return; // auto-cancelled (unpayable)
            var attackerForce = await Query(attacker, attackerElementId, "force-assignment");
            await Submit(attacker, attackerElementId, "force-assignment", attackerForce.Revision, attackerForce.Refs[0]);
            var defenderForce = await Query(defender, defenderElementId, "force-assignment");
            await Submit(defender, defenderElementId, "force-assignment", defenderForce.Revision, defenderForce.Refs[0]);

            if (await Available(defender, defenderElementId, "post-assault-retreat"))
            {
                var retreat = await Query(defender, defenderElementId, "post-assault-retreat");
                await Submit(defender, defenderElementId, "post-assault-retreat", retreat.Revision, retreat.Refs[0]);
            }
            if (CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)))
            {
                var custodyOnAttacker = await Available(attacker, attackerElementId, "custody");
                var (custodyCred, custodyElement) = custodyOnAttacker
                    ? (attacker, attackerElementId) : (defender, defenderElementId);
                var custody = await Query(custodyCred, custodyElement, "custody");
                await Submit(custodyCred, custodyElement, "custody", custody.Revision, custody.Refs[0]);
            }
        }

        // 1. Initiative Order: Axis wins this fixture deterministically (confirmed, not assumed).
        var winner = await Available(axis, "land.initiative-order", "initiative-order") ? axis : commonwealth;
        Assert.Equal(CampaignActionAudience.Axis, winner.Audience);
        var other = commonwealth;
        var (initRevision, initRefs, initVisible) = await Query(winner, "land.initiative-order", "initiative-order");
        var actFirstIndex = Array.IndexOf(initVisible.GetProperty("choices").EnumerateArray()
            .Select(value => value.GetString()).ToArray(), "act-first");
        await Submit(winner, "land.initiative-order", "initiative-order", initRevision, initRefs[actFirstIndex]);

        // 2. Reserve Designation: the real "designate" branch over real HTTP for the first time
        // -- hold axis-infantry-second back, then finish (two queries: Core re-derives the
        // remaining choice set after each designation, down to {designate axis-infantry,
        // complete}).
        var (reserveRevision, reserveRefs, reserveVisible) = await Query(winner, "land.reserve-designation", "reserve-designation");
        var reserveChoices = reserveVisible.GetProperty("choices").EnumerateArray().ToArray();
        var designateSecondIndex = Array.FindIndex(reserveChoices, value =>
            value.GetProperty("kind").GetString() == "designate"
            && value.GetProperty("elementId").GetString() == "axis-infantry-second");
        Assert.True(designateSecondIndex >= 0);
        await Submit(winner, "land.reserve-designation", "reserve-designation", reserveRevision, reserveRefs[designateSecondIndex]);

        var (reserveRevision2, reserveRefs2, reserveVisible2) = await Query(winner, "land.reserve-designation", "reserve-designation");
        var completeIndex = Array.FindIndex(reserveVisible2.GetProperty("choices").EnumerateArray().ToArray(),
            value => value.GetProperty("kind").GetString() == "complete");
        Assert.True(completeIndex >= 0);
        await Submit(winner, "land.reserve-designation", "reserve-designation", reserveRevision2, reserveRefs2[completeIndex]);

        // 3. Movement: axis-infantry starts adjacent/in-contact (preserveAdjacentInfantry, same
        // as every direct-Core reference for this fixture) -- leave it unmoved and go straight
        // to Movement Segment Completion, exactly as a real player facing an already-favorable
        // position would.
        var (completeMoveRevision, completeMoveRefs, _) =
            await Query(winner, "land.movement-segment-completion", "movement-segment-completion");
        await Submit(winner, "land.movement-segment-completion", "movement-segment-completion", completeMoveRevision, completeMoveRefs[0]);

        // 4. First contact: Combat Selection for axis-infantry (its one assault offer), then the
        // full Force-Assignment/Retreat/Custody chain over HTTP.
        var combatOpening = await Query(winner, "axis-infantry", "combat-selection");
        await Submit(winner, "axis-infantry", "combat-selection", combatOpening.Revision, combatOpening.Refs[0]);
        var firstDefenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        await ResolveOneAssault(winner, "axis-infantry", commonwealth, firstDefenderId);

        // 5. Reserve Release for axis-infantry-second, over real HTTP for the first time.
        var releaseOpening = await Query(winner, "axis-infantry-second", "reserve-release");
        await Submit(winner, "axis-infantry-second", "reserve-release", releaseOpening.Revision, releaseOpening.Refs[0]);

        // 6. Cycle Control offers "Repeat" (a second contact is still unresolved for the
        // released reserve element) -- take it.
        var cycleOpening = await Query(winner, "land.cycle-control", "cycle-control");
        var choices = cycleOpening.Visible.GetProperty("choices").EnumerateArray().Select(v => v.GetString()).ToArray();
        Assert.Contains("Repeat", choices);
        await Submit(winner, "land.cycle-control", "cycle-control", cycleOpening.Revision, cycleOpening.Refs[Array.IndexOf(choices, "Repeat")]);

        // 7. Repeat Movement over real HTTP for the first time: axis-infantry-second moves one
        // step toward its own (separate) contact.
        var repeatMove = await Query(winner, "land.repeat-movement", "repeat-movement");
        Assert.True(repeatMove.Refs.Length > 0);
        await Submit(winner, "land.repeat-movement", "repeat-movement", repeatMove.Revision, repeatMove.Refs[^1]);

        // 8. Repeat Combat Selection over real HTTP for the first time: the second, genuinely
        // separate contact (this fixture's own "west" apparent location, distinct from the
        // first contact's "north-east").
        var repeatCombat = await Query(winner, "axis-infantry-second", "repeat-combat-selection");
        var repeatAssaultOffers = repeatCombat.Visible.GetProperty("assaultOffers").EnumerateArray().ToArray();
        Assert.Single(repeatAssaultOffers);
        Assert.Equal("west", repeatAssaultOffers[0].GetProperty("apparentLocationId").GetString());
        await Submit(winner, "axis-infantry-second", "repeat-combat-selection", repeatCombat.Revision, repeatCombat.Refs[0]);
        var secondDefenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        Assert.Equal("commonwealth-infantry-west", secondDefenderId);
        await ResolveOneAssault(winner, "axis-infantry-second", commonwealth, secondDefenderId);

        // 9. Both contacts resolved: Cycle Control now offers only "Finish".
        var finalCycle = await Query(winner, "land.cycle-control", "cycle-control");
        var finalChoices = finalCycle.Visible.GetProperty("choices").EnumerateArray()
            .Select(v => v.GetString()).ToArray();
        Assert.Equal(["Finish"], finalChoices);
        await Submit(winner, "land.cycle-control", "cycle-control", finalCycle.Revision, finalCycle.Refs[^1]);

        var convoy = await Query(winner, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(winner, "land.truck-convoy-pass", "truck-convoy-pass", convoy.Revision, convoy.Refs[0]);
        for (var index = 0; index < 4; index++)
        {
            var phase = await Query(winner, "land.post-combat-pass", "post-combat-pass");
            await Submit(winner, "land.post-combat-pass", "post-combat-pass", phase.Revision, phase.Refs[0]);
        }

        // 10. Second side: a real player facing the exact same preamble this project's own
        // outward HTTP API reference doc (2026-11-22) cites Core as rejecting -- the second
        // acting side has no Repeat edge at all this release. Verify that is a clean HTTP 409,
        // not a crash, rather than trusting the citation alone.
        var otherInfantry = "commonwealth-infantry";
        var secondReserve = await Query(other, "land.second-reserve", "second-reserve");
        await Submit(other, "land.second-reserve", "second-reserve", secondReserve.Revision, secondReserve.Refs[^1]);
        var secondMove = await Query(other, otherInfantry, "second-movement");
        if (secondMove.Refs.Length > 0)
            await Submit(other, otherInfantry, "second-movement", secondMove.Revision, secondMove.Refs[0]);
        var secondMoveCompletion = await Query(other, "land.second-movement-completion", "second-movement-completion");
        await Submit(other, "land.second-movement-completion", "second-movement-completion",
            secondMoveCompletion.Revision, secondMoveCompletion.Refs[0]);

        var secondCombatProbe = await Query(other, otherInfantry, "combat-selection");
        var (repeatMoveStatus, repeatMoveBody) = await outward.OpeningResponse(other, "land.repeat-movement", "repeat-movement");
        Assert.Equal(HttpStatusCode.Conflict, repeatMoveStatus);
        using (var repeatMoveJson = JsonDocument.Parse(repeatMoveBody))
            Assert.Equal("unavailable", repeatMoveJson.RootElement.GetProperty("code").GetString());
        var (repeatCombatStatus, repeatCombatBody) = await outward.OpeningResponse(other, otherInfantry, "repeat-combat-selection");
        Assert.Equal(HttpStatusCode.Conflict, repeatCombatStatus);
        using (var repeatCombatJson = JsonDocument.Parse(repeatCombatBody))
            Assert.Equal("unavailable", repeatCombatJson.RootElement.GetProperty("code").GetString());

        // The two probes above are read-only rejections; the second side's own real Combat
        // Selection continues normally afterward.
        await Submit(other, otherInfantry, "combat-selection", secondCombatProbe.Revision, secondCombatProbe.Refs[^1]);

        var secondCycle = await Query(other, "land.cycle-control", "cycle-control");
        await Submit(other, "land.cycle-control", "cycle-control", secondCycle.Revision, secondCycle.Refs[^1]);
        var secondConvoy = await Query(other, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(other, "land.truck-convoy-pass", "truck-convoy-pass", secondConvoy.Revision, secondConvoy.Refs[0]);
        for (var index = 0; index < 4; index++)
        {
            var phase = await Query(other, "land.post-combat-pass", "post-combat-pass");
            await Submit(other, "land.post-combat-pass", "post-combat-pass", phase.Revision, phase.Refs[0]);
        }

        // Cold-replay verify the entire chain, reaching the same Operation Stage 1 end position
        // as every other proof this session, this time via two genuinely separate contacts
        // resolved in one Movement segment plus a real Reserve Designation/Release/Repeat
        // Movement/Repeat Combat Selection chain, entirely over the real HTTP API.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
        Assert.Equal("land.position.operation-2.initiative-declaration",
            CampaignExercises.QueryCheckpoint(verified).PositionId);
    }
}
