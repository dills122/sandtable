using Cna.Core.Actions;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    internal (CampaignActionAudience Audience, CampaignHostObservation View, long? Deadline)
        QueryObservation(CampaignSeatCredential credential)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: true);
        var seat = AuthenticateSeat(connection, transaction, credential, out var verifiedHead)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var head = verifiedHead ?? Load(connection, transaction, seat.CampaignId);
        if (head.LegacySession is not { } session)
            return (seat.Audience, CampaignHostObservation.Unavailable(), null);
        var view = CampaignHostObservation.Query(session, seat.Audience);
        return (seat.Audience, view, ObservationDeadline(connection, transaction, head, seat));
    }

    // Only report an already collecting round if the existing owner draft exposes
    // a choice to this seat at this exact retained head. Never preview-drain, arm,
    // renew, or resolve rounds in a read. Bound round readers validate retained
    // timing/authority rather than selecting an arbitrary stale deadline.
    private long? ObservationDeadline(SqliteConnection connection, SqliteTransaction transaction,
        Loaded head, CampaignSeatAuthority seat)
    {
        if (schemaVersion != SchemaVersion || head.Timing is null) return null;
        var session = head.Session;
        var candidates = CampaignOpenSeatCatalog.CandidatesFor(session);
        var stages = candidates.IsLegacyChain ? OpeningStages
            : candidates.Stages.Select(name => OpeningStagesByName[name]);
        foreach (var stage in stages)
        {
            var table = stage.Kind switch
            {
                CampaignOwnerOpeningKind.CombatSelection or CampaignOwnerOpeningKind.RepeatCombatSelection => "selection_rounds",
                CampaignOwnerOpeningKind.RetreatBeforeAssault => "rba_rounds",
                CampaignOwnerOpeningKind.ForceAssignment => "force_rounds",
                CampaignOwnerOpeningKind.PostAssaultRetreat => "retreat_rounds",
                CampaignOwnerOpeningKind.Custody => "custody_rounds",
                CampaignOwnerOpeningKind.ReserveRelease => "reserve_rounds",
                CampaignOwnerOpeningKind.CycleControl => "cycle_rounds",
                _ => null,
            };
            if (table is null) continue;
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"SELECT deadline_ms FROM {table} WHERE campaign_id=$id AND status='collecting'";
            command.Parameters.AddWithValue("$id", seat.CampaignId);
            if (command.ExecuteScalar() is not long deadline
                || deadline <= clock.GetUtcNow().ToUnixTimeMilliseconds()) continue;
            var ids = stage.ElementKey is { } key ? [key]
                : CampaignOpenSeatCatalog.OwnedElementIds(session, seat.Audience);
            foreach (var element in ids)
            {
                try
                {
                    var draft = openingQueries[stage.Kind](session, seat.Audience, element);
                    if (draft.ActionIds.Length == 0) continue;
                    return stage.Kind switch
                    {
                        CampaignOwnerOpeningKind.CombatSelection or CampaignOwnerOpeningKind.RepeatCombatSelection => ReadActiveSelectionRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        CampaignOwnerOpeningKind.RetreatBeforeAssault => ReadActiveRbaRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        CampaignOwnerOpeningKind.ForceAssignment => ReadActiveForceRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        CampaignOwnerOpeningKind.PostAssaultRetreat => ReadActiveRetreatRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        CampaignOwnerOpeningKind.Custody => ReadActiveCustodyRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        CampaignOwnerOpeningKind.ReserveRelease => ReadActiveReserveRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        CampaignOwnerOpeningKind.CycleControl => ReadActiveCycleRound(connection, transaction, head, seat.CampaignId).DeadlineMs,
                        _ => null,
                    };
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    // Same pure owner draft rejection as opening discovery.
                }
            }
        }
        return null;
    }
}
