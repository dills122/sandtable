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
        CampaignSeatCredential Defender, string DefenderId) PrepareRetreatRound(
        CampaignDurableAuthorityJournal journal, Action? beforeEntry = null)
    {
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 6);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        beforeEntry?.Invoke();
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
        return (request, attacker, defender, defenderId);
    }

    private static (CampaignSeatCredential Captor, string ElementId, CampaignOwnerOpening Opening)
        RetreatOpening(CampaignDurableAuthorityJournal journal, CampaignSeatCredential attacker,
            CampaignSeatCredential defender, string defenderId)
    {
        return (defender, defenderId, journal.QueryPostAssaultRetreatOwnerOpening(defender, defenderId));
    }

    private static JsonDocument RetreatTimeoutEvent(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id AND action_kind='native-combat-retreat-timeout' ORDER BY sequence DESC LIMIT 1";
        query.Parameters.AddWithValue("$id", campaignId);
        return JsonDocument.Parse((byte[])query.ExecuteScalar()!);
    }

    [Fact]
    public void RetreatTimeoutStartsAtEntryAndExpiresWithoutQueryAfterRestart()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareRetreatRound(journal);
        Assert.Contains(request.CampaignId, journal.CollectingRetreatCampaigns("", 16));
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(journal.TickRetreatRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var cold = fixture.Open(clock);
        new CampaignCombatTimeoutService(cold, clock, NullLogger<CampaignCombatTimeoutService>.Instance)
            .SweepOnce(CancellationToken.None);
        var session = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.NotEqual(22, CampaignExercises.QueryCheckpoint(session).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.False(cold.TickRetreatRound(request.CampaignId));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-post-assault-retreat"));
        using var evidence = RetreatTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("refuse-retreat", evidence.RootElement.GetProperty("fallback").GetString());
        Assert.Equal("deadline", evidence.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void RetreatTimeoutPreservesAcceptedOwnerReceiptAndDoesNotRunAgain()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId) = PrepareRetreatRound(journal);
        var (captor, element, opening) = RetreatOpening(journal, attacker, defender, defenderId);
        var accepted = journal.SubmitOwnerPostAssaultRetreat(captor, element, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        var cold = fixture.Open(clock);
        Assert.False(cold.TickRetreatRound(request.CampaignId));
        var retry = cold.SubmitOwnerPostAssaultRetreat(captor, element, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-retreat-timeout"));
    }

    [Fact]
    public void RetreatTimeoutInvalidLateProposalCannotConsumeRound()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId) = PrepareRetreatRound(journal);
        var (captor, element, opening) = RetreatOpening(journal, attacker, defender, defenderId);
        var before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(journal.SubmitOwnerPostAssaultRetreat(captor, element, opening.Revision, "invalid").IsAccepted);
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
        Assert.False(journal.SubmitOwnerPostAssaultRetreat(captor, element, opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-retreat-timeout"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetreatTimeoutClockFailureOrRegressionRetainsExplicitEvidence(bool regress)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareRetreatRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.False(journal.TickRetreatRound(request.CampaignId));
        if (regress) clock.Advance(TimeSpan.FromSeconds(-1));
        else clock.IsAvailable = false;
        Assert.True(journal.TickRetreatRound(request.CampaignId));
        using var evidence = RetreatTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(regress ? "clock-regression" : "clock-unavailable", evidence.RootElement.GetProperty("reason").GetString());
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetreatTimeoutMissingOrAlteredRoundFailsClosed(bool missing)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareRetreatRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = missing ? "DELETE FROM retreat_rounds" : "UPDATE retreat_rounds SET deadline_ms=deadline_ms+1";
        command.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => journal.TickRetreatRound(request.CampaignId));
    }

    [Fact]
    public void RetreatTimeoutLegacyTwelveRemainsReadOnly()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId) = PrepareRetreatRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM retreat_rounds; PRAGMA user_version=12";
        command.ExecuteNonQuery();
        var legacy = fixture.Open(clock);
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        Assert.False(legacy.TickRetreatRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => RetreatOpening(legacy, attacker, defender, defenderId));
    }

    [Fact]
    public void RetreatTimeoutRoundInsertFailureRollsBackEntry()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        long before = -1;
        string? campaign = null;
        Assert.Throws<SqliteException>(() => PrepareRetreatRound(journal, () =>
        {
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT campaign_id FROM campaigns";
            campaign = (string)query.ExecuteScalar()!;
            before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(campaign)).StateVersion;
            query.CommandText = "CREATE TRIGGER fail_retreat BEFORE INSERT ON retreat_rounds BEGIN SELECT RAISE(ABORT,'injected retreat failure'); END";
            query.ExecuteNonQuery();
        }));
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(campaign!)).StateVersion);
    }

    [Fact]
    public void RetreatTimeoutSettlementFailureRollsBackDispositionAndRoundClosure()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _) = PrepareRetreatRound(journal);
        var before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TRIGGER fail_retreat_successor BEFORE INSERT ON accepted_steps WHEN NEW.action_kind='native-combat-world-settlement' BEGIN SELECT RAISE(ABORT,'injected successor failure'); END";
        command.ExecuteNonQuery();
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Throws<SqliteException>(() => journal.TickRetreatRound(request.CampaignId));
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        Assert.Contains(request.CampaignId, journal.CollectingRetreatCampaigns("", 16));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-retreat-timeout"));
    }

    [Fact]
    public void RetreatTimeoutAllowsGenuineRepeatedAssaultColdReplay()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 6,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickRetreatRound(request.CampaignId));
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
        // Fixed seed has no second required retreat. Verify real continuation,
        // not a second timeout or seed-selected support claim.
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.False(fixture.Open(clock).TickRetreatRound(request.CampaignId));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-retreat-timeout"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

}
