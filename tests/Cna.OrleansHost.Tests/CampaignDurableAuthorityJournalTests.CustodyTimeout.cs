using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static (CampaignCreationRequest Request, CampaignSeatCredential Attacker,
        CampaignSeatCredential Defender, string DefenderId) PrepareCustodyRound(
        CampaignDurableAuthorityJournal journal, Action? beforeEntry = null)
    {
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 5);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        beforeEntry?.Invoke();
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var session = journal.ReadVerified(request.CampaignId);
        if (CampaignExercises.QueryCheckpoint(session).ContractVersion == 22)
        {
            var opening = journal.QueryPostAssaultRetreatOwnerOpening(defender, defenderId);
            Assert.True(journal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
                opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
            session = journal.ReadVerified(request.CampaignId);
        }
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(session));
        return (request, attacker, defender, defenderId);
    }

    private static (CampaignSeatCredential Captor, string ElementId, CampaignOwnerOpening Opening)
        CustodyOpening(CampaignDurableAuthorityJournal journal, CampaignSeatCredential attacker,
            CampaignSeatCredential defender, string defenderId)
    {
        try { return (attacker, "axis-infantry", journal.QueryCustodyOwnerOpening(attacker, "axis-infantry")); }
        catch (InvalidOperationException) { return (defender, defenderId, journal.QueryCustodyOwnerOpening(defender, defenderId)); }
    }

    private static JsonDocument CustodyTimeoutEvent(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id AND action_kind='native-combat-custody-timeout' ORDER BY sequence DESC LIMIT 1";
        query.Parameters.AddWithValue("$id", campaignId);
        return JsonDocument.Parse((byte[])query.ExecuteScalar()!);
    }

    [Fact]
    public void CustodyTimeoutStartsAtEntryAndExpiresWithoutQueryAfterRestart()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareCustodyRound(journal);
        Assert.Contains(request.CampaignId, journal.CollectingCustodyCampaigns("", 16));
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(journal.TickCustodyRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var cold = fixture.Open(clock);
        new CampaignCombatTimeoutService(cold, clock, NullLogger<CampaignCombatTimeoutService>.Instance)
            .SweepOnce(CancellationToken.None);
        var session = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(session));
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.False(cold.TickCustodyRound(request.CampaignId));
        Assert.Contains(request.CampaignId, cold.CollectingCycleCampaigns("", 16));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-combat-custody"));
        using var evidence = CustodyTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("leave-unguarded", evidence.RootElement.GetProperty("fallback").GetString());
        Assert.Equal("deadline", evidence.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void CustodyTimeoutPreservesAcceptedOwnerReceiptAndDoesNotRunAgain()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId) = PrepareCustodyRound(journal);
        var (captor, element, opening) = CustodyOpening(journal, attacker, defender, defenderId);
        var accepted = journal.SubmitOwnerCustody(captor, element, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        var cold = fixture.Open(clock);
        Assert.False(cold.TickCustodyRound(request.CampaignId));
        var retry = cold.SubmitOwnerCustody(captor, element, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-custody-timeout"));
    }

    [Fact]
    public void CustodyTimeoutInvalidLateProposalCannotConsumeRound()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId) = PrepareCustodyRound(journal);
        var (captor, element, opening) = CustodyOpening(journal, attacker, defender, defenderId);
        var before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(journal.SubmitOwnerCustody(captor, element, opening.Revision, "invalid").IsAccepted);
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
        Assert.False(journal.SubmitOwnerCustody(captor, element, opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-custody-timeout"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustodyTimeoutClockFailureOrRegressionRetainsExplicitEvidence(bool regress)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareCustodyRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.False(journal.TickCustodyRound(request.CampaignId));
        if (regress) clock.Advance(TimeSpan.FromSeconds(-1));
        else clock.IsAvailable = false;
        Assert.True(journal.TickCustodyRound(request.CampaignId));
        using var evidence = CustodyTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(regress ? "clock-regression" : "clock-unavailable", evidence.RootElement.GetProperty("reason").GetString());
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustodyTimeoutMissingOrAlteredRoundFailsClosed(bool missing)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareCustodyRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = missing ? "DELETE FROM custody_rounds" : "UPDATE custody_rounds SET deadline_ms=deadline_ms+1";
        command.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => journal.TickCustodyRound(request.CampaignId));
    }

    [Fact]
    public void CustodyTimeoutLegacyElevenRemainsReadOnly()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId) = PrepareCustodyRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM custody_rounds; PRAGMA user_version=11";
        command.ExecuteNonQuery();
        var legacy = fixture.Open(clock);
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        Assert.False(legacy.TickCustodyRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => CustodyOpening(legacy, attacker, defender, defenderId));
    }

    [Fact]
    public void CustodyTimeoutRoundInsertFailureRollsBackEntry()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        long before = -1;
        string? campaign = null;
        Assert.Throws<SqliteException>(() => PrepareCustodyRound(journal, () =>
        {
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT campaign_id FROM campaigns";
            campaign = (string)query.ExecuteScalar()!;
            before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(campaign)).StateVersion;
            query.CommandText = "CREATE TRIGGER fail_custody BEFORE INSERT ON custody_rounds BEGIN SELECT RAISE(ABORT,'injected custody failure'); END";
            query.ExecuteNonQuery();
        }));
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(campaign!)).StateVersion);
    }

    [Fact]
    public void CustodyTimeoutSuccessorFailureRollsBackDispositionAndRoundClosure()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareCustodyRound(journal);
        var before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TRIGGER fail_custody_successor BEFORE INSERT ON accepted_steps WHEN NEW.action_kind='native-combat-successor' BEGIN SELECT RAISE(ABORT,'injected successor failure'); END";
        command.ExecuteNonQuery();
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Throws<SqliteException>(() => journal.TickCustodyRound(request.CampaignId));
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        Assert.Contains(request.CampaignId, journal.CollectingCustodyCampaigns("", 16));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-custody-timeout"));
    }
    [Fact]
    public void CustodyTimeoutAllowsGenuineRepeatedAssaultColdReplay()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 5,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, defenderId);
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)));
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickCustodyRound(request.CampaignId));
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision, control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision, movement.PublicActionRefs[1]).IsAccepted);
        var selection = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry-second");
        using (var visible = JsonDocument.Parse(selection.VisibleBytes))
        {
            var offers = visible.RootElement.GetProperty("assaultOffers");
            var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                offers[index].GetProperty("apparentLocationId").GetString() == "west");
            Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-second",
                selection.Revision, selection.PublicActionRefs[west]).IsAccepted);
        }
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender, rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        var attack = journal.QueryForceOwnerOpening(attacker, "axis-infantry-second");
        var defend = journal.QueryForceOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-second", attack.Revision, attack.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, westDefender, defend.Revision, defend.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, westDefender);
        // This fixed seed has no second captive lot: prove continuation, not a
        // repeated-custody timeout or a seed-selected support claim.
        Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)));
        Assert.False(fixture.Open(clock).TickCustodyRound(request.CampaignId));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-custody-timeout"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    private static void CompleteRetreatIfPending(CampaignDurableAuthorityJournal journal,
        string campaignId, CampaignSeatCredential defender, string defenderId)
    {
        if (CampaignExercises.QueryCheckpoint(journal.ReadVerified(campaignId)).ContractVersion != 22) return;
        var retreat = journal.QueryPostAssaultRetreatOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
            retreat.Revision, retreat.PublicActionRefs[0]).IsAccepted);
    }

}
