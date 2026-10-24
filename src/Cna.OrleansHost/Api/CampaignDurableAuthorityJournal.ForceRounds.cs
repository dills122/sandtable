using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private sealed record ForceRound(string CampaignId, string DecisionActionId,
        string DeclineEventHash, string ConfigurationHash, long OpenedMs,
        long DeadlineMs, long HighWaterMs, string Status);

    private ForceRound ReadActiveForceRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId)
    {
        if (schemaVersion != SchemaVersion || authority.Timing is null)
            throw new InvalidOperationException("Force round requires current durable timing authority.");
        var binding = CampaignHostAdapter.QueryNativeCombatForceRoundBinding(authority.Session);
        ForceRound? round = null;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = """
                SELECT decision_action_id,decline_event_hash,configuration_hash,opened_ms,deadline_ms,
                  high_water_ms,status,record_hash FROM force_rounds
                WHERE campaign_id=$id AND decline_event_hash=$decline
                """;
            query.Parameters.AddWithValue("$id", campaignId);
            query.Parameters.AddWithValue("$decline", binding.DeclineEventHash);
            using var reader = query.ExecuteReader();
            if (reader.Read())
            {
                round = new ForceRound(campaignId, reader.GetString(0),
                    reader.GetString(1), reader.GetString(2), reader.GetInt64(3),
                    reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6));
                if (reader.GetString(7) != ForceRoundHash(round))
                    throw new InvalidOperationException("Retained Force round changed.");
            }
        }
        if (round is null)
            throw new InvalidOperationException("Force round is missing at decision entry.");
        RequireForceRoundBinding(round, binding.DecisionActionId,
            binding.DeclineEventHash, authority.Timing);
        if (round.Status != "collecting")
            throw new InvalidOperationException("Force round is closed.");
        return round;
    }

    private static bool ForceRoundExists(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId, string declineEventHash)
    {
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT 1 FROM force_rounds WHERE campaign_id=$id AND decline_event_hash=$decline
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$decline", declineEventHash);
        return query.ExecuteScalar() is not null;
    }

    private void CancelForceRound(SqliteConnection connection, SqliteTransaction transaction,
        Loaded authority, ForceRound round, string reason, long? cancelledAt)
    {
        var accepted = CampaignHostAdapter.CancelNativeCombatForceAssignment(authority.Session,
            authority.Timing!, round.OpenedMs, round.DeadlineMs, round.HighWaterMs,
            cancelledAt, reason);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Core rejected the trusted Force cancellation.");
        var command = CampaignHostAdapter.NativeCombatForceCancellationCommand(
            authority.Session, accepted);
        PersistAccepted(connection, transaction, round.CampaignId, authority,
            CampaignHostAcceptedStep.NativeCombatForceCancellationKind,
            null, null, null, null, command, accepted);
        UpdateForceRound(connection, transaction, round with { Status = "cancelled" });
        AppendEmptyAntiArmor(connection, transaction, round.CampaignId);
    }

    private void InitializeForceRound(SqliteConnection connection,
        SqliteTransaction transaction, Loaded authority, string campaignId, long opened)
    {
        if (CampaignHostAdapter.IsInitialNativeCombatForceRound(authority.Session)
            && (authority.Timing is null
                || !CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(authority.Session)))
        {
            // Unavailable before opening: retain absent clock evidence, not a fabricated deadline.
            var cancelled = CampaignHostAdapter.CancelNativeCombatForceAssignment(
                authority.Session, authority.Timing, -1, -1, -1, null, "force-unavailable");
            if (!cancelled.IsAccepted)
                throw new InvalidOperationException("Core rejected the unavailable Force continuation.");
            var command = CampaignHostAdapter.NativeCombatForceCancellationCommand(authority.Session, cancelled);
            PersistAccepted(connection, transaction, campaignId, authority,
                CampaignHostAcceptedStep.NativeCombatForceCancellationKind,
                null, null, null, null, command, cancelled);
            AppendEmptyAntiArmor(connection, transaction, campaignId);
            return;
        }
        if (authority.Timing is null || opened < 0)
            throw new InvalidOperationException("Force entry requires trusted timing authority.");
        var binding = CampaignHostAdapter.QueryNativeCombatForceRoundBinding(authority.Session);
        var round = new ForceRound(campaignId, binding.DecisionActionId,
            binding.DeclineEventHash, authority.Timing.Hash, opened,
            checked(opened + authority.Timing.Budget("force-assignment")), opened, "collecting");
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO force_rounds VALUES ($id,$decision,$decline,$config,$opened,
              $deadline,$highWater,$status,$hash)
            """;
        BindForceRound(insert, round);
        insert.ExecuteNonQuery();
        _ = Load(connection, transaction, campaignId);
    }

    private void AppendEmptyAntiArmor(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId)
    {
        var head = Load(connection, transaction, campaignId);
        var accepted = CampaignHostAdapter.CompleteNativeCombatAntiArmor(head.Session);
        if (!accepted.IsAccepted) return;
        var command = CampaignHostAdapter.NativeCombatAntiArmorCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.NativeCombatAntiArmorKind,
            null, null, null, null, command, accepted);
        var closeAssaultHead = Load(connection, transaction, campaignId);
        var cancelled = CampaignHostAdapter.CompleteNativeCombatCancelledAssault(
            closeAssaultHead.Session);
        if (cancelled.IsAccepted)
        {
            var cancelledCommand = CampaignHostAdapter.NativeCombatCancelledAssaultCommand(
                closeAssaultHead.Session, cancelled);
            var reserveHead = PersistAccepted(connection, transaction, campaignId, closeAssaultHead,
                CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind,
                null, null, null, null, cancelledCommand, cancelled);
            AppendEmptyReserveRelease(connection, transaction, campaignId, reserveHead);
            return;
        }
        var result = CampaignHostAdapter.ResolveNativeCombatAssault(closeAssaultHead.Session);
        if (!result.IsAccepted) return;
        var resultCommand = CampaignHostAdapter.NativeCombatAssaultResultCommand(
            closeAssaultHead.Session, result);
        PersistAccepted(connection, transaction, campaignId, closeAssaultHead,
            CampaignHostAcceptedStep.NativeCombatAssaultResultKind,
            null, null, null, null, resultCommand, result);
        if (CampaignHostAdapter.IsNativeCombatNoPostResultRetreat(result.SuccessorSession!))
        {
            var postResultHead = Load(connection, transaction, campaignId);
            var noRetreat = CampaignHostAdapter.CompleteNoPostAssaultRetreat(
                postResultHead.Session);
            if (!noRetreat.IsAccepted)
                throw new InvalidOperationException(
                    "No-retreat result cannot complete its normal System choice.");
            var noRetreatCommand = CampaignHostAdapter
                .NativeCombatNoPostAssaultRetreatCommand(
                    postResultHead.Session, noRetreat);
            PersistAccepted(connection, transaction, campaignId, postResultHead,
                CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind,
                null, null, null, null, noRetreatCommand, noRetreat);
            AppendWorldSettlement(connection, transaction, campaignId);
        }
    }

    private void AppendWorldSettlement(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId)
    {
        var head = Load(connection, transaction, campaignId);
        var settled = CampaignHostAdapter.SettleNativeCombatWorld(head.Session);
        if (!settled.IsAccepted)
            throw new InvalidOperationException(
                "Accepted Combat choice cannot settle normal World.");
        var command = CampaignHostAdapter.NativeCombatSettlementCommand(
            head.Session, settled);
        var custodyHead = PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.NativeCombatSettlementKind,
            null, null, null, null, command, settled);
        if (CampaignHostAdapter.IsNativeCombatNoCustody(settled.SuccessorSession!))
        {
            var completed = CampaignHostAdapter.CompleteNoNativeCombatCustody(
                custodyHead.Session);
            if (!completed.IsAccepted)
                throw new InvalidOperationException(
                    "No-captive settlement cannot complete its System disposition.");
            var custodyCommand = CampaignHostAdapter.NativeCombatNoCustodyCommand(
                custodyHead.Session, completed);
            var completedHead = PersistAccepted(connection, transaction, campaignId, custodyHead,
                CampaignHostAcceptedStep.NativeCombatNoCustodyKind,
                null, null, null, null, custodyCommand, completed);
            AppendNativeCombatSuccessor(connection, transaction, campaignId, completedHead);
        }
        else InitializeCustodyRound(connection, transaction, custodyHead, campaignId);
    }

    private void AppendNativeCombatSuccessor(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId, Loaded head)
    {
        var completed = CampaignHostAdapter.CompleteNativeCombatSuccessor(
            head.Session);
        if (!completed.IsAccepted)
            throw new InvalidOperationException(
                "Accepted Combat custody cannot resume the normal sequence.");
        var command = CampaignHostAdapter.NativeCombatSuccessorCommand(
            head.Session, completed);
        var reserveHead = PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.NativeCombatSuccessorKind,
            null, null, null, null, command, completed);
        AppendEmptyReserveRelease(connection, transaction, campaignId, reserveHead);
    }

    private void AppendEmptyReserveRelease(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId, Loaded reserveHead)
    {
        if (CampaignHostAdapter.IsNativeReserveReleaseEmpty(reserveHead.Session))
        {
            var released = CampaignHostAdapter.CompleteNativeReserveRelease(
                reserveHead.Session);
            if (!released.IsAccepted)
                throw new InvalidOperationException(
                    "Empty Reserve Release cannot finish after Combat.");
            var releaseCommand = CampaignHostAdapter.NativeReserveReleaseCompletionCommand(
                reserveHead.Session, released);
            var completedHead = PersistAccepted(connection, transaction, campaignId, reserveHead,
                CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind,
                null, null, null, null, releaseCommand, released);
            InitializeCycleRound(connection, transaction, completedHead, campaignId);
        }
        else InitializeReserveRoundIfMissing(connection, transaction, reserveHead, campaignId);
    }

    internal IReadOnlyList<string> CollectingForceCampaigns(string after, int limit)
    {
        if (schemaVersion != SchemaVersion) return [];
        using var connection = Connect(create: false);
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT DISTINCT campaign_id FROM force_rounds
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

    internal bool TickForceRound(string campaignId)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var authority = Load(connection, transaction, campaignId);
        if (schemaVersion != SchemaVersion || authority.Timing is null)
            return false;
        (string DecisionActionId, string DeclineEventHash) binding;
        try
        {
            binding = CampaignHostAdapter.QueryNativeCombatForceRoundBinding(authority.Session);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        if (!ForceRoundExists(connection, transaction, campaignId, binding.DeclineEventHash))
            return false;
        var round = ReadActiveForceRound(connection, transaction, authority, campaignId);
        var gate = GateForceRound(connection, transaction, round);
        if (gate.CancellationReason is { } reason)
        {
            CancelForceRound(connection, transaction, authority, gate.Round,
                reason, gate.CancelledAt);
            transaction.Commit();
            return true;
        }
        transaction.Commit();
        return false;
    }

    private static void RequireForceRoundBinding(ForceRound round, string decisionActionId,
        string declineHash,
        CampaignDecisionTimingConfigurationV1 timing)
    {
        if (round.DecisionActionId != decisionActionId
            || round.DeclineEventHash != declineHash
            || round.ConfigurationHash != timing.Hash
            || round.OpenedMs < 0 || round.DeadlineMs <= round.OpenedMs
            || round.DeadlineMs - round.OpenedMs != timing.Budget("force-assignment")
            || round.HighWaterMs < round.OpenedMs
            || round.HighWaterMs >= round.DeadlineMs
            || round.Status is not ("collecting" or "completed" or "cancelled"))
            throw new InvalidOperationException("Force round differs from campaign authority.");
    }

    private (ForceRound Round, string? CancellationReason, long? CancelledAt)
        GateForceRound(SqliteConnection connection, SqliteTransaction transaction,
            ForceRound round)
    {
        if (!TryTrustedNow(out var now)) return (round, "clock-unavailable", null);
        if (now < round.HighWaterMs) return (round, "clock-regression", now);
        if (now >= round.DeadlineMs) return (round, "deadline", now);
        if (now > round.HighWaterMs)
        {
            round = round with { HighWaterMs = now };
            UpdateForceRound(connection, transaction, round);
        }
        return (round, null, null);
    }

    private bool TryTrustedNow(out long now)
    {
        try
        {
            now = clock.GetUtcNow().ToUnixTimeMilliseconds();
            return now >= 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            now = 0;
            return false;
        }
    }

    private static void UpdateForceRound(SqliteConnection connection,
        SqliteTransaction transaction, ForceRound round)
    {
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE force_rounds SET high_water_ms=$highWater,status=$status,
              record_hash=$hash WHERE campaign_id=$id AND decline_event_hash=$decline
            """;
        BindForceRound(update, round);
        if (update.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Force round is missing.");
    }

    private static void BindForceRound(SqliteCommand command, ForceRound round)
    {
        command.Parameters.AddWithValue("$id", round.CampaignId);
        command.Parameters.AddWithValue("$decision", round.DecisionActionId);
        command.Parameters.AddWithValue("$decline", round.DeclineEventHash);
        command.Parameters.AddWithValue("$config", round.ConfigurationHash);
        command.Parameters.AddWithValue("$opened", round.OpenedMs);
        command.Parameters.AddWithValue("$deadline", round.DeadlineMs);
        command.Parameters.AddWithValue("$highWater", round.HighWaterMs);
        command.Parameters.AddWithValue("$status", round.Status);
        command.Parameters.AddWithValue("$hash", ForceRoundHash(round));
    }

    private static string ForceRoundHash(ForceRound round)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-force-round-v1");
            writer.Write(round.CampaignId);
            writer.Write(round.DecisionActionId);
            writer.Write(round.DeclineEventHash);
            writer.Write(round.ConfigurationHash);
            writer.Write(round.OpenedMs);
            writer.Write(round.DeadlineMs);
            writer.Write(round.HighWaterMs);
            writer.Write(round.Status);
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private void VerifyForceRounds(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, IReadOnlyList<CampaignHostAcceptedStep> steps,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        if (schemaVersion < ForceSchemaVersion) return;
        var declines = new Dictionary<string, string>(StringComparer.Ordinal);
        var barrageByDecision = new Dictionary<string, string>(StringComparer.Ordinal);
        var barrageByDecline = new Dictionary<string, string>(StringComparer.Ordinal);
        var activeByDecision = new Dictionary<string, string>(StringComparer.Ordinal);
        var endings = new Dictionary<string, string>(StringComparer.Ordinal);
        var requiredRounds = new HashSet<string>(StringComparer.Ordinal);
        var noRoundCancellations = new HashSet<string>(StringComparer.Ordinal);
        var cancellations = new Dictionary<string, (string ConfigurationHash,
            long OpenedMs, long DeadlineMs, long HighWaterMs)>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (step.Kind is not (CampaignHostAcceptedStep.NativeCombatBarrageKind
                or CampaignHostAcceptedStep.ContactRbaDeclineKind
                or CampaignHostAcceptedStep.ContactForceSealKind
                or CampaignHostAcceptedStep.NativeCombatForceCompletionKind
                or CampaignHostAcceptedStep.NativeCombatForceCancellationKind)) continue;
            using var document = JsonDocument.Parse(step.EventRecords[0]);
            var decision = document.RootElement.GetProperty("decisionActionId").GetString()!;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatBarrageKind)
            {
                barrageByDecision[decision] = "sha256:" + Convert.ToHexStringLower(
                    SHA256.HashData(step.EventRecords[0]));
                continue;
            }
            if (step.Kind == CampaignHostAcceptedStep.ContactRbaDeclineKind)
            {
                var declineHash = "sha256:" + Convert.ToHexStringLower(
                    SHA256.HashData(step.EventRecords[0]));
                declines.Add(declineHash, decision);
                if (schemaVersion >= ForceEntrySchemaVersion)
                    barrageByDecline.Add(declineHash, barrageByDecision[decision]);
                activeByDecision[decision] = declineHash;
            }
            else
            {
                if (!activeByDecision.TryGetValue(decision, out var declineHash))
                    throw new InvalidOperationException("Force event has no prior decline.");
                var unavailable = step.Kind
                    == CampaignHostAcceptedStep.NativeCombatForceCancellationKind
                    && document.RootElement.GetProperty("reason").GetString()
                        == "force-unavailable";
                if (unavailable)
                {
                    var root = document.RootElement;
                    if (root.GetProperty("configurationHash").GetString()
                            != (timing?.Hash
                                ?? CampaignHostAdapter.NoTimingForceCancellationHash)
                        || root.GetProperty("openedAtUnixMilliseconds").GetInt64()
                            != -1
                        || root.GetProperty("deadlineUnixMilliseconds").GetInt64()
                            != -1
                        || root.GetProperty("highWaterUnixMilliseconds").GetInt64()
                            != -1
                        || root.TryGetProperty("retainedSealHash", out _)
                        || requiredRounds.Contains(declineHash))
                        throw new InvalidOperationException(
                            "Force-unavailable cancellation claimed a round.");
                    noRoundCancellations.Add(declineHash);
                }
                else requiredRounds.Add(declineHash);
                if (step.Kind != CampaignHostAcceptedStep.ContactForceSealKind)
                    endings.Add(declineHash,
                        step.Kind == CampaignHostAcceptedStep.NativeCombatForceCompletionKind
                            ? "completed" : "cancelled");
                if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCancellationKind)
                {
                    var root = document.RootElement;
                    cancellations.Add(declineHash, (root.GetProperty("configurationHash").GetString()!,
                        root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                        root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                        root.GetProperty("highWaterUnixMilliseconds").GetInt64()));
                }
            }
        }
        var entryTimes = new Dictionary<string, long>(StringComparer.Ordinal);
        if (schemaVersion >= ForceEntrySchemaVersion)
        {
            using var entryQuery = connection.CreateCommand();
            entryQuery.Transaction = transaction;
            entryQuery.CommandText = """
                SELECT barrage_event_hash,high_water_ms FROM rba_rounds
                WHERE campaign_id=$id AND status='completed'
                """;
            entryQuery.Parameters.AddWithValue("$id", campaignId);
            using var entries = entryQuery.ExecuteReader();
            while (entries.Read()) entryTimes.Add(entries.GetString(0), entries.GetInt64(1));
        }
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT decision_action_id,decline_event_hash,configuration_hash,
              opened_ms,deadline_ms,high_water_ms,status,record_hash
            FROM force_rounds WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var foundRounds = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var round = new ForceRound(campaignId, reader.GetString(0), reader.GetString(1),
                reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4),
                reader.GetInt64(5), reader.GetString(6));
            foundRounds.Add(round.DeclineEventHash);
            if (reader.GetString(7) != ForceRoundHash(round)
                || timing is null
                || !declines.TryGetValue(round.DeclineEventHash, out var expectedDecision))
                throw new InvalidOperationException("Retained Force round changed.");
            RequireForceRoundBinding(round, expectedDecision, round.DeclineEventHash, timing);
            if (schemaVersion >= ForceEntrySchemaVersion
                && (!entryTimes.TryGetValue(barrageByDecline[round.DeclineEventHash], out var entered)
                    || round.OpenedMs != entered))
                throw new InvalidOperationException("Force opening differs from accepted RBA decline time.");
            if (round.Status != (endings.GetValueOrDefault(round.DeclineEventHash) ?? "collecting"))
                throw new InvalidOperationException("Force round status differs from accepted events.");
            if (cancellations.TryGetValue(round.DeclineEventHash, out var cancelled)
                && (cancelled.ConfigurationHash != round.ConfigurationHash
                    || cancelled.OpenedMs != round.OpenedMs
                    || cancelled.DeadlineMs != round.DeadlineMs
                    || cancelled.HighWaterMs != round.HighWaterMs))
                throw new InvalidOperationException("Force cancellation differs from retained round.");
        }
        if (schemaVersion >= ForceEntrySchemaVersion
            && declines.Keys.Any(hash => !foundRounds.Contains(hash)
                && !noRoundCancellations.Contains(hash)))
            throw new InvalidOperationException("Accepted Force entry has no retained round.");
        if (!requiredRounds.IsSubsetOf(foundRounds))
            throw new InvalidOperationException("Accepted Force decision has no retained round.");
        if (noRoundCancellations.Overlaps(foundRounds))
            throw new InvalidOperationException(
                "Force-unavailable cancellation has a retained round.");
    }
}
