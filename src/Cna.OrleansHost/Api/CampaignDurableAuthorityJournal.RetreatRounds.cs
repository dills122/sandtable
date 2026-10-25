using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record RetreatRound(string CampaignId, string ResultHash,
        long EntryStateVersion, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private void InitializeRetreatRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(authority.Session);
        if (authority.Timing is null || !TryTrustedNow(out var opened))
        {
            var completed = PersistRetreatTimeout(connection, transaction, authority, campaignId,
                -1, -1, -1, null, "retreat-unavailable");
            AppendWorldSettlement(connection, transaction, campaignId, completed);
            return;
        }
        var round = new RetreatRound(campaignId, binding.ResultHash, binding.StateVersion,
            authority.Timing.Hash, opened, checked(opened + authority.Timing.Budget("retreat")),
            opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO retreat_rounds VALUES ($id,$result,$version,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindRetreatRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private static RetreatRound ReadActiveRetreatRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT result_hash,entry_state_version,configuration_hash,opened_ms,
              deadline_ms,high_water_ms,status,record_hash FROM retreat_rounds
            WHERE campaign_id=$id AND result_hash=$result
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$result", binding.ResultHash);
        using var reader = query.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Retreat round is missing.");
        var round = new RetreatRound(campaignId, reader.GetString(0), reader.GetInt64(1),
            reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
        if (reader.GetString(7) != RetreatRoundHash(round) || authority.Timing is null)
            throw new InvalidOperationException("Retained retreat round changed.");
        RequireRetreatRoundBinding(round, binding.StateVersion, authority.Timing);
        if (round.Status != "collecting") throw new InvalidOperationException("Retreat round is closed.");
        return round;
    }

    private static Loaded PersistRetreatTimeout(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId, long opened, long deadline, long highWater,
        long? cancelledAt, string reason)
    {
        var accepted = CampaignHostAdapter.TimeoutNativeCombatRetreat(authority.Session,
            authority.Timing, opened, deadline, highWater, cancelledAt, reason);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Core rejected trusted retreat timeout.");
        var command = CampaignHostAdapter.NativeCombatRetreatTimeoutCommand(authority.Session, accepted);
        return PersistAccepted(connection, transaction, campaignId, authority,
            CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind,
            null, null, null, null, command, accepted);
    }

    private void TimeoutRetreatRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, RetreatRound round, string reason, long? cancelledAt)
    {
        var completed = PersistRetreatTimeout(connection, transaction, authority, round.CampaignId,
            round.OpenedMs, round.DeadlineMs, round.HighWaterMs, cancelledAt, reason);
        UpdateRetreatRound(connection, transaction, round with { Status = "timed-out" });
        AppendWorldSettlement(connection, transaction, round.CampaignId, completed);
    }

    internal bool TickRetreatRound(string campaignId)
    {
        if (schemaVersion != SchemaVersion) return false;
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        try { _ = CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return false; }
        var round = ReadActiveRetreatRound(connection, transaction, authority, campaignId);
        var gate = GateRetreatRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
            TimeoutRetreatRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
        transaction.Commit();
        return gate.CancellationReason is not null;
    }

    internal IReadOnlyList<string> CollectingRetreatCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM retreat_rounds
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

    private static void RequireRetreatRoundBinding(RetreatRound round, long entryStateVersion,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.EntryStateVersion != entryStateVersion
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("retreat")
            || round.HighWaterMs < round.OpenedMs || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "timed-out"))
            throw new InvalidOperationException("Retreat round differs from campaign authority.");
    }

    private (RetreatRound Round, string? CancellationReason, long? CancelledAt)
        GateRetreatRound(SqliteConnection connection, SqliteTransaction transaction, RetreatRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateRetreatRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private static void UpdateRetreatRound(SqliteConnection connection, SqliteTransaction transaction,
        RetreatRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE retreat_rounds SET high_water_ms=$highWater,status=$status,record_hash=$hash
            WHERE campaign_id=$id AND result_hash=$result
            """;
        BindRetreatRound(update, round);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Retreat round is missing.");
    }

    private static void BindRetreatRound(SqliteCommand command, RetreatRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$result", round.ResultHash);
        command.Parameters.AddWithValue("$version", round.EntryStateVersion);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", RetreatRoundHash(round));
    }

    private static string RetreatRoundHash(RetreatRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-retreat-round-v1");
            writer.Write(round.CampaignId);
            writer.Write(round.ResultHash);
            writer.Write(round.EntryStateVersion);
            writer.Write(round.ConfigurationHash);
            writer.Write(round.OpenedMs);
            writer.Write(round.DeadlineMs);
            writer.Write(round.HighWaterMs);
            writer.Write(round.Status);
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private void VerifyRetreatRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < RetreatSchemaVersion) return;
        var results = new Dictionary<string, long>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);
        var timeouts = new Dictionary<string, (string Config, long Opened, long Deadline, long HighWater)>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (step.Kind is not (CampaignHostAcceptedStep.NativeCombatAssaultResultKind
                or CampaignHostAcceptedStep.ContactPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind)) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var root = document.RootElement;
            if (step.Kind is CampaignHostAcceptedStep.NativeCombatAssaultResultKind)
            {
                if (root.GetProperty("facts").GetProperty("requiredRetreat").GetInt32() == 0) continue;
                var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(step.EventRecords[0]));
                results.Add(hash, checked(root.GetProperty("priorStateVersion").GetInt64() + 1));
                continue;
            }
            var result = root.GetProperty("resultHash").GetString()!;
            if (!results.ContainsKey(result))
                throw new InvalidOperationException("Retreat ending has no pending result.");
            endings.Add(result, step.Kind == CampaignHostAcceptedStep.ContactPostAssaultRetreatKind ? "completed" : "timed-out");
            if (step.Kind != CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind) continue;
            var evidence = (root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64());
            timeouts.Add(result, evidence);
            if (root.GetProperty("reason").GetString() == "retreat-unavailable")
            {
                if (evidence.Item2 != -1 || evidence.Item3 != -1 || evidence.Item4 != -1
                    || evidence.Item1 != (timing?.Hash ?? CampaignHostAdapter.NoTimingRetreatTimeoutHash))
                    throw new InvalidOperationException("Unavailable retreat timeout claimed clock evidence.");
                unavailable.Add(result);
            }
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT result_hash,entry_state_version,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM retreat_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new RetreatRound(campaignId, reader.GetString(0), reader.GetInt64(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
            if (reader.GetString(7) != RetreatRoundHash(round) || timing is null
                || !results.TryGetValue(round.ResultHash, out var stateVersion)
                || unavailable.Contains(round.ResultHash))
                throw new InvalidOperationException("Retained retreat round changed.");
            RequireRetreatRoundBinding(round, stateVersion, timing);
            if (round.Status != (endings.GetValueOrDefault(round.ResultHash) ?? "collecting"))
                throw new InvalidOperationException("Retreat round status differs from accepted events.");
            if (timeouts.TryGetValue(round.ResultHash, out var timeout)
                && timeout != (round.ConfigurationHash, round.OpenedMs, round.DeadlineMs, round.HighWaterMs))
                throw new InvalidOperationException("Retreat timeout differs from retained round.");
            found.Add(round.ResultHash);
        }
        if (results.Keys.Any(hash => !found.Contains(hash) && !unavailable.Contains(hash)))
            throw new InvalidOperationException("Pending retreat has no retained retreat round.");
    }
}
