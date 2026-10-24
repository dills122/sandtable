using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static (Cna.Core.Campaigns.CampaignCreationRequest Request, CampaignSeatCredential Owner)
        PrepareCycleRound(CampaignDurableAuthorityJournal journal, bool withTiming = true, Action? beforeFinish = null)
    {
        var request = Request();
        AdvanceToContactCombat(journal, request, withTiming ? Timing(request) : null);
        var owner = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        beforeFinish?.Invoke();
        Assert.True(journal.SubmitOwnerCombatSelection(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[1]).IsAccepted);
        return (request, owner);
    }

    [Fact]
    public void CycleTimeoutExpiresAfterRestartWithoutOwnerQuery()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _) = PrepareCycleRound(fixture.Create(clock));
        var cold = fixture.Open(clock);
        clock.Advance(TimeSpan.FromSeconds(30));
        var scheduler = new CampaignCombatTimeoutService(cold, clock,
            NullLogger<CampaignCombatTimeoutService>.Instance);
        scheduler.SweepOnce(CancellationToken.None);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(cold.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void CycleTimeoutInvalidLateReferenceCannotConsumeDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareCycleRound(journal);
        var opening = journal.QueryCycleControlOwnerOpening(owner);
        var head = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(journal.SubmitOwnerCycleControl(owner, opening.Revision, "bad-ref").IsAccepted);
        Assert.Equal(head, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
        Assert.False(journal.SubmitOwnerCycleControl(owner, opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-cycle-control"));
    }

    [Fact]
    public void CycleTimeoutQueriesDoNotResetDeadlineAndOwnerRetryRemainsIdempotent()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareCycleRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var opening = journal.QueryCycleControlOwnerOpening(owner);
        var accepted = journal.SubmitOwnerCycleControl(owner, opening.Revision, opening.PublicActionRefs[^1]);
        Assert.True(accepted.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        var cold = fixture.Open(clock);
        Assert.False(cold.TickCycleRound(request.CampaignId));
        var retry = cold.SubmitOwnerCycleControl(owner, opening.Revision, opening.PublicActionRefs[^1]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(cold.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.DoesNotContain(request.CampaignId, cold.CollectingCycleCampaigns("", 16));
    }

    [Fact]
    public void CycleTimeoutLateFirstQueryClosesOriginalEntryDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareCycleRound(journal);
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        _ = journal.QueryCycleControlOwnerOpening(owner);
        Assert.False(journal.TickCycleRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).QueryCycleControlOwnerOpening(owner));
        Assert.False(journal.TickCycleRound(request.CampaignId));
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CycleTimeoutClockFailureOrRegressionFinishesWithoutOwner(bool regress)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareCycleRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        _ = journal.QueryCycleControlOwnerOpening(owner);
        if (regress) clock.Advance(TimeSpan.FromSeconds(-1));
        else clock.IsAvailable = false;
        Assert.True(journal.TickCycleRound(request.CampaignId));
        Assert.False(fixture.Open(clock).TickCycleRound(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-cycle-control"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CycleTimeoutUnavailableEntryInventsNoRound(bool missingClock)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        Cna.Core.Campaigns.CampaignCreationRequest request;
        if (missingClock)
        {
            (request, _, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true);
            clock.IsAvailable = false;
            Assert.True(journal.TickForceRound(request.CampaignId));
        }
        else (request, _) = PrepareCycleRound(journal, withTiming: false);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
        Assert.Empty(journal.CollectingCycleCampaigns("", 16));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void CycleTimeoutInsertFailureRollsBackCompletion()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        long prior = -1;
        Assert.Throws<SqliteException>(() => PrepareCycleRound(journal, beforeFinish: () =>
        {
            prior = CampaignExercises.QueryCheckpoint(journal.ReadVerified(Request().CampaignId)).StateVersion;
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TRIGGER fail_cycle BEFORE INSERT ON cycle_rounds BEGIN SELECT RAISE(ABORT,'injected cycle failure'); END";
            command.ExecuteNonQuery();
        }));
        Assert.Equal(prior, CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(Request().CampaignId)).StateVersion);
        Assert.Empty(journal.CollectingCycleCampaigns("", 16));
    }

    [Fact]
    public void CycleTimeoutMissingRoundRejectsAndLegacyNineRemainsReadOnly()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, owner) = PrepareCycleRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "DELETE FROM cycle_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => journal.QueryCycleControlOwnerOpening(owner));
        change.CommandText = "PRAGMA user_version=9";
        change.ExecuteNonQuery();
        var legacy = fixture.Open();
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        Assert.False(legacy.TickCycleRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => legacy.QueryCycleControlOwnerOpening(owner));
    }


    [Fact]
    public void CycleTimeoutLegacyNinePreservesForceEntryVerification()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var (request, _, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "PRAGMA user_version=9";
        change.ExecuteNonQuery();
        Assert.NotNull(fixture.Open().ReadVerified(request.CampaignId));
        change.CommandText = "DELETE FROM force_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Fact]
    public void CycleTimeoutSchedulerRotatesPastCorruptionWithoutOtherQueueStarvation()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareCycleRound(journal);
        for (var i = 0; i < CampaignCombatTimeoutService.BatchSize; i++)
        {
            var template = Request();
            var corrupt = new Cna.Core.Campaigns.CampaignCreationRequest(template.ContractVersion,
                $"a-corrupt-cycle-{i:D2}", template.RulesetHash, template.Seed,
                template.SetupId, template.SetupHash, template.ContentPackId,
                template.ContentHash, template.ScenarioId);
            journal.CreateCampaign(corrupt, Timing(corrupt));
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO cycle_rounds VALUES ($id,'bad',1,'bad',0,1,0,'collecting','bad')";
            insert.Parameters.AddWithValue("$id", corrupt.CampaignId);
            insert.ExecuteNonQuery();
            insert.CommandText = "INSERT INTO force_rounds VALUES ($id,'bad','bad','bad',0,1,0,'collecting','bad')";
            insert.ExecuteNonQuery();
            insert.CommandText = "INSERT INTO rba_rounds VALUES ($id,'bad','bad','bad',0,1,0,'collecting','bad')";
            insert.ExecuteNonQuery();
            insert.CommandText = "UPDATE campaigns SET seed='bad' WHERE campaign_id=$id";
            insert.ExecuteNonQuery();
        }
        var scheduler = new CampaignCombatTimeoutService(journal, clock,
            NullLogger<CampaignCombatTimeoutService>.Instance);
        clock.Advance(TimeSpan.FromSeconds(30));
        scheduler.SweepOnce(CancellationToken.None);
        Assert.Contains(request.CampaignId, journal.CollectingCycleCampaigns("", 100));
        scheduler.SweepOnce(CancellationToken.None);
        Assert.DoesNotContain(request.CampaignId, journal.CollectingCycleCampaigns("", 100));
        Assert.Equal(CampaignCombatTimeoutService.BatchSize, journal.CollectingCycleCampaigns("", 100).Count);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
    }

}
