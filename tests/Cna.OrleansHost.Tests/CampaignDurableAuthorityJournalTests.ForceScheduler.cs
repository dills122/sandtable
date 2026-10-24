using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public void ForceSchedulerRotatesPastCorruptionIndependentlyOfRbaPage()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true);
        for (var i = 0; i < CampaignCombatTimeoutService.BatchSize; i++)
        {
            var template = Request();
            var corrupt = new Cna.Core.Campaigns.CampaignCreationRequest(template.ContractVersion,
                $"a-corrupt-force-{i:D2}", template.RulesetHash, template.Seed,
                template.SetupId, template.SetupHash, template.ContentPackId,
                template.ContentHash, template.ScenarioId);
            journal.CreateCampaign(corrupt, Timing(corrupt));
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO force_rounds VALUES ($id,'bad','bad','bad',0,1,0,'collecting','bad')";
            insert.Parameters.AddWithValue("$id", corrupt.CampaignId);
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
        Assert.Contains(request.CampaignId, journal.CollectingForceCampaigns("", 100));
        scheduler.SweepOnce(CancellationToken.None);
        Assert.DoesNotContain(request.CampaignId, journal.CollectingForceCampaigns("", 100));
        Assert.Equal(CampaignCombatTimeoutService.BatchSize,
            journal.CollectingForceCampaigns("", 100).Count);
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock)
            .ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForceSchedulerExpiresDisconnectedRoundAfterRestart(bool retainSeal)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, _, _, opening, _) = PrepareInfantryForceRound(journal, positive: true);
        if (retainSeal)
            Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
                opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var cold = fixture.Open(clock);
        var scheduler = new CampaignCombatTimeoutService(cold, clock,
            NullLogger<CampaignCombatTimeoutService>.Instance);
        clock.Advance(TimeSpan.FromSeconds(30));
        scheduler.SweepOnce(CancellationToken.None);
        var after = cold.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(after).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        var head = CampaignExercises.QueryCheckpoint(after).StateVersion;
        scheduler.SweepOnce(CancellationToken.None);
        Assert.Equal(head, CampaignExercises.QueryCheckpoint(
            fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id AND action_kind='contact-force-seal'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(retainSeal ? 1L : 0L, query.ExecuteScalar());
        query.CommandText = "SELECT status FROM force_rounds WHERE campaign_id=$id";
        Assert.Equal("cancelled", query.ExecuteScalar());
    }
}
