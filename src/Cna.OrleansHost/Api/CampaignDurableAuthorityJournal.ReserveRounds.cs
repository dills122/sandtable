using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record ReserveRound(string CampaignId, string ReleaseEventHash,
        long EntryStateVersion, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private void InitializeReserveRoundIfMissing(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT COUNT(*) FROM reserve_rounds WHERE campaign_id=$id AND release_event_hash=$release";
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$release", binding.ReleaseEventHash);
        if ((long)query.ExecuteScalar()! != 0)
        {
            _ = ReadActiveReserveRound(connection, transaction, authority, campaignId);
            _ = Load(connection, transaction, campaignId);
            return;
        }
        InitializeReserveRound(connection, transaction, authority, campaignId);
    }

    private void InitializeReserveRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(authority.Session);
        if (authority.Timing is null || !TryTrustedNow(out var opened))
        {
            var completedHead = PersistReserveTimeout(connection, transaction, authority, campaignId,
                -1, -1, -1, null, "reserve-release-unavailable");
            InitializeCycleRound(connection, transaction, completedHead, campaignId);
            return;
        }
        var round = new ReserveRound(campaignId, binding.ReleaseEventHash, binding.StateVersion,
            authority.Timing.Hash, opened, checked(opened + authority.Timing.Budget("reserve-release")),
            opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO reserve_rounds VALUES ($id,$release,$version,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindReserveRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private static ReserveRound ReadActiveReserveRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT release_event_hash,entry_state_version,configuration_hash,opened_ms,
              deadline_ms,high_water_ms,status,record_hash FROM reserve_rounds
            WHERE campaign_id=$id AND release_event_hash=$release
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$release", binding.ReleaseEventHash);
        using var reader = query.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Reserve round is missing.");
        var round = new ReserveRound(campaignId, reader.GetString(0), reader.GetInt64(1),
            reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
        if (reader.GetString(7) != ReserveRoundHash(round) || authority.Timing is null)
            throw new InvalidOperationException("Retained reserve round changed.");
        RequireReserveRoundBinding(round, round.EntryStateVersion, authority.Timing);
        if (round.EntryStateVersion > binding.StateVersion)
            throw new InvalidOperationException("Reserve entry is after current authority.");
        if (round.Status != "collecting") throw new InvalidOperationException("Reserve round is closed.");
        return round;
    }

    private static Loaded PersistReserveTimeout(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId, long opened, long deadline, long highWater,
        long? cancelledAt, string reason)
    {
        var accepted = CampaignHostAdapter.TimeoutNativeReserveRelease(authority.Session,
            authority.Timing, opened, deadline, highWater, cancelledAt, reason);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Core rejected trusted reserve timeout.");
        var command = CampaignHostAdapter.NativeReserveReleaseTimeoutCommand(authority.Session, accepted);
        return PersistAccepted(connection, transaction, campaignId, authority,
            CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind,
            null, null, null, null, command, accepted);
    }

    private void TimeoutReserveRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, ReserveRound round, string reason, long? cancelledAt)
    {
        var completedHead = PersistReserveTimeout(connection, transaction, authority, round.CampaignId,
            round.OpenedMs, round.DeadlineMs, round.HighWaterMs, cancelledAt, reason);
        UpdateReserveRound(connection, transaction, round with { Status = "timed-out" });
        InitializeCycleRound(connection, transaction, completedHead, round.CampaignId);
    }

    internal bool TickReserveRound(string campaignId)
    {
        if (schemaVersion != SchemaVersion) return false;
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        try { _ = CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return false; }
        var round = ReadActiveReserveRound(connection, transaction, authority, campaignId);
        var gate = GateReserveRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
            TimeoutReserveRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
        transaction.Commit();
        return gate.CancellationReason is not null;
    }

    internal IReadOnlyList<string> CollectingReserveCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM reserve_rounds
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

    private static void RequireReserveRoundBinding(ReserveRound round, long entryStateVersion,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.EntryStateVersion != entryStateVersion
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("reserve-release")
            || round.HighWaterMs < round.OpenedMs || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "timed-out"))
            throw new InvalidOperationException("Reserve round differs from campaign authority.");
    }

    private (ReserveRound Round, string? CancellationReason, long? CancelledAt)
        GateReserveRound(SqliteConnection connection, SqliteTransaction transaction, ReserveRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateReserveRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private static void UpdateReserveRound(SqliteConnection connection, SqliteTransaction transaction,
        ReserveRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE reserve_rounds SET high_water_ms=$highWater,status=$status,record_hash=$hash
            WHERE campaign_id=$id AND release_event_hash=$release
            """;
        BindReserveRound(update, round);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Reserve round is missing.");
    }

    private static void BindReserveRound(SqliteCommand command, ReserveRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$release", round.ReleaseEventHash);
        command.Parameters.AddWithValue("$version", round.EntryStateVersion);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", ReserveRoundHash(round));
    }

    private static string ReserveRoundHash(ReserveRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-reserve-round-v1");
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


    private void VerifyReserveRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < ReserveSchemaVersion) return;
        var entries = new Dictionary<string, long>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var empty = new HashSet<string>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);
        var timeouts = new Dictionary<string, (string Config, long Opened, long Deadline, long HighWater)>(StringComparer.Ordinal);
        string? active = null;
        var chosen = false;
        foreach (var step in steps)
        {
            var isEntry = step.Kind is CampaignHostAcceptedStep.NativeCombatNoAttackKind
                or CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
                or CampaignHostAcceptedStep.NativeCombatRbaCancellationKind
                or CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
                or CampaignHostAcceptedStep.NativeCombatSuccessorKind;
            if (!isEntry && step.Kind is not (CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind
                or CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
                or CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind)) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var root = document.RootElement;
            if (isEntry)
            {
                if (active is not null) throw new InvalidOperationException("Reserve entry overlaps unresolved release.");
                active = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(step.EventRecords[0]));
                entries.Add(active, checked(root.GetProperty("priorStateVersion").GetInt64() + 1));
                chosen = false;
                continue;
            }
            if (active is null) throw new InvalidOperationException("Reserve disposition has no entry.");
            if (step.Kind == CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind)
            {
                chosen = true;
                continue;
            }
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind)
            {
                endings.Add(active, "completed");
                if (!chosen) empty.Add(active);
            }
            else
            {
                if (root.GetProperty("releaseEventHash").GetString() != active)
                    throw new InvalidOperationException("Reserve timeout has a foreign entry.");
                endings.Add(active, "timed-out");
                var evidence = (root.GetProperty("configurationHash").GetString()!,
                    root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                    root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                    root.GetProperty("highWaterUnixMilliseconds").GetInt64());
                timeouts.Add(active, evidence);
                if (root.GetProperty("reason").GetString() == "reserve-release-unavailable")
                {
                    if (chosen || evidence.Item2 != -1 || evidence.Item3 != -1 || evidence.Item4 != -1
                        || evidence.Item1 != (timing?.Hash ?? CampaignHostAdapter.NoTimingReserveReleaseTimeoutHash))
                        throw new InvalidOperationException("Unavailable Reserve timeout claimed clock evidence.");
                    unavailable.Add(active);
                }
            }
            active = null;
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT release_event_hash,entry_state_version,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM reserve_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new ReserveRound(campaignId, reader.GetString(0), reader.GetInt64(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
            if (reader.GetString(7) != ReserveRoundHash(round) || timing is null
                || !entries.TryGetValue(round.ReleaseEventHash, out var stateVersion)
                || unavailable.Contains(round.ReleaseEventHash) || empty.Contains(round.ReleaseEventHash))
                throw new InvalidOperationException("Retained Reserve round changed.");
            RequireReserveRoundBinding(round, stateVersion, timing);
            if (round.Status != (endings.GetValueOrDefault(round.ReleaseEventHash) ?? "collecting"))
                throw new InvalidOperationException("Reserve round status differs from accepted events.");
            if (timeouts.TryGetValue(round.ReleaseEventHash, out var timeout)
                && timeout != (round.ConfigurationHash, round.OpenedMs, round.DeadlineMs, round.HighWaterMs))
                throw new InvalidOperationException("Reserve timeout differs from retained round.");
            found.Add(round.ReleaseEventHash);
        }
        if (entries.Keys.Any(hash => !found.Contains(hash) && !unavailable.Contains(hash) && !empty.Contains(hash)))
            throw new InvalidOperationException("Reserve Release has no retained round.");
    }
}
