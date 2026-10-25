using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record SelectionRound(string CampaignId, string EntryHash,
        long EntryStateVersion, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private void InitializeSelectionRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(authority.Session);
        if (authority.Timing is null || !TryTrustedNow(out var opened))
        {
            var completed = PersistSelectionTimeout(connection, transaction, authority, campaignId,
                -1, -1, -1, null, "selection-unavailable");
            AppendSelectionNoAttack(connection, transaction, campaignId, completed);
            return;
        }
        var round = new SelectionRound(campaignId, binding.EntryHash, binding.StateVersion,
            authority.Timing.Hash, opened, checked(opened + authority.Timing.Budget("selection")),
            opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO selection_rounds VALUES ($id,$entry,$version,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindSelectionRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private static SelectionRound ReadActiveSelectionRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId)
    {
        var binding = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(authority.Session);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT entry_hash,entry_state_version,configuration_hash,opened_ms,
              deadline_ms,high_water_ms,status,record_hash FROM selection_rounds
            WHERE campaign_id=$id AND entry_hash=$entry
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$entry", binding.EntryHash);
        using var reader = query.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Selection round is missing.");
        var round = new SelectionRound(campaignId, reader.GetString(0), reader.GetInt64(1),
            reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
        if (reader.GetString(7) != SelectionRoundHash(round) || authority.Timing is null)
            throw new InvalidOperationException("Retained selection round changed.");
        RequireSelectionRoundBinding(round, binding.StateVersion, authority.Timing);
        if (round.Status != "collecting") throw new InvalidOperationException("Selection round is closed.");
        return round;
    }

    private static Loaded PersistSelectionTimeout(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId, long opened, long deadline, long highWater,
        long? cancelledAt, string reason)
    {
        var accepted = CampaignHostAdapter.TimeoutNativeCombatSelection(authority.Session,
            authority.Timing, opened, deadline, highWater, cancelledAt, reason);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Core rejected trusted selection timeout.");
        var command = CampaignHostAdapter.NativeCombatSelectionTimeoutCommand(authority.Session, accepted);
        return PersistAccepted(connection, transaction, campaignId, authority,
            CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind,
            null, null, null, null, command, accepted);
    }

    private void TimeoutSelectionRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, SelectionRound round, string reason, long? cancelledAt)
    {
        var completed = PersistSelectionTimeout(connection, transaction, authority, round.CampaignId,
            round.OpenedMs, round.DeadlineMs, round.HighWaterMs, cancelledAt, reason);
        UpdateSelectionRound(connection, transaction, round with { Status = "timed-out" });
        AppendSelectionNoAttack(connection, transaction, round.CampaignId, completed);
    }

    internal bool TickSelectionRound(string campaignId)
    {
        if (schemaVersion != SchemaVersion) return false;
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        try { _ = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return false; }
        var round = ReadActiveSelectionRound(connection, transaction, authority, campaignId);
        var gate = GateSelectionRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
            TimeoutSelectionRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
        transaction.Commit();
        return gate.CancellationReason is not null;
    }

    internal IReadOnlyList<string> CollectingSelectionCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM selection_rounds
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

    private static void RequireSelectionRoundBinding(SelectionRound round, long entryStateVersion,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.EntryStateVersion != entryStateVersion
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("selection")
            || round.HighWaterMs < round.OpenedMs || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "timed-out"))
            throw new InvalidOperationException("Selection round differs from campaign authority.");
    }

    private (SelectionRound Round, string? CancellationReason, long? CancelledAt)
        GateSelectionRound(SqliteConnection connection, SqliteTransaction transaction, SelectionRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateSelectionRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private static void UpdateSelectionRound(SqliteConnection connection, SqliteTransaction transaction,
        SelectionRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE selection_rounds SET high_water_ms=$highWater,status=$status,record_hash=$hash
            WHERE campaign_id=$id AND entry_hash=$entry
            """;
        BindSelectionRound(update, round);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Selection round is missing.");
    }

    private static void BindSelectionRound(SqliteCommand command, SelectionRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$entry", round.EntryHash);
        command.Parameters.AddWithValue("$version", round.EntryStateVersion);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", SelectionRoundHash(round));
    }

    private static string SelectionRoundHash(SelectionRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-selection-round-v1");
            writer.Write(round.CampaignId);
            writer.Write(round.EntryHash);
            writer.Write(round.EntryStateVersion);
            writer.Write(round.ConfigurationHash);
            writer.Write(round.OpenedMs);
            writer.Write(round.DeadlineMs);
            writer.Write(round.HighWaterMs);
            writer.Write(round.Status);
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private void TryInitializeSelectionRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, string campaignId)
    {
        try { _ = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(authority.Session); }
        catch (InvalidOperationException) { return; }
        InitializeSelectionRound(connection, transaction, authority, campaignId);
    }

    private void AppendSelectionNoAttack(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, Loaded authority)
    {
        var repeated = CampaignHostAdapter.IsNativeRepeatCombatVoluntaryFinish(authority.Session);
        var accepted = repeated
            ? CampaignHostAdapter.CompleteNativeRepeatCombatNoAttack(authority.Session)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(authority.Session);
        if (!accepted.IsAccepted) throw new InvalidOperationException("Selection timeout cannot complete no-attack Combat.");
        var command = repeated
            ? CampaignHostAdapter.NativeRepeatCombatNoAttackCommand(authority.Session, accepted)
            : CampaignHostAdapter.NativeCombatNoAttackCommand(authority.Session, accepted);
        var completed = PersistAccepted(connection, transaction, campaignId, authority,
            repeated ? CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
                : CampaignHostAcceptedStep.NativeCombatNoAttackKind,
            null, null, null, null, command, accepted);
        AppendEmptyReserveRelease(connection, transaction, campaignId, completed);
    }

    private void VerifySelectionRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < SelectionSchemaVersion) return;
        var results = new Dictionary<string, long>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);
        var timeouts = new Dictionary<string, (string Config, long Opened, long Deadline, long HighWater)>(StringComparer.Ordinal);
        string? pending = null;
        foreach (var step in steps)
        {
            var entry = CampaignHostAdapter.QueryNativeCombatSelectionEntryBinding(step);
            if (entry is { } binding)
            {
                if (pending is not null) throw new InvalidOperationException("Selection entry overlaps pending round.");
                results.Add(binding.EntryHash, binding.StateVersion);
                pending = binding.EntryHash;
                continue;
            }
            if (pending is null)
            {
                if (step.Kind == CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind)
                    throw new InvalidOperationException("Selection timeout has no pending entry.");
                continue;
            }
            if (step.Kind is not (CampaignHostAcceptedStep.ContactCombatSelectionKind
                or CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind
                or CampaignHostAcceptedStep.LegacyKind
                or CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind))
                throw new InvalidOperationException("Pending Selection advanced without completion.");
            var result = pending;
            pending = null;
            endings.Add(result, step.Kind == CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind ? "timed-out" : "completed");
            if (step.Kind != CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var root = document.RootElement;
            if (root.GetProperty("entryHash").GetString() != result)
                throw new InvalidOperationException("Selection timeout differs from entry.");
            var evidence = (root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64());
            timeouts.Add(result, evidence);
            if (root.GetProperty("reason").GetString() == "selection-unavailable")
            {
                if (evidence.Item2 != -1 || evidence.Item3 != -1 || evidence.Item4 != -1
                    || evidence.Item1 != (timing?.Hash ?? CampaignHostAdapter.NoTimingSelectionTimeoutHash))
                    throw new InvalidOperationException("Unavailable selection timeout claimed clock evidence.");
                unavailable.Add(result);
            }
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT entry_hash,entry_state_version,configuration_hash,opened_ms,deadline_ms,
              high_water_ms,status,record_hash FROM selection_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new SelectionRound(campaignId, reader.GetString(0), reader.GetInt64(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
            if (reader.GetString(7) != SelectionRoundHash(round) || timing is null
                || !results.TryGetValue(round.EntryHash, out var stateVersion)
                || unavailable.Contains(round.EntryHash))
                throw new InvalidOperationException("Retained selection round changed.");
            RequireSelectionRoundBinding(round, stateVersion, timing);
            if (round.Status != (endings.GetValueOrDefault(round.EntryHash) ?? "collecting"))
                throw new InvalidOperationException("Selection round status differs from accepted events.");
            if (timeouts.TryGetValue(round.EntryHash, out var timeout)
                && timeout != (round.ConfigurationHash, round.OpenedMs, round.DeadlineMs, round.HighWaterMs))
                throw new InvalidOperationException("Selection timeout differs from retained round.");
            found.Add(round.EntryHash);
        }
        if (results.Keys.Any(hash => !found.Contains(hash) && !unavailable.Contains(hash)))
            throw new InvalidOperationException("Pending selection has no retained selection round.");
    }
}
