using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static (CampaignCreationRequest Request, CampaignSeatCredential Owner)
        PrepareReserveRound(CampaignDurableAuthorityJournal journal, bool two = false,
            bool withTiming = true, Action? beforeFinish = null)
    {
        if (two)
        {
            var (combatRequest, attacker, defender, defenderId, attackerForce, defenderForce) =
                PrepareInfantryForceRound(journal, positive: true, doubleContact: true,
                    reserveElementId: "axis-infantry", secondReserveElementId: "axis-infantry-second");
            Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
                attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
            Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
                defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
            return (combatRequest, attacker);
        }
        var request = PositiveDoubleContactRequest(12345);
        AdvanceToContactCombat(journal, request, withTiming ? Timing(request) : null,
            preserveAdjacentInfantry: true, reserveElementId: "axis-infantry",
            secondReserveElementId: two ? "axis-infantry-second" : null);
        var owner = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        beforeFinish?.Invoke();
        Assert.True(journal.SubmitOwnerCombatSelection(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        return (request, owner);
    }

    private static JsonDocument ReserveTimeoutEvent(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id AND action_kind='native-reserve-release-timeout' ORDER BY sequence DESC LIMIT 1";
        query.Parameters.AddWithValue("$id", campaignId);
        return JsonDocument.Parse((byte[])query.ExecuteScalar()!);
    }

    [Fact]
    public void ReserveTimeoutStartsAtEntryAndExpiresWithoutOwnerQueryAfterRestart()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareReserveRound(journal);
        Assert.Contains(request.CampaignId, journal.CollectingReserveCampaigns("", 16));
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(journal.TickReserveRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var cold = fixture.Open(clock);
        new CampaignCombatTimeoutService(cold, clock, NullLogger<CampaignCombatTimeoutService>.Instance)
            .SweepOnce(CancellationToken.None);
        var session = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(35, CampaignExercises.QueryCheckpoint(session).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.False(cold.TickReserveRound(request.CampaignId));
        Assert.Contains(request.CampaignId, cold.CollectingCycleCampaigns("", 16));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-reserve-release-choice"));
        using var evidence = ReserveTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("convert-unresolved-i-retain-ii", evidence.RootElement.GetProperty("fallback").GetString());
        Assert.Equal("axis-infantry", Assert.Single(evidence.RootElement.GetProperty("convertedElementIds").EnumerateArray()).GetString());
    }

    [Fact]
    public void ReserveTimeoutPreservesAcceptedOwnerChoiceAndOriginalDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareReserveRound(journal, two: true);
        clock.Advance(TimeSpan.FromSeconds(20));
        var opening = journal.QueryReserveReleaseOwnerOpening(owner, "axis-infantry");
        var accepted = journal.SubmitOwnerReserveRelease(owner, "axis-infantry", opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(fixture.Open(clock).TickReserveRound(request.CampaignId));
        var cold = fixture.Open(clock);
        var retry = cold.SubmitOwnerReserveRelease(owner, "axis-infantry", opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        using var evidence = ReserveTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("axis-infantry-second", Assert.Single(evidence.RootElement.GetProperty("convertedElementIds").EnumerateArray()).GetString());
        var cycle = cold.QueryCycleControlOwnerOpening(owner);
        using var visible = JsonDocument.Parse(cycle.VisibleBytes);
        Assert.Equal("Repeat", visible.RootElement.GetProperty("choices")[0].GetString());
        Assert.True(cold.SubmitOwnerCycleControl(owner, cycle.Revision, cycle.PublicActionRefs[0]).IsAccepted);
        var movement = cold.QueryRepeatMovementOwnerOpening(owner);
        Assert.True(cold.SubmitOwnerRepeatMovement(owner, movement.Revision, movement.PublicActionRefs[0]).IsAccepted);
        var combat = cold.QueryRepeatCombatOwnerOpening(owner, "axis-infantry");
        Assert.True(cold.SubmitOwnerRepeatCombat(owner, "axis-infantry", combat.Revision, combat.PublicActionRefs[^1]).IsAccepted);
        Assert.Contains(request.CampaignId, cold.CollectingReserveCampaigns("", 16));
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickReserveRound(request.CampaignId));
        using var repeatedEvidence = ReserveTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(2, repeatedEvidence.RootElement.GetProperty("cycleOrdinal").GetInt32());
        Assert.Empty(repeatedEvidence.RootElement.GetProperty("convertedElementIds").EnumerateArray());
        Assert.Equal("axis-infantry-second", Assert.Single(repeatedEvidence.RootElement.GetProperty("retainedElementIds").EnumerateArray()).GetString());
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void ReserveTimeoutInvalidLateProposalDoesNotConsumeDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareReserveRound(journal);
        var opening = journal.QueryReserveReleaseOwnerOpening(owner, "axis-infantry");
        var prior = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(journal.SubmitOwnerReserveRelease(owner, "axis-infantry", opening.Revision, "invalid").IsAccepted);
        Assert.Equal(prior, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
        Assert.False(journal.SubmitOwnerReserveRelease(owner, "axis-infantry", opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(35, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReserveTimeoutClockFailureOrRegressionUsesExplicitEvidence(bool regress)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareReserveRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        _ = journal.QueryReserveReleaseOwnerOpening(owner, "axis-infantry");
        if (regress) clock.Advance(TimeSpan.FromSeconds(-1));
        else clock.IsAvailable = false;
        Assert.True(journal.TickReserveRound(request.CampaignId));
        using var evidence = ReserveTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(regress ? "clock-regression" : "clock-unavailable", evidence.RootElement.GetProperty("reason").GetString());
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReserveTimeoutUnavailableEntryInventsNoClockOrOwnerChoice(bool missingClock)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        CampaignCreationRequest request;
        if (missingClock)
        {
            (request, _, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true,
                reserveElementId: "axis-infantry");
            clock.IsAvailable = false;
            Assert.True(journal.TickForceRound(request.CampaignId));
        }
        else (request, _) = PrepareReserveRound(journal, withTiming: false);
        Assert.Empty(journal.CollectingReserveCampaigns("", 16));
        using var evidence = ReserveTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("reserve-release-unavailable", evidence.RootElement.GetProperty("reason").GetString());
        Assert.Equal(-1, evidence.RootElement.GetProperty("openedAtUnixMilliseconds").GetInt64());
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-reserve-release-choice"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void ReserveTimeoutInsertFailureRollsBackCombatEntry()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        long prior = -1;
        Assert.Throws<SqliteException>(() => PrepareReserveRound(journal, beforeFinish: () =>
        {
            prior = CampaignExercises.QueryCheckpoint(journal.ReadVerified(PositiveDoubleContactRequest(12345).CampaignId)).StateVersion;
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TRIGGER fail_reserve BEFORE INSERT ON reserve_rounds BEGIN SELECT RAISE(ABORT,'injected reserve failure'); END";
            command.ExecuteNonQuery();
        }));
        Assert.Equal(prior, CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(PositiveDoubleContactRequest(12345).CampaignId)).StateVersion);
        Assert.Empty(journal.CollectingReserveCampaigns("", 16));
    }

    [Fact]
    public void ReserveTimeoutCycleInsertFailureRollsBackFallbackAndRoundClosure()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareReserveRound(journal);
        var prior = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "CREATE TRIGGER fail_next_cycle BEFORE INSERT ON cycle_rounds BEGIN SELECT RAISE(ABORT,'injected cycle failure'); END";
        change.ExecuteNonQuery();
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Throws<SqliteException>(() => journal.TickReserveRound(request.CampaignId));
        Assert.Equal(prior, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        Assert.Contains(request.CampaignId, journal.CollectingReserveCampaigns("", 16));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-reserve-release-timeout"));
        change.CommandText = "DROP TRIGGER fail_next_cycle";
        change.ExecuteNonQuery();
        Assert.True(journal.TickReserveRound(request.CampaignId));
    }

    [Fact]
    public void ReserveTimeoutLegacyTenRetainsCycleRoundVerification()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, _) = PrepareCycleRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "PRAGMA user_version=10";
        change.ExecuteNonQuery();
        Assert.NotNull(fixture.Open().ReadVerified(request.CampaignId));
        change.CommandText = "DELETE FROM cycle_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Fact]
    public void ReserveTimeoutAlteredRoundRejectsColdReplay()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, _) = PrepareReserveRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "UPDATE reserve_rounds SET deadline_ms=deadline_ms+1";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => journal.TickReserveRound(request.CampaignId));
    }

    [Fact]
    public void ReserveTimeoutMissingRowRejectsAndLegacyTenRemainsReadOnly()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, owner) = PrepareReserveRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "DELETE FROM reserve_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
        change.CommandText = "PRAGMA user_version=10";
        change.ExecuteNonQuery();
        var legacy = fixture.Open();
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        Assert.False(legacy.TickReserveRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => legacy.QueryReserveReleaseOwnerOpening(owner, "axis-infantry"));
    }
}
