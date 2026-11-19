using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Cna.OrleansHost.Timeouts;

/// <summary>
/// Implements <see cref="ICampaignRoundTimeoutGrain"/>'s timer+reminder hybrid. See that
/// interface's remarks for the design and <see cref="ICampaignRoundDeadlineSink"/> for the
/// durable-state boundary this grain deliberately keeps itself on the far side of.
/// </summary>
internal sealed class CampaignRoundTimeoutGrain(ICampaignRoundDeadlineSink sink, TimeProvider clock,
    ILogger<CampaignRoundTimeoutGrain> logger) : Grain, ICampaignRoundTimeoutGrain
{
    /// <summary>
    /// Coarse, minutes-scale backstop period (design doc §4.2: reminders are not precise and
    /// should be minutes/hours, never a substitute for the timer). Orleans itself enforces a
    /// >1-minute floor on reminder periods; this is comfortably above it while still re-arming
    /// promptly after a crash relative to typical round budgets (minutes, not days).
    /// </summary>
    internal static readonly TimeSpan ReminderPeriod = TimeSpan.FromMinutes(3);
    internal const string ReminderName = "deadline-backstop";

    private readonly string activationId = Guid.NewGuid().ToString("N");
    private string campaignId = null!;
    private CampaignRoundKind roundKind;
    private DateTimeOffset? armedDeadline;
    private IGrainTimer? timer;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (campaignId, roundKind) = CampaignRoundTimeoutGrainKey.Parse(this.GetPrimaryKeyString());
        // Reactivation recovery (crash, deactivation, or the first activation after this silo's
        // own process restarted): the grain keeps no durable state of its own, so re-derive
        // whatever is current from the sink rather than trusting anything in memory. This is
        // the one codepath both a normal activation and the reminder backstop share.
        var deadline = await sink.GetDeadlineAsync(campaignId, roundKind, cancellationToken);
        if (deadline is { } value) await ArmInternalAsync(value);
        else await ClearAsync();
    }

    public Task ArmAsync(DateTimeOffset deadlineUtc) => ArmInternalAsync(deadlineUtc);

    public Task DisarmAsync() => ClearAsync();

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName != ReminderName) return;
        // The durability backstop: re-derive the deadline from the sink and re-arm the
        // in-memory timer if this activation lost it (process restart, or a timer that never
        // got a chance to register because the grain was deactivated before it fired). If the
        // round already closed through some other path while this grain was inactive, there is
        // nothing to re-arm -- clear up instead of leaving a stale reminder running forever.
        var deadline = await sink.GetDeadlineAsync(campaignId, roundKind, CancellationToken.None);
        if (deadline is { } value) await ArmInternalAsync(value);
        else await ClearAsync();
    }

    public Task<string> ActivationIdForTestAsync() => Task.FromResult(activationId);

    public Task DeactivateForTestAsync()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> ArmedDeadlineForTestAsync() => Task.FromResult(armedDeadline);

    private async Task ArmInternalAsync(DateTimeOffset deadlineUtc)
    {
        armedDeadline = deadlineUtc;
        timer?.Dispose();
        var dueTime = deadlineUtc - clock.GetUtcNow();
        if (dueTime < TimeSpan.Zero) dueTime = TimeSpan.Zero;
        // One-shot (Period = Timeout.InfiniteTimeSpan): this is the precise mechanism, armed
        // fresh for each deadline rather than ticking on a fixed cadence. Deliberately not
        // KeepAlive: if this grain idles out before the timer fires, that is an expected,
        // recoverable gap -- the reminder backstop below exists precisely to re-arm a fresh
        // timer on the next reactivation (design doc §4.2), not to be prevented from happening.
        timer = this.RegisterGrainTimer(OnTimerFiredAsync,
            new GrainTimerCreationOptions(dueTime, Timeout.InfiniteTimeSpan));
        await this.RegisterOrUpdateReminder(ReminderName, ReminderPeriod, ReminderPeriod);
    }

    private async Task ClearAsync()
    {
        armedDeadline = null;
        timer?.Dispose();
        timer = null;
        var reminder = await this.GetReminder(ReminderName);
        if (reminder is not null) await this.UnregisterReminder(reminder);
    }

    private async Task OnTimerFiredAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await sink.TickAsync(campaignId, roundKind, cancellationToken);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogTickFailed(logger, campaignId, roundKind, exception);
        }
        // The round may have been re-opened (a new deadline for the same campaign+round kind)
        // between the timer firing and this continuation running, or it may genuinely be done.
        // Re-derive from the sink rather than assuming either.
        var deadline = await sink.GetDeadlineAsync(campaignId, roundKind, cancellationToken);
        if (deadline is { } value) await ArmInternalAsync(value);
        else await ClearAsync();
    }

    private static readonly Action<ILogger, string, CampaignRoundKind, Exception> LogTickFailed =
        LoggerMessage.Define<string, CampaignRoundKind>(LogLevel.Error,
            new EventId(1, "CampaignRoundDeadlineTickFailed"),
            "Deadline tick failed for campaign {CampaignId} round {RoundKind}.");
}
