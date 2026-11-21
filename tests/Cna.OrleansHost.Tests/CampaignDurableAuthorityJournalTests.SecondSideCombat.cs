using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Second-side Combat resolution through the real durable Host: the second
// acting side's own assault pick enters Barrage, the first side's defender
// takes the RBA decision, and Force Assignment follows, all reusing the
// V14-V26 stage events, each step persisted, retried and cold-replay-verified.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private const string SecondCombatPrefix =
        "land.position.operation-1.second-player.movement-and-combat.";

    private static (CampaignCreationRequest Request, CampaignSeatCredential Axis,
        CampaignSeatCredential Commonwealth) SecondSideAssaultThroughRba(JournalFixture fixture,
            ManualClock clock, CampaignCreationRequest? payable = null, string? secondReserveElementId = null)
    {
        var journal = fixture.Create(clock);
        var (request, axis, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal, payable,
            secondReserveElementId);
        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        using (var visible = JsonDocument.Parse(opening.VisibleBytes))
        {
            var offers = visible.RootElement.GetProperty("assaultOffers");
            var north = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                offers[index].GetProperty("apparentLocationId").GetString() == "north");
            Assert.True(journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
                opening.Revision, opening.PublicActionRefs[north]).IsAccepted);
        }

        // Barrage completes automatically and opens the first side's RBA round.
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        Assert.Equal(SecondCombatPrefix + "combat.retreat-before-assault",
            CampaignExercises.QueryCheckpoint(cold).PositionId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-barrage"));

        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(cold, CampaignActionAudience.Axis);
        Assert.Equal("axis-infantry", defenderId);
        var rba = fixture.Open(clock).QueryRbaOwnerOpening(axis, defenderId);
        var declined = fixture.Open(clock).SubmitOwnerRbaDecline(axis, defenderId, rba.Revision,
            rba.PublicActionRefs[^1]);
        Assert.True(declined.IsAccepted);
        var retried = fixture.Open(clock).SubmitOwnerRbaDecline(axis, defenderId, rba.Revision,
            rba.PublicActionRefs[^1]);
        Assert.True(retried.IsAccepted);
        Assert.Equal(declined.CanonicalReceipt, retried.CanonicalReceipt);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-rba-decline"));
        return (request, axis, commonwealth);
    }

    // The non-payable fixture: Core's own Force pre-check finds no legal role
    // pair, so the source-backed V19 force-unavailable cancellation runs and
    // the second side reaches its own Reserve Release.
    [Fact]
    public void SecondSideUnpayableAssaultCancelsIntoItsOwnReserveRelease()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _, _) = SecondSideAssaultThroughRba(fixture, clock);
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-force-cancellation"));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-cancelled-assault"));
        var checkpoint = CampaignExercises.QueryCheckpoint(cold);
        Assert.Equal(27, checkpoint.ContractVersion);
        Assert.Equal(SecondCombatPrefix + "reserve-release", checkpoint.PositionId);
    }

    [Fact]
    public void SecondSidePayableAssaultSealsForceAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, axis, commonwealth) = SecondSideAssaultThroughRba(fixture, clock,
            PositiveCombatRequest());
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(15 == CampaignExercises.QueryCheckpoint(cold).ContractVersion,
            StepKinds(fixture.Path, request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);

        // Force Assignment: the second side attacks, the first side defends.
        var attackerForce = fixture.Open(clock).QueryForceOwnerOpening(commonwealth, "commonwealth-infantry");
        var defenderForce = fixture.Open(clock).QueryForceOwnerOpening(axis, "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(commonwealth, "commonwealth-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(axis, "axis-infantry",
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);

        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-force-completion"));
        // The full V18-V26 chain runs to the second side's own Reserve Release.
        foreach (var kind in new[] { "native-combat-anti-armor", "native-combat-assault-result",
                     "native-combat-world-settlement", "native-combat-successor" })
            Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, kind));
        // One Reserve Release completion per side.
        Assert.Equal(2, CountAcceptedStep(fixture.Path, request.CampaignId, "native-reserve-release-completion"));
        var checkpoint = CampaignExercises.QueryCheckpoint(cold);
        Assert.Equal(27, checkpoint.ContractVersion);
        Assert.Equal(SecondCombatPrefix + "reserve-release", checkpoint.PositionId);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    [Fact]
    public void SecondSideVoluntaryFinishCompletesOperationStageOne()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal);
        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    // Second-side Cycle Control Finish, its own Truck Convoy pass and its
    // four rail/repair/patrol passes end Operation Stage 1. The successor
    // position (Operation Stage 2's initiative declaration) has no authority
    // this release, so no further owner opening exists.
    private static void FinishSecondSideOperationStage(JournalFixture fixture, ManualClock clock,
        CampaignCreationRequest request, CampaignSeatCredential owner)
    {
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        // A Cycle Control timeout may already have taken the Finish edge.
        if (CampaignExercises.QueryCheckpoint(cold).PositionId != "land.position.operation-1.second-player.truck-convoy-movement")
        {
            var control = fixture.Open(clock).QueryCycleControlOwnerOpening(owner);
            var visible = System.Text.Encoding.UTF8.GetString(control.VisibleBytes);
            Assert.Contains("Finish", visible, StringComparison.Ordinal);
            Assert.True(fixture.Open(clock).SubmitOwnerCycleControl(owner, control.Revision,
                control.PublicActionRefs[^1]).IsAccepted);
            cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            Assert.Equal(28, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        }
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal("land.position.operation-1.second-player.truck-convoy-movement",
            CampaignExercises.QueryCheckpoint(cold).PositionId);

        var convoy = fixture.Open(clock).QueryTruckConvoyPassOwnerOpening(owner);
        Assert.True(fixture.Open(clock).SubmitOwnerTruckConvoyPass(owner, convoy.Revision,
            convoy.PublicActionRefs[0]).IsAccepted);
        for (var index = 0; index < 4; index++)
        {
            var phase = fixture.Open(clock).QueryPostCombatPassOwnerOpening(owner);
            var passed = fixture.Open(clock).SubmitOwnerPostCombatPass(owner, phase.Revision,
                Assert.Single(phase.PublicActionRefs));
            Assert.True(passed.IsAccepted);
            Assert.Equal(passed.CanonicalReceipt, fixture.Open(clock).SubmitOwnerPostCombatPass(owner,
                phase.Revision, Assert.Single(phase.PublicActionRefs)).CanonicalReceipt);
        }
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        var end = CampaignExercises.QueryCheckpoint(cold);
        Assert.Equal(42, end.ContractVersion);
        Assert.Equal("land.position.operation-2.initiative-declaration", end.PositionId);
        Assert.Equal(8, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-post-combat-pass"));
        Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).QueryPostCombatPassOwnerOpening(owner));
        Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).QueryCycleControlOwnerOpening(owner));
    }

    // Owner ruling 2026-11-21 (FID-002): the second side's Repeat edge stays
    // in its legal set with a real action ref, and submitting it is refused
    // over HTTP as a rule not yet implemented; Finish remains accepted.
    [Fact]
    public async Task SecondSideRepeatIsOfferedButRefusedAsRuleNotImplemented()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, axis, commonwealth) = SecondSideAssaultThroughRba(fixture, clock,
            PositiveTripleContactRequest(12345), "commonwealth-infantry-far-north-west");
        var attackerForce = fixture.Open(clock).QueryForceOwnerOpening(commonwealth, "commonwealth-infantry");
        var defenderForce = fixture.Open(clock).QueryForceOwnerOpening(axis, "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(commonwealth, "commonwealth-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(axis, "axis-infantry",
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var release = fixture.Open(clock).QueryReserveReleaseOwnerOpening(commonwealth,
            "commonwealth-infantry-far-north-west");
        Assert.True(fixture.Open(clock).SubmitOwnerReserveRelease(commonwealth,
            "commonwealth-infantry-far-north-west", release.Revision, release.PublicActionRefs[0]).IsAccepted,
            StepKinds(fixture.Path, request.CampaignId));

        var journal = fixture.Open(clock);
        var control = journal.QueryCycleControlOwnerOpening(commonwealth);
        using (var visible = JsonDocument.Parse(control.VisibleBytes))
        {
            var choices = visible.RootElement.GetProperty("choices").EnumerateArray()
                .Select(value => value.GetString()).ToArray();
            Assert.Equal(["Repeat", "Finish"], choices);
        }
        Assert.Equal(2, control.PublicActionRefs.Count);
        await using (var outward = await OwnerHttpHarness.Start(journal, request.CampaignId))
        {
            var refused = await outward.SubmitResponse(commonwealth, control.ActingElementId, "cycle-control",
                control.Revision, control.PublicActionRefs[0]);
            Assert.Equal(System.Net.HttpStatusCode.Conflict, refused.Status);
            using var body = JsonDocument.Parse(refused.Body);
            Assert.Equal("rule-not-implemented", body.RootElement.GetProperty("code").GetString());
        }
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.False(fixture.Open(clock).SubmitOwnerCycleControl(commonwealth, control.Revision,
            control.PublicActionRefs[0]).IsAccepted);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    // Item 1' (owner release bar): from campaign creation, the first side
    // assaults and resolves, finishes and passes its post-Combat phases; the
    // second side then takes its own Reserve, Movement, Breakdown and Combat
    // and finishes its block. Operation Stage 1 ends at the next stage's
    // initiative declaration, with every step cold-replay verified.
    [Fact]
    public void BothSidesPlayACompleteOperationStageOne()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, axis, commonwealth, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(axis, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(commonwealth, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-assault-result"));

        var control = fixture.Open(clock).QueryCycleControlOwnerOpening(axis);
        Assert.True(fixture.Open(clock).SubmitOwnerCycleControl(axis, control.Revision,
            control.PublicActionRefs[^1]).IsAccepted);
        var convoy = fixture.Open(clock).QueryTruckConvoyPassOwnerOpening(axis);
        Assert.True(fixture.Open(clock).SubmitOwnerTruckConvoyPass(axis, convoy.Revision,
            convoy.PublicActionRefs[0]).IsAccepted);
        for (var index = 0; index < 4; index++)
        {
            var phase = fixture.Open(clock).QueryPostCombatPassOwnerOpening(axis);
            Assert.True(fixture.Open(clock).SubmitOwnerPostCombatPass(axis, phase.Revision,
                Assert.Single(phase.PublicActionRefs)).IsAccepted);
        }
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal("land.position.operation-1.second-player.reserve-designation",
            CampaignExercises.QueryCheckpoint(cold).PositionId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);

        var secondReserve = fixture.Open(clock).QuerySecondReserveOwnerOpening(commonwealth);
        Assert.True(fixture.Open(clock).SubmitOwnerSecondReserve(commonwealth, secondReserve.Revision,
            secondReserve.PublicActionRefs[^1]).IsAccepted);
        var movementCompletion = fixture.Open(clock).QuerySecondMovementCompletionOwnerOpening(commonwealth);
        Assert.True(fixture.Open(clock).SubmitOwnerSecondMovementCompletion(commonwealth,
            movementCompletion.Revision, Assert.Single(movementCompletion.PublicActionRefs)).IsAccepted);
        var selection = fixture.Open(clock).QueryCombatOwnerOpening(commonwealth, defenderId);
        Assert.True(fixture.Open(clock).SubmitOwnerCombatSelection(commonwealth, defenderId,
            selection.Revision, selection.PublicActionRefs[^1]).IsAccepted);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
        Assert.Equal(2, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-truck-convoy-pass"));
    }

    private static string StepKinds(string path, string campaignId)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT action_kind FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence";
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var kinds = new List<string>();
        while (reader.Read()) kinds.Add(reader.GetString(0));
        return string.Join(",", kinds.TakeLast(12));
    }
}
