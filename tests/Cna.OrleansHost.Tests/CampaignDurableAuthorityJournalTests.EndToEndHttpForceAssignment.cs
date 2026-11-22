using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Post-E2E adversarial scenario testing (2026-11-22): TwoRealHttpPlayersConductAnEntireOperationStageOne
// (EndToEndHttpRelease.cs) proved the real outward HTTP API end to end, but it deliberately took the
// Combat Selection "finish without attack" branch on both sides specifically to keep that proof about
// HTTP reachability rather than Combat RNG resolution -- so it never drove combat-selection's actual
// assault branch, nor retreat-before-assault/rba-decline, force-assignment, post-assault-retreat or
// custody over HTTP at all. Every one of those five stages was otherwise only ever driven through
// direct Core/Journal calls (PrepareInfantryForceRound and friends, across Rba.cs/ForceEntry.cs/
// RetreatTimeout.cs/CustodyTimeout.cs/RepeatedRetreat.cs/RepeatedCustody.cs/etc.) -- confirmed by
// grepping every outward.OpeningResponse/Submit/SubmitResponse/SubmitStatus call site in this test
// project: none of them ever names "retreat-before-assault", "rba-decline", "force-assignment",
// "post-assault-retreat" or "custody". This test closes that real HTTP-reachability gap for an
// actual, payable assault, reusing PositiveCombatRequest at the two seeds this project's own existing
// direct-Core tests already use to reach the two different post-assault branches deterministically:
// seed 5 (CustodyTimeout.cs's PrepareCustodyRound) resolves straight to a "no-post-assault-retreat"
// Custody decision with no explicit Retreat step; seed 6 (RetreatTimeout.cs's PrepareRetreatRound)
// opens a genuine owner Post-Assault Retreat decision instead. Bisected directly against this
// project's own existing PrepareInfantryForceRound/PrepareRetreatRound helpers (scratch tests, not
// kept) to confirm which branch each seed takes before writing this -- it is not a guess. Both
// branches are driven dynamically here (discovered the same way EndToEndHttpRelease.cs's own
// "Available" probe works) rather than hard-coded, so this test is not sensitive to which branch a
// given seed happens to take.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData(5ul)] // No explicit owner Retreat decision: straight to Custody.
    [InlineData(6ul)] // A genuine owner Post-Assault Retreat decision, no Custody afterward.
    public async Task RealHttpPlayersDriveAnActualAssaultThroughForceAssignmentRetreatOrCustody(ulong seed)
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = PositiveCombatRequest(seed);
        AdvanceToContactCombat(journal, request, Timing(request), preserveAdjacentInfantry: true);
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;

        // Everything from here on is the real outward HTTP API, not direct journal calls -- the
        // same OwnerHttpHarness every other HTTP-reachability test in this project uses.
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

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

        // 1. Combat Selection over real HTTP: the actual assault offer, not voluntary finish --
        // the one branch the flagship E2E proof deliberately skipped.
        var (combatRevision, combatRefs, _) = await Query(attacker, "axis-infantry", "combat-selection");
        await Submit(attacker, "axis-infantry", "combat-selection", combatRevision, combatRefs[0]);

        // Barrage authority is complete now that Combat Selection has been submitted: safe to
        // identify the defender element id (mirrors PrepareInfantryForceRound's own ordering).
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);

        // 2. Retreat Before Assault over real HTTP: the defender declines (020D: Host route is
        // decline-only). First time "retreat-before-assault"/"rba-decline" are driven over HTTP.
        var (rbaRevision, rbaRefs, _) = await Query(defender, defenderId, "retreat-before-assault");
        await Submit(defender, defenderId, "rba-decline", rbaRevision, rbaRefs[^1]);

        // 3. Force Assignment over real HTTP: both sides seal. First time "force-assignment" is
        // driven over HTTP for either side.
        var (attackerForceRevision, attackerForceRefs, _) = await Query(attacker, "axis-infantry", "force-assignment");
        await Submit(attacker, "axis-infantry", "force-assignment", attackerForceRevision, attackerForceRefs[0]);
        var (defenderForceRevision, defenderForceRefs, _) = await Query(defender, defenderId, "force-assignment");
        await Submit(defender, defenderId, "force-assignment", defenderForceRevision, defenderForceRefs[0]);

        // 4. Post-Assault Retreat over real HTTP, only when this seed's deterministic combat
        // result actually opens one (seed 6's branch; seed 5 skips straight to Custody instead).
        // First time "post-assault-retreat" is driven over HTTP whenever it fires.
        if (await Available(defender, defenderId, "post-assault-retreat"))
        {
            var (retreatRevision, retreatRefs, _) = await Query(defender, defenderId, "post-assault-retreat");
            await Submit(defender, defenderId, "post-assault-retreat", retreatRevision, retreatRefs[0]);
        }

        // 5. Custody over real HTTP, only when this seed's result actually has custody pending
        // (seed 5's branch). First time "custody" is driven over HTTP whenever it fires.
        if (CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)))
        {
            var custodyOnAttacker = await Available(attacker, "axis-infantry", "custody");
            var (custodyCredential, custodyElementId) = custodyOnAttacker
                ? (attacker, "axis-infantry") : (defender, defenderId);
            var (custodyRevision, custodyRefs, _) = await Query(custodyCredential, custodyElementId, "custody");
            await Submit(custodyCredential, custodyElementId, "custody", custodyRevision, custodyRefs[0]);
        }

        // The combat round is fully resolved now: Cycle Control becomes reachable for the acting
        // (first-mover) side next, exactly as in the flagship E2E proof's own step 9 onward.
        var acting = await Available(attacker, "land.cycle-control", "cycle-control") ? attacker : defender;
        var other = acting == attacker ? defender : attacker;

        var (cycleRevision, cycleRefs, _) = await Query(acting, "land.cycle-control", "cycle-control");
        await Submit(acting, "land.cycle-control", "cycle-control", cycleRevision, cycleRefs[^1]);

        var (convoyRevision, convoyRefs, _) = await Query(acting, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(acting, "land.truck-convoy-pass", "truck-convoy-pass", convoyRevision, convoyRefs[0]);

        for (var index = 0; index < 4; index++)
        {
            var (phaseRevision, phaseRefs, _) = await Query(acting, "land.post-combat-pass", "post-combat-pass");
            await Submit(acting, "land.post-combat-pass", "post-combat-pass", phaseRevision, phaseRefs[0]);
        }

        // Second side: Reserve, finished empty; Movement Completion with no real move offered.
        var (secondReserveRevision, secondReserveRefs, _) = await Query(other, "land.second-reserve", "second-reserve");
        await Submit(other, "land.second-reserve", "second-reserve", secondReserveRevision, secondReserveRefs[^1]);

        var otherInfantry = other.Audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
        var (secondMoveRevision, secondMoveRefs, _) = await Query(other, otherInfantry, "second-movement");
        if (secondMoveRefs.Length > 0)
            await Submit(other, otherInfantry, "second-movement", secondMoveRevision, secondMoveRefs[0]);

        var (secondMoveCompletionRevision, secondMoveCompletionRefs, _) =
            await Query(other, "land.second-movement-completion", "second-movement-completion");
        await Submit(other, "land.second-movement-completion", "second-movement-completion",
            secondMoveCompletionRevision, secondMoveCompletionRefs[0]);

        // Second-side Combat Selection: finish without attack (same reasoning as the flagship
        // proof's step 8/16 -- this test's own point is the FIRST side's real assault chain).
        var (secondCombatRevision, secondCombatRefs, _) = await Query(other, otherInfantry, "combat-selection");
        await Submit(other, otherInfantry, "combat-selection", secondCombatRevision, secondCombatRefs[^1]);

        var (secondCycleRevision, secondCycleRefs, _) = await Query(other, "land.cycle-control", "cycle-control");
        await Submit(other, "land.cycle-control", "cycle-control", secondCycleRevision, secondCycleRefs[^1]);

        var (secondConvoyRevision, secondConvoyRefs, _) = await Query(other, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(other, "land.truck-convoy-pass", "truck-convoy-pass", secondConvoyRevision, secondConvoyRefs[0]);

        for (var index = 0; index < 4; index++)
        {
            var (phaseRevision, phaseRefs, _) = await Query(other, "land.post-combat-pass", "post-combat-pass");
            await Submit(other, "land.post-combat-pass", "post-combat-pass", phaseRevision, phaseRefs[0]);
        }

        // Cold-replay verify the entire chain -- the same bar every other test in this session
        // uses -- and that it actually reached the same Operation Stage 1 end position as the
        // flagship proof, this time via a real paid assault with Retreat and/or Custody resolved.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
        Assert.Equal("land.position.operation-2.initiative-declaration",
            CampaignExercises.QueryCheckpoint(verified).PositionId);
    }
}
