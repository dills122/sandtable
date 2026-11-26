using System.Text.Json;
using Cna.Core.Exercises;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal static partial class CampaignDurableOutwardApi
{
    private static void MapVerification(RouteGroupBuilder api) =>
        api.MapGet("/{campaignId}/verification", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(context, async () =>
            {
                RequireOperator(context, journal);
                var verification = await journal.VerifyReadOnlyAsync(campaignId, context.RequestAborted);
                return verification is null ? Results.NotFound() : Results.Json(verification);
            }));
}

internal sealed record CampaignVerification(string? ChainHeadHash, string? ReconstructionHash,
    long? EventCount, long? CanonicalByteCount, string? PositionId, int? GameTurn, bool IsVerified)
{
    internal static CampaignVerification Invalid { get; } = new(null, null, null, null, null, null, false);
}

internal sealed partial class CampaignDurableAuthorityJournalStore
{
    // Deliberately bypass the ordinary write-capable dispatch and grain activation: no
    // FlushAsync, timer drain, or cached journal insertion on this operator read.
    internal async Task<CampaignVerification?> VerifyReadOnlyAsync(string campaignId, CancellationToken cancellationToken)
    {
        RequireSafeCampaignId(campaignId);
        try
        {
            if (fixedJournal is not null) return fixedJournal.VerifyReadOnly(campaignId);
            if (marten is null)
            {
                var path = JournalPath(directory!, campaignId);
                return File.Exists(path)
                    ? CampaignDurableAuthorityJournal.OpenExisting(path, clock, contentResolver).VerifyReadOnly(campaignId)
                    : null;
            }
            // A missing reference key only needs a placeholder in the disposable working
            // set. Do not consume the host's injected randomness on a read.
            using var workingSet = await marten.OpenAsync([campaignId], static () => new byte[32], cancellationToken);
            if (!workingSet.CampaignExists(campaignId)) return null;
            return CampaignDurableAuthorityJournal.ForWorkingSet(workingSet.Connect, marten.AuthenticateOperator,
                marten.Clock, marten.ContentResolver, marten.RandomBytes).VerifyReadOnly(campaignId);
        }
        catch (Exception error) when (error is InvalidOperationException or JsonException or ArgumentException
            or KeyNotFoundException or FormatException or OverflowException)
        {
            // Malformed envelopes can fail while the working set is being loaded, before
            // the journal sees the canonical bytes. Fail closed at either verification layer.
            return CampaignVerification.Invalid;
        }
    }
}

internal sealed partial class CampaignDurableAuthorityJournal
{
    // Load is the same verified-read implementation used by ReadVerified. Retain its
    // verified head and project metadata in the very same transaction, avoiding a
    // second read that could race an append. Never reconstruct from unverified bytes.
    internal CampaignVerification? VerifyReadOnly(string campaignId)
    {
        using var connection = workingSetConnect is not null
            ? Connect(create: false)
            : new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        if (connection.State != System.Data.ConnectionState.Open) connection.Open();
        using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$id", campaignId);
        command.CommandText = "SELECT 1 FROM campaigns WHERE campaign_id=$id";
        if (command.ExecuteScalar() is null) return null;
        var loaded = Load(connection, transaction, campaignId);
        var reconstruction = CampaignExercises.Reconstruct(loaded.Session);
        if (!reconstruction.IsVerified) return CampaignVerification.Invalid;
        var checkpoint = CampaignExercises.QueryCheckpoint(loaded.Session);

        // The canonical event byte count includes genesis, just like the checkpoint
        // HTTP proof. The latest explicit turn stamp is metadata from verified history,
        // not a new state transition or an independent replay.
        command.CommandText = """
            SELECT canonical FROM (
                SELECT 0 AS sequence, creation_event AS canonical FROM campaigns WHERE campaign_id=$id
                UNION ALL
                SELECT sequence, canonical_event FROM accepted_steps WHERE campaign_id=$id
            ) ORDER BY sequence DESC
            """;
        using var reader = command.ExecuteReader();
        long count = 0, bytes = 0;
        int? gameTurn = null;
        while (reader.Read())
        {
            var canonical = reader.GetFieldValue<byte[]>(0);
            count++;
            bytes += canonical.Length;
            if (gameTurn is not null) continue;
            using var record = JsonDocument.Parse(canonical);
            var root = record.RootElement;
            if (root.TryGetProperty("nextGameTurn", out var turn) || root.TryGetProperty("gameTurn", out turn))
                gameTurn = turn.GetInt32();
            else if (root.TryGetProperty("sequencePosition", out var position)
                && position.TryGetProperty("gameTurn", out turn))
                gameTurn = turn.GetInt32();
        }
        return new CampaignVerification(loaded.Hash, reconstruction.ReconstructedSnapshotHash, count, bytes,
            checkpoint.PositionId, gameTurn, true);
    }
}
