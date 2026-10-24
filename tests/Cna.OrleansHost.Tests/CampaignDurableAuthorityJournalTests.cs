using System.Net;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Cna.OrleansHost.Tests;

public sealed class CampaignDurableAuthorityJournalTests
{
    private const string OperatorSecret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

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

    [Fact]
    public void SchemaFiveCampaignRemainsReadableButCannotAcquireImplicitTiming()
    {
        using var fixture = new JournalFixture();
        var request = Request();
        fixture.Create().CreateCampaign(request);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            byte[][] evidence;
            using (var query = connection.CreateCommand())
            {
                query.CommandText = "SELECT creation_event,initial_snapshot,initial_contacts "
                    + "FROM campaigns WHERE campaign_id=$id";
                query.Parameters.AddWithValue("$id", request.CampaignId);
                using var reader = query.ExecuteReader();
                Assert.True(reader.Read());
                evidence = [reader.GetFieldValue<byte[]>(0), reader.GetFieldValue<byte[]>(1),
                    reader.GetFieldValue<byte[]>(2)];
            }
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("campaign-durable-authority-v2:creation");
                writer.Write(request.ContractVersion);
                writer.Write(request.CampaignId);
                writer.Write(request.RulesetHash);
                writer.Write(request.Seed);
                writer.Write(request.SetupId);
                writer.Write(request.SetupHash);
                writer.Write(request.ContentPackId);
                writer.Write(request.ContentHash);
                writer.Write(request.ScenarioId);
                foreach (var bytes in evidence)
                {
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }
            }
            using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE campaigns SET head_hash=$hash WHERE campaign_id=$id";
                update.Parameters.AddWithValue("$hash", "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(stream.ToArray())));
                update.Parameters.AddWithValue("$id", request.CampaignId);
                Assert.Equal(1, update.ExecuteNonQuery());
            }
            using var change = connection.CreateCommand();
            change.CommandText = "ALTER TABLE campaigns DROP COLUMN timing_configuration; PRAGMA user_version=5";
            change.ExecuteNonQuery();
        }
        var legacy = fixture.Open();
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => legacy.CreateCampaign(request));
    }

    [Fact]
    public void SchemaSixCampaignRetainsTimingButCannotCreateNewCampaign()
    {
        using var fixture = new JournalFixture();
        var request = Request();
        fixture.Create().CreateCampaign(request, Timing(request));
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var change = connection.CreateCommand();
            change.CommandText = "DROP TABLE force_rounds; PRAGMA user_version=6";
            change.ExecuteNonQuery();
        }
        var previous = fixture.Open();
        Assert.NotNull(previous.ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() =>
            previous.CreateCampaign(CombatInfantryRequest(), Timing(CombatInfantryRequest())));
    }

    [Fact]
    public void ContactCombatOpeningCommitsOnceAndColdReplaysSelectedAuthority()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        var session = AdvanceToContactCombat(journal, request);
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
        var session = AdvanceToContactCombat(journal, request);
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
        var rba = cold.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(rba.PublicActionRefs.Count > 1);
        await outward.Submit(defender, westDefender,
            "retreat-before-assault", "rba-move", rba,
            rba.PublicActionRefs[0]);
        var afterMove = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterMove).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterMove).IsVerified);
        rba = cold.QueryRbaOwnerOpening(defender, westDefender);
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
                var lateSeal = Assert.Throws<InvalidOperationException>(() =>
                    fixture.Open(clock).SubmitOwnerForceSeal(defender,
                        thirdDefender, thirdDefenderForce.Revision,
                        thirdDefenderForce.PublicActionRefs[0]));
                Assert.Equal("Force round is closed.", lateSeal.Message);
                if (sealsBeforeTimeout == 0)
                    Assert.Throws<InvalidOperationException>(() =>
                        fixture.Open(clock).SubmitOwnerForceSeal(attacker,
                            "axis-infantry-third", thirdAttackerForce.Revision,
                            thirdAttackerForce.PublicActionRefs[0]));
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
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(after).ContractVersion);
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
    public void DefenderRetreatMoveAndExplicitStopColdReplayThroughOwnerSeat()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        AdvanceToContactCombat(journal, request);
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
        var moved = journal.SubmitOwnerRbaMove(defender, defenderId,
            opening.Revision, moveRef);
        Assert.True(moved.IsAccepted);
        var cold = fixture.Open();
        Assert.Equal(moved.CanonicalReceipt,
            cold.SubmitOwnerRbaMove(defender, defenderId,
                opening.Revision, moveRef).CanonicalReceipt);
        var afterMove = cold.ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterMove).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterMove).IsVerified);
        var next = cold.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(next.Revision > opening.Revision);
        Assert.True(next.PublicActionRefs.Count > 1);
        var repeated = cold.SubmitOwnerRbaMove(defender, defenderId,
            next.Revision, next.PublicActionRefs[0]);
        Assert.True(repeated.IsAccepted);
        var afterRepeat = fixture.Open().ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterRepeat).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterRepeat).IsVerified);
        var finalOpening = cold.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(finalOpening.Revision > next.Revision);
        var stopped = cold.SubmitOwnerRbaDecline(defender, defenderId,
            finalOpening.Revision, finalOpening.PublicActionRefs[^1]);
        Assert.True(stopped.IsAccepted);
        var final = fixture.Open().ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(final).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(final).IsVerified);
        var control = cold.QueryCycleControlOwnerOpening(attacker);
        Assert.Single(control.PublicActionRefs);
    }

    [Fact]
    public void VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        AdvanceToContactCombat(journal, request);
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

        var system = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var advance = new CampaignActionSubmission(1, system.CampaignId,
            system.StateVersion, system.PositionId, system.Audience,
            system.Candidates.Single().ActionId);
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
        private readonly HttpClient client;
        private readonly string campaignId;

        private OwnerHttpHarness(WebApplication app, HttpClient client, string campaignId)
        {
            this.app = app;
            this.client = client;
            this.campaignId = campaignId;
        }

        internal static async Task<OwnerHttpHarness> Start(
            CampaignDurableAuthorityJournal journal, string campaignId)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Development";
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services.AddSingleton(journal);
            var app = builder.Build();
            app.MapDurableCampaignApi();
            await app.StartAsync(TestContext.Current.CancellationToken);
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            return new OwnerHttpHarness(app, new HttpClient { BaseAddress = new Uri(address) },
                campaignId);
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

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            client.Dispose();
            await app.DisposeAsync();
        }
    }

    private static (CampaignCreationRequest Request, CampaignSeatCredential Attacker,
        CampaignSeatCredential Defender, string DefenderId,
        CampaignOwnerOpening AttackerForce, CampaignOwnerOpening DefenderForce)
        PrepareInfantryForceRound(CampaignDurableAuthorityJournal journal,
            bool positive = false, ulong? seed = null,
            string? reserveElementId = null, bool doubleContact = false,
            bool tripleContact = false, string? secondReserveElementId = null)
    {
        var request = tripleContact ? PositiveTripleContactRequest(seed ?? 12345)
            : doubleContact ? PositiveDoubleContactRequest(seed ?? 12345)
            : positive ? PositiveCombatRequest(seed ?? 12345) : CombatInfantryRequest();
        AdvanceToContactCombat(journal, request, Timing(request),
            preserveAdjacentInfantry: true,
            reserveElementId: reserveElementId,
            secondReserveElementId: secondReserveElementId);
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
        string? reserveElementId = null, string? secondReserveElementId = null)
    {
        var session = journal.CreateCampaign(request, timing);
        var pendingReserves = new Queue<string>(new[] { reserveElementId,
            secondReserveElementId }.OfType<string>());
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
            var accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId));
            Assert.True(accepted.IsAccepted);
            if (reserveDesignation is not null)
                _ = pendingReserves.Dequeue();
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static ExerciseSession AdvanceToContactMovement(
        CampaignDurableAuthorityJournal journal, CampaignCreationRequest request)
    {
        var session = journal.CreateCampaign(request);
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
        internal string Path => System.IO.Path.Combine(directory, "campaign.sqlite");
        internal CampaignDurableAuthorityJournal Create(TimeProvider? clock = null) =>
            CampaignDurableAuthorityJournal.CreateNew(Path, OperatorSecret, clock);
        internal CampaignDurableAuthorityJournal Open(TimeProvider? clock = null) =>
            CampaignDurableAuthorityJournal.OpenExisting(Path, clock);
        public void Dispose() => Directory.Delete(directory, recursive: true);
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
