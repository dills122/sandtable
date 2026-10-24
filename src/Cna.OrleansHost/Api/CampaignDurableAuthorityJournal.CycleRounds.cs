using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record CycleRound(string CampaignId, string ReleaseEventHash,
        long EntryStateVersion, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private void InitializeCycleRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCycleControlRoundBinding(authority.Session);
        if (authority.Timing is null || !TryTrustedNow(out var opened))
        {
            PersistCycleTimeout(connection, transaction, authority, campaignId,
                -1, -1, -1, null, "cycle-control-unavailable");
            _ = Load(connection, transaction, campaignId);
            return;
        }
        var round = new CycleRound(campaignId, binding.ReleaseEventHash, binding.StateVersion,
            authority.Timing.Hash, opened, checked(opened + authority.Timing.Budget("cycle-control")),
            opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO cycle_rounds VALUES ($id,$release,$version,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindCycleRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private static CycleRound ReadActiveCycleRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCycleControlRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT release_event_hash,entry_state_version,configuration_hash,opened_ms,
              deadline_ms,high_water_ms,status,record_hash FROM cycle_rounds
            WHERE campaign_id=$id AND release_event_hash=$release
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$release", binding.ReleaseEventHash);
        using var reader = query.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Cycle round is missing.");
        var round = new CycleRound(campaignId, reader.GetString(0), reader.GetInt64(1),
            reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
        if (reader.GetString(7) != CycleRoundHash(round) || authority.Timing is null)
            throw new InvalidOperationException("Retained cycle round changed.");
        RequireCycleRoundBinding(round, binding.StateVersion, authority.Timing);
        if (round.Status != "collecting") throw new InvalidOperationException("Cycle round is closed.");
        return round;
    }

    private static void PersistCycleTimeout(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId, long opened, long deadline, long highWater,
        long? cancelledAt, string reason)
    {
        var accepted = CampaignHostAdapter.TimeoutNativeCycleControl(authority.Session,
            authority.Timing, opened, deadline, highWater, cancelledAt, reason);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Core rejected trusted cycle timeout.");
        var command = CampaignHostAdapter.NativeCycleControlTimeoutCommand(authority.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, authority,
            CampaignHostAcceptedStep.NativeCycleControlTimeoutKind,
            null, null, null, null, command, accepted);
    }

    private void TimeoutCycleRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, CycleRound round, string reason, long? cancelledAt)
    {
        PersistCycleTimeout(connection, transaction, authority, round.CampaignId,
            round.OpenedMs, round.DeadlineMs, round.HighWaterMs, cancelledAt, reason);
        UpdateCycleRound(connection, transaction, round with { Status = "timed-out" });
        _ = Load(connection, transaction, round.CampaignId);
    }

    internal bool TickCycleRound(string campaignId)
    {
        if (schemaVersion != SchemaVersion) return false;
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        try { _ = CampaignHostAdapter.QueryNativeCycleControlRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return false; }
        var round = ReadActiveCycleRound(connection, transaction, authority, campaignId);
        var gate = GateCycleRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
            TimeoutCycleRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
        transaction.Commit();
        return gate.CancellationReason is not null;
    }

    internal IReadOnlyList<string> CollectingCycleCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM cycle_rounds
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

    private static void RequireCycleRoundBinding(CycleRound round, long entryStateVersion,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.EntryStateVersion != entryStateVersion
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("cycle-control")
            || round.HighWaterMs < round.OpenedMs || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "timed-out"))
            throw new InvalidOperationException("Cycle round differs from campaign authority.");
    }

    private (CycleRound Round, string? CancellationReason, long? CancelledAt)
        GateCycleRound(SqliteConnection connection, SqliteTransaction transaction, CycleRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateCycleRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private static void UpdateCycleRound(SqliteConnection connection, SqliteTransaction transaction,
        CycleRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE cycle_rounds SET high_water_ms=$highWater,status=$status,record_hash=$hash
            WHERE campaign_id=$id AND release_event_hash=$release
            """;
        BindCycleRound(update, round);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Cycle round is missing.");
    }

    private static void BindCycleRound(SqliteCommand command, CycleRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$release", round.ReleaseEventHash);
        command.Parameters.AddWithValue("$version", round.EntryStateVersion);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", CycleRoundHash(round));
    }

    private static string CycleRoundHash(CycleRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-cycle-round-v1");
            writer.Write(round.CampaignId);
            writer.Write(round.ReleaseEventHash);
            writer.Write(round.EntryStateVersion);
            writer.Write(round.ConfigurationHash);
            writer.Write(round.OpenedMs);
            writer.Write(round.DeadlineMs);
            writer.Write(round.HighWaterMs);
            writer.Write(round.Status);
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private void VerifyCycleRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < CycleSchemaVersion) return;
        var releases = new Dictionary<string, long>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);
        var timeouts = new Dictionary<string, (string Config, long Opened, long Deadline, long HighWater)>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (step.Kind is not (CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
                or CampaignHostAcceptedStep.ContactCycleControlKind
                or CampaignHostAcceptedStep.NativeCycleControlTimeoutKind)) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var root = document.RootElement;
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind)
            {
                var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(step.EventRecords[0]));
                releases.Add(hash, checked(root.GetProperty("priorStateVersion").GetInt64() + 1));
                continue;
            }
            var release = root.GetProperty("priorEventHash").GetString()!;
            if (!releases.ContainsKey(release))
                throw new InvalidOperationException("Cycle ending has no completed Reserve Release.");
            endings.Add(release, step.Kind == CampaignHostAcceptedStep.ContactCycleControlKind ? "completed" : "timed-out");
            if (step.Kind != CampaignHostAcceptedStep.NativeCycleControlTimeoutKind) continue;
            var evidence = (root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64());
            timeouts.Add(release, evidence);
            if (root.GetProperty("reason").GetString() == "cycle-control-unavailable")
            {
                if (evidence.Item2 != -1 || evidence.Item3 != -1 || evidence.Item4 != -1
                    || evidence.Item1 != (timing?.Hash ?? CampaignHostAdapter.NoTimingCycleControlTimeoutHash))
                    throw new InvalidOperationException("Unavailable cycle timeout claimed clock evidence.");
                unavailable.Add(release);
            }
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT release_event_hash,entry_state_version,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM cycle_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new CycleRound(campaignId, reader.GetString(0), reader.GetInt64(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
            if (reader.GetString(7) != CycleRoundHash(round) || timing is null
                || !releases.TryGetValue(round.ReleaseEventHash, out var stateVersion)
                || unavailable.Contains(round.ReleaseEventHash))
                throw new InvalidOperationException("Retained cycle round changed.");
            RequireCycleRoundBinding(round, stateVersion, timing);
            if (round.Status != (endings.GetValueOrDefault(round.ReleaseEventHash) ?? "collecting"))
                throw new InvalidOperationException("Cycle round status differs from accepted events.");
            if (timeouts.TryGetValue(round.ReleaseEventHash, out var timeout)
                && timeout != (round.ConfigurationHash, round.OpenedMs, round.DeadlineMs, round.HighWaterMs))
                throw new InvalidOperationException("Cycle timeout differs from retained round.");
            found.Add(round.ReleaseEventHash);
        }
        if (releases.Keys.Any(hash => !found.Contains(hash) && !unavailable.Contains(hash)))
            throw new InvalidOperationException("Completed Reserve Release has no retained cycle round.");
    }
}
