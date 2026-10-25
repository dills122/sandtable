using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public void ForceEntryInsertFailureRollsBackDeclineAndRbaClosure()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        var prior = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var trigger = connection.CreateCommand();
            trigger.CommandText = "CREATE TRIGGER fail_force BEFORE INSERT ON force_rounds BEGIN SELECT RAISE(ABORT,'injected Force failure'); END";
            trigger.ExecuteNonQuery();
        }
        Assert.Throws<SqliteException>(() => journal.SubmitOwnerRbaDecline(defender,
            defenderId, rba.Revision, rba.PublicActionRefs[^1]));
        Assert.Equal(prior, CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(request.CampaignId)).StateVersion);
        Assert.Contains(request.CampaignId, journal.CollectingRbaCampaigns("", 16));
        Assert.Empty(journal.CollectingForceCampaigns("", 16));
    }

    [Fact]
    public void ForceEntryMissingRoundRejectsReadAndQueryWithoutRepair()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, attacker, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "DELETE FROM force_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => journal.QueryForceOwnerOpening(attacker, "axis-infantry"));
        change.CommandText = "SELECT COUNT(*) FROM force_rounds";
        Assert.Equal(0L, change.ExecuteScalar());
    }

    [Fact]
    public void ForceEntryLegacyEightReadsLazyHistoryButStillVerifiesRba()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "DELETE FROM force_rounds; PRAGMA user_version=8";
        change.ExecuteNonQuery();
        var old = fixture.Open();
        Assert.NotNull(old.ReadVerified(request.CampaignId));
        Assert.False(old.TickForceRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => old.QueryForceOwnerOpening(defender, defenderId));
        Assert.Throws<InvalidOperationException>(() => old.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret));
        change.CommandText = "DELETE FROM rba_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => old.ReadVerified(request.CampaignId));
    }

    [Fact]
    public void ForceEntryBindsValidatedDeclineTimeAndRejectsRehashedShift()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        clock.Advance(TimeSpan.FromSeconds(5));
        var entered = clock.GetUtcNow().ToUnixTimeMilliseconds();
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Single(journal.QueryForceOwnerOpening(defender, defenderId).PublicActionRefs);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT campaign_id,decision_action_id,decline_event_hash,configuration_hash,opened_ms,deadline_ms,high_water_ms,status FROM force_rounds";
        string hash;
        using (var reader = query.ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(entered, reader.GetInt64(4));
            Assert.Equal(entered + 30_000, reader.GetInt64(5));
            using var bytes = new MemoryStream();
            using (var writer = new BinaryWriter(bytes, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("campaign-force-round-v1");
                for (var i = 0; i < 4; i++) writer.Write(reader.GetString(i));
                writer.Write(reader.GetInt64(4) + 1);
                writer.Write(reader.GetInt64(5) + 1);
                writer.Write(reader.GetInt64(6));
                writer.Write(reader.GetString(7));
            }
            hash = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes.ToArray()));
        }
        query.CommandText = "UPDATE force_rounds SET opened_ms=opened_ms+1,deadline_ms=deadline_ms+1,record_hash=$hash";
        query.Parameters.AddWithValue("$hash", hash);
        query.ExecuteNonQuery();
        var error = Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.Contains("accepted RBA decline time", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ForceEntryExpiresWithoutAnyOwnerForceQuery()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        var cold = fixture.Open(clock);
        var scheduler = new CampaignCombatTimeoutService(cold, clock,
            NullLogger<CampaignCombatTimeoutService>.Instance);
        scheduler.SweepOnce(CancellationToken.None);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(cold.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void ForceEntryLateFirstQueryCannotRestartDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Throws<InvalidOperationException>(() =>
            fixture.Open(clock).QueryForceOwnerOpening(defender, defenderId));
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
    }
}
