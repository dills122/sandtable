namespace Cna.OrleansHost.Timeouts;

/// <summary>
/// What a <see cref="CampaignRoundTimeoutGrain"/> consults/drives. The grain owns scheduling
/// only (design doc §4.2) -- durable "what is the deadline" state, and the actual tick/
/// adjudication side effect, live behind this abstraction so the grain has no dependency on
/// whichever store currently backs a campaign's rounds (today SQLite via
/// <c>CampaignDurableAuthorityJournalStore</c>; post-Phase-C3, Marten). No implementation wired
/// to that store ships in this phase -- see
/// docs/work/plans/2026-11-19-phase-c4-grain-wiring-fork.md for why.
/// </summary>
internal interface ICampaignRoundDeadlineSink
{
    /// <summary>
    /// The current durable deadline for this campaign's round, if one is open. Consulted on
    /// grain activation and on every reminder tick to re-derive what the in-memory timer should
    /// be re-armed to -- the grain itself keeps no durable state of its own.
    /// </summary>
    Task<DateTimeOffset?> GetDeadlineAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts the actual timeout/cancellation side effect for this campaign's round (the
    /// moral equivalent of <c>CampaignCombatTimeoutService</c>'s per-lane
    /// <c>journal.TickXRound(campaignId)</c> calls). Returns whether the tick had an effect.
    /// Core remains sole adjudication authority: this only tells the durable store "the deadline
    /// has passed," it does not itself decide the outcome.
    /// </summary>
    Task<bool> TickAsync(string campaignId, CampaignRoundKind roundKind, CancellationToken cancellationToken);
}
