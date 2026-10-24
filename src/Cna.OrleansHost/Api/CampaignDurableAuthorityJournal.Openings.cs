using System.Security.Cryptography;
using System.Text;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private const string CycleControlOpeningKey = "land.cycle-control";
    private const string RepeatMovementOpeningKey = "land.repeat-movement";
    /// <summary>
    /// Project one normal owner opening from the committed Core head. Visible bytes and public
    /// references remain stable when only hidden authority changes; the private map is replaced
    /// transactionally with the current Core action IDs and authority fence.
    /// </summary>
    internal CampaignOwnerOpening QueryOwnerOpening(CampaignSeatCredential credential,
        string actingElementId) => QueryOwnerOpeningCore(credential, actingElementId,
            CampaignOwnerOpeningKind.Movement);

    internal CampaignOwnerOpening QueryCombatOwnerOpening(CampaignSeatCredential credential,
        string actingElementId) => QueryOwnerOpeningCore(credential, actingElementId,
            CampaignOwnerOpeningKind.CombatSelection);

    internal CampaignOwnerOpening QueryRbaOwnerOpening(CampaignSeatCredential credential,
        string defenderElementId) => QueryOwnerOpeningCore(credential, defenderElementId,
            CampaignOwnerOpeningKind.RetreatBeforeAssault);

    internal CampaignOwnerOpening QueryForceOwnerOpening(CampaignSeatCredential credential,
        string elementId) => QueryOwnerOpeningCore(credential, elementId,
            CampaignOwnerOpeningKind.ForceAssignment);

    internal CampaignOwnerOpening QueryPostAssaultRetreatOwnerOpening(
        CampaignSeatCredential credential, string defenderElementId) =>
        QueryOwnerOpeningCore(credential, defenderElementId,
            CampaignOwnerOpeningKind.PostAssaultRetreat);

    internal CampaignOwnerOpening QueryCustodyOwnerOpening(
        CampaignSeatCredential credential, string captorElementId) =>
        QueryOwnerOpeningCore(credential, captorElementId,
            CampaignOwnerOpeningKind.Custody);

    internal CampaignOwnerOpening QueryReserveReleaseOwnerOpening(
        CampaignSeatCredential credential, string reserveElementId) =>
        QueryOwnerOpeningCore(credential, reserveElementId,
            CampaignOwnerOpeningKind.ReserveRelease);

    internal CampaignOwnerOpening QueryCycleControlOwnerOpening(
        CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, CycleControlOpeningKey,
            CampaignOwnerOpeningKind.CycleControl);

    internal CampaignOwnerOpening QueryRepeatMovementOwnerOpening(
        CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, RepeatMovementOpeningKey,
            CampaignOwnerOpeningKind.RepeatMovement);

    internal CampaignOwnerOpening QueryRepeatCombatOwnerOpening(
        CampaignSeatCredential credential, string actingElementId) =>
        QueryOwnerOpeningCore(credential, actingElementId,
            CampaignOwnerOpeningKind.RepeatCombatSelection);

    private CampaignOwnerOpening QueryOwnerOpeningCore(CampaignSeatCredential credential,
        string actingElementId, CampaignOwnerOpeningKind kind)
    {
        RequireWritable();
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var seat = AuthenticateSeat(connection, transaction, credential)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var authority = Load(connection, transaction, seat.CampaignId);
        if (kind == CampaignOwnerOpeningKind.ForceAssignment && authority.Timing is null)
            throw new InvalidOperationException("Force Assignment needs a pinned campaign timing configuration.");
        byte[] visible;
        long authorityVersion;
        string[] actionIds;
        try
        {
            if (kind == CampaignOwnerOpeningKind.CombatSelection)
            {
                var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ForceAssignment)
            {
                var draft = CampaignHostAdapter.QueryContactForceOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
            {
                var draft = CampaignHostAdapter.QueryContactRbaOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
            {
                var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.Custody)
            {
                var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ReserveRelease)
            {
                var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.CycleControl)
            {
                var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(
                    authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RepeatMovement)
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(
                    authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RepeatCombatSelection)
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else
            {
                var draft = CampaignHostAdapter.QueryContactOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateMovementActions.OrderBy(value => value.OriginLocationId,
                    StringComparer.Ordinal).ThenBy(value => value.DestinationLocationId,
                    StringComparer.Ordinal).Select(value => value.ActionId).ToArray();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException("No owner opening is available at this cut.");
        }
        if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
        {
            var round = ReadActiveRbaRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateRbaRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                CancelRbaRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("RBA round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.ForceAssignment)
        {
            var round = ReadActiveForceRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateForceRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                CancelForceRound(connection, transaction, authority, gate.Round,
                    reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Force Assignment round is closed.");
            }
        }
        RequireBounded(visible);
        var digest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(visible));
        var side = Side(seat.Audience);
        long revision = 1;
        using (var prior = connection.CreateCommand())
        {
            prior.Transaction = transaction;
            prior.CommandText = """
                SELECT seat_epoch,revision,visible_digest,visible_bytes
                FROM owner_openings
                WHERE campaign_id=$id AND side=$side AND acting_element_id=$element
                """;
            BindOpeningKey(prior, seat.CampaignId, side, actingElementId);
            using var reader = prior.ExecuteReader();
            if (reader.Read())
            {
                var priorVisible = ReadBounded(reader, 3);
                var priorDigest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorVisible));
                if (reader.GetString(2) != priorDigest)
                    throw new InvalidOperationException("Retained owner opening changed.");
                if (reader.GetInt64(0) == seat.Epoch && priorDigest == digest
                    && priorVisible.AsSpan().SequenceEqual(visible))
                    revision = reader.GetInt64(1);
                else
                    revision = checked(reader.GetInt64(1) + 1);
            }
        }
        var key = ReadReferenceKey(connection, transaction);
        var refs = actionIds.Select((_, index) => Reference(key, seat.CampaignId, side,
            actingElementId, seat.Epoch, revision, digest, index)).ToArray();
        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = transaction;
            upsert.CommandText = """
                INSERT INTO owner_openings VALUES ($id,$side,$element,$epoch,$revision,
                  $digest,$visible,$authority,$count)
                ON CONFLICT(campaign_id,side,acting_element_id) DO UPDATE SET
                  seat_epoch=excluded.seat_epoch, revision=excluded.revision,
                  visible_digest=excluded.visible_digest, visible_bytes=excluded.visible_bytes,
                  authority_version=excluded.authority_version, action_count=excluded.action_count
                """;
            BindOpeningKey(upsert, seat.CampaignId, side, actingElementId);
            upsert.Parameters.AddWithValue("$epoch", seat.Epoch);
            upsert.Parameters.AddWithValue("$revision", revision);
            upsert.Parameters.AddWithValue("$digest", digest);
            upsert.Parameters.AddWithValue("$visible", visible);
            upsert.Parameters.AddWithValue("$authority", authorityVersion);
            upsert.Parameters.AddWithValue("$count", refs.Length);
            upsert.ExecuteNonQuery();
        }
        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = """
                DELETE FROM owner_actions WHERE campaign_id=$id AND side=$side
                  AND acting_element_id=$element
                """;
            BindOpeningKey(delete, seat.CampaignId, side, actingElementId);
            delete.ExecuteNonQuery();
        }
        for (var index = 0; index < refs.Length; index++)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO owner_actions VALUES ($id,$side,$element,$ref,$ordinal,$action)
                """;
            BindOpeningKey(insert, seat.CampaignId, side, actingElementId);
            insert.Parameters.AddWithValue("$ref", refs[index]);
            insert.Parameters.AddWithValue("$ordinal", index);
            insert.Parameters.AddWithValue("$action", actionIds[index]);
            insert.ExecuteNonQuery();
        }
        transaction.Commit();
        return new CampaignOwnerOpening(seat.CampaignId, seat.Audience, actingElementId,
            revision, visible, refs);
    }

    private static void BindOpeningKey(SqliteCommand command, string campaignId,
        string side, string actingElementId)
    {
        command.Parameters.AddWithValue("$id", campaignId);
        command.Parameters.AddWithValue("$side", side);
        command.Parameters.AddWithValue("$element", actingElementId);
    }

    private static byte[] ReadReferenceKey(SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT secret FROM reference_key WHERE singleton=1";
        if (query.ExecuteScalar() is not byte[] key || key.Length != 32)
            throw new InvalidOperationException("Owner reference key is missing.");
        return key;
    }

    private static string Reference(byte[] key, string campaignId, string side,
        string actingElementId, long epoch, long revision, string visibleDigest, int ordinal)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-owner-ref-v1");
            writer.Write(campaignId);
            writer.Write(side);
            writer.Write(actingElementId);
            writer.Write(epoch);
            writer.Write(revision);
            writer.Write(visibleDigest);
            writer.Write(ordinal);
        }
        return "act." + Convert.ToHexStringLower(HMACSHA256.HashData(key, stream.ToArray()));
    }
}

internal enum CampaignOwnerOpeningKind
{
    Movement,
    CombatSelection,
    RetreatBeforeAssault,
    ForceAssignment,
    PostAssaultRetreat,
    Custody,
    ReserveRelease,
    CycleControl,
    RepeatMovement,
    RepeatCombatSelection,
}

internal sealed class CampaignOwnerOpening
{
    private readonly byte[] visibleBytes;
    private readonly string[] publicActionRefs;

    internal CampaignOwnerOpening(string campaignId, Cna.Core.Actions.CampaignActionAudience audience,
        string actingElementId, long revision, byte[] visibleBytes,
        IReadOnlyList<string> publicActionRefs)
    {
        CampaignId = campaignId;
        Audience = audience;
        ActingElementId = actingElementId;
        Revision = revision;
        this.visibleBytes = visibleBytes.ToArray();
        this.publicActionRefs = publicActionRefs.ToArray();
    }

    public string CampaignId { get; }
    public Cna.Core.Actions.CampaignActionAudience Audience { get; }
    public string ActingElementId { get; }
    public long Revision { get; }
    public byte[] VisibleBytes => visibleBytes.ToArray();
    public IReadOnlyList<string> PublicActionRefs => Array.AsReadOnly(publicActionRefs.ToArray());
}
