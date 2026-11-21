using System.Text.Json;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Second-side Combat rounds must expire like the first side's, so an idle
// second player cannot stall the campaign. Each case lets the real round
// deadline pass, cold-replays, and still finishes Operation Stage 1.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public void SecondSideSelectionTimeoutFinishesItsCombatAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal);
        Assert.Contains(request.CampaignId, journal.CollectingSelectionCampaigns("", 16));
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(fixture.Open(clock).TickSelectionRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(fixture.Open(clock).TickSelectionRound(request.CampaignId));
        Assert.False(fixture.Open(clock).TickSelectionRound(request.CampaignId));

        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-second-combat-no-attack"));
        using (var timeout = LastEvent(fixture.Path, request.CampaignId, "native-combat-selection-timeout"))
        {
            Assert.Equal(48, timeout.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("second-combat-selection-timeout", timeout.RootElement.GetProperty("eventType").GetString());
            Assert.Equal("commonwealth", timeout.RootElement.GetProperty("side").GetString());
        }
        Assert.Equal(SecondCombatPrefix + "reserve-release", CampaignExercises.QueryCheckpoint(cold).PositionId);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    [Fact]
    public void SecondSideLateSelectionIsRefusedAndTimesTheRoundOut()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal);
        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(fixture.Open(clock).SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(SecondCombatPrefix + "reserve-release", CampaignExercises.QueryCheckpoint(cold).PositionId);
    }

    [Fact]
    public void SecondSideRbaTimeoutCancelsIntoItsOwnReserveRelease()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal,
            PositiveCombatRequest());
        SubmitSecondSideAssault(journal, commonwealth);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickRbaRound(request.CampaignId));
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-rba-cancellation"));
        Assert.Equal(SecondCombatPrefix + "reserve-release", CampaignExercises.QueryCheckpoint(cold).PositionId);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    [Fact]
    public void SecondSideForceTimeoutCancelsIntoItsOwnReserveRelease()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _, commonwealth) = SecondSideAssaultThroughRba(fixture, clock, PositiveCombatRequest());
        var attackerForce = fixture.Open(clock).QueryForceOwnerOpening(commonwealth, "commonwealth-infantry");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(commonwealth, "commonwealth-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickForceRound(request.CampaignId));
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-force-cancellation"));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-assault-result"));
        Assert.Equal(SecondCombatPrefix + "reserve-release", CampaignExercises.QueryCheckpoint(cold).PositionId);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    [Fact]
    public void SecondSideReserveAndCycleTimeoutsFinishItsBlock()
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
        // The pending Reserve I member converts on timeout.
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickReserveRound(request.CampaignId));
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(35, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        using (var reserve = LastEvent(fixture.Path, request.CampaignId, "native-reserve-release-timeout"))
            Assert.Equal("commonwealth-infantry-far-north-west",
                Assert.Single(reserve.RootElement.GetProperty("convertedElementIds").EnumerateArray()).GetString());
        // Cycle Control's timeout takes the source-backed Finish edge.
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickCycleRound(request.CampaignId));
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal("land.position.operation-1.second-player.truck-convoy-movement",
            CampaignExercises.QueryCheckpoint(cold).PositionId);
        var convoy = fixture.Open(clock).QueryTruckConvoyPassOwnerOpening(commonwealth);
        Assert.True(fixture.Open(clock).SubmitOwnerTruckConvoyPass(commonwealth, convoy.Revision,
            convoy.PublicActionRefs[0]).IsAccepted);
        for (var index = 0; index < 4; index++)
        {
            var phase = fixture.Open(clock).QueryPostCombatPassOwnerOpening(commonwealth);
            Assert.True(fixture.Open(clock).SubmitOwnerPostCombatPass(commonwealth, phase.Revision,
                Assert.Single(phase.PublicActionRefs)).IsAccepted);
        }
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal("land.position.operation-2.initiative-declaration",
            CampaignExercises.QueryCheckpoint(cold).PositionId);
    }

    // Seed 6 makes the second side's assault require a post-assault
    // retreat (decided by the first side's defender); seed 5 leaves a
    // captured lot for Custody. Each is resolved by owner choice or timeout.
    [Theory]
    [InlineData(6ul, false)]
    [InlineData(6ul, true)]
    [InlineData(5ul, false)]
    [InlineData(5ul, true)]
    public void SecondSideRetreatAndCustodyResolveByChoiceOrTimeout(ulong seed, bool timeout)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, axis, commonwealth) = SecondSideAssaultThroughRba(fixture, clock, PositiveCombatRequest(seed));
        var attackerForce = fixture.Open(clock).QueryForceOwnerOpening(commonwealth, "commonwealth-infantry");
        var defenderForce = fixture.Open(clock).QueryForceOwnerOpening(axis, "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(commonwealth, "commonwealth-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(axis, "axis-infantry",
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        if (seed == 6)
        {
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
            if (timeout)
            {
                clock.Advance(TimeSpan.FromSeconds(30));
                Assert.True(fixture.Open(clock).TickRetreatRound(request.CampaignId));
                Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-retreat-timeout"));
            }
            else
            {
                var retreat = fixture.Open(clock).QueryPostAssaultRetreatOwnerOpening(axis, "axis-infantry");
                Assert.True(fixture.Open(clock).SubmitOwnerPostAssaultRetreat(axis, "axis-infantry",
                    retreat.Revision, retreat.PublicActionRefs[0]).IsAccepted);
            }
        }
        else
        {
            Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-world-settlement"));
            if (timeout)
            {
                clock.Advance(TimeSpan.FromSeconds(30));
                Assert.True(fixture.Open(clock).TickCustodyRound(request.CampaignId));
                Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-custody-timeout"));
            }
            else
            {
                var (captor, element, custody) = SecondSideCustodyOpening(fixture.Open(clock), commonwealth, axis);
                Assert.True(fixture.Open(clock).SubmitOwnerCustody(captor, element,
                    custody.Revision, custody.PublicActionRefs[0]).IsAccepted);
            }
        }
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified,
            StepKinds(fixture.Path, request.CampaignId));
        // Whatever followed, the second side reaches its own Reserve Release.
        if (CampaignExercises.QueryCheckpoint(cold).PositionId != SecondCombatPrefix + "reserve-release")
        {
            var (captor, element, custody) = SecondSideCustodyOpening(fixture.Open(clock), commonwealth, axis);
            Assert.True(fixture.Open(clock).SubmitOwnerCustody(captor, element,
                custody.Revision, custody.PublicActionRefs[0]).IsAccepted);
            cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        }
        Assert.Equal(SecondCombatPrefix + "reserve-release", CampaignExercises.QueryCheckpoint(cold).PositionId);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-successor"));
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    private static (CampaignSeatCredential Captor, string ElementId, CampaignOwnerOpening Opening)
        SecondSideCustodyOpening(CampaignDurableAuthorityJournal journal, CampaignSeatCredential commonwealth,
            CampaignSeatCredential axis)
    {
        try { return (commonwealth, "commonwealth-infantry", journal.QueryCustodyOwnerOpening(commonwealth, "commonwealth-infantry")); }
        catch (InvalidOperationException) { return (axis, "axis-infantry", journal.QueryCustodyOwnerOpening(axis, "axis-infantry")); }
    }

    private static void SubmitSecondSideAssault(CampaignDurableAuthorityJournal journal,
        CampaignSeatCredential commonwealth)
    {
        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        using var visible = JsonDocument.Parse(opening.VisibleBytes);
        var offers = visible.RootElement.GetProperty("assaultOffers");
        var north = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
            offers[index].GetProperty("apparentLocationId").GetString() == "north");
        Assert.True(journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, opening.PublicActionRefs[north]).IsAccepted);
    }

    private static JsonDocument LastEvent(string path, string campaignId, string kind)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind=$kind ORDER BY sequence DESC LIMIT 1";
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$kind", kind);
        return JsonDocument.Parse((byte[])query.ExecuteScalar()!);
    }
}
