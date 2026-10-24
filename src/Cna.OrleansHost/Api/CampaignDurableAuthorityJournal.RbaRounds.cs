using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record RbaRound(string CampaignId, string DecisionActionId,
        string BarrageEventHash, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private void InitializeRbaRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatRbaRoundBinding(authority.Session);
        if (authority.Timing is null || !TryTrustedNow(out var opened))
        {
            var reserveHead = PersistRbaCancellation(connection, transaction, authority, campaignId,
                -1, -1, -1, null, "rba-unavailable");
            AppendEmptyReserveRelease(connection, transaction, campaignId, reserveHead);
            return;
        }
        var round = new RbaRound(campaignId, binding.DecisionActionId, binding.BarrageEventHash,
            authority.Timing.Hash, opened, checked(opened + authority.Timing.Budget("rba")),
            opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO rba_rounds VALUES ($id,$decision,$barrage,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindRbaRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private static RbaRound ReadActiveRbaRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatRbaRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT decision_action_id,barrage_event_hash,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM rba_rounds
            WHERE campaign_id=$id AND barrage_event_hash=$barrage
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$barrage", binding.BarrageEventHash);
        using var reader = query.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("RBA round is missing.");
        var round = new RbaRound(campaignId, reader.GetString(0), reader.GetString(1),
            reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5),
            reader.GetString(6));
        if (reader.GetString(7) != RbaRoundHash(round) || authority.Timing is null)
            throw new InvalidOperationException("Retained RBA round changed.");
        RequireRbaRoundBinding(round, binding.DecisionActionId, binding.BarrageEventHash, authority.Timing);
        if (round.Status != "collecting") throw new InvalidOperationException("RBA round is closed.");
        return round;
    }

    private static Loaded PersistRbaCancellation(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId, long opened, long deadline, long highWater,
        long? cancelledAt, string reason)
    {
        var accepted = CampaignHostAdapter.CancelNativeCombatRba(authority.Session,
            authority.Timing, opened, deadline, highWater, cancelledAt, reason);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Core rejected trusted RBA cancellation.");
        var command = CampaignHostAdapter.NativeCombatRbaCancellationCommand(authority.Session, accepted);
        return PersistAccepted(connection, transaction, campaignId, authority,
            CampaignHostAcceptedStep.NativeCombatRbaCancellationKind,
            null, null, null, null, command, accepted);
    }

    private void CancelRbaRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, RbaRound round, string reason, long? cancelledAt)
    {
        var reserveHead = PersistRbaCancellation(connection, transaction, authority, round.CampaignId,
            round.OpenedMs, round.DeadlineMs, round.HighWaterMs, cancelledAt, reason);
        UpdateRbaRound(connection, transaction, round with { Status = "cancelled" });
        AppendEmptyReserveRelease(connection, transaction, round.CampaignId, reserveHead);
    }

    internal bool TickRbaRound(string campaignId)
    {
        if (schemaVersion != SchemaVersion) return false;
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        try { _ = CampaignHostAdapter.QueryNativeCombatRbaRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return false; }
        var round = ReadActiveRbaRound(connection, transaction, authority, campaignId);
        var gate = GateRbaRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
            CancelRbaRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
        transaction.Commit();
        return gate.CancellationReason is not null;
    }

    internal IReadOnlyList<string> CollectingRbaCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM rba_rounds
            WHERE status='collecting' AND campaign_id > $after COLLATE BINARY
            ORDER BY campaign_id COLLATE BINARY LIMIT $limit
            """;
        query.Parameters.AddWithValue("$after", after);
        query.Parameters.AddWithValue("$limit", limit);
        using var reader = query.ExecuteReader();
        var result = new List<string>();
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private static void RequireRbaRoundBinding(RbaRound round, string decisionActionId,
        string barrageHash,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.DecisionActionId != decisionActionId
            || round.BarrageEventHash != barrageHash
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("rba")
            || round.HighWaterMs < round.OpenedMs
            || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "cancelled"))
            throw new InvalidOperationException("Rba round differs from campaign authority.");
    }

    private (RbaRound Round, string? CancellationReason, long? CancelledAt)
        GateRbaRound(SqliteConnection connection, SqliteTransaction transaction,
            RbaRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateRbaRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private static void UpdateRbaRound(SqliteConnection connection,
        SqliteTransaction transaction, RbaRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE rba_rounds SET high_water_ms=$highWater,status=$status,
              record_hash=$hash WHERE campaign_id=$id AND barrage_event_hash=$barrage
            """;
        BindRbaRound(update, round);
        if (update.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Rba round is missing.");
    }

    private static void BindRbaRound(SqliteCommand command, RbaRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$decision", round.DecisionActionId);
        command.Parameters.AddWithValue("$barrage", round.BarrageEventHash);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", RbaRoundHash(round));
    }

    private static string RbaRoundHash(RbaRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-rba-round-v1");
            writer.Write(round.CampaignId);
            writer.Write(round.DecisionActionId);
            writer.Write(round.BarrageEventHash);
            writer.Write(round.ConfigurationHash);
            writer.Write(round.OpenedMs);
            writer.Write(round.DeadlineMs);
            writer.Write(round.HighWaterMs);
            writer.Write(round.Status);
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }


    private void VerifyRbaRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < RbaSchemaVersion) return;
        var barrages = new Dictionary<string, string>(StringComparer.Ordinal);
        var active = new Dictionary<string, string>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);
        var cancellations = new Dictionary<string, (string Config, long Opened, long Deadline, long HighWater)>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (step.Kind is not (CampaignHostAcceptedStep.NativeCombatBarrageKind
                or CampaignHostAcceptedStep.ContactRbaDeclineKind
                or CampaignHostAcceptedStep.NativeCombatRbaCancellationKind)) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var root = document.RootElement;
            var decision = root.GetProperty("decisionActionId").GetString()!;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatBarrageKind)
            {
                var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(step.EventRecords[0]));
                barrages.Add(hash, decision);
                active[decision] = hash;
                continue;
            }
            if (!active.Remove(decision, out var barrage))
                throw new InvalidOperationException("RBA ending has no active Barrage.");
            endings.Add(barrage, step.Kind == CampaignHostAcceptedStep.ContactRbaDeclineKind ? "completed" : "cancelled");
            if (step.Kind != CampaignHostAcceptedStep.NativeCombatRbaCancellationKind) continue;
            if (root.GetProperty("barrageEventHash").GetString() != barrage)
                throw new InvalidOperationException("RBA cancellation bound to another Barrage.");
            var evidence = (root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64());
            cancellations.Add(barrage, evidence);
            if (root.GetProperty("reason").GetString() == "rba-unavailable")
            {
                if (evidence.Item2 != -1 || evidence.Item3 != -1 || evidence.Item4 != -1
                    || evidence.Item1 != (timing?.Hash ?? CampaignHostAdapter.NoTimingRbaCancellationHash))
                    throw new InvalidOperationException("Unavailable RBA claimed clock evidence.");
                unavailable.Add(barrage);
            }
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT decision_action_id,barrage_event_hash,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM rba_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new RbaRound(campaignId, reader.GetString(0), reader.GetString(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
            if (reader.GetString(7) != RbaRoundHash(round) || timing is null
                || !barrages.TryGetValue(round.BarrageEventHash, out var decision)
                || unavailable.Contains(round.BarrageEventHash))
                throw new InvalidOperationException("Retained RBA round changed.");
            RequireRbaRoundBinding(round, decision, round.BarrageEventHash, timing);
            if (round.Status != (endings.GetValueOrDefault(round.BarrageEventHash) ?? "collecting"))
                throw new InvalidOperationException("RBA round status differs from accepted events.");
            if (cancellations.TryGetValue(round.BarrageEventHash, out var cancellation)
                && cancellation != (round.ConfigurationHash, round.OpenedMs, round.DeadlineMs, round.HighWaterMs))
                throw new InvalidOperationException("RBA cancellation differs from retained round.");
            found.Add(round.BarrageEventHash);
        }
        if (barrages.Keys.Any(hash => !found.Contains(hash) && !unavailable.Contains(hash)))
            throw new InvalidOperationException("Accepted Barrage has no retained RBA round.");
    }
}
