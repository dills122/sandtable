using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record CustodyRound(string CampaignId, string SettlementHash,
        long EntryStateVersion, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private void InitializeCustodyRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatCustodyRoundBinding(authority.Session);
        if (authority.Timing is null || !TryTrustedNow(out var opened))
        {
            var completed = PersistCustodyTimeout(connection, transaction, authority, campaignId,
                -1, -1, -1, null, "custody-unavailable");
            AppendNativeCombatSuccessor(connection, transaction, campaignId, completed);
            return;
        }
        var round = new CustodyRound(campaignId, binding.SettlementHash, binding.StateVersion,
            authority.Timing.Hash, opened, checked(opened + authority.Timing.Budget("custody")),
            opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO custody_rounds VALUES ($id,$settlement,$version,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindCustodyRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private static CustodyRound ReadActiveCustodyRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatCustodyRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT settlement_hash,entry_state_version,configuration_hash,opened_ms,
              deadline_ms,high_water_ms,status,record_hash FROM custody_rounds
            WHERE campaign_id=$id AND settlement_hash=$settlement
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$settlement", binding.SettlementHash);
        using var reader = query.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Custody round is missing.");
        var round = new CustodyRound(campaignId, reader.GetString(0), reader.GetInt64(1),
            reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
        if (reader.GetString(7) != CustodyRoundHash(round) || authority.Timing is null)
            throw new InvalidOperationException("Retained custody round changed.");
        RequireCustodyRoundBinding(round, binding.StateVersion, authority.Timing);
        if (round.Status != "collecting") throw new InvalidOperationException("Custody round is closed.");
        return round;
    }

    private static Loaded PersistCustodyTimeout(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId, long opened, long deadline, long highWater,
        long? cancelledAt, string reason)
    {
        var accepted = CampaignHostAdapter.TimeoutNativeCombatCustody(authority.Session,
            authority.Timing, opened, deadline, highWater, cancelledAt, reason);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Core rejected trusted custody timeout.");
        var command = CampaignHostAdapter.NativeCombatCustodyTimeoutCommand(authority.Session, accepted);
        return PersistAccepted(connection, transaction, campaignId, authority,
            CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind,
            null, null, null, null, command, accepted);
    }

    private void TimeoutCustodyRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, CustodyRound round, string reason, long? cancelledAt)
    {
        var completed = PersistCustodyTimeout(connection, transaction, authority, round.CampaignId,
            round.OpenedMs, round.DeadlineMs, round.HighWaterMs, cancelledAt, reason);
        UpdateCustodyRound(connection, transaction, round with { Status = "timed-out" });
        AppendNativeCombatSuccessor(connection, transaction, round.CampaignId, completed);
    }

    internal bool TickCustodyRound(string campaignId)
    {
        if (schemaVersion != SchemaVersion) return false;
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        try { _ = CampaignHostAdapter.QueryNativeCombatCustodyRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return false; }
        var round = ReadActiveCustodyRound(connection, transaction, authority, campaignId);
        var gate = GateCustodyRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
            TimeoutCustodyRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
        transaction.Commit();
        return gate.CancellationReason is not null;
    }

    internal IReadOnlyList<string> CollectingCustodyCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM custody_rounds
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

    private static void RequireCustodyRoundBinding(CustodyRound round, long entryStateVersion,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.EntryStateVersion != entryStateVersion
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("custody")
            || round.HighWaterMs < round.OpenedMs || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "timed-out"))
            throw new InvalidOperationException("Custody round differs from campaign authority.");
    }

    private (CustodyRound Round, string? CancellationReason, long? CancelledAt)
        GateCustodyRound(SqliteConnection connection, SqliteTransaction transaction, CustodyRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateCustodyRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private static void UpdateCustodyRound(SqliteConnection connection, SqliteTransaction transaction,
        CustodyRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE custody_rounds SET high_water_ms=$highWater,status=$status,record_hash=$hash
            WHERE campaign_id=$id AND settlement_hash=$settlement
            """;
        BindCustodyRound(update, round);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Custody round is missing.");
    }

    private static void BindCustodyRound(SqliteCommand command, CustodyRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$settlement", round.SettlementHash);
        command.Parameters.AddWithValue("$version", round.EntryStateVersion);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", CustodyRoundHash(round));
    }

    private static string CustodyRoundHash(CustodyRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-custody-round-v1");
            writer.Write(round.CampaignId);
            writer.Write(round.SettlementHash);
            writer.Write(round.EntryStateVersion);
            writer.Write(round.ConfigurationHash);
            writer.Write(round.OpenedMs);
            writer.Write(round.DeadlineMs);
            writer.Write(round.HighWaterMs);
            writer.Write(round.Status);
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private void VerifyCustodyRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < CustodySchemaVersion) return;
        var settlements = new Dictionary<string, long>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);
        var timeouts = new Dictionary<string, (string Config, long Opened, long Deadline, long HighWater)>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (step.Kind is not (CampaignHostAcceptedStep.NativeCombatSettlementKind
                or CampaignHostAcceptedStep.ContactCombatCustodyKind
                or CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind)) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var root = document.RootElement;
            if (step.Kind is CampaignHostAcceptedStep.NativeCombatSettlementKind)
            {
                if (root.GetProperty("pendingCaptives").GetArrayLength() == 0) continue;
                var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(step.EventRecords[0]));
                settlements.Add(hash, checked(root.GetProperty("priorStateVersion").GetInt64() + 1));
                continue;
            }
            var settlement = root.GetProperty("settlementHash").GetString()!;
            if (!settlements.ContainsKey(settlement))
                throw new InvalidOperationException("Custody ending has no pending settlement.");
            endings.Add(settlement, step.Kind == CampaignHostAcceptedStep.ContactCombatCustodyKind ? "completed" : "timed-out");
            if (step.Kind != CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind) continue;
            var evidence = (root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64());
            timeouts.Add(settlement, evidence);
            if (root.GetProperty("reason").GetString() == "custody-unavailable")
            {
                if (evidence.Item2 != -1 || evidence.Item3 != -1 || evidence.Item4 != -1
                    || evidence.Item1 != (timing?.Hash ?? CampaignHostAdapter.NoTimingCustodyTimeoutHash))
                    throw new InvalidOperationException("Unavailable custody timeout claimed clock evidence.");
                unavailable.Add(settlement);
            }
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT settlement_hash,entry_state_version,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM custody_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new CustodyRound(campaignId, reader.GetString(0), reader.GetInt64(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
            if (reader.GetString(7) != CustodyRoundHash(round) || timing is null
                || !settlements.TryGetValue(round.SettlementHash, out var stateVersion)
                || unavailable.Contains(round.SettlementHash))
                throw new InvalidOperationException("Retained custody round changed.");
            RequireCustodyRoundBinding(round, stateVersion, timing);
            if (round.Status != (endings.GetValueOrDefault(round.SettlementHash) ?? "collecting"))
                throw new InvalidOperationException("Custody round status differs from accepted events.");
            if (timeouts.TryGetValue(round.SettlementHash, out var timeout)
                && timeout != (round.ConfigurationHash, round.OpenedMs, round.DeadlineMs, round.HighWaterMs))
                throw new InvalidOperationException("Custody timeout differs from retained round.");
            found.Add(round.SettlementHash);
        }
        if (settlements.Keys.Any(hash => !found.Contains(hash) && !unavailable.Contains(hash)))
            throw new InvalidOperationException("Pending custody has no retained custody round.");
    }
}
