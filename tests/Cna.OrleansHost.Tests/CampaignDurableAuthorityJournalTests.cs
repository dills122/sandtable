using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private const string OperatorSecret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    /// <summary>
    /// Phase C3a of issue #439: <c>CampaignDurableOutwardApi</c>'s <c>/{campaignId}/actions</c>
    /// route now dispatches through <c>ICampaignGrain</c>, so any test that builds a
    /// <c>WebApplication</c> via <c>MapDurableCampaignApi</c> and exercises that route needs a
    /// real Orleans silo backing it. Built as its own <c>Host.CreateApplicationBuilder()</c> silo
    /// (same pattern as <c>CampaignDurableOutwardApiTests.cs</c> and
    /// <c>GrainEquivalence.cs</c>) rather than through the <c>WebApplication</c>'s own
    /// <c>Host</c> property; the silo's <c>IGrainFactory</c> is shared into the web app's own DI
    /// container by the caller. Caller is responsible for stopping/disposing the returned host.
    /// </summary>
    private static async Task<IHost> StartOutwardApiSiloAsync(CampaignDurableAuthorityJournalStore store)
    {
        var siloBuilder = Host.CreateApplicationBuilder();
        siloBuilder.Services.AddSingleton(store);
        // CampaignGrain arms/disarms a CampaignRoundTimeoutGrain for any round-timer signal
        // accumulated on the underlying journal -- including, in a test that mixes direct
        // journal calls with grain-routed ones, a backlog from calls made before this silo
        // existed. That grain's own constructor needs ICampaignRoundDeadlineSink resolvable
        // even when this particular test never exercises deadline behavior itself.
        siloBuilder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
        siloBuilder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: 0);
            silo.AddReminders();
            silo.UseInMemoryReminderService();
        });
        var silo = siloBuilder.Build();
        await silo.StartAsync(TestContext.Current.CancellationToken);
        return silo;
    }

    private static async Task StopOutwardApiSiloAsync(IHost silo)
    {
        await silo.StopAsync(TestContext.Current.CancellationToken);
        silo.Dispose();
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [Fact]
    public void ExplicitCampaignTimingIsCanonicalBoundAndColdVerified()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        var timing = new CampaignDecisionTimingConfigurationV1(
            "rules-lab.campaign-timing.1", request.RulesetHash, request.ContentHash,
            [new("custody", 30_000, "leave-unguarded"),
                new("cycle-control", 30_000, "finish-phase"),
                new("force-assignment", 30_000, "cancel-incomplete-voluntary-round"),
                new("rba", 30_000, "cancel-without-decline"),
                new("reserve-release", 30_000, "convert-unresolved-i-retain-ii"),
                new("retreat", 30_000, "refuse-retreat"),
                new("selection", 30_000, "no-selection")]);
        var canonical = timing.SerializeCanonical();
        Assert.Equal(timing.Hash, "sha256:" + Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(canonical)));
        Assert.Equal(canonical, CampaignDecisionTimingConfigurationV1
            .DeserializeCanonical(canonical).SerializeCanonical());
        Assert.Throws<ArgumentException>(() => journal.CreateCampaign(request,
            new CampaignDecisionTimingConfigurationV1(timing.ConfigId, request.RulesetHash,
                "sha256:" + new string('0', 64), timing.Windows)));
        journal.CreateCampaign(request, timing);
        Assert.NotNull(journal.ReadVerified(request.CampaignId));
        var reopened = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path);
        Assert.NotNull(reopened.ReadVerified(request.CampaignId));
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE campaigns SET timing_configuration=$bytes WHERE campaign_id=$id";
            command.Parameters.AddWithValue("$bytes", new CampaignDecisionTimingConfigurationV1(
                timing.ConfigId, timing.RulesetHash, timing.ContentHash,
                timing.Windows.Select(value => value.Kind == "force-assignment"
                    ? value with { DecisionBudgetMilliseconds = 29_999 } : value))
                .SerializeCanonical());
            command.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1, command.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => reopened.ReadVerified(request.CampaignId));
    }

    [Theory]
    [InlineData("force_rounds_status_campaign")]
    [InlineData("rba_rounds_status_campaign")]
    [InlineData("cycle_rounds_status_campaign")]
    [InlineData("reserve_rounds_status_campaign")]
    [InlineData("custody_rounds_status_campaign")]
    [InlineData("retreat_rounds_status_campaign")]
    [InlineData("selection_rounds_status_campaign")]
    public void RoundTableGetsStatusCampaignIndexOnCreateAndOnReopenOfAnOlderJournal(string indexName)
    {
        using var fixture = new JournalFixture();
        fixture.Create();

        // CreateNew must leave the index in place immediately.
        Assert.True(IndexExists(fixture.Path, indexName),
            $"Expected {indexName} to exist right after CreateNew.");

        // Simulate a journal file written by a build that predates this index: there is no
        // ALTER TABLE/migration mechanism in this codebase (issue #330), so an older on-disk
        // journal is indistinguishable from one that simply never had the index created.
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP INDEX {indexName}";
            drop.ExecuteNonQuery();
        }
        Assert.False(IndexExists(fixture.Path, indexName),
            $"Expected {indexName} to be gone after simulating an older journal.");

        // OpenExisting itself only opens a read-only connection to check the schema version, so
        // the index must reappear the first time a real operation opens a writable connection
        // via Connect(create: false) -- proving that path (used by every CollectingXCampaigns /
        // TickXRound / CreateCampaign call on a reopened journal), not just CreateNew, restores
        // the index on a pre-existing database file.
        var reopened = fixture.Open();
        _ = reopened.CollectingSelectionCampaigns("", 1);
        Assert.True(IndexExists(fixture.Path, indexName),
            $"Expected {indexName} to be recreated after OpenExisting plus one operation.");

        static bool IndexExists(string path, string indexName)
        {
            using var connection = new SqliteConnection($"Data Source={path}");
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT 1 FROM sqlite_master WHERE type='index' AND name=$name";
            query.Parameters.AddWithValue("$name", indexName);
            return query.ExecuteScalar() is not null;
        }
    }

    [Fact]
    public void ContactCombatOpeningCommitsOnceAndColdReplaysSelectedAuthority()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        var session = AdvanceToContactCombat(journal, request, Timing(request));
        Assert.NotEmpty(CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Axis, "axis-infantry").PrivateCombatActionIds);
        var credential = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(credential, "axis-infantry");
        Assert.Equal(2, opening.PublicActionRefs.Count);
        var visible = System.Text.Encoding.UTF8.GetString(opening.VisibleBytes);
        Assert.DoesNotContain("boundRepresentationId", visible, StringComparison.Ordinal);
        var privateAction = CampaignHostAdapter.QueryContactCombatOwnerDraft(
            session, CampaignActionAudience.Axis, "axis-infantry").PrivateCombatActionIds[0];
        Assert.DoesNotContain(privateAction, visible, StringComparison.Ordinal);
        var actionRef = opening.PublicActionRefs[0];
        Assert.False(journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, "act.forged").IsAccepted);
        var acceptedCombat = journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, actionRef);
        Assert.True(acceptedCombat.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, actionRef);
        Assert.True(retry.IsAccepted);
        Assert.Equal(acceptedCombat.CanonicalReceipt, retry.CanonicalReceipt);
        var verified = reopened.ReadVerified(request.CampaignId);
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion + 2,
            CampaignExercises.QueryCheckpoint(verified).StateVersion);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified, reconstruction.FailureReason.ToString());
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(verified).ContractVersion);
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(verified,
            CampaignActionAudience.Commonwealth);
        var defenderCredential = reopened.RedeemSeatGrant(reopened.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var defenderOpening = reopened.QueryRbaOwnerOpening(defenderCredential,
            defenderId);
        Assert.Contains("decline-retreat-before-assault",
            System.Text.Encoding.UTF8.GetString(defenderOpening.VisibleBytes),
            StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = reopened.QueryRbaOwnerOpening(credential, defenderId);
        });
        var declineRef = defenderOpening.PublicActionRefs[^1];
        var acceptedDecline = reopened.SubmitOwnerRbaDecline(defenderCredential,
            defenderId, defenderOpening.Revision, declineRef);
        Assert.True(acceptedDecline.IsAccepted);
        var coldDecline = fixture.Open();
        var declineRetry = coldDecline.SubmitOwnerRbaDecline(defenderCredential,
            defenderId, defenderOpening.Revision, declineRef);
        Assert.True(declineRetry.IsAccepted);
        Assert.Equal(acceptedDecline.CanonicalReceipt, declineRetry.CanonicalReceipt);
        var afterDecline = coldDecline.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(afterDecline).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterDecline).IsVerified);
        Assert.Throws<InvalidOperationException>(() =>
            coldDecline.QueryForceOwnerOpening(credential, "axis-infantry"));
        Assert.False(coldDecline.SubmitOwnerForceSeal(credential, "axis-infantry", 1,
            "act." + new string('0', 64)).IsAccepted);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps "
            + "WHERE campaign_id=$id AND action_kind='native-combat-barrage'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1L, query.ExecuteScalar());
    }

    [Fact]
    public void BarrageInsertFailureRollsBackOwnerSelectionAndColdRetryCommitsOnce()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        var session = AdvanceToContactCombat(journal, request, Timing(request));
        var credential = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(credential, "axis-infantry");
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.Parameters.AddWithValue("$id", request.CampaignId);
        query.Parameters.AddWithValue("$ref", opening.PublicActionRefs[0]);
        query.CommandText = "SELECT head_sequence || ':' || head_hash FROM campaigns WHERE campaign_id=$id";
        var originalHead = query.ExecuteScalar();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        var originalCount = (long)query.ExecuteScalar()!;
        var originalCheckpoint = CampaignExercises.Reconstruct(session).ReconstructedSnapshotBytes;
        query.CommandText = """
            CREATE TRIGGER fail_barrage BEFORE INSERT ON accepted_steps
            WHEN NEW.action_kind='native-combat-barrage'
            BEGIN SELECT RAISE(ABORT, 'injected barrage insertion failure'); END
            """;
        query.ExecuteNonQuery();
        var failure = Assert.Throws<SqliteException>(() =>
            journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
                opening.Revision, opening.PublicActionRefs[0]));
        Assert.Equal(19, failure.SqliteErrorCode);
        Assert.Equal(1811, failure.SqliteExtendedErrorCode);
        Assert.Contains("injected barrage insertion failure", failure.Message,
            StringComparison.Ordinal);
        query.CommandText = "SELECT head_sequence || ':' || head_hash FROM campaigns WHERE campaign_id=$id";
        Assert.Equal(originalHead, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        Assert.Equal(originalCount, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id AND owner_public_ref=$ref";
        Assert.Equal(0L, query.ExecuteScalar());
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion,
            CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(request.CampaignId)).StateVersion);
        Assert.Equal(originalCheckpoint, CampaignExercises.Reconstruct(
            fixture.Open().ReadVerified(request.CampaignId)).ReconstructedSnapshotBytes);
        query.CommandText = "DROP TRIGGER fail_barrage";
        query.ExecuteNonQuery();
        var accepted = fixture.Open().SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        query.CommandText = "SELECT head_sequence || ':' || head_hash FROM campaigns WHERE campaign_id=$id";
        var acceptedHead = query.ExecuteScalar();
        var duplicate = fixture.Open().SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(duplicate.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, duplicate.CanonicalReceipt);
        Assert.Equal(acceptedHead, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id AND owner_public_ref=$ref";
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        Assert.Equal(originalCount + 2, (long)query.ExecuteScalar()!);
        Assert.Equal(1L, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-combat-barrage"));
        Assert.True(CampaignExercises.Reconstruct(
            fixture.Open().ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void CompletedInfantryForceRoundAdvancesAntiArmorAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-anti-armor'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1L, query.ExecuteScalar());
    }

    [Fact]
    public void SourceBackedTenToeForceRoundRetainsResultAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-assault-result'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-reserve-release-completion'";
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-no-post-assault-retreat'";
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-world-settlement'";
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-no-custody'";
        Assert.Equal(1L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-successor'";
        Assert.Equal(1L, query.ExecuteScalar());
    }

    [Fact]
    public void SourceBackedReserveReleaseChoiceRetainsOwnerReceiptAfterRestart()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true,
                reserveElementId: "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(
            journal.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.Throws<InvalidOperationException>(() =>
            journal.QueryReserveReleaseOwnerOpening(defender, "axis-infantry"));
        var opening = journal.QueryReserveReleaseOwnerOpening(attacker,
            "axis-infantry");
        Assert.Equal(2, opening.PublicActionRefs.Count);
        using (var visible = System.Text.Json.JsonDocument.Parse(opening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            Assert.False(visible.RootElement.TryGetProperty("worldHash", out _));
            Assert.Equal("axis-infantry",
                visible.RootElement.GetProperty("elementId").GetString());
        }
        Assert.False(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry",
            opening.Revision, "act." + new string('0', 64)).IsAccepted);
        var accepted = journal.SubmitOwnerReserveRelease(attacker, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        var cold = fixture.Open(clock);
        var retained = cold.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(retained).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(retained).IsVerified);
        var retry = cold.SubmitOwnerReserveRelease(attacker, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Throws<InvalidOperationException>(() =>
            cold.QueryCycleControlOwnerOpening(defender));
        var controlOpening = cold.QueryCycleControlOwnerOpening(attacker);
        Assert.NotEmpty(controlOpening.PublicActionRefs);
        using (var visible = System.Text.Json.JsonDocument.Parse(
                   controlOpening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("worldHash", out _));
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            Assert.Equal("Finish", visible.RootElement.GetProperty("choices")
                .EnumerateArray().Last().GetString());
        }
        Assert.False(cold.SubmitOwnerCycleControl(attacker,
            controlOpening.Revision, "act." + new string('0', 64)).IsAccepted);
        var finished = cold.SubmitOwnerCycleControl(attacker,
            controlOpening.Revision, controlOpening.PublicActionRefs[^1]);
        Assert.True(finished.IsAccepted);
        var afterControl = fixture.Open(clock);
        var controlHead = afterControl.ReadVerified(request.CampaignId);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(controlHead).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(controlHead).IsVerified);
        var controlRetry = afterControl.SubmitOwnerCycleControl(attacker,
            controlOpening.Revision, controlOpening.PublicActionRefs[^1]);
        Assert.True(controlRetry.IsAccepted);
        Assert.Equal(finished.CanonicalReceipt, controlRetry.CanonicalReceipt);
    }

    [Fact]
    public void ReleasedMemberCanRepeatIntoSameSideMovementAfterColdRestart()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true,
                reserveElementId: "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var release = journal.QueryReserveReleaseOwnerOpening(attacker,
            "axis-infantry");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var opening = journal.QueryCycleControlOwnerOpening(attacker);
        using (var visible = System.Text.Json.JsonDocument.Parse(opening.VisibleBytes))
            Assert.Equal("Repeat", visible.RootElement.GetProperty("choices")
                .EnumerateArray().First().GetString());
        var repeated = journal.SubmitOwnerCycleControl(attacker,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(repeated.IsAccepted);
        var cold = fixture.Open(clock);
        var next = cold.ReadVerified(request.CampaignId);
        var checkpoint = CampaignExercises.QueryCheckpoint(next);
        Assert.Equal(28, checkpoint.ContractVersion);
        Assert.Contains("movement", checkpoint.PositionId, StringComparison.Ordinal);
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var retry = cold.SubmitOwnerCycleControl(attacker,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(repeated.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Throws<InvalidOperationException>(() =>
            cold.QueryRepeatMovementOwnerOpening(defender));
        var movementOpening = cold.QueryRepeatMovementOwnerOpening(attacker);
        Assert.NotEmpty(movementOpening.PublicActionRefs);
        Assert.False(cold.SubmitOwnerRepeatMovement(attacker,
            movementOpening.Revision,
            "act." + new string('0', 64)).IsAccepted);
        var moved = cold.SubmitOwnerRepeatMovement(attacker,
            movementOpening.Revision, movementOpening.PublicActionRefs[0]);
        Assert.True(moved.IsAccepted);
        var afterMove = fixture.Open(clock);
        var movedSession = afterMove.ReadVerified(request.CampaignId);
        var afterBreakdown = CampaignExercises.QueryCheckpoint(movedSession);
        Assert.Equal(30, afterBreakdown.ContractVersion);
        Assert.Contains("combat.position-determination", afterBreakdown.PositionId,
            StringComparison.Ordinal);
        Assert.True(CampaignExercises.Reconstruct(movedSession).IsVerified);
        var movedRetry = afterMove.SubmitOwnerRepeatMovement(attacker,
            movementOpening.Revision, movementOpening.PublicActionRefs[0]);
        Assert.True(movedRetry.IsAccepted);
        Assert.Equal(moved.CanonicalReceipt, movedRetry.CanonicalReceipt);
        const string repeatActor = "axis-infantry";
        Assert.Throws<InvalidOperationException>(() =>
            afterMove.QueryRepeatCombatOwnerOpening(defender, repeatActor));
        var combatOpening = afterMove.QueryRepeatCombatOwnerOpening(
            attacker, repeatActor);
        Assert.Contains("finish-combat-without-attack",
            System.Text.Encoding.UTF8.GetString(combatOpening.VisibleBytes),
            StringComparison.Ordinal);
        Assert.False(afterMove.SubmitOwnerRepeatCombat(attacker, repeatActor,
            combatOpening.Revision, "act." + new string('0', 64)).IsAccepted);
        var combatDecision = afterMove.SubmitOwnerRepeatCombat(attacker,
            repeatActor, combatOpening.Revision,
            combatOpening.PublicActionRefs[^1]);
        Assert.True(combatDecision.IsAccepted);
        var afterCombat = fixture.Open(clock);
        var combatSession = afterCombat.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(combatSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(combatSession).IsVerified);
        var combatRetry = afterCombat.SubmitOwnerRepeatCombat(attacker,
            repeatActor, combatOpening.Revision,
            combatOpening.PublicActionRefs[^1]);
        Assert.True(combatRetry.IsAccepted);
        Assert.Equal(combatDecision.CanonicalReceipt, combatRetry.CanonicalReceipt);
        var nextCycleOpening = afterCombat.QueryCycleControlOwnerOpening(attacker);
        Assert.Contains("Finish", System.Text.Encoding.UTF8.GetString(
            nextCycleOpening.VisibleBytes), StringComparison.Ordinal);
        var nextCycle = afterCombat.SubmitOwnerCycleControl(attacker,
            nextCycleOpening.Revision, nextCycleOpening.PublicActionRefs[^1]);
        Assert.True(nextCycle.IsAccepted);
        var afterNextCycle = fixture.Open(clock);
        var nextCycleSession = afterNextCycle.ReadVerified(request.CampaignId);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(
            nextCycleSession).ContractVersion);
        Assert.Contains("truck", CampaignExercises.QueryCheckpoint(
            nextCycleSession).PositionId, StringComparison.Ordinal);
        Assert.True(CampaignExercises.Reconstruct(nextCycleSession).IsVerified);
    }

    [Fact]
    public async Task SecondContactAssaultEntersBarrageAndColdReplays()
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
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.Submit(attacker, "axis-infantry-second",
            "repeat-combat-selection", "repeat-combat-selection", opening,
            opening.PublicActionRefs[west]);
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
        // 020D: the Host RBA route is decline-only (CMB-POL-001). One ref, no move offers, and the
        // retired rba-move action stage is refused before reaching the grain.
        var rba = cold.QueryRbaOwnerOpening(defender, westDefender);
        Assert.Single(rba.PublicActionRefs);
        Assert.DoesNotContain("moveOffers", System.Text.Encoding.UTF8.GetString(rba.VisibleBytes),
            StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.BadRequest, await outward.SubmitStatus(defender, westDefender,
            "rba-move", rba.Revision, rba.PublicActionRefs[0]));
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(fixture.Open(clock)
            .ReadVerified(request.CampaignId)).ContractVersion);
        await outward.Submit(defender, westDefender,
            "retreat-before-assault", "rba-decline", rba,
            rba.PublicActionRefs[^1]);
        var afterDecline = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(afterDecline).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterDecline).IsVerified);
        var secondAttackerForce = cold.QueryForceOwnerOpening(attacker, "axis-infantry-second");
        var secondDefenderForce = cold.QueryForceOwnerOpening(defender, westDefender);
        clock.Advance(TimeSpan.FromSeconds(1));
        await outward.Submit(attacker, "axis-infantry-second",
            "force-assignment", "force-assignment", secondAttackerForce,
            secondAttackerForce.PublicActionRefs[0]);
        clock.Advance(TimeSpan.FromSeconds(1));
        await outward.Submit(defender, westDefender,
            "force-assignment", "force-assignment", secondDefenderForce,
            secondDefenderForce.PublicActionRefs[0]);
        var afterForce = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(afterForce).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterForce).IsVerified);
        var nextCycle = cold.QueryCycleControlOwnerOpening(attacker);
        Assert.Single(nextCycle.PublicActionRefs);
        await outward.Submit(attacker, nextCycle.ActingElementId,
            "cycle-control", "cycle-control", nextCycle,
            nextCycle.PublicActionRefs[^1]);
        var afterFinish = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(afterFinish).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterFinish).IsVerified);
    }

    [Theory]
    [InlineData(true, -1)]
    [InlineData(false, -1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public async Task SecondAssaultCanRepeatIntoThirdContactCombatAfterColdRestart(
        bool thirdAssault, int sealsBeforeTimeout)
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
        await using var outward = await OwnerHttpHarness.Start(journal,
            request.CampaignId);
        await outward.Submit(attacker, "axis-infantry-third",
            "repeat-combat-selection", "repeat-combat-selection", thirdCombat,
            thirdAssault ? thirdCombat.PublicActionRefs[farNorthWest]
                : thirdCombat.PublicActionRefs[^1]);
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        if (thirdAssault)
        {
            Assert.Equal(14, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
            var thirdDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
                cold, CampaignActionAudience.Commonwealth);
            var thirdRba = journal.QueryRbaOwnerOpening(defender, thirdDefender);
            Assert.True(journal.SubmitOwnerRbaDecline(defender, thirdDefender,
                thirdRba.Revision, thirdRba.PublicActionRefs[^1]).IsAccepted);
            var thirdAttackerForce = journal.QueryForceOwnerOpening(attacker,
                "axis-infantry-third");
            var thirdDefenderForce = journal.QueryForceOwnerOpening(defender,
                thirdDefender);
            var resultCount = CountAcceptedStep(fixture.Path, request.CampaignId,
                "native-combat-assault-result");
            if (sealsBeforeTimeout != 0)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
                Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-third",
                    thirdAttackerForce.Revision,
                    thirdAttackerForce.PublicActionRefs[0]).IsAccepted);
            }
            if (sealsBeforeTimeout < 0)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
                Assert.True(journal.SubmitOwnerForceSeal(defender, thirdDefender,
                    thirdDefenderForce.Revision,
                    thirdDefenderForce.PublicActionRefs[0]).IsAccepted);
            }
            else
            {
                clock.Advance(TimeSpan.FromSeconds(30));
                Assert.True(fixture.Open(clock).TickForceRound(request.CampaignId));
                var cancelled = CampaignExercises.Reconstruct(
                    fixture.Open(clock).ReadVerified(request.CampaignId)).ReconstructedSnapshotBytes;
                Assert.False(fixture.Open(clock).SubmitOwnerForceSeal(defender,
                    thirdDefender, thirdDefenderForce.Revision,
                    thirdDefenderForce.PublicActionRefs[0]).IsAccepted);
                if (sealsBeforeTimeout == 0)
                    Assert.False(fixture.Open(clock).SubmitOwnerForceSeal(attacker,
                        "axis-infantry-third", thirdAttackerForce.Revision,
                        thirdAttackerForce.PublicActionRefs[0]).IsAccepted);
                Assert.Equal(cancelled, CampaignExercises.Reconstruct(
                    fixture.Open(clock).ReadVerified(request.CampaignId)).ReconstructedSnapshotBytes);
                Assert.Equal(resultCount, CountAcceptedStep(fixture.Path, request.CampaignId,
                    "native-combat-assault-result"));
                Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
                    "native-combat-cancelled-assault"));
                using var connection = new SqliteConnection($"Data Source={fixture.Path}");
                connection.Open();
                using var query = connection.CreateCommand();
                query.CommandText = "SELECT canonical_event FROM accepted_steps "
                    + "WHERE campaign_id=$id AND action_kind='native-repeat-breakdown' "
                    + "ORDER BY sequence DESC LIMIT 1";
                query.Parameters.AddWithValue("$id", request.CampaignId);
                using var before = JsonDocument.Parse((byte[])query.ExecuteScalar()!);
                query.CommandText = "SELECT snapshot_checkpoint FROM accepted_steps "
                    + "WHERE campaign_id=$id ORDER BY sequence DESC LIMIT 1";
                using var after = JsonDocument.Parse((byte[])query.ExecuteScalar()!);
                // World includes ammunition: cancellation must retain the pre-assault World.
                Assert.Equal(before.RootElement.GetProperty("worldHash").GetString(),
                    after.RootElement.GetProperty("worldHash").GetString());
                Assert.True(after.RootElement.GetProperty("hasRetainedCombatRandomCursor")
                    .GetBoolean());
            }
            cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        }
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        control = fixture.Open(clock).QueryCycleControlOwnerOpening(attacker);
        Assert.Single(control.PublicActionRefs);
        Assert.True(control.Revision <= CampaignExercises.QueryCheckpoint(cold).StateVersion,
            $"opening={control.Revision} checkpoint={CampaignExercises.QueryCheckpoint(cold).StateVersion}");
        Assert.True(fixture.Open(clock).SubmitOwnerCycleControl(attacker,
            control.Revision, control.PublicActionRefs[^1]).IsAccepted);
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
    }

    [Fact]
    public void TimedOutSecondAssaultReturnsToReserveReleaseWithoutResult()
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
        var combat = journal.QueryRepeatCombatOwnerOpening(attacker,
            "axis-infantry-second");
        using var visible = JsonDocument.Parse(combat.VisibleBytes);
        var offers = visible.RootElement.GetProperty("assaultOffers");
        var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
            offers[index].GetProperty("apparentLocationId").GetString() == "west");
        Assert.True(journal.SubmitOwnerRepeatCombat(attacker,
            "axis-infantry-second", combat.Revision,
            combat.PublicActionRefs[west]).IsAccepted);
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        _ = journal.QueryForceOwnerOpening(attacker, "axis-infantry-second");
        var resultCount = CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-combat-assault-result");
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickForceRound(request.CampaignId));
        var cold = fixture.Open(clock);
        var after = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(resultCount, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-combat-assault-result"));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-combat-cancelled-assault"));
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(after).ContractVersion);
        var nextControl = cold.QueryCycleControlOwnerOpening(attacker);
        Assert.True(cold.SubmitOwnerCycleControl(attacker,
            nextControl.Revision, nextControl.PublicActionRefs[^1]).IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(
            fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void PostResultDefenderRetreatUsesSeatOpeningAndColdReplay()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 6);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(
            fixture.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
        Assert.Throws<InvalidOperationException>(() =>
            journal.QueryPostAssaultRetreatOwnerOpening(attacker, defenderId));
        var opening = journal.QueryPostAssaultRetreatOwnerOpening(defender, defenderId);
        Assert.NotEmpty(opening.PublicActionRefs);
        using (var visible = System.Text.Json.JsonDocument.Parse(opening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            Assert.False(visible.RootElement.GetProperty("moveOffers")[0]
                .TryGetProperty("actionId", out _));
        }
        Assert.False(journal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
            opening.Revision, "act." + new string('0', 64)).IsAccepted);
        var first = journal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(first.IsAccepted);
        var cold = fixture.Open(clock);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(
            cold.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(
            cold.ReadVerified(request.CampaignId)).IsVerified);
        var retry = cold.SubmitOwnerPostAssaultRetreat(defender, defenderId,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(first.CanonicalReceipt, retry.CanonicalReceipt);
    }

    [Fact]
    public void CaptorCustodyUsesSideSafeOpeningAndColdReceipt()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 5);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var session = journal.ReadVerified(request.CampaignId);
        if (CampaignExercises.QueryCheckpoint(session).ContractVersion == 22)
        {
            var defenderOpening = journal.QueryPostAssaultRetreatOwnerOpening(
                defender, defenderId);
            Assert.True(journal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
                defenderOpening.Revision, defenderOpening.PublicActionRefs[0]).IsAccepted);
            session = journal.ReadVerified(request.CampaignId);
        }
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(session));
        CampaignSeatCredential captor;
        string captorId;
        CampaignOwnerOpening opening;
        try
        {
            opening = journal.QueryCustodyOwnerOpening(attacker, "axis-infantry");
            captor = attacker;
            captorId = "axis-infantry";
            Assert.Throws<InvalidOperationException>(() =>
                journal.QueryCustodyOwnerOpening(defender, captorId));
        }
        catch (InvalidOperationException)
        {
            opening = journal.QueryCustodyOwnerOpening(defender, defenderId);
            captor = defender;
            captorId = defenderId;
            Assert.Throws<InvalidOperationException>(() =>
                journal.QueryCustodyOwnerOpening(attacker, captorId));
        }
        Assert.NotEmpty(opening.PublicActionRefs);
        var coldOpening = fixture.Open(clock).QueryCustodyOwnerOpening(
            captor, captorId);
        Assert.Equal(opening.VisibleBytes, coldOpening.VisibleBytes);
        Assert.Equal(opening.PublicActionRefs, coldOpening.PublicActionRefs);
        Assert.Equal(opening.Revision, coldOpening.Revision);
        using (var visible = System.Text.Json.JsonDocument.Parse(opening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            Assert.False(visible.RootElement.TryGetProperty("settlementHash", out _));
            Assert.False(visible.RootElement.TryGetProperty("victimElementId", out _));
            Assert.False(visible.RootElement.GetProperty("offers")[0]
                .TryGetProperty("actionId", out _));
        }
        Assert.False(journal.SubmitOwnerCustody(captor, captorId,
            opening.Revision, "act." + new string('0', 64)).IsAccepted);
        var accepted = journal.SubmitOwnerCustody(captor, captorId,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(accepted.IsAccepted);
        var cold = fixture.Open(clock);
        var retained = cold.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(retained).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(retained).IsVerified);
        var retry = cold.SubmitOwnerCustody(captor, captorId,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
    }

    [Fact]
    public void ForceRoundSharesDeadlineAndCancelsOneSealAtEqualityAfterColdReadback()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT COUNT(*) FROM force_rounds WHERE campaign_id=$id";
            query.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1L, query.ExecuteScalar());
        }
        clock.Advance(TimeSpan.FromSeconds(1));
        var first = journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]);
        Assert.True(first.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(29));
        var expired = journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]);
        Assert.False(expired.IsAccepted);
        var cold = fixture.Open(clock);
        var after = cold.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(after).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Throws<InvalidOperationException>(() =>
            cold.QueryForceOwnerOpening(defender, defenderId));
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var seals = connection.CreateCommand();
            seals.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
                + "AND action_kind='contact-force-seal'";
            seals.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1L, seals.ExecuteScalar());
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE force_rounds SET high_water_ms=high_water_ms+1 "
                + "WHERE campaign_id=$id";
            update.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => cold.ReadVerified(request.CampaignId));
    }

    [Theory]
    [InlineData("forged-reference")]
    [InlineData("stale-revision")]
    [InlineData("foreign-element")]
    [InlineData("foreign-reference")]
    public void InvalidForceProposalAtDeadlineCannotCancelCurrentRound(string invalidKind)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true);
        clock.Advance(TimeSpan.FromSeconds(1));
        var first = journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]);
        Assert.True(first.IsAccepted);
        var before = StoredAuthority();
        clock.Advance(TimeSpan.FromSeconds(29));
        var cold = fixture.Open(clock);
        var rejected = cold.SubmitOwnerForceSeal(defender,
            invalidKind == "foreign-element" ? "axis-infantry" : defenderId,
            invalidKind == "stale-revision" ? defenderForce.Revision + 1 : defenderForce.Revision,
            invalidKind switch
            {
                "forged-reference" => "act." + new string('0', 64),
                "foreign-reference" => attackerForce.PublicActionRefs[0],
                _ => defenderForce.PublicActionRefs[0],
            });
        Assert.False(rejected.IsAccepted);
        Assert.Equal(before, StoredAuthority());
        Assert.True(CampaignExercises.Reconstruct(
            fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);

        var retry = cold.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(first.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(before, StoredAuthority());

        // The genuine current offer still triggers the existing System deadline fallback.
        Assert.False(cold.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var after = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(after).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            "contact-force-seal"));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-combat-cancelled-assault"));
        var cancelled = StoredAuthority();
        var retained = fixture.Open(clock).SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]);
        Assert.True(retained.IsAccepted);
        Assert.Equal(first.CanonicalReceipt, retained.CanonicalReceipt);
        Assert.Equal(cancelled, StoredAuthority());

        string StoredAuthority()
        {
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            var tables = new List<List<string[]>>();
            foreach (var table in new[] { "campaigns", "accepted_steps", "force_rounds",
                "owner_openings", "owner_actions", "seats" })
            {
                using var query = connection.CreateCommand();
                query.CommandText = $"SELECT * FROM {table} WHERE campaign_id=$id ORDER BY rowid";
                query.Parameters.AddWithValue("$id", request.CampaignId);
                using var reader = query.ExecuteReader();
                var rows = new List<string[]>();
                while (reader.Read())
                    rows.Add(Enumerable.Range(0, reader.FieldCount).Select(index =>
                        reader.GetValue(index) is byte[] bytes ? Convert.ToHexString(bytes)
                            : Convert.ToString(reader.GetValue(index),
                                global::System.Globalization.CultureInfo.InvariantCulture)!).ToArray());
                tables.Add(rows);
            }
            return JsonSerializer.Serialize(tables);
        }
    }

    [Fact]
    public void ForceRoundTickCancelsWithoutFabricatingASealWhenClockUnavailable()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true);
        clock.IsAvailable = false;
        Assert.True(journal.TickForceRound(request.CampaignId));
        Assert.False(journal.TickForceRound(request.CampaignId));
        var cold = fixture.Open(clock);
        var after = cold.ReadVerified(request.CampaignId);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(after).ContractVersion);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-cycle-control-timeout"));
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='contact-force-seal'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(0L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
            + "AND action_kind='native-combat-cancelled-assault'";
        Assert.Equal(1L, query.ExecuteScalar());
    }

    [Fact]
    public void ForceRoundTickCancelsWhenTrustedClockRegressesBelowHighWater()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true);
        clock.Advance(TimeSpan.FromSeconds(5));
        _ = journal.QueryForceOwnerOpening(attacker, "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(-1));
        Assert.True(journal.TickForceRound(request.CampaignId));
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
    }

    [Fact]
    public void DefenderRbaOpeningIsDeclineOnlyAndExplicitStopColdReplaysThroughOwnerSeat()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
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
        // 020D: Core still derives move offers for this defender, but the Host opening publishes
        // only the decline-only codec and maps exactly one ref.
        var startingSession = journal.ReadVerified(request.CampaignId);
        Assert.True(CampaignHostAdapter.QueryContactRbaOwnerDraft(startingSession,
            CampaignActionAudience.Commonwealth, defenderId).PrivateActionIds.Count > 1);
        var declineDraft = CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(startingSession,
            CampaignActionAudience.Commonwealth, defenderId);
        Assert.Equal(declineDraft.VisibleBytes, opening.VisibleBytes);
        Assert.Single(opening.PublicActionRefs);
        Assert.Single(declineDraft.PrivateActionIds);
        Assert.DoesNotContain("moveOffers", System.Text.Encoding.UTF8.GetString(opening.VisibleBytes),
            StringComparison.Ordinal);
        Assert.False(journal.SubmitOwnerRbaDecline(defender, defenderId,
            opening.Revision, "act." + new string('0', 64)).IsAccepted);
        var stopped = journal.SubmitOwnerRbaDecline(defender, defenderId,
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(stopped.IsAccepted);
        var cold = fixture.Open();
        Assert.Equal(stopped.CanonicalReceipt, cold.SubmitOwnerRbaDecline(defender, defenderId,
            opening.Revision, opening.PublicActionRefs[0]).CanonicalReceipt);
        var final = fixture.Open().ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(final).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(final).IsVerified);
        var control = cold.QueryCycleControlOwnerOpening(attacker);
        Assert.Single(control.PublicActionRefs);
    }

    [Fact]
    public async Task VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        AdvanceToContactCombat(journal, request, Timing(request));
        var credential = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(credential, "axis-infantry");
        Assert.Equal(2, opening.PublicActionRefs.Count);
        Assert.Contains("finish-combat-without-attack",
            System.Text.Encoding.UTF8.GetString(opening.VisibleBytes), StringComparison.Ordinal);

        var finishRef = opening.PublicActionRefs[1];
        var accepted = journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, finishRef);
        Assert.True(accepted.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, finishRef);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        var verified = reopened.ReadVerified(request.CampaignId);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified);
        using var snapshot = System.Text.Json.JsonDocument.Parse(
            reconstruction.ReconstructedSnapshotBytes!);
        Assert.Equal(27, snapshot.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal(CampaignExercises.QueryCheckpoint(verified).PositionId,
            snapshot.RootElement.GetProperty("positionId").GetString());
        Assert.True(snapshot.RootElement.GetProperty("completed").GetBoolean());
        var control = reopened.QueryCycleControlOwnerOpening(credential);
        var finished = reopened.SubmitOwnerCycleControl(credential,
            control.Revision, control.PublicActionRefs[^1]);
        Assert.True(finished.IsAccepted);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(
            fixture.Open().ReadVerified(request.CampaignId)).ContractVersion);
        var opponent = reopened.RedeemSeatGrant(reopened.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        Assert.Throws<InvalidOperationException>(() => reopened.QueryTruckConvoyPassOwnerOpening(opponent));
        var convoy = reopened.QueryTruckConvoyPassOwnerOpening(credential);
        Assert.Single(convoy.PublicActionRefs);
        Assert.False(reopened.SubmitOwnerTruckConvoyPass(credential, convoy.Revision + 1,
            convoy.PublicActionRefs[0]).IsAccepted);
        var passed = reopened.SubmitOwnerTruckConvoyPass(credential, convoy.Revision, convoy.PublicActionRefs[0]);
        Assert.True(passed.IsAccepted);
        var cold = fixture.Open();
        var passRetry = cold.SubmitOwnerTruckConvoyPass(credential, convoy.Revision, convoy.PublicActionRefs[0]);
        Assert.True(passRetry.IsAccepted);
        Assert.Equal(passed.CanonicalReceipt, passRetry.CanonicalReceipt);
        var afterPass = cold.ReadVerified(request.CampaignId);
        Assert.Equal(41, CampaignExercises.QueryCheckpoint(afterPass).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterPass).IsVerified);
        CampaignOwnerOpening? firstPhase = null;
        CampaignOwnerSubmissionResult? firstPhaseReceipt = null;
        for (var index = 0; index < 4; index++)
        {
            cold = fixture.Open();
            Assert.Throws<InvalidOperationException>(() => cold.QueryPostCombatPassOwnerOpening(opponent));
            var phase = cold.QueryPostCombatPassOwnerOpening(credential);
            var phaseResult = cold.SubmitOwnerPostCombatPass(credential, phase.Revision,
                Assert.Single(phase.PublicActionRefs));
            Assert.True(phaseResult.IsAccepted);
            firstPhase ??= phase;
            firstPhaseReceipt ??= phaseResult;
        }
        cold = fixture.Open();
        var laterRetry = cold.SubmitOwnerPostCombatPass(credential, firstPhase!.Revision, firstPhase.PublicActionRefs[0]);
        Assert.True(laterRetry.IsAccepted);
        Assert.Equal(firstPhaseReceipt!.CanonicalReceipt, laterRetry.CanonicalReceipt);
        var nextSide = cold.ReadVerified(request.CampaignId);
        Assert.Equal(42, CampaignExercises.QueryCheckpoint(nextSide).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(nextSide).IsVerified);
        Assert.Throws<InvalidOperationException>(() => cold.QueryPostCombatPassOwnerOpening(credential));
        Assert.Throws<InvalidOperationException>(() => cold.QuerySecondReserveOwnerOpening(credential));
        var secondReserve = cold.QuerySecondReserveOwnerOpening(opponent);
        using var secondVisible = System.Text.Json.JsonDocument.Parse(secondReserve.VisibleBytes);
        var secondChoices = secondVisible.RootElement.GetProperty("choices");
        Assert.True(secondChoices.GetArrayLength() > 1);
        var movementElement = secondChoices[0].GetProperty("elementId").GetString()!;
        using var movementCopy = IntegratedCampaignSave(fixture.Path, TimeProvider.System, []);
        Assert.DoesNotContain("axis-infantry", System.Text.Encoding.UTF8.GetString(secondReserve.VisibleBytes),
            StringComparison.Ordinal);
        var designated = cold.SubmitOwnerSecondReserve(opponent, secondReserve.Revision, secondReserve.PublicActionRefs[0]);
        Assert.True(designated.IsAccepted);
        var afterDesignation = fixture.Open();
        var designationRetry = afterDesignation.SubmitOwnerSecondReserve(opponent, secondReserve.Revision,
            secondReserve.PublicActionRefs[0]);
        Assert.True(designationRetry.IsAccepted);
        Assert.Equal(designated.CanonicalReceipt, designationRetry.CanonicalReceipt);
        var completeReserve = afterDesignation.QuerySecondReserveOwnerOpening(opponent);
        var completedReserve = afterDesignation.SubmitOwnerSecondReserve(opponent, completeReserve.Revision,
            completeReserve.PublicActionRefs[^1]);
        Assert.True(completedReserve.IsAccepted);
        var finalReserve = fixture.Open().ReadVerified(request.CampaignId);
        Assert.Equal(43, CampaignExercises.QueryCheckpoint(finalReserve).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(finalReserve).IsVerified);
        var child = await FreshProcessRunDeadlineChild(fixture.Path, request.CampaignId,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Assert.True(child.IsVerified);
        Assert.NotEqual(Environment.ProcessId, child.ProcessId);
        Assert.Equal(43, child.ContractVersion);
        Assert.Equal(CampaignExercises.Reconstruct(finalReserve).EventStreamHash, child.EventStreamHash);

        var completionJournal = fixture.Open();
        Assert.Throws<InvalidOperationException>(() => completionJournal.QuerySecondMovementCompletionOwnerOpening(credential));
        var movementCompletion = completionJournal.QuerySecondMovementCompletionOwnerOpening(opponent);
        Assert.Contains("complete-movement", Encoding.UTF8.GetString(movementCompletion.VisibleBytes), StringComparison.Ordinal);
        var completionStore = CampaignDurableAuthorityJournalStore.ForSingleCampaign(completionJournal);
        var completionSilo = await StartOutwardApiSiloAsync(completionStore);
        var completionBuilder = WebApplication.CreateBuilder();
        completionBuilder.Environment.EnvironmentName = "Development";
        completionBuilder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        completionBuilder.Services.AddSingleton(completionStore);
        completionBuilder.Services.AddSingleton(completionSilo.Services.GetRequiredService<IGrainFactory>());
        await using (var completionApp = completionBuilder.Build())
        {
            completionApp.MapDurableCampaignApi();
            await completionApp.StartAsync(TestContext.Current.CancellationToken);
            try
            {
                var address = completionApp.Services.GetRequiredService<IServer>().Features
                    .Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address) };
                // Not a same-origin-gate test; simulate a genuine same-origin fetch so the
                // v2 middleware's stricter header-presence check (#319) doesn't block it.
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                object Seat(CampaignSeatCredential value) => new
                {
                    campaignId = value.CampaignId,
                    audience = value.Audience == CampaignActionAudience.Axis ? "axis" : "commonwealth",
                    epoch = value.Epoch,
                    secret = value.Secret,
                    expiresUnixMs = value.ExpiresUnixMs,
                };
                StringContent OpeningFor(CampaignSeatCredential value) => new(JsonSerializer.Serialize(new
                {
                    contractVersion = 2,
                    credential = Seat(value),
                    stage = "second-movement-completion",
                    actingElementId = "land.second-movement-completion",
                }), Encoding.UTF8, "application/json");
                var path = $"/api/v2/campaigns/{request.CampaignId}/openings";
                var denied = await client.PostAsync(path, OpeningFor(credential), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
                var completionResponse = await client.PostAsync(path, OpeningFor(opponent), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, completionResponse.StatusCode);
                using var outward = JsonDocument.Parse(await completionResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
                Assert.Equal(movementCompletion.Revision, outward.RootElement.GetProperty("revision").GetInt64());
                Assert.Equal(movementCompletion.VisibleBytes,
                    Convert.FromBase64String(outward.RootElement.GetProperty("visibleCutBase64").GetString()!));
                var action = JsonSerializer.Serialize(new
                {
                    contractVersion = 2,
                    credential = Seat(opponent),
                    stage = "second-movement-completion",
                    actingElementId = "land.second-movement-completion",
                    openingRevision = movementCompletion.Revision,
                    publicActionRef = movementCompletion.PublicActionRefs[0],
                });
                var completionAccepted = await client.PostAsync($"/api/v2/campaigns/{request.CampaignId}/actions",
                    new StringContent(action, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, completionAccepted.StatusCode);
                var receipt = await completionAccepted.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.DoesNotContain("actionId", receipt, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("axis-infantry", receipt, StringComparison.Ordinal);
            }
            finally
            {
                await completionApp.StopAsync(TestContext.Current.CancellationToken);
            }
        }
        await StopOutwardApiSiloAsync(completionSilo);
        var completedMovement = completionJournal.SubmitOwnerSecondMovementCompletion(opponent,
            movementCompletion.Revision, Assert.Single(movementCompletion.PublicActionRefs));
        Assert.True(completedMovement.IsAccepted);
        var completedCold = fixture.Open();
        var completionRetry = completedCold.SubmitOwnerSecondMovementCompletion(opponent,
            movementCompletion.Revision, movementCompletion.PublicActionRefs[0]);
        Assert.True(completionRetry.IsAccepted);
        Assert.Equal(completedMovement.CanonicalReceipt, completionRetry.CanonicalReceipt);
        var completedHead = completedCold.ReadVerified(request.CampaignId);
        var completedCheckpoint = CampaignExercises.QueryCheckpoint(completedHead);
        Assert.Equal(46, completedCheckpoint.ContractVersion);
        Assert.DoesNotContain("breakdown-determination", completedCheckpoint.PositionId, StringComparison.Ordinal);
        Assert.True(CampaignExercises.Reconstruct(completedHead).IsVerified);
        Assert.Throws<InvalidOperationException>(() => completedCold.QuerySecondMovementCompletionOwnerOpening(opponent));
        var completionChild = await FreshProcessRunDeadlineChild(fixture.Path, request.CampaignId,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Assert.True(completionChild.IsVerified);
        Assert.Equal(46, completionChild.ContractVersion);
        Assert.Equal(CampaignExercises.Reconstruct(completedHead).EventStreamHash, completionChild.EventStreamHash);

        // Corrupt an intermediate retained predecessor in memory. Verifying the
        // head must replay the whole chain, not just the latest completion.
        const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var breakdownPredecessor = typeof(ExerciseSession).GetProperty("SecondBreakdownPredecessor", hidden)!;
        var completionPredecessor = typeof(ExerciseSession).GetProperty("SecondMovementCompletionPredecessor", hidden)!;
        var reservePredecessor = typeof(ExerciseSession).GetProperty("SecondReservePredecessor", hidden)!;
        var movementCompleteHead = (ExerciseSession)breakdownPredecessor.GetValue(completedHead)!;
        var reserveHead = (ExerciseSession)completionPredecessor.GetValue(movementCompleteHead)!;
        var intermediate = (ExerciseSession)reservePredecessor.GetValue(reserveHead)!;
        reservePredecessor.SetValue(intermediate, intermediate);
        Assert.False(CampaignExercises.Reconstruct(completedHead).IsVerified);

        var movementJournal = movementCopy.Open();
        var noReserve = movementJournal.QuerySecondReserveOwnerOpening(opponent);
        Assert.True(movementJournal.SubmitOwnerSecondReserve(opponent, noReserve.Revision,
            noReserve.PublicActionRefs[^1]).IsAccepted);
        var beforeMovement = movementJournal.ReadVerified(request.CampaignId);
        var movementCheckpoint = CampaignExercises.QueryCheckpoint(beforeMovement);
        using var unblockedCopy = IntegratedCampaignSave(movementCopy.Path, TimeProvider.System, []);
        using var noReserveVisible = JsonDocument.Parse(noReserve.VisibleBytes);
        string? unblockedElement = null;
        var unblockedIndex = -1;
        foreach (var choice in noReserveVisible.RootElement.GetProperty("choices").EnumerateArray())
        {
            if (choice.GetProperty("elementId").ValueKind == JsonValueKind.Null) continue;
            var id = choice.GetProperty("elementId").GetString()!;
            var draft = CampaignHostAdapter.QuerySecondMovementOwnerDraft(beforeMovement,
                CampaignActionAudience.Commonwealth, id);
            for (var index = 0; index < draft.PrivateActionIds.Count; index++)
            {
                var candidate = CampaignHostAdapter.SubmitSecondMovement(beforeMovement,
                    new CampaignActionSubmission(1, request.CampaignId, movementCheckpoint.StateVersion,
                        movementCheckpoint.PositionId, CampaignActionAudience.Commonwealth,
                        draft.PrivateActionIds[index]), id);
                if (!candidate.IsAccepted) continue;
                using var candidateEvent = JsonDocument.Parse(candidate.Evidence!.EventRecords[0]);
                if (candidateEvent.RootElement.GetProperty("reactionWindow").ValueKind != JsonValueKind.Null) continue;
                unblockedElement = id;
                unblockedIndex = index;
                break;
            }
            if (unblockedElement is not null) break;
        }
        Assert.NotNull(unblockedElement);
        var unblockedJournal = unblockedCopy.Open();
        var unblockedOpening = unblockedJournal.QuerySecondMovementOwnerOpening(opponent, unblockedElement);
        Assert.True(unblockedJournal.SubmitOwnerSecondMovement(opponent, unblockedElement,
            unblockedOpening.Revision, unblockedOpening.PublicActionRefs[unblockedIndex]).IsAccepted);
        var finishAfterMove = unblockedJournal.QuerySecondMovementCompletionOwnerOpening(opponent);
        Assert.True(unblockedJournal.SubmitOwnerSecondMovementCompletion(opponent, finishAfterMove.Revision,
            Assert.Single(finishAfterMove.PublicActionRefs)).IsAccepted);
        var afterUnblockedMove = unblockedCopy.Open().ReadVerified(request.CampaignId);
        Assert.Equal(46, CampaignExercises.QueryCheckpoint(afterUnblockedMove).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterUnblockedMove).IsVerified);
        var unblockedChild = await FreshProcessRunDeadlineChild(unblockedCopy.Path, request.CampaignId,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Assert.True(unblockedChild.IsVerified);
        Assert.Equal(46, unblockedChild.ContractVersion);
        Assert.Equal(CampaignExercises.Reconstruct(afterUnblockedMove).EventStreamHash, unblockedChild.EventStreamHash);
        var movementDraft = CampaignHostAdapter.QuerySecondMovementOwnerDraft(beforeMovement,
            CampaignActionAudience.Commonwealth, movementElement);
        var triggeredIndex = Enumerable.Range(0, movementDraft.PrivateActionIds.Count).FirstOrDefault(index =>
        {
            var step = CampaignHostAdapter.SubmitSecondMovement(beforeMovement,
                new CampaignActionSubmission(1, request.CampaignId, movementCheckpoint.StateVersion,
                    movementCheckpoint.PositionId, CampaignActionAudience.Commonwealth,
                    movementDraft.PrivateActionIds[index]), movementElement);
            if (!step.IsAccepted) return false;
            using var candidate = System.Text.Json.JsonDocument.Parse(step.Evidence!.EventRecords[0]);
            return candidate.RootElement.GetProperty("reactionWindow").ValueKind != System.Text.Json.JsonValueKind.Null;
        }, -1);
        Assert.True(triggeredIndex >= 0);
        Assert.Throws<InvalidOperationException>(() => movementJournal.QuerySecondMovementOwnerOpening(credential, movementElement));
        var moveOpening = movementJournal.QuerySecondMovementOwnerOpening(opponent, movementElement);
        Assert.NotEmpty(moveOpening.PublicActionRefs);
        Assert.Contains("move-and-stop", System.Text.Encoding.UTF8.GetString(moveOpening.VisibleBytes), StringComparison.Ordinal);
        var movementStore = CampaignDurableAuthorityJournalStore.ForSingleCampaign(movementJournal);
        var movementSilo = await StartOutwardApiSiloAsync(movementStore);
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(movementStore);
        builder.Services.AddSingleton(movementSilo.Services.GetRequiredService<IGrainFactory>());
        await using (var app = builder.Build())
        {
            app.MapDurableCampaignApi();
            await app.StartAsync(TestContext.Current.CancellationToken);
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features
                    .Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address) };
                // Not a same-origin-gate test; simulate a genuine same-origin fetch so the
                // v2 middleware's stricter header-presence check (#319) doesn't block it.
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                object Seat(CampaignSeatCredential value) => new
                {
                    campaignId = value.CampaignId,
                    audience = value.Audience == CampaignActionAudience.Axis ? "axis" : "commonwealth",
                    epoch = value.Epoch,
                    secret = value.Secret,
                    expiresUnixMs = value.ExpiresUnixMs,
                };
                StringContent BodyFor(CampaignSeatCredential value) => new(JsonSerializer.Serialize(new
                {
                    contractVersion = 2,
                    credential = Seat(value),
                    stage = "second-movement",
                    actingElementId = movementElement,
                }), Encoding.UTF8, "application/json");
                var path = $"/api/v2/campaigns/{request.CampaignId}/openings";
                var denied = await client.PostAsync(path, BodyFor(credential), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
                var response = await client.PostAsync(path, BodyFor(opponent), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var outward = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
                var visibleBytes = Convert.FromBase64String(outward.RootElement.GetProperty("visibleCutBase64").GetString()!);
                Assert.Equal(moveOpening.VisibleBytes, visibleBytes);
                Assert.DoesNotContain("axis-infantry", Encoding.UTF8.GetString(visibleBytes), StringComparison.Ordinal);
                Assert.Equal(moveOpening.Revision, outward.RootElement.GetProperty("revision").GetInt64());
                var action = JsonSerializer.Serialize(new
                {
                    contractVersion = 2,
                    credential = Seat(opponent),
                    stage = "second-movement",
                    actingElementId = movementElement,
                    openingRevision = moveOpening.Revision,
                    publicActionRef = moveOpening.PublicActionRefs[triggeredIndex],
                });
                var acceptedMove = await client.PostAsync($"/api/v2/campaigns/{request.CampaignId}/actions",
                    new StringContent(action, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, acceptedMove.StatusCode);
                var receipt = await acceptedMove.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.DoesNotContain("axis-infantry", receipt, StringComparison.Ordinal);
                Assert.DoesNotContain("actionId", receipt, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await app.StopAsync(TestContext.Current.CancellationToken);
            }
        }
        await StopOutwardApiSiloAsync(movementSilo);
        var moved = movementJournal.SubmitOwnerSecondMovement(opponent, movementElement,
            moveOpening.Revision, moveOpening.PublicActionRefs[triggeredIndex]);
        Assert.True(moved.IsAccepted);
        var movementCold = movementCopy.Open();
        var movementRetry = movementCold.SubmitOwnerSecondMovement(opponent, movementElement,
            moveOpening.Revision, moveOpening.PublicActionRefs[triggeredIndex]);
        Assert.True(movementRetry.IsAccepted);
        Assert.Equal(moved.CanonicalReceipt, movementRetry.CanonicalReceipt);
        var movementHead = movementCold.ReadVerified(request.CampaignId);
        using var movementEvent = System.Text.Json.JsonDocument.Parse(
            CampaignExercises.Reconstruct(movementHead).ReconstructedSnapshotBytes!);
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, movementEvent.RootElement.GetProperty("reactionWindow").ValueKind);
        Assert.Equal(44, CampaignExercises.QueryCheckpoint(movementHead).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(movementHead).IsVerified);
        Assert.Throws<InvalidOperationException>(() => movementCold.QuerySecondMovementCompletionOwnerOpening(opponent));
        var movementChild = await FreshProcessRunDeadlineChild(movementCopy.Path, request.CampaignId,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Assert.True(movementChild.IsVerified);
        Assert.Equal(44, movementChild.ContractVersion);
        Assert.Equal(CampaignExercises.Reconstruct(movementHead).EventStreamHash, movementChild.EventStreamHash);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void VisibleAssaultWithoutPayableForceRoundCancelsAndContinues(
        bool withTiming, bool positive)
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var request = positive ? PositiveCombatRequest() : CombatInfantryRequest();
        AdvanceToContactCombat(journal, request,
            withTiming ? Timing(request) : null,
            preserveAdjacentInfantry: true);
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        if (!withTiming)
        {
            Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
            Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-combat-selection"));
            Assert.True(CampaignExercises.Reconstruct(fixture.Open(new ManualClock()).ReadVerified(request.CampaignId)).IsVerified);
            return;
        }
        var opening = journal.QueryCombatOwnerOpening(attacker, "axis-infantry");
        Assert.Contains("assaultOffers", System.Text.Encoding.UTF8.GetString(
            opening.VisibleBytes), StringComparison.Ordinal);
        var chosen = journal.SubmitOwnerCombatSelection(attacker, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(chosen.IsAccepted);

        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        var declined = journal.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]);
        Assert.True(declined.IsAccepted);

        var cold = fixture.Open(new ManualClock());
        var verified = cold.ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(verified).ContractVersion);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified, reconstruction.FailureReason.ToString());
        Assert.Throws<InvalidOperationException>(() =>
            cold.QueryForceOwnerOpening(attacker, "axis-infantry"));
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT COUNT(*) FROM force_rounds WHERE campaign_id=$id";
            query.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(0L, query.ExecuteScalar());
            query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id "
                + "AND action_kind='native-combat-force-cancellation'";
            Assert.Equal(1L, query.ExecuteScalar());
        }
        var retry = cold.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(declined.CanonicalReceipt, retry.CanonicalReceipt);
        var control = cold.QueryCycleControlOwnerOpening(attacker);
        Assert.Single(control.PublicActionRefs);
        Assert.True(cold.SubmitOwnerCycleControl(attacker,
            control.Revision, control.PublicActionRefs[^1]).IsAccepted);
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(
            fixture.Open(new ManualClock()).ReadVerified(request.CampaignId))
            .ContractVersion);
    }

    [Fact]
    public void SeatCredentialSurvivesRestartAndRotationRevokesIt()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        journal.CreateCampaign(Request());
        Assert.Throws<UnauthorizedAccessException>(() => journal.IssueSeatGrant(
            "campaign-durable-journal", CampaignActionAudience.Axis,
            new string('0', 64)));
        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        Assert.DoesNotContain(grant.Secret, grant.ToString());
        var credential = fixture.Open().RedeemSeatGrant(grant)!;
        Assert.NotNull(credential);
        Assert.DoesNotContain(credential.Secret, credential.ToString());
        Assert.Null(fixture.Open().RedeemSeatGrant(grant));
        Assert.Equal(grant.Epoch, fixture.Open().AuthenticateSeat(credential)!.Epoch);
        Assert.Null(fixture.Open().AuthenticateSeat(credential with { Secret = new string('0', 64) }));

        var replacement = fixture.Open().IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        Assert.Equal(grant.Epoch + 1, replacement.Epoch);
        Assert.Null(fixture.Open().AuthenticateSeat(credential));
        Assert.NotNull(fixture.Open().AuthenticateSeat(fixture.Open().RedeemSeatGrant(replacement)!));
    }

    [Fact]
    public void GrantAndCredentialExpireAtTheirStoredDeadlines()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        journal.CreateCampaign(Request());
        var expiredGrant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Null(fixture.Open(clock).RedeemSeatGrant(expiredGrant));

        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = fixture.Open(clock).RedeemSeatGrant(grant)!;
        Assert.NotNull(fixture.Open(clock).AuthenticateSeat(credential));
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Null(fixture.Open(clock).AuthenticateSeat(credential));
    }

    [Fact]
    public void OwnerOpeningRemainsByteIdenticalAcrossUnseenAuthorityAdvanceAndRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var elementId = CampaignHostAdapter.QueryPrivateSideCut(session,
            CampaignActionAudience.Axis)!.OwnElements[0].ElementId;
        var first = journal.QueryOwnerOpening(credential, elementId);
        var reopened = fixture.Open().QueryOwnerOpening(credential, elementId);
        Assert.Equal(first.VisibleBytes, reopened.VisibleBytes);
        Assert.Equal(first.PublicActionRefs, reopened.PublicActionRefs);
        Assert.Equal(first.Revision, reopened.Revision);

        // UX-001 auto-drain (PR #504): the first QueryOwnerOpening above may already have
        // silently advanced System past the leading preamble steps as a side effect, so
        // `session`'s own captured System candidate can be stale by now -- pick whichever
        // audience (System, Axis, or Commonwealth) currently has something pending instead of
        // assuming System specifically, to drive the same "unseen authority advance elsewhere"
        // this test means to simulate regardless of exactly where the drain left things.
        var pending = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(
                journal.ReadVerified("campaign-durable-journal"), audience).ActionSet)
            .First(value => value is { Candidates.Count: > 0 })!;
        var advance = new CampaignActionSubmission(1, pending.CampaignId,
            pending.StateVersion, pending.PositionId, pending.Audience,
            pending.Candidates[0].ActionId);
        Assert.True(journal.Append(advance.CampaignId, advance).IsAccepted);
        var after = fixture.Open().QueryOwnerOpening(credential, elementId);
        Assert.Equal(first.VisibleBytes, after.VisibleBytes);
        Assert.Equal(first.PublicActionRefs, after.PublicActionRefs);
        Assert.Equal(first.Revision, after.Revision);

        var replacementGrant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var replacement = journal.RedeemSeatGrant(replacementGrant)!;
        Assert.Throws<UnauthorizedAccessException>(() => journal.QueryOwnerOpening(
            credential, elementId));
        Assert.Equal(first.Revision + 1,
            journal.QueryOwnerOpening(replacement, elementId).Revision);
    }

    [Fact]
    public void ContactMovementReferenceCommitsOnceAndReplaysAfterRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = new CampaignCreationRequest(1, "campaign-contact-owner-host",
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.reaction.v1",
            "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
            "rules-lab.content.breakdown-reaction.v1",
            "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
            "breakdown-reaction-lab");
        var session = journal.CreateCampaign(request);
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, next.ActionId);
            var accepted = journal.Append(request.CampaignId, command);
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
        }
        var grant = journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);
        var reference = opening.PublicActionRefs[0];
        Assert.False(journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, "act.forged").IsAccepted);
        Assert.False(journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, "act." + new string('a', 1_000_000)).IsAccepted);

        var acceptedOwner = journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, reference);
        Assert.True(acceptedOwner.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, reference);
        Assert.True(retry.IsAccepted);
        Assert.Equal(acceptedOwner.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.False(reopened.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision + 1, reference).IsAccepted);
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion + 1,
            CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId)).StateVersion);

        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = """
                UPDATE accepted_steps SET owner_revision=owner_revision+1
                WHERE owner_public_ref IS NOT NULL
                """;
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Fact]
    public void RetainedMovementReferenceUsesCurrentVisibleCutWhenPrivateFenceIsOlder()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ContactRequest();
        AdvanceToContactMovement(journal, request);
        var grant = journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);

        // Retain the same opening/action map with an earlier private authority fence. This
        // isolates the host's equal-visible submission rule from the small fixture's sight
        // changes when another formation moves.
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE owner_openings SET authority_version=authority_version-1 "
                + "WHERE campaign_id=$id AND acting_element_id='axis-infantry'";
            update.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.True(fixture.Open().SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
    }

    [Fact]
    public void RetainedMovementReferenceSurvivesActualUnseenFormationMove()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ContactRequest();
        var session = AdvanceToContactMovement(journal, request);
        var credential = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        var set = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
        var move = set.Candidates.OfType<MoveElementAction>()
            .First(value => value.ElementId == "axis-truck");
        var accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
            set.CampaignId, set.StateVersion, set.PositionId, set.Audience, move.ActionId));
        var next = accepted.SuccessorSession!;
        var pending = CampaignExercises.Query(next, CampaignActionAudience.Axis).ActionSet!;
        var stop = pending.Candidates.OfType<StopElementMovementAction>().Single();
        accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
            pending.CampaignId, pending.StateVersion, pending.PositionId,
            pending.Audience, stop.ActionId));
        Assert.True(accepted.IsAccepted);
        next = accepted.SuccessorSession!;
        var system = CampaignExercises.Query(next, CampaignActionAudience.System).ActionSet!;
        var resolve = system.Candidates.OfType<ResolveBreakdownStopAction>().Single();
        accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
            system.CampaignId, system.StateVersion, system.PositionId,
            system.Audience, resolve.ActionId));
        Assert.True(accepted.IsAccepted);
        var afterHiddenStep = fixture.Open();
        var current = CampaignHostAdapter.QueryContactOwnerDraft(
            afterHiddenStep.ReadVerified(request.CampaignId), CampaignActionAudience.Axis,
            "axis-infantry");
        Assert.Equal(opening.VisibleBytes, current.VisibleBytes);
        var submitted = afterHiddenStep.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(submitted.IsAccepted);
        Assert.Equal(submitted.CanonicalReceipt, fixture.Open().SubmitOwnerMovement(
            credential, "axis-infantry", opening.Revision,
            opening.PublicActionRefs[0]).CanonicalReceipt);
    }

    [Fact]
    public void ContactEvidenceIsRetainedAndTamperingBlocksColdRead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ContactRequest();
        var session = AdvanceToContactMovement(journal, request);
        var grant = journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);
        Assert.True(journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        Assert.NotNull(fixture.Open().ReadVerified(request.CampaignId));

        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT initial_contacts FROM campaigns WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.True(((byte[])query.ExecuteScalar()!).Length >= sizeof(int));
        query.CommandText = "SELECT canonical_contacts FROM accepted_steps "
            + "WHERE campaign_id=$id AND action_kind='contact-movement'";
        Assert.True(((byte[])query.ExecuteScalar()!).Length >= sizeof(int));
        using var tamper = connection.CreateCommand();
        tamper.CommandText = "UPDATE accepted_steps SET canonical_contacts=x'01000000' "
            + "WHERE campaign_id=$id AND action_kind='contact-movement'";
        tamper.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1, tamper.ExecuteNonQuery());
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Fact]
    public void ExistingSharedDirectoryIsRejectedWithoutChangingItsPermissions()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-authority-shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var sharedMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute;
        File.SetUnixFileMode(directory, sharedMode);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                CampaignDurableAuthorityJournal.CreateNew(System.IO.Path.Combine(directory,
                    "campaign.sqlite"), OperatorSecret));
            Assert.Equal(sharedMode, File.GetUnixFileMode(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SuppliedContentResolverIsUsedForColdJournalReplay()
    {
        using var fixture = new JournalFixture();
        var resolver = new RecordingContentResolver();
        var journal = CampaignDurableAuthorityJournal.CreateNew(fixture.Path, OperatorSecret,
            contentResolver: resolver);
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);
        var accepted = journal.Append(command.CampaignId, command);
        Assert.True(accepted.IsAccepted);
        var callsBeforeReopen = resolver.Calls;
        var reopened = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path, contentResolver: resolver);
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(reopened.ReadVerified(command.CampaignId)).StateVersion);
        Assert.True(resolver.Calls > callsBeforeReopen);
        var unavailable = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path,
            contentResolver: new RecordingContentResolver(reject: true));
        Assert.Throws<InvalidOperationException>(() => unavailable.ReadVerified(command.CampaignId));
    }

    private sealed class RecordingContentResolver(bool reject = false) : IContentPackV6Resolver
    {
        private readonly Cna.Core.Content.ContentPackV6Artifact artifact =
            Cna1979SyntheticContentResolver.Instance.ResolveV6(Request().ContentPackId, Request().ContentHash).Artifact!;
        public int Calls { get; private set; }

        public Cna.Core.Content.ContentPackV6CatalogResolution ResolveV6(string packId, string expectedHash)
        {
            Calls++;
            return reject || artifact.Identity.PackId != packId || artifact.Identity.Hash != expectedHash
                ? Cna.Core.Content.ContentPackV6CatalogResolution.Rejected(
                    Cna.Core.Content.ContentCatalogRejectionReason.UnknownPackId)
                : Cna.Core.Content.ContentPackV6CatalogResolution.Resolved(artifact);
        }
    }

    [Fact]
    public void AcceptedCommandSurvivesColdReadAndStaleRetryDoesNotAdvanceHead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);

        var accepted = journal.Append(command.CampaignId, command);
        Assert.True(accepted.IsAccepted);
        var reopened = fixture.Open();
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(reopened.ReadVerified(command.CampaignId)).StateVersion);

        var stale = reopened.Append(command.CampaignId, command);
        Assert.False(stale.IsAccepted);
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(command.CampaignId)).StateVersion);
    }

    [Fact]
    public void AlteredAcceptedEventFailsColdRead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);
        Assert.True(journal.Append(command.CampaignId, command).IsAccepted);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE accepted_steps SET canonical_event=x'00'";
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(command.CampaignId));
    }

    private static long CountAcceptedStep(string path, string campaignId, string kind)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM accepted_steps "
            + "WHERE campaign_id=$id AND action_kind=$kind";
        command.Parameters.AddWithValue("$id", campaignId);
        command.Parameters.AddWithValue("$kind", kind);
        return (long)command.ExecuteScalar()!;
    }

    private sealed class OwnerHttpHarness : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly IHost silo;
        private readonly HttpClient client;
        private readonly string campaignId;

        private OwnerHttpHarness(WebApplication app, IHost silo, HttpClient client, string campaignId)
        {
            this.app = app;
            this.silo = silo;
            this.client = client;
            this.campaignId = campaignId;
        }

        internal static async Task<OwnerHttpHarness> Start(
            CampaignDurableAuthorityJournal journal, string campaignId)
        {
            var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal);
            var silo = await StartOutwardApiSiloAsync(store);
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Development";
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services.AddSingleton(store);
            builder.Services.AddSingleton(silo.Services.GetRequiredService<IGrainFactory>());
            var app = builder.Build();
            app.MapDurableCampaignApi();
            await app.StartAsync(TestContext.Current.CancellationToken);
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            var client = new HttpClient { BaseAddress = new Uri(address) };
            // Not a same-origin-gate test; simulate a genuine same-origin fetch so the
            // v2 middleware's stricter header-presence check (#319) doesn't block it.
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            return new OwnerHttpHarness(app, silo, client, campaignId);
        }

        // Real outward HTTP API campaign creation (POST /api/v2/campaigns/), not a direct
        // journal.CreateCampaign shortcut -- the actual boundary a real operator/setup flow
        // crosses. Asserts 201 Created; the body is just {contractVersion, campaignId}.
        internal async Task CreateCampaign(CampaignCreationRequest request,
            CampaignDecisionTimingConfigurationV1 timing, string operatorSecret)
        {
            using var timingJson = JsonDocument.Parse(timing.SerializeCanonical());
            var body = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                campaignId = request.CampaignId,
                rulesetHash = request.RulesetHash,
                seed = request.Seed,
                setupId = request.SetupId,
                setupHash = request.SetupHash,
                contentPackId = request.ContentPackId,
                contentHash = request.ContentHash,
                scenarioId = request.ScenarioId,
                timing = timingJson.RootElement,
            });
            using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v2/campaigns/")
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            message.Headers.Add("X-Sandtable-Operator", operatorSecret);
            using var response = await client.SendAsync(message, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // Real outward HTTP API seat issuance + redemption (POST /grants/{audience} then
        // POST /seats), not journal.IssueSeatGrant/RedeemSeatGrant directly.
        internal async Task<CampaignSeatCredential> IssueAndRedeemSeat(
            CampaignActionAudience audience, string operatorSecret)
        {
            var side = audience == CampaignActionAudience.Axis ? "axis" : "commonwealth";
            using var grantMessage = new HttpRequestMessage(HttpMethod.Post,
                $"/api/v2/campaigns/{campaignId}/grants/{side}")
            { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            grantMessage.Headers.Add("X-Sandtable-Operator", operatorSecret);
            using var grantResponse = await client.SendAsync(grantMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
            using var grant = JsonDocument.Parse(await grantResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            var seatBody = "{\"contractVersion\":2,\"grant\":"
                + grant.RootElement.GetProperty("grant").GetRawText() + "}";
            using var seatResponse = await client.PostAsync($"/api/v2/campaigns/{campaignId}/seats",
                new StringContent(seatBody, Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, seatResponse.StatusCode);
            using var credentialJson = JsonDocument.Parse(await seatResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            var value = credentialJson.RootElement.GetProperty("credential");
            return new CampaignSeatCredential(
                value.GetProperty("campaignId").GetString()!, audience,
                value.GetProperty("epoch").GetInt64(), value.GetProperty("secret").GetString()!,
                value.GetProperty("expiresUnixMs").GetInt64());
        }

        internal async Task Submit(CampaignSeatCredential credential, string actor,
            string openingStage, string actionStage, CampaignOwnerOpening expected,
            string reference)
        {
            var seat = new
            {
                campaignId = credential.CampaignId,
                audience = credential.Audience == CampaignActionAudience.Axis
                    ? "axis" : "commonwealth",
                epoch = credential.Epoch,
                secret = credential.Secret,
                expiresUnixMs = credential.ExpiresUnixMs,
            };
            var openingBody = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                credential = seat,
                stage = openingStage,
                actingElementId = actor,
            });
            using var opened = await client.PostAsync($"/api/v2/campaigns/{campaignId}/openings",
                new StringContent(openingBody, Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
            using var openingJson = JsonDocument.Parse(await opened.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            Assert.Equal(expected.VisibleBytes, Convert.FromBase64String(openingJson.RootElement
                .GetProperty("visibleCutBase64").GetString()!));
            Assert.Equal(expected.Revision, openingJson.RootElement.GetProperty("revision")
                .GetInt64());
            Assert.Contains(reference, openingJson.RootElement.GetProperty("publicActionRefs")
                .EnumerateArray().Select(item => item.GetString()));
            var actionBody = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                credential = seat,
                stage = actionStage,
                actingElementId = actor,
                openingRevision = expected.Revision,
                publicActionRef = reference,
            });
            using var submitted = await client.PostAsync($"/api/v2/campaigns/{campaignId}/actions",
                new StringContent(actionBody, Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
            var receipt = await submitted.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);
            Assert.DoesNotContain("actionId", receipt, StringComparison.OrdinalIgnoreCase);
            using var retried = await client.PostAsync($"/api/v2/campaigns/{campaignId}/actions",
                new StringContent(actionBody, Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
            Assert.Equal(receipt, await retried.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
        }

        // Posts one action body and returns the HTTP status without asserting success (020D: the
        // retired rba-move action stage must be refused by the outward route).
        internal async Task<HttpStatusCode> SubmitStatus(CampaignSeatCredential credential, string actor,
            string actionStage, long openingRevision, string reference) =>
            (await SubmitResponse(credential, actor, actionStage, openingRevision, reference)).Status;

        // Status and body of one action post, without asserting success.
        internal async Task<(HttpStatusCode Status, string Body)> SubmitResponse(
            CampaignSeatCredential credential, string actor,
            string actionStage, long openingRevision, string reference)
        {
            var actionBody = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                credential = new
                {
                    campaignId = credential.CampaignId,
                    audience = credential.Audience == CampaignActionAudience.Axis ? "axis" : "commonwealth",
                    epoch = credential.Epoch,
                    secret = credential.Secret,
                    expiresUnixMs = credential.ExpiresUnixMs,
                },
                stage = actionStage,
                actingElementId = actor,
                openingRevision,
                publicActionRef = reference,
            });
            using var submitted = await client.PostAsync($"/api/v2/campaigns/{campaignId}/actions",
                new StringContent(actionBody, Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
            return (submitted.StatusCode, await submitted.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
        }

        // Status and body of one opening post, without asserting success (Reaction:
        // CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException must surface as
        // an explicit 409 rule-not-implemented, not the generic 409 "unavailable").
        internal async Task<(HttpStatusCode Status, string Body)> OpeningResponse(
            CampaignSeatCredential credential, string actor, string openingStage)
        {
            var openingBody = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                credential = new
                {
                    campaignId = credential.CampaignId,
                    audience = credential.Audience == CampaignActionAudience.Axis ? "axis" : "commonwealth",
                    epoch = credential.Epoch,
                    secret = credential.Secret,
                    expiresUnixMs = credential.ExpiresUnixMs,
                },
                stage = openingStage,
                actingElementId = actor,
            });
            using var opened = await client.PostAsync($"/api/v2/campaigns/{campaignId}/openings",
                new StringContent(openingBody, Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
            return (opened.StatusCode, await opened.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            client.Dispose();
            await app.DisposeAsync();
            await StopOutwardApiSiloAsync(silo);
        }
    }

    private static (CampaignCreationRequest Request, CampaignSeatCredential Attacker,
        CampaignSeatCredential Defender, string DefenderId,
        CampaignOwnerOpening AttackerForce, CampaignOwnerOpening DefenderForce)
        PrepareInfantryForceRound(CampaignDurableAuthorityJournal journal,
            bool positive = false, ulong? seed = null,
            string? reserveElementId = null, bool doubleContact = false,
            bool tripleContact = false, string? secondReserveElementId = null,
            string? thirdReserveElementId = null)
    {
        var request = tripleContact ? PositiveTripleContactRequest(seed ?? 12345)
            : doubleContact ? PositiveDoubleContactRequest(seed ?? 12345)
            : positive ? PositiveCombatRequest(seed ?? 12345) : CombatInfantryRequest();
        AdvanceToContactCombat(journal, request, Timing(request),
            preserveAdjacentInfantry: true,
            reserveElementId: reserveElementId,
            secondReserveElementId: secondReserveElementId,
            thirdReserveElementId: thirdReserveElementId);
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var selected = journal.QueryCombatOwnerOpening(attacker, "axis-infantry");
        using var combatVisible = System.Text.Json.JsonDocument.Parse(selected.VisibleBytes);
        var assaultOffers = combatVisible.RootElement.GetProperty("assaultOffers");
        var infantryOffer = Enumerable.Range(0, assaultOffers.GetArrayLength())
            .Single(index => assaultOffers[index].GetProperty("apparentLocationId")
                .GetString() == "north-east");
        Assert.True(journal.SubmitOwnerCombatSelection(attacker, "axis-infantry",
            selected.Revision, selected.PublicActionRefs[infantryOffer]).IsAccepted);
        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        var attackerForce = journal.QueryForceOwnerOpening(attacker, "axis-infantry");
        var defenderForce = journal.QueryForceOwnerOpening(defender, defenderId);
        Assert.Single(attackerForce.PublicActionRefs);
        Assert.Single(defenderForce.PublicActionRefs);
        return (request, attacker, defender, defenderId, attackerForce, defenderForce);
    }

    private static CampaignDecisionTimingConfigurationV1 Timing(CampaignCreationRequest request) =>
        new("rules-lab.campaign-timing.1", request.RulesetHash, request.ContentHash,
            [new("custody", 30_000, "leave-unguarded"),
                new("cycle-control", 30_000, "finish-phase"),
                new("force-assignment", 30_000, "cancel-incomplete-voluntary-round"),
                new("rba", 30_000, "cancel-without-decline"),
                new("reserve-release", 30_000, "convert-unresolved-i-retain-ii"),
                new("retreat", 30_000, "refuse-retreat"),
                new("selection", 30_000, "no-selection")]);

    private static CampaignCreationRequest Request()
    {
        return new CampaignCreationRequest(1, "campaign-durable-journal",
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.truck.v1", TemporaryCampaignGrain.SetupHash,
            "rules-lab.content.breakdown-truck.v1", TemporaryCampaignGrain.ContentHash,
            TemporaryCampaignGrain.ScenarioId);
    }

    private static CampaignCreationRequest ContactRequest() => new(1,
        "campaign-contact-owner-host", Cna1979Ruleset.Manifest.Hash, 12345,
        "rules-lab.breakdown.reaction.v1",
        "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
        "rules-lab.content.breakdown-reaction.v1",
        "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
        "breakdown-reaction-lab");

    private static CampaignCreationRequest CombatInfantryRequest() => new(1,
        "campaign-native-combat-infantry", Cna1979Ruleset.Manifest.Hash, 12345,
        "rules-lab.breakdown.reaction.combat-infantry.v1",
        "sha256:18734d3693747a35d855205f869917dc53dfdbdb9333ee32cb7764cbddaff3ac",
        "rules-lab.content.breakdown-reaction.combat-infantry.v1",
        "sha256:f6c8e7cbed61be1e99c877323471857b2d6b43a5bcfbe9475a59df9599e3498e",
        "breakdown-reaction-combat-infantry");

    private static CampaignCreationRequest PositiveCombatRequest(ulong seed = 12345) => new(1,
        "campaign-native-combat-ten-toe", Cna1979Ruleset.Manifest.Hash, seed,
        "rules-lab.breakdown.reaction.combat-assault.v1",
        "sha256:69db7f2c961f88f5469627a9e82f76c6a8b85330aa48a94ee3eaf0d22d18bbb3",
        "rules-lab.content.breakdown-reaction.combat-assault.v1",
        "sha256:d2d569a3f04f4915108e2220ac27ddfc25c97fcc047a4d4a67b496a6d62ca19c",
        "breakdown-reaction-combat-assault");

    private static CampaignCreationRequest PositiveDoubleContactRequest(ulong seed) => new(1,
        "campaign-native-combat-double", Cna1979Ruleset.Manifest.Hash, seed,
        "rules-lab.breakdown.reaction.combat-double-fresh.v1",
        "sha256:d02a9996e2c87b4f5feda75bd0b6cf4a6756b0abbb83abb36ffe037e6fac8b5d",
        "rules-lab.content.breakdown-reaction.combat-double-fresh.v1",
        "sha256:44a76f21f1b492493c898495721f5f684acc83a14c391289c5e8d66220b6a648",
        "breakdown-reaction-combat-double-fresh");

    private static CampaignCreationRequest PositiveTripleContactRequest(ulong seed) => new(1,
        "campaign-native-combat-triple", Cna1979Ruleset.Manifest.Hash, seed,
        "rules-lab.breakdown.reaction.combat-triple.v1",
        "sha256:9383b8fda7d2b33cedbd699d5bd564cbbea9900f520c00a9d3827d6447328049",
        "rules-lab.content.breakdown-reaction.combat-triple.v1",
        "sha256:eb81034809fcbb7403de47db4e3f3bd6abb42fec430f9017603484f7764c7d71",
        "breakdown-reaction-combat-triple");

    private static ExerciseSession AdvanceToContactCombat(
        CampaignDurableAuthorityJournal journal, CampaignCreationRequest request,
        CampaignDecisionTimingConfigurationV1? timing = null,
        string movementDestination = "north", bool preserveAdjacentInfantry = false,
        string? reserveElementId = null, string? secondReserveElementId = null,
        Action? beforeSelectionEntry = null, string? thirdReserveElementId = null)
    {
        var session = journal.CreateCampaign(request, timing);
        var pendingReserves = new Queue<string>(new[] { reserveElementId,
            secondReserveElementId, thirdReserveElementId }.OfType<string>());
        for (var index = 0; index < 80; index++)
        {
            try
            {
                _ = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
                    CampaignActionAudience.Axis, "axis-infantry");
                return session;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // Advance through admitted pre-Combat positions.
            }
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(set);
            var reserveDesignation = pendingReserves.Count == 0 ? null
                : set.Candidates.OfType<DesignateReserveAction>()
                    .FirstOrDefault(value => value.ElementId == pendingReserves.Peek());
            if (pendingReserves.Count > 0
                && set.Candidates.OfType<DesignateReserveAction>().Any())
                Assert.NotNull(reserveDesignation);
            var next = reserveDesignation is not null
                ? reserveDesignation
                : preserveAdjacentInfantry
                ? set.Candidates.FirstOrDefault(value => value is not MoveElementAction)
                    ?? set.Candidates.FirstOrDefault(value => value is MoveElementAction move
                        && move.ElementId is not ("axis-infantry" or "commonwealth-infantry"))
                    ?? set.Candidates[0]
                : set.Candidates.OfType<MoveElementAction>().FirstOrDefault(value =>
                    value.ElementId == "axis-infantry"
                    && value.DestinationLocationId == movementDestination)
                    ?? set.Candidates[0];
            var submission = new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId);
            if (beforeSelectionEntry is not null)
            {
                var preview = CampaignHostAdapter.Submit(session, submission);
                var entersSelection = false;
                if (preview.IsAccepted)
                {
                    try
                    {
                        _ = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(preview.SuccessorSession!);
                        entersSelection = true;
                    }
                    catch (InvalidOperationException) { }
                }
                if (entersSelection) beforeSelectionEntry();
            }
            var accepted = journal.Append(request.CampaignId, submission);
            Assert.True(accepted.IsAccepted);
            if (reserveDesignation is not null)
                _ = pendingReserves.Dequeue();
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static ExerciseSession AdvanceToContactMovement(
        CampaignDurableAuthorityJournal journal, CampaignCreationRequest request,
        CampaignDecisionTimingConfigurationV1? timing = null)
    {
        var session = journal.CreateCampaign(request, timing);
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) return session;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            var accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId));
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("No owner movement cut was reached.");
    }

    private sealed class JournalFixture : IDisposable
    {
        private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-authority-journal-" + Guid.NewGuid().ToString("N"));
        private Persistence.MartenJournalDatabase? marten;
        internal string Path => System.IO.Path.Combine(directory, "campaign.sqlite");

        /// <summary>
        /// Phase C3b equivalence gate: with <c>SANDTABLE_JOURNAL_TEST_BACKEND=marten</c> every test
        /// using this fixture runs against a real Postgres (Testcontainers) Marten backend instead of
        /// a SQLite file -- each journal call loading its state from Postgres and flushing it back.
        /// </summary>
        internal static bool UsesMarten =>
            Environment.GetEnvironmentVariable("SANDTABLE_JOURNAL_TEST_BACKEND") == "marten";

        internal CampaignDurableAuthorityJournal Create(TimeProvider? clock = null)
        {
            if (!UsesMarten) return CampaignDurableAuthorityJournal.CreateNew(Path, OperatorSecret, clock);
            marten ??= Persistence.MartenJournalDatabase.Create(OperatorSecret, clock);
            return marten.Journal(OperatorSecret, clock);
        }

        internal CampaignDurableAuthorityJournal Open(TimeProvider? clock = null) => UsesMarten
            ? (marten ?? throw new InvalidOperationException("Journal is missing.")).Journal(OperatorSecret, clock)
            : CampaignDurableAuthorityJournal.OpenExisting(Path, clock);

        public void Dispose()
        {
            marten?.Dispose();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z",
            global::System.Globalization.CultureInfo.InvariantCulture);
        internal bool IsAvailable { get; set; } = true;
        public override DateTimeOffset GetUtcNow() => IsAvailable
            ? now : throw new InvalidOperationException("Trusted clock unavailable.");
        internal void Advance(TimeSpan duration) => now = now.Add(duration);
    }
}
