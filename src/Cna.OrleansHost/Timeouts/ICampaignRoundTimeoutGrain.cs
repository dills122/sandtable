using Orleans.Concurrency;
using Orleans;
using Orleans.Runtime;

namespace Cna.OrleansHost.Timeouts;

/// <summary>
/// The seven round kinds <c>CampaignCombatTimeoutService</c> sweeps today (selection/RBA/force/
/// retreat/custody/reserve/cycle -- see that class's own remarks). Named independently rather
/// than reusing a string so a grain key can be built/parsed without any dependency on
/// <c>Cna.OrleansHost.Api</c>'s SQLite-journal types (Phase C4 is infrastructure, kept decoupled
/// from the journal cutover that is Phase C3's job -- see
/// docs/work/plans/2026-11-19-phase-c4-grain-wiring-fork.md).
/// </summary>
public enum CampaignRoundKind
{
    Selection,
    Rba,
    Force,
    Retreat,
    Custody,
    Reserve,
    Cycle,
}

/// <summary>
/// One campaign's one round-kind deadline, scheduled with the Microsoft-documented timer+reminder
/// hybrid (design doc §4.2, docs/work/plans/2026-11-18-postgres-migration-design.md): a fast
/// in-memory <c>RegisterGrainTimer</c> owns real-time precision while this grain is active; a
/// coarse (minutes-scale) Orleans reminder exists purely as a durability backstop that re-arms
/// the timer from <see cref="ICampaignRoundDeadlineSink"/>'s durable state after a deactivation
/// or crash. "Core remains sole adjudication authority"
/// (<c>CampaignCombatTimeoutService</c>'s own doc comment, preserved here): this grain only
/// schedules/triggers <see cref="ICampaignRoundDeadlineSink.TickAsync"/>, it never adjudicates.
/// </summary>
/// <remarks>
/// Keyed by a composite string (<c>"{campaignId}|{roundKind}"</c>, see
/// <see cref="CampaignRoundTimeoutGrainKey"/>) since Orleans grain keys are single string/Guid/
/// long values and one campaign can have an independent deadline per round kind. Internal and
/// not reachable from any HTTP endpoint in this phase -- see the fork doc for why it is not yet
/// armed by production code.
/// </remarks>
internal interface ICampaignRoundTimeoutGrain : IGrainWithStringKey, IRemindable
{
    /// <summary>
    /// Arms (or re-arms) this grain's deadline. Registers a precise one-shot grain timer for
    /// <paramref name="deadlineUtc"/> and a coarse backstop reminder. Idempotent: calling this
    /// again before the timer fires replaces the previous arming.
    /// </summary>
    /// <remarks>
    /// <c>[AlwaysInterleave]</c> (Phase C3b of issue #439): <c>CampaignGrain</c> awaits Arm/Disarm
    /// inside its own turn, while this grain's timer callback awaits <c>CampaignGrain.TickRoundAsync</c>
    /// (through the sink) inside its turn. Without interleaving, a timer firing while that campaign's
    /// grain is mid-call is a two-grain await cycle that deadlocks until Orleans' 30s response
    /// timeout -- reproduced once owner-opening queries started routing through <c>CampaignGrain</c>.
    /// Arm/Disarm only replace the in-memory timer and the reminder registration, and the timer
    /// callback re-derives its final state from the durable deadline after the tick, so letting them
    /// interleave cannot leave a state the store does not justify.
    /// </remarks>
    [AlwaysInterleave]
    Task ArmAsync(DateTimeOffset deadlineUtc);

    /// <summary>
    /// Disarms this grain: cancels any pending timer and unregisters the backstop reminder.
    /// Called once a round completes before its deadline, so nothing is left to re-arm on a
    /// later reactivation.
    /// </summary>
    /// <remarks>See <see cref="ArmAsync"/>'s remarks on <c>[AlwaysInterleave]</c>.</remarks>
    [AlwaysInterleave]
    Task DisarmAsync();

    /// <summary>Test-only inspection, same convention as <c>TemporaryCampaignGrain</c>'s
    /// <c>ActivationIdForTestAsync</c>/<c>DeactivateForTestAsync</c>.</summary>
    Task<string> ActivationIdForTestAsync();

    /// <summary>Test-only: forces deactivation via <c>DeactivateOnIdle</c>, same convention as
    /// <c>TemporaryCampaignGrain.DeactivateForTestAsync</c>.</summary>
    Task DeactivateForTestAsync();

    /// <summary>Test-only inspection of the currently-armed in-memory deadline, if any.</summary>
    Task<DateTimeOffset?> ArmedDeadlineForTestAsync();
}

/// <summary>Builds/parses <see cref="ICampaignRoundTimeoutGrain"/>'s composite string key.</summary>
internal static class CampaignRoundTimeoutGrainKey
{
    private const char Separator = '|';

    public static string For(string campaignId, CampaignRoundKind roundKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        if (campaignId.Contains(Separator))
            throw new ArgumentException($"Campaign id must not contain '{Separator}'.", nameof(campaignId));
        return $"{campaignId}{Separator}{roundKind}";
    }

    public static (string CampaignId, CampaignRoundKind RoundKind) Parse(string key)
    {
        var parts = key.Split(Separator, 2);
        if (parts.Length != 2 || !Enum.TryParse<CampaignRoundKind>(parts[1], out var roundKind))
            throw new FormatException($"'{key}' is not a valid campaign round timeout grain key.");
        return (parts[0], roundKind);
    }
}
