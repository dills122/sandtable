using System.Collections.Immutable;
using Marten.Schema;

namespace Cna.OrleansHost.Persistence;

// Phase C3b of issue #439 (docs/work/plans/2026-11-19-phase-c3b-durable-state-design-spike.md
// §3.2/§3.3): host operational state that is NOT game history and has no event behind it --
// seats/credentials, owner openings/actions, the per-campaign reference key, the open rounds'
// high-water marks and the host-wide operator capability. Plain Marten documents, every one under
// optimistic concurrency, written in the same IDocumentSession (one Postgres transaction) as any
// event append of the same unit of work. None of these are hash-chained; that matches SQLite,
// where they were also side tables outside the chain.

/// <summary>One campaign seat (SQLite <c>seats</c> row). Id: <c>{campaignId}/{side}</c>.</summary>
[UseOptimisticConcurrency]
public sealed record CampaignSeatDocument(
    string Id,
    string CampaignId,
    string Side,
    long Epoch,
    byte[]? GrantDigest,
    long? GrantExpiresMs,
    byte[]? CredentialDigest,
    long? CredentialExpiresMs)
{
    public static string IdFor(string campaignId, string side) => campaignId + "/" + side;
}

/// <summary>One owner-action row embedded in its opening (SQLite <c>owner_actions</c>).</summary>
public sealed record CampaignOwnerActionRow(string PublicRef, long Ordinal, string CoreActionId);

/// <summary>
/// One owner opening and its actions (SQLite <c>owner_openings</c> + <c>owner_actions</c>, which
/// were only ever read and rewritten together). Id: <c>{campaignId}/{side}/{actingElementId}</c>.
/// </summary>
[UseOptimisticConcurrency]
public sealed record CampaignOwnerOpeningDocument(
    string Id,
    string CampaignId,
    string Side,
    string ActingElementId,
    long SeatEpoch,
    long Revision,
    string VisibleDigest,
    byte[] VisibleBytes,
    long AuthorityVersion,
    long ActionCount,
    CampaignOwnerActionRow[] Actions)
{
    public static string IdFor(string campaignId, string side, string actingElementId) =>
        campaignId + "/" + side + "/" + actingElementId;
}

/// <summary>
/// The campaign's 32-byte public-action-reference HMAC key (SQLite <c>reference_key</c>). Secret:
/// deliberately a document, never an event, so it does not travel into the append-only archived
/// stream. Id: the campaign id.
/// </summary>
[UseOptimisticConcurrency]
public sealed record CampaignReferenceKeyDocument(string Id, byte[] Secret);

/// <summary>Current high-water mark (and the round row's matching record hash) of one open round.</summary>
public sealed record CampaignRoundHighWater(long HighWaterMs, string RecordHash);

/// <summary>
/// The only genuinely mutable round-timing state (design spike &#167;3.3): the high-water mark of
/// each currently <c>collecting</c> round, which advances on owner-opening polls that append no
/// event. One document per campaign; keys are <c>{table}/{roundKey}</c>. Closed rounds are removed
/// -- their final high-water is retained by the closing event's round-row header instead.
/// </summary>
[UseOptimisticConcurrency]
public sealed record CampaignRoundHighWaterDocument(string Id,
    ImmutableDictionary<string, CampaignRoundHighWater> Entries);

/// <summary>
/// Host-wide operator capability digest (SQLite <c>_host-identity.db</c>'s <c>operator_capability</c>).
/// Exactly one document, id <see cref="SingletonId"/>.
/// </summary>
[UseOptimisticConcurrency]
public sealed record HostOperatorCapabilityDocument(string Id, byte[] Digest)
{
    public const string SingletonId = "host";
}
