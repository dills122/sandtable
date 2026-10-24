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
    private static (CampaignCreationRequest Request, CampaignSeatCredential Defender, string DefenderId)
        PrepareRbaRound(CampaignDurableAuthorityJournal journal, Action? beforeBarrage = null)
    {
        var request = PositiveCombatRequest();
        AdvanceToContactCombat(journal, request, Timing(request), preserveAdjacentInfantry: true);
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(attacker, "axis-infantry");
        beforeBarrage?.Invoke();
        Assert.True(journal.SubmitOwnerCombatSelection(attacker, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        return (request, defender, defenderId);
    }

    [Fact]
    public void RbaDeadlineExpiresAfterColdRestartWithoutOwnerOpening()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _, _) = PrepareRbaRound(fixture.Create(clock));
        var cold = fixture.Open(clock);
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(cold.TickRbaRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(cold.TickRbaRound(request.CampaignId));
        Assert.False(cold.TickRbaRound(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        AssertRbaCancellationOnly(fixture.Path);
    }

    [Fact]
    public void RbaInvalidLateReferenceDoesNotConsumeDeadlineButValidLateDeclineCancels()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        var opening = journal.QueryRbaOwnerOpening(defender, defenderId);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(journal.SubmitOwnerRbaDecline(defender, defenderId, opening.Revision,
            "invalid").IsAccepted);
        Assert.Single(journal.CollectingRbaCampaigns("", 16));
        Assert.False(journal.SubmitOwnerRbaDecline(defender, defenderId, opening.Revision,
            opening.PublicActionRefs[^1]).IsAccepted);
        Assert.Empty(journal.CollectingRbaCampaigns("", 16));
        AssertRbaCancellationOnly(fixture.Path);
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RbaClockLossOrRegressionCancelsWithoutDecline(bool regression)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _) = PrepareRbaRound(journal);
        if (regression) clock.Advance(TimeSpan.FromMilliseconds(-1));
        else clock.IsAvailable = false;
        Assert.True(journal.TickRbaRound(request.CampaignId));
        AssertRbaCancellationOnly(fixture.Path);
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void RbaSchedulerExpiresUnansweredRoundAndHonorsCancellation()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _) = PrepareRbaRound(journal);
        var scheduler = new CampaignCombatTimeoutService(journal, clock,
            NullLogger<CampaignCombatTimeoutService>.Instance);
        scheduler.SweepOnce(CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(30));
        scheduler.SweepOnce(CancellationToken.None);
        Assert.Empty(journal.CollectingRbaCampaigns("", 16));
        AssertRbaCancellationOnly(fixture.Path);
        Assert.Throws<OperationCanceledException>(() => scheduler.SweepOnce(new CancellationToken(true)));
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void RbaMissingOrAlteredDeadlineRejectsColdReplay()
    {
        using var fixture = new JournalFixture();
        var (request, _, _) = PrepareRbaRound(fixture.Create(new ManualClock()));
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var change = connection.CreateCommand();
        change.CommandText = "UPDATE rba_rounds SET deadline_ms=deadline_ms+1";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
        change.CommandText = "DELETE FROM rba_rounds";
        change.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("18446744073709551616")]
    public void RbaSchedulerRotatesPastCorruptCampaigns(string invalidSeed)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _) = PrepareRbaRound(journal);
        for (var i = 0; i < CampaignCombatTimeoutService.BatchSize; i++)
        {
            var template = Request();
            var corrupt = new CampaignCreationRequest(template.ContractVersion, $"a-corrupt-{i:D2}",
                template.RulesetHash, template.Seed, template.SetupId, template.SetupHash,
                template.ContentPackId, template.ContentHash, template.ScenarioId);
            journal.CreateCampaign(corrupt, Timing(corrupt));
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO rba_rounds VALUES ($id,'bad','bad','bad',0,1,0,'collecting','bad')";
            insert.Parameters.AddWithValue("$id", corrupt.CampaignId);
            insert.ExecuteNonQuery();
            insert.CommandText = "UPDATE campaigns SET seed=$seed WHERE campaign_id=$id";
            insert.Parameters.AddWithValue("$seed", invalidSeed);
            insert.ExecuteNonQuery();
        }
        var scheduler = new CampaignCombatTimeoutService(journal, clock,
            NullLogger<CampaignCombatTimeoutService>.Instance);
        clock.Advance(TimeSpan.FromSeconds(30));
        scheduler.SweepOnce(CancellationToken.None);
        Assert.Contains(request.CampaignId, journal.CollectingRbaCampaigns("", 100));
        scheduler.SweepOnce(CancellationToken.None);
        Assert.DoesNotContain(request.CampaignId, journal.CollectingRbaCampaigns("", 100));
        AssertRbaCancellationOnly(fixture.Path);
    }

    [Fact]
    public void RbaSchemaSevenReadsWithoutInventedDeadlineAndRejectsMutation()
    {
        using var fixture = new JournalFixture();
        var request = Request();
        fixture.Create().CreateCampaign(request, Timing(request));
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var change = connection.CreateCommand();
            change.CommandText = "DROP TABLE rba_rounds; PRAGMA user_version=7";
            change.ExecuteNonQuery();
        }
        var previous = fixture.Open();
        Assert.NotNull(previous.ReadVerified(request.CampaignId));
        Assert.False(previous.TickRbaRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => previous.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret));
    }

    [Fact]
    public void RbaRetreatMovesAndRetriesRetainOriginalDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = Request();
        AdvanceToContactCombat(journal, request, Timing(request));
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var combatOpening = journal.QueryCombatOwnerOpening(attacker, "axis-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(attacker, "axis-infantry",
            combatOpening.Revision, combatOpening.PublicActionRefs[0]).IsAccepted);
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var opening = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(opening.PublicActionRefs.Count > 1);
        var startingSession = journal.ReadVerified(request.CampaignId);
        var draft = CampaignHostAdapter.QueryContactRbaOwnerDraft(startingSession,
            CampaignActionAudience.Commonwealth, defenderId);
        var moveOrdinal = Enumerable.Range(0, draft.PrivateActionIds.Count - 1).First(index =>
        {
            var candidate = CampaignHostAdapter.SubmitContactDefenderRbaMove(startingSession,
                new CampaignActionSubmission(1, request.CampaignId,
                    CampaignExercises.QueryCheckpoint(startingSession).StateVersion,
                    CampaignExercises.QueryCheckpoint(startingSession).PositionId,
                    CampaignActionAudience.Commonwealth, draft.PrivateActionIds[index]),
                defenderId);
            return candidate.IsAccepted
                && CampaignHostAdapter.QueryContactRbaOwnerDraft(candidate.SuccessorSession!,
                    CampaignActionAudience.Commonwealth, defenderId).PrivateActionIds.Count > 1;
        });
        var moveRef = opening.PublicActionRefs[moveOrdinal];
        Assert.False(journal.SubmitOwnerRbaDecline(defender, defenderId,
            opening.Revision, moveRef).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(10));
        var moved = journal.SubmitOwnerRbaMove(defender, defenderId,
            opening.Revision, moveRef);
        Assert.True(moved.IsAccepted);
        var cold = fixture.Open(clock);
        Assert.Equal(moved.CanonicalReceipt,
            cold.SubmitOwnerRbaMove(defender, defenderId,
                opening.Revision, moveRef).CanonicalReceipt);
        var afterMove = cold.ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterMove).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterMove).IsVerified);
        var next = cold.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(next.Revision > opening.Revision);
        Assert.True(next.PublicActionRefs.Count > 1);
        clock.Advance(TimeSpan.FromSeconds(10));
        var repeated = cold.SubmitOwnerRbaMove(defender, defenderId,
            next.Revision, next.PublicActionRefs[0]);
        Assert.True(repeated.IsAccepted);
        var afterRepeat = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterRepeat).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterRepeat).IsVerified);
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(cold.TickRbaRound(request.CampaignId));
        AssertRbaCancellationOnly(fixture.Path);
        Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE action_kind='contact-rba-move'";
        Assert.Equal(2L, query.ExecuteScalar());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RbaRepeatedAssaultOrForceEntryTimeoutColdReplaysRealHistory(bool forceEntry)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var beforeRelease = journal.ReadVerified(request.CampaignId);
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(beforeRelease).ContractVersion);
        _ = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(beforeRelease,
            CampaignActionAudience.Axis, "axis-infantry-second");
        var release = journal.QueryReserveReleaseOwnerOpening(attacker,
            "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker,
            control.Revision, control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker,
            movement.Revision, movement.PublicActionRefs[1]).IsAccepted);
        var opening = journal.QueryRepeatCombatOwnerOpening(attacker,
            "axis-infantry-second");
        using var visible = System.Text.Json.JsonDocument.Parse(opening.VisibleBytes);
        var offers = visible.RootElement.GetProperty("assaultOffers");
        var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
            offers[index].GetProperty("apparentLocationId").GetString() == "west");
        Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-second",
            opening.Revision, opening.PublicActionRefs[west]).IsAccepted);
        var cold = fixture.Open(clock);
        var session = cold.ReadVerified(request.CampaignId);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Equal(14, checkpoint.ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        var retry = cold.SubmitOwnerRepeatCombat(attacker,
            "axis-infantry-second", opening.Revision, opening.PublicActionRefs[west]);
        Assert.True(retry.IsAccepted);
        Assert.NotEmpty(retry.CanonicalReceipt!);
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            session, CampaignActionAudience.Commonwealth);
        Assert.Equal("commonwealth-infantry-west", westDefender);
        if (forceEntry)
        {
            var defenderOpening = cold.QueryRbaOwnerOpening(defender, westDefender);
            Assert.True(cold.SubmitOwnerRbaDecline(defender, westDefender,
                defenderOpening.Revision, defenderOpening.PublicActionRefs[^1]).IsAccepted);
        }
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(forceEntry ? cold.TickForceRound(request.CampaignId) : cold.TickRbaRound(request.CampaignId));
        Assert.False(forceEntry ? cold.TickForceRound(request.CampaignId) : cold.TickRbaRound(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM rba_rounds WHERE status='completed'";
        Assert.Equal(forceEntry ? 2L : 1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM rba_rounds WHERE status='cancelled'";
        Assert.Equal(forceEntry ? 0L : 1L, query.ExecuteScalar());
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickCycleRound(request.CampaignId));
        Assert.False(fixture.Open(clock).TickCycleRound(request.CampaignId));
        var cycleFinished = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(cycleFinished).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cycleFinished).IsVerified);
    }

    [Fact]
    public void RbaDeadlinePersistenceFailureRollsBackSelectionAndBarrage()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var request = PositiveCombatRequest();
        long prior = -1;
        Assert.Throws<SqliteException>(() => PrepareRbaRound(journal, () =>
        {
            prior = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var trigger = connection.CreateCommand();
            trigger.CommandText = "CREATE TRIGGER fail_rba BEFORE INSERT ON rba_rounds BEGIN SELECT RAISE(ABORT,'injected RBA failure'); END";
            trigger.ExecuteNonQuery();
        }));
        Assert.Equal(prior, CampaignExercises.QueryCheckpoint(
            fixture.Open(new ManualClock()).ReadVerified(request.CampaignId)).StateVersion);
        Assert.Empty(journal.CollectingRbaCampaigns("", 16));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RbaThirdAssaultOrForceEntryTimeoutColdReplaysRealHistory(bool forceEntry)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, firstDefenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true,
                reserveElementId: "axis-infantry-second", tripleContact: true,
                secondReserveElementId: "axis-infantry-third");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, firstDefenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var release = journal.QueryReserveReleaseOwnerOpening(attacker,
            "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker,
            "axis-infantry-second", release.Revision,
            release.PublicActionRefs[0]).IsAccepted);
        var heldThird = journal.QueryReserveReleaseOwnerOpening(attacker,
            "axis-infantry-third");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker,
            "axis-infantry-third", heldThird.Revision,
            heldThird.PublicActionRefs[^1]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision,
            control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision,
            movement.PublicActionRefs[1]).IsAccepted,
            System.Text.Encoding.UTF8.GetString(movement.VisibleBytes));
        var selection = journal.QueryRepeatCombatOwnerOpening(attacker,
            "axis-infantry-second");
        using (var visible = JsonDocument.Parse(selection.VisibleBytes))
        {
            var offers = visible.RootElement.GetProperty("assaultOffers");
            var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                offers[index].GetProperty("apparentLocationId").GetString() == "west");
            Assert.True(journal.SubmitOwnerRepeatCombat(attacker,
                "axis-infantry-second", selection.Revision,
                selection.PublicActionRefs[west]).IsAccepted);
        }
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        var secondAttackerForce = journal.QueryForceOwnerOpening(attacker,
            "axis-infantry-second");
        var secondDefenderForce = journal.QueryForceOwnerOpening(defender,
            westDefender);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-second",
            secondAttackerForce.Revision,
            secondAttackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, westDefender,
            secondDefenderForce.Revision,
            secondDefenderForce.PublicActionRefs[0]).IsAccepted);
        release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-third");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-third",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.Contains("Repeat", System.Text.Encoding.UTF8.GetString(control.VisibleBytes),
            StringComparison.Ordinal);
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision,
            control.PublicActionRefs[0]).IsAccepted);
        movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision,
            movement.PublicActionRefs[0]).IsAccepted);
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        var thirdCombat = fixture.Open(clock).QueryRepeatCombatOwnerOpening(attacker,
            "axis-infantry-third");
        using var thirdVisible = JsonDocument.Parse(thirdCombat.VisibleBytes);
        var thirdOffers = thirdVisible.RootElement.GetProperty("assaultOffers");
        var farNorthWest = Enumerable.Range(0, thirdOffers.GetArrayLength())
            .Single(index => thirdOffers[index]
                .GetProperty("apparentLocationId").GetString() == "far-north-west");
        Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-third",
            thirdCombat.Revision, thirdCombat.PublicActionRefs[farNorthWest]).IsAccepted);
        if (forceEntry)
        {
            var thirdDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
                journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
            var thirdRba = journal.QueryRbaOwnerOpening(defender, thirdDefender);
            Assert.True(journal.SubmitOwnerRbaDecline(defender, thirdDefender,
                thirdRba.Revision, thirdRba.PublicActionRefs[^1]).IsAccepted);
        }
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(forceEntry ? fixture.Open(clock).TickForceRound(request.CampaignId)
            : fixture.Open(clock).TickRbaRound(request.CampaignId));
        Assert.False(forceEntry ? fixture.Open(clock).TickForceRound(request.CampaignId)
            : fixture.Open(clock).TickRbaRound(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        Assert.Equal(forceEntry ? 0 : 1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-rba-cancellation"));
        Assert.Equal(forceEntry ? 3 : 2, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-rba-decline"));
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(fixture.Open(clock).TickCycleRound(request.CampaignId));
        Assert.False(fixture.Open(clock).TickCycleRound(request.CampaignId));
        var cycleFinished = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(cycleFinished).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cycleFinished).IsVerified);
    }

    private static void AssertRbaCancellationOnly(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE action_kind='native-combat-rba-cancellation'";
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE action_kind IN ('contact-rba-decline','contact-force-seal','native-combat-assault-result')";
        Assert.Equal(0L, query.ExecuteScalar());
    }
}
