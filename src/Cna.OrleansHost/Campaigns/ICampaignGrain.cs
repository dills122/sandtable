using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Orleans;

namespace Cna.OrleansHost.Campaigns;

/// <summary>
/// Phase C3a of issue #439 (docs/work/plans/2026-11-18-postgres-migration-design.md &#167;1/&#167;2/
/// &#167;5): the per-campaign activation/lifecycle boundary production campaigns did not have
/// before this phase -- see docs/work/plans/2026-11-19-phase-c4-grain-wiring-fork.md's finding
/// that <c>CampaignDurableOutwardApi</c>/<c>CampaignDurableAuthorityJournalStore</c> called SQLite
/// directly, with no grain anywhere in that path. Keyed by campaign id, matching Orleans' one-
/// activation-per-key guarantee onto this codebase's existing single-writer-per-campaign
/// assumption (design doc &#167;5).
/// </summary>
/// <remarks>
/// <para>This grain deliberately does NOT reimplement <c>CampaignDurableAuthorityJournal</c>'s
/// read/write logic -- see <c>CampaignGrain</c>'s own remarks for why its surface is narrow:
/// exactly the mutating entry points that can open or close one of the seven combat round kinds
/// (the single <c>SubmitOwnerActionCore</c> chokepoint behind every
/// <c>SubmitOwner*</c>/<c>SubmitSource*</c>/<c>PlaceSource*</c> method in
/// <c>CampaignDurableAuthorityJournal.Submissions.cs</c>, dispatched here by the same
/// <c>stage</c> string <c>CampaignDurableOutwardApi</c>'s <c>/{campaignId}/actions</c> endpoint
/// already switched on) plus the seven <c>TickXRound</c> entry points (now reached only from
/// <c>MartenCampaignRoundDeadlineSink.TickAsync</c>, never from a cross-campaign sweep). Every
/// other durable-journal entry point -- reads, and the pregame/operator Admit*/Promote*/Activate*/
/// Complete*/Resolve*/Announce*/Correct* progression calls -- is verified, by direct inspection of
/// every <c>InitializeXRound</c>/<c>UpdateXRound(... Status = "completed")</c> call site, to never
/// open or close a combat round, and continues to call
/// <see cref="Cna.OrleansHost.Api.CampaignDurableAuthorityJournalStore"/> directly from
/// <c>CampaignDurableOutwardApi</c>, unchanged. Routing those through this grain too, for general
/// single-writer enforcement beyond what round-deadline correctness requires, is a reasonable
/// follow-up but out of this phase's scope.</para>
/// </remarks>
internal interface ICampaignGrain : IGrainWithStringKey
{
    /// <summary>
    /// Dispatches to the <c>CampaignDurableAuthorityJournalStore</c> method matching <paramref
    /// name="stage"/> (the same string values <c>CampaignDurableOutwardApi</c>'s stage switch
    /// already used), then arms/disarms any round-timeout grain affected by this call before
    /// returning.
    /// </summary>
    Task<CampaignGrainSubmissionResult> SubmitOwnerActionAsync(string stage,
        CampaignSeatCredential credential, string actingElementId, long openingRevision,
        string publicActionRef);

    /// <summary>
    /// The moral equivalent of <c>CampaignCombatTimeoutService</c>'s per-lane
    /// <c>journal.TickXRound(campaignId)</c> call for this grain's campaign, reached from
    /// <c>MartenCampaignRoundDeadlineSink.TickAsync</c> instead of a cross-campaign sweep.
    /// </summary>
    Task<bool> TickRoundAsync(CampaignRoundKind roundKind);

    /// <summary>
    /// Phase C3b of issue #439: dispatches an owner-opening query (same stage strings
    /// <c>CampaignDurableOutwardApi</c>'s <c>/{campaignId}/openings</c> endpoint switches on) behind
    /// this grain's single-writer boundary, because those queries write durable state too (see
    /// <c>CampaignGrain.QueryOwnerOpeningAsync</c>).
    /// </summary>
    Task<CampaignGrainOpeningResult> QueryOwnerOpeningAsync(string stage, CampaignSeatCredential credential,
        string actingElementId);
}

/// <summary>Orleans-serializable projection of <c>CampaignOwnerOpening</c>.</summary>
[GenerateSerializer]
internal sealed record CampaignGrainOpeningResult
{
    [Id(0)] public string CampaignId { get; init; } = "";
    [Id(1)] public Cna.Core.Actions.CampaignActionAudience Audience { get; init; }
    [Id(2)] public string ActingElementId { get; init; } = "";
    [Id(3)] public long Revision { get; init; }
    [Id(4)] public byte[] VisibleBytes { get; init; } = [];
    [Id(5)] public string[] PublicActionRefs { get; init; } = [];
}

/// <summary>
/// Orleans-serializable projection of <c>CampaignOwnerSubmissionResult</c> (which has no
/// <c>[GenerateSerializer]</c> shape of its own, and is not otherwise used across a grain
/// boundary) for <see cref="ICampaignGrain.SubmitOwnerActionAsync"/>'s return.
/// </summary>
[GenerateSerializer]
internal sealed record CampaignGrainSubmissionResult
{
    [Id(0)] public bool IsAccepted { get; init; }
    [Id(1)] public byte[]? CanonicalReceipt { get; init; }
    [Id(2)] public string? RejectionCode { get; init; }
}
