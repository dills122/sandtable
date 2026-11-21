using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Owner ruling (release-blocking, same audit as InitiativeOrder): the first acting side's
// own Reserve Designation (DesignateReserveAction/CompleteReserveDesignationAction,
// land.phase.reserve-designation) is the very next preamble step after ActFirst/ActLast and
// has the identical gap -- no owner opening, no HTTP stage. Built the same way, for the same
// reason: Core already computes and replay-verifies this (CampaignV11Preamble,
// CampaignLegalActions.GenerateForSide) via the generic CampaignExercises.Query/Submit path.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Resolves initiative through the real (now-merged) InitiativeOrder owner opening, then
    // drains the five system-audience preamble steps that follow it (weather, organization,
    // naval convoy arrival, fleet assignment, fleet repair) -- all audience=System automatic
    // resolutions, not an owner gap, so draining them via the generic legal-action set is the
    // correct existing pattern, matching AdvanceToInitiativeDeclaration's own system-step loop.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Acting,
        CampaignSeatCredential Other) AdvanceToReserveDesignation(CampaignDurableAuthorityJournal journal)
    {
        var (request, winner, loser) = AdvanceToInitiativeDeclaration(journal);
        var initiative = journal.QueryInitiativeOrderOwnerOpening(winner);
        Assert.True(journal.SubmitOwnerInitiativeOrder(winner, initiative.Revision,
            initiative.PublicActionRefs[0]).IsAccepted);
        var session = journal.ReadVerified(request.CampaignId);
        for (var index = 0; index < 10; index++)
        {
            var system = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet;
            if (system is null || system.Candidates.Count == 0) break;
            var pick = system.Candidates[0];
            var command = new CampaignActionSubmission(1, system.CampaignId, system.StateVersion,
                system.PositionId, system.Audience, pick.ActionId);
            var accepted = journal.Append(request.CampaignId, command);
            if (!accepted.IsAccepted)
                throw new InvalidOperationException("System preamble step was not accepted.");
            session = accepted.SuccessorSession!;
        }
        CampaignSeatCredential acting, other;
        try
        {
            journal.QueryReserveDesignationOwnerOpening(winner);
            acting = winner;
            other = loser;
        }
        catch (InvalidOperationException)
        {
            acting = loser;
            other = winner;
        }
        return (request, acting, other);
    }

    [Fact]
    public async Task ReserveDesignationIsOfferedOnlyToTheActingSideAndAcceptedThroughHttp()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToReserveDesignation(journal);

        // Side-safe: the other side has no opening here at all.
        Assert.Throws<InvalidOperationException>(() => journal.QueryReserveDesignationOwnerOpening(other));

        var opening = journal.QueryReserveDesignationOwnerOpening(acting);
        // CombatInfantryRequest's acting side has exactly one independently-owned,
        // reserve-eligible, non-vehicle element at this cut (its own infantry; the truck is
        // excluded by VehicleBreakdownRisk), so there are exactly two legal choices: designate
        // it, or finish with nobody in reserve.
        Assert.Equal(2, opening.PublicActionRefs.Count);
        string? completeRef = null;
        using (var visible = JsonDocument.Parse(opening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            var choices = visible.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            Assert.Equal(2, choices.Length);
            Assert.Contains(choices, value => value.GetProperty("kind").GetString() == "designate"
                && value.GetProperty("elementId").ValueKind != JsonValueKind.Null);
            var completeIndex = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "complete");
            Assert.True(completeIndex >= 0);
            Assert.Equal(JsonValueKind.Null, choices[completeIndex].GetProperty("elementId").ValueKind);
            completeRef = opening.PublicActionRefs[completeIndex];
        }

        // The real outward HTTP API, not CampaignDurableAuthorityJournal.Append: finish with
        // nobody in reserve (the simplest closed path -- same choice AdvanceToContactCombat's
        // own helpers rely on for the first side's own preamble in every existing test).
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.Submit(acting, opening.ActingElementId, "reserve-designation",
            "reserve-designation", opening, completeRef!);

        var cold = fixture.Open();
        var afterHttp = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(afterHttp).IsVerified);

        // Retry through the normal durable Host: idempotent, same canonical receipt.
        var retried = cold.SubmitOwnerReserveDesignation(acting, opening.Revision, completeRef!);
        Assert.True(retried.IsAccepted);

        // The decision is gone: a second query throws, same pattern every other one-shot
        // decision uses once its own successor position is reached.
        Assert.Throws<InvalidOperationException>(() => cold.QueryReserveDesignationOwnerOpening(acting));
    }

    [Fact]
    public void ReserveDesignationRejectsStaleForgedAndWrongAudienceSubmissions()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, other) = AdvanceToReserveDesignation(journal);
        var opening = journal.QueryReserveDesignationOwnerOpening(acting);
        var legalRef = opening.PublicActionRefs[0];
        var before = StoredAuthority(fixture.Path, request.CampaignId);

        // Wrong audience: the other side cannot submit even a well-formed reference.
        Assert.False(journal.SubmitOwnerReserveDesignation(other, opening.Revision, legalRef).IsAccepted);
        // Stale revision.
        Assert.False(journal.SubmitOwnerReserveDesignation(acting, opening.Revision + 1, legalRef).IsAccepted);
        // Forged reference: well-formed ("act." + 64 hex) but never issued.
        var forged = "act." + new string(legalRef[4] == '0' ? '1' : '0', 64);
        Assert.NotEqual(legalRef, forged);
        Assert.False(journal.SubmitOwnerReserveDesignation(acting, opening.Revision, forged).IsAccepted);
        // Oversized reference.
        Assert.False(journal.SubmitOwnerReserveDesignation(acting, opening.Revision,
            "act." + new string('0', 1_000_000)).IsAccepted);

        Assert.Equal(before, StoredAuthority(fixture.Path, request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);

        // The genuine offer still works after every rejected attempt above.
        var accepted = journal.SubmitOwnerReserveDesignation(acting, opening.Revision, legalRef);
        Assert.True(accepted.IsAccepted);
        var cold = fixture.Open();
        Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
    }

    // Item 1' / the end-to-end proof this whole preamble chain exists for: once Reserve
    // Designation finishes, does the real Host API's "movement" stage finally open a real
    // Movement opening for a pure-HTTP player? This is the first point since campaign
    // creation where that becomes possible.
    [Fact]
    public void CompletingReserveDesignationFinallyReachesARealMovementOpening()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToReserveDesignation(journal);
        var opening = journal.QueryReserveDesignationOwnerOpening(acting);
        using var visible = JsonDocument.Parse(opening.VisibleBytes);
        var choices = visible.RootElement.GetProperty("choices").EnumerateArray().ToArray();
        var completeIndex = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "complete");
        Assert.True(journal.SubmitOwnerReserveDesignation(acting, opening.Revision,
            opening.PublicActionRefs[completeIndex]).IsAccepted);

        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
        var checkpoint = CampaignExercises.QueryCheckpoint(verified);
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.movement", checkpoint.PositionId);

        // The real, already-existing "movement" owner opening -- QueryOwnerOpening/
        // SubmitOwnerMovement, proven correct independently by ContactMovementReferenceCommitsOnceAndReplaysAfterRestart
        // -- now actually opens for the acting side's own infantry, with real move offers.
        var movement = cold.QueryOwnerOpening(acting, CombatInfantryElementId(acting));
        Assert.NotEmpty(movement.PublicActionRefs);
    }

    private static string CombatInfantryElementId(CampaignSeatCredential credential) =>
        credential.Audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
}
