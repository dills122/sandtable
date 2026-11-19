using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Hosting;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace Cna.OrleansHost.Persistence;

/// <summary>
/// The seven round-table projections for Phase C2 of issue #439
/// (<c>docs/work/plans/2026-11-18-postgres-migration-design.md</c> §3.2), replacing the
/// hand-rolled <c>VerifyXRounds</c> full-table-scan functions
/// (<c>CampaignDurableAuthorityJournal.*Rounds.cs</c>) with Marten
/// <c>SingleStreamProjection&lt;TDoc,string&gt;</c> read models, one document per campaign per
/// round kind.
/// </summary>
/// <remarks>
/// <para><b>Deliberate, user-confirmed scope narrowing -- see
/// <c>docs/work/plans/2026-11-19-phase-c2-round-table-projection-fork.md</c> and its resolution
/// addendum.</b> These projections cover only the fields a pure fold of
/// <see cref="CampaignAcceptedStepEvent"/> can actually produce: a round's identifying hash, its
/// <see cref="RoundEntry.Status"/>, and (where the stream happens to carry it)
/// <see cref="RoundEntry.ConfigurationHash"/>. They deliberately omit <c>opened_ms</c>/
/// <c>deadline_ms</c>/<c>high_water_ms</c>: those are wall-clock reads taken between accepted-step
/// events today, not data the event stream carries for the common (non-timeout) completion path,
/// and Phase C4's planned grain-timer-plus-reminder-backstop design (design doc §4.2) is intended
/// to own live deadline tracking going forward rather than this read model.</para>
/// <para><b><see cref="RoundEntry.ConfigurationHash"/> is itself only partially event-derivable,
/// found while implementing this -- flagged honestly rather than silently narrowed further.</b>
/// The hand-rolled round tables set <c>configuration_hash</c> at round-open time from the
/// campaign's live <c>authority.Timing.Hash</c> (per-campaign config, read from the <c>campaigns</c>
/// table, not from any per-step event) -- the *same* category of gap as the timing columns, just
/// per-campaign-constant rather than continuously varying. The one place the accepted-step stream
/// *does* carry <c>configurationHash</c> explicitly is inside each table's timeout/cancellation
/// event's own JSON body (confirmed by grep across <c>Cna.Core/Campaigns/*TimeoutV3*.cs</c> and
/// <c>*CancellationV19/V33.cs</c>). These projections populate <see cref="RoundEntry.ConfigurationHash"/>
/// exactly when and only when that happens -- it stays <c>null</c> for a round that completes
/// normally (the common case), the same way the hand-rolled table would never have shown a value
/// not actually present without reading the live campaign config. A caller that needs
/// <c>configuration_hash</c> for every round, not just timed-out ones, still needs the campaign's
/// own stored timing configuration (unaffected by this phase; it is not part of the event stream
/// in either the old or the new design).</para>
/// <para><b>These are derivations, not verifiers.</b> Unlike <c>VerifyXRounds</c>, a projection's
/// <c>Apply</c> cannot throw on a malformed history without corrupting Marten's projection
/// daemon/inline-append path -- and it should not try to re-implement the verification role here
/// anyway, since the actual trust boundary stays the host-layer hash chain (Phase C3's job).
/// These methods therefore degrade gracefully (no-op) on any step shape they do not recognize
/// rather than throwing.</para>
/// <para><b>The "-unavailable" round-never-created cases</b> (<c>force-unavailable</c>,
/// <c>rba-unavailable</c>, <c>selection-unavailable</c>, <c>cycle-control-unavailable</c>,
/// <c>reserve-release-unavailable</c>, <c>custody-unavailable</c>, <c>retreat-unavailable</c> --
/// one per table, all confirmed by reading each table's <c>InitializeXRound</c> function, which
/// skips the <c>INSERT</c> entirely when timing is unavailable) are handled uniformly: the entry
/// event optimistically adds a <c>"collecting"</c> round entry (matching the real system's
/// synchronous "entry step, then immediately the unavailable-cancellation step, same unit of
/// work" ordering closely enough that no durable intermediate read ever actually observes the
/// optimistic entry), and the following unavailable-reason ending event removes it instead of
/// marking it terminal -- reproducing the real tables' "this round was never retained" outcome
/// without needing look-ahead across events, which Marten's one-event-at-a-time <c>Apply</c> does
/// not support. Reserve rounds have one more such case, "empty" (no explicit release choice before
/// auto-completing), handled the same way.</para>
/// </remarks>
public sealed record RoundEntry(string Status, string? ConfigurationHash, long? EntryStateVersion,
    string? DecisionActionId);

internal static class RoundProjectionSupport
{
    /// <summary>
    /// Builds a <see cref="CampaignHostAcceptedStep"/> from the envelope, or <c>null</c> if its
    /// <see cref="CampaignAcceptedStepEvent.ActionKind"/>/<see cref="CampaignAcceptedStepEvent.ActingElementId"/>
    /// pairing is not one <see cref="CampaignHostAcceptedStep"/>'s constructor recognizes as
    /// consistent (it validates this and throws <see cref="ArgumentException"/>). Real production
    /// data always round-trips a recognized Cna.Core step kind; this guard exists because these
    /// projections are derivations, not verifiers (see this file's type-level remarks) -- an
    /// unrecognized shape (e.g. synthetic test data exercising only the envelope/header
    /// round-trip, as <c>CampaignEventStreamSchemaTests</c> does) should be skipped, not crash the
    /// whole inline-projection pipeline for every other projection sharing the same
    /// <c>SaveChangesAsync</c> call.
    /// </summary>
    public static CampaignHostAcceptedStep? TryToStep(CampaignAcceptedStepEvent data)
    {
        try
        {
            return new CampaignHostAcceptedStep(data.CanonicalCommand, [data.CanonicalEvent], data.CanonicalReceipt,
                data.SnapshotCheckpoint, data.ActionKind, data.ActingElementId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static string EventHash(CampaignAcceptedStepEvent data) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(data.CanonicalEvent));
}

// ---------------------------------------------------------------------------------------------
// Selection rounds -- entry detection reuses Cna.Core's own
// CampaignHostAdapter.QueryNativeCombatSelectionEntryBinding(step) (the parameterless-resolver
// overload, native-only -- the legacy-initiative-owner fallback is a backward-compatibility
// affordance for pre-Marten legacy contract versions, not replicated here) instead of hand-parsed
// JSON, for exact parity with the one table whose entry binding is not a plain JSON field read.
// ---------------------------------------------------------------------------------------------

public sealed record SelectionRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds,
    string? PendingEntryHash)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class SelectionRoundsProjection : SingleStreamProjection<SelectionRoundsDocument, string>
{
    public static SelectionRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateSelectionEmpty(metadata.StreamKey!), @event, metadata);

    private static SelectionRoundsDocument CreateSelectionEmpty(string id) =>
        new SelectionRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty, null);

    public static SelectionRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateSelectionEmpty(metadata.StreamKey!);

    public static SelectionRoundsDocument Apply(SelectionRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "selection_rounds", metadata) };
    }

    private static SelectionRoundsDocument ApplyCore(SelectionRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var step = RoundProjectionSupport.TryToStep(@event);
        var binding = step is null ? null : CampaignHostAdapter.QueryNativeCombatSelectionEntryBinding(step);
        if (binding is { } entry)
            return current with
            {
                Rounds = current.Rounds.SetItem(entry.EntryHash,
                    new RoundEntry("collecting", null, entry.StateVersion, null)),
                PendingEntryHash = entry.EntryHash,
            };

        if (current.PendingEntryHash is not { } pending || @event.ActionKind is not (
            CampaignHostAcceptedStep.ContactCombatSelectionKind
            or CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind
            or CampaignHostAcceptedStep.LegacyKind
            or CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind))
            return current;

        if (@event.ActionKind != CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind)
            return current with
            {
                Rounds = current.Rounds.SetItem(pending, current.Rounds[pending] with { Status = "completed" }),
                PendingEntryHash = null,
            };

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;
        if (root.GetProperty("reason").GetString() == "selection-unavailable")
            return current with { Rounds = current.Rounds.Remove(pending), PendingEntryHash = null };
        return current with
        {
            Rounds = current.Rounds.SetItem(pending, current.Rounds[pending] with
            {
                Status = "timed-out",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
            PendingEntryHash = null,
        };
    }
}

// ---------------------------------------------------------------------------------------------
// Force rounds -- entry is ContactRbaDeclineKind (decline opens the Force decision window);
// endings are ContactForceSealKind (no status change -- a seal is a mid-round step, not a close),
// NativeCombatForceCompletionKind ("completed"), NativeCombatForceCancellationKind ("cancelled",
// or deleted on "force-unavailable").
// ---------------------------------------------------------------------------------------------

public sealed record ForceRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds,
    ImmutableDictionary<string, string> ActiveDeclineByDecision)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class ForceRoundsProjection : SingleStreamProjection<ForceRoundsDocument, string>
{
    public static ForceRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateForceEmpty(metadata.StreamKey!), @event, metadata);

    private static ForceRoundsDocument CreateForceEmpty(string id) =>
        new ForceRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty, ImmutableDictionary<string, string>.Empty);

    public static ForceRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateForceEmpty(metadata.StreamKey!);

    public static ForceRoundsDocument Apply(ForceRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "force_rounds", metadata) };
    }

    private static ForceRoundsDocument ApplyCore(ForceRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        if (@event.ActionKind is not (CampaignHostAcceptedStep.ContactRbaDeclineKind
            or CampaignHostAcceptedStep.ContactForceSealKind
            or CampaignHostAcceptedStep.NativeCombatForceCompletionKind
            or CampaignHostAcceptedStep.NativeCombatForceCancellationKind))
            return current;

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;
        var decision = root.GetProperty("decisionActionId").GetString()!;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactRbaDeclineKind)
        {
            var declineHash = RoundProjectionSupport.EventHash(@event);
            return current with
            {
                Rounds = current.Rounds.SetItem(declineHash, new RoundEntry("collecting", null, null, decision)),
                ActiveDeclineByDecision = current.ActiveDeclineByDecision.SetItem(decision, declineHash),
            };
        }

        if (!current.ActiveDeclineByDecision.TryGetValue(decision, out var active)
            || !current.Rounds.TryGetValue(active, out var activeRound))
            return current;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactForceSealKind)
            return current; // mid-round step; no status change.

        if (@event.ActionKind == CampaignHostAcceptedStep.NativeCombatForceCompletionKind)
            return current with { Rounds = current.Rounds.SetItem(active, activeRound with { Status = "completed" }) };

        if (root.GetProperty("reason").GetString() == "force-unavailable")
            return current with { Rounds = current.Rounds.Remove(active) };
        return current with
        {
            Rounds = current.Rounds.SetItem(active, activeRound with
            {
                Status = "cancelled",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
        };
    }
}

// ---------------------------------------------------------------------------------------------
// RBA rounds -- entry is NativeCombatBarrageKind; endings are ContactRbaDeclineKind ("completed")
// or NativeCombatRbaCancellationKind ("cancelled", or deleted on "rba-unavailable"). Note
// ContactRbaDeclineKind plays the opposite role here (ending) from its role in Force rounds
// (entry) -- same event kind, two different round tables, by design (one phase's close triggers
// the next phase's open).
// ---------------------------------------------------------------------------------------------

public sealed record RbaRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds,
    ImmutableDictionary<string, string> ActiveBarrageByDecision)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class RbaRoundsProjection : SingleStreamProjection<RbaRoundsDocument, string>
{
    public static RbaRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateRbaEmpty(metadata.StreamKey!), @event, metadata);

    private static RbaRoundsDocument CreateRbaEmpty(string id) =>
        new RbaRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty, ImmutableDictionary<string, string>.Empty);

    public static RbaRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateRbaEmpty(metadata.StreamKey!);

    public static RbaRoundsDocument Apply(RbaRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "rba_rounds", metadata) };
    }

    private static RbaRoundsDocument ApplyCore(RbaRoundsDocument current, CampaignAcceptedStepEvent @event, IEvent metadata)
    {
        if (@event.ActionKind is not (CampaignHostAcceptedStep.NativeCombatBarrageKind
            or CampaignHostAcceptedStep.ContactRbaDeclineKind
            or CampaignHostAcceptedStep.NativeCombatRbaCancellationKind))
            return current;

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;
        var decision = root.GetProperty("decisionActionId").GetString()!;

        if (@event.ActionKind == CampaignHostAcceptedStep.NativeCombatBarrageKind)
        {
            var barrageHash = RoundProjectionSupport.EventHash(@event);
            return current with
            {
                Rounds = current.Rounds.SetItem(barrageHash, new RoundEntry("collecting", null, null, decision)),
                ActiveBarrageByDecision = current.ActiveBarrageByDecision.SetItem(decision, barrageHash),
            };
        }

        if (!current.ActiveBarrageByDecision.TryGetValue(decision, out var active)
            || !current.Rounds.TryGetValue(active, out var activeRound))
            return current;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactRbaDeclineKind)
            return current with { Rounds = current.Rounds.SetItem(active, activeRound with { Status = "completed" }) };

        if (root.GetProperty("reason").GetString() == "rba-unavailable")
            return current with { Rounds = current.Rounds.Remove(active) };
        return current with
        {
            Rounds = current.Rounds.SetItem(active, activeRound with
            {
                Status = "cancelled",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
        };
    }
}

// ---------------------------------------------------------------------------------------------
// Cycle rounds -- entry is NativeReserveReleaseCompletionKind/NativeReserveReleaseTimeoutKind;
// endings (ContactCycleControlKind "completed", NativeCycleControlTimeoutKind "timed-out", or
// deleted on "cycle-control-unavailable") reference the entry directly via the ending event's own
// "priorEventHash" field, so no pending/active scratch state is needed.
// ---------------------------------------------------------------------------------------------

public sealed record CycleRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class CycleRoundsProjection : SingleStreamProjection<CycleRoundsDocument, string>
{
    public static CycleRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateCycleEmpty(metadata.StreamKey!), @event, metadata);

    private static CycleRoundsDocument CreateCycleEmpty(string id) =>
        new CycleRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty);

    public static CycleRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateCycleEmpty(metadata.StreamKey!);

    public static CycleRoundsDocument Apply(CycleRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "cycle_rounds", metadata) };
    }

    private static CycleRoundsDocument ApplyCore(CycleRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        if (@event.ActionKind is not (CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
            or CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind
            or CampaignHostAcceptedStep.ContactCycleControlKind
            or CampaignHostAcceptedStep.NativeCycleControlTimeoutKind))
            return current;

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;

        if (@event.ActionKind is CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
            or CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind)
        {
            var hash = RoundProjectionSupport.EventHash(@event);
            var version = checked(root.GetProperty("priorStateVersion").GetInt64() + 1);
            return current with { Rounds = current.Rounds.SetItem(hash, new RoundEntry("collecting", null, version, null)) };
        }

        var release = root.GetProperty("priorEventHash").GetString()!;
        if (!current.Rounds.TryGetValue(release, out var releaseRound)) return current;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactCycleControlKind)
            return current with { Rounds = current.Rounds.SetItem(release, releaseRound with { Status = "completed" }) };

        if (root.GetProperty("reason").GetString() == "cycle-control-unavailable")
            return current with { Rounds = current.Rounds.Remove(release) };
        return current with
        {
            Rounds = current.Rounds.SetItem(release, releaseRound with
            {
                Status = "timed-out",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
        };
    }
}

// ---------------------------------------------------------------------------------------------
// Reserve rounds -- entry is any of five Combat-resolution step kinds that open a Reserve Release
// decision window; endings are ContactReserveReleaseChoiceKind (mid-round, no status change --
// marks "chosen"), NativeReserveReleaseCompletionKind ("completed", or deleted if no explicit
// choice was made -- the "empty" case the hand-rolled table never persists either), or
// NativeReserveReleaseTimeoutKind ("timed-out", or deleted on "reserve-release-unavailable").
// ---------------------------------------------------------------------------------------------

public sealed record ReserveRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds,
    string? ActiveEntryHash, bool Chosen)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class ReserveRoundsProjection : SingleStreamProjection<ReserveRoundsDocument, string>
{
    public static ReserveRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateReserveEmpty(metadata.StreamKey!), @event, metadata);

    private static ReserveRoundsDocument CreateReserveEmpty(string id) =>
        new ReserveRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty, null, false);

    public static ReserveRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateReserveEmpty(metadata.StreamKey!);

    public static ReserveRoundsDocument Apply(ReserveRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "reserve_rounds", metadata) };
    }

    private static ReserveRoundsDocument ApplyCore(ReserveRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var isEntry = @event.ActionKind is CampaignHostAcceptedStep.NativeCombatNoAttackKind
            or CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
            or CampaignHostAcceptedStep.NativeCombatRbaCancellationKind
            or CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
            or CampaignHostAcceptedStep.NativeCombatSuccessorKind;
        if (!isEntry && @event.ActionKind is not (CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind
            or CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
            or CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind))
            return current;

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;

        if (isEntry)
        {
            var hash = RoundProjectionSupport.EventHash(@event);
            var version = checked(root.GetProperty("priorStateVersion").GetInt64() + 1);
            return current with
            {
                Rounds = current.Rounds.SetItem(hash, new RoundEntry("collecting", null, version, null)),
                ActiveEntryHash = hash,
                Chosen = false,
            };
        }

        if (current.ActiveEntryHash is not { } active) return current;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind)
            return current with { Chosen = true };

        if (@event.ActionKind == CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind)
        {
            var rounds = current.Chosen
                ? current.Rounds.SetItem(active, current.Rounds[active] with { Status = "completed" })
                : current.Rounds.Remove(active); // "empty": never persisted in the hand-rolled table either.
            return current with { Rounds = rounds, ActiveEntryHash = null, Chosen = false };
        }

        if (root.GetProperty("reason").GetString() == "reserve-release-unavailable")
            return current with { Rounds = current.Rounds.Remove(active), ActiveEntryHash = null, Chosen = false };
        return current with
        {
            Rounds = current.Rounds.SetItem(active, current.Rounds[active] with
            {
                Status = "timed-out",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
            ActiveEntryHash = null,
            Chosen = false,
        };
    }
}

// ---------------------------------------------------------------------------------------------
// Custody rounds -- entry is NativeCombatSettlementKind, only when it carries pending captives;
// endings (ContactCombatCustodyKind "completed", NativeCombatCustodyTimeoutKind "timed-out", or
// deleted on "custody-unavailable") reference the entry directly via the ending event's own
// "settlementHash" field.
// ---------------------------------------------------------------------------------------------

public sealed record CustodyRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class CustodyRoundsProjection : SingleStreamProjection<CustodyRoundsDocument, string>
{
    public static CustodyRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateCustodyEmpty(metadata.StreamKey!), @event, metadata);

    private static CustodyRoundsDocument CreateCustodyEmpty(string id) =>
        new CustodyRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty);

    public static CustodyRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateCustodyEmpty(metadata.StreamKey!);

    public static CustodyRoundsDocument Apply(CustodyRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "custody_rounds", metadata) };
    }

    private static CustodyRoundsDocument ApplyCore(CustodyRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        if (@event.ActionKind is not (CampaignHostAcceptedStep.NativeCombatSettlementKind
            or CampaignHostAcceptedStep.ContactCombatCustodyKind
            or CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind))
            return current;

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;

        if (@event.ActionKind == CampaignHostAcceptedStep.NativeCombatSettlementKind)
        {
            if (root.GetProperty("pendingCaptives").GetArrayLength() == 0) return current;
            var hash = RoundProjectionSupport.EventHash(@event);
            var version = checked(root.GetProperty("priorStateVersion").GetInt64() + 1);
            return current with { Rounds = current.Rounds.SetItem(hash, new RoundEntry("collecting", null, version, null)) };
        }

        var settlement = root.GetProperty("settlementHash").GetString()!;
        if (!current.Rounds.TryGetValue(settlement, out var settlementRound)) return current;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactCombatCustodyKind)
            return current with { Rounds = current.Rounds.SetItem(settlement, settlementRound with { Status = "completed" }) };

        if (root.GetProperty("reason").GetString() == "custody-unavailable")
            return current with { Rounds = current.Rounds.Remove(settlement) };
        return current with
        {
            Rounds = current.Rounds.SetItem(settlement, settlementRound with
            {
                Status = "timed-out",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
        };
    }
}

// ---------------------------------------------------------------------------------------------
// Retreat rounds -- entry is NativeCombatAssaultResultKind, only when it requires a retreat;
// endings (ContactPostAssaultRetreatKind "completed", NativeCombatRetreatTimeoutKind "timed-out",
// or deleted on "retreat-unavailable") reference the entry directly via the ending event's own
// "resultHash" field.
// ---------------------------------------------------------------------------------------------

public sealed record RetreatRoundsDocument(string Id, ImmutableDictionary<string, RoundEntry> Rounds)
{
    /// <summary>Phase C3b: retained round rows folded from <see cref="CampaignRoundRowHeader"/>
    /// event headers -- the journal's own round-table rows, timing columns included.</summary>
    public ImmutableDictionary<string, RetainedRoundRow> Retained { get; init; } =
        ImmutableDictionary<string, RetainedRoundRow>.Empty;
}

public sealed class RetreatRoundsProjection : SingleStreamProjection<RetreatRoundsDocument, string>
{
    public static RetreatRoundsDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        Apply(CreateRetreatEmpty(metadata.StreamKey!), @event, metadata);

    private static RetreatRoundsDocument CreateRetreatEmpty(string id) =>
        new RetreatRoundsDocument(id, ImmutableDictionary<string, RoundEntry>.Empty);

    public static RetreatRoundsDocument Create(CampaignGenesisEvent @event, IEvent metadata) =>
        CreateRetreatEmpty(metadata.StreamKey!);

    public static RetreatRoundsDocument Apply(RetreatRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        var next = ApplyCore(current, @event, metadata);
        return next with { Retained = CampaignRoundRowHeader.FoldRetained(next.Retained, "retreat_rounds", metadata) };
    }

    private static RetreatRoundsDocument ApplyCore(RetreatRoundsDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata)
    {
        if (@event.ActionKind is not (CampaignHostAcceptedStep.NativeCombatAssaultResultKind
            or CampaignHostAcceptedStep.ContactPostAssaultRetreatKind
            or CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind))
            return current;

        using var document = JsonDocument.Parse(@event.CanonicalEvent);
        var root = document.RootElement;

        if (@event.ActionKind == CampaignHostAcceptedStep.NativeCombatAssaultResultKind)
        {
            if (root.GetProperty("facts").GetProperty("requiredRetreat").GetInt32() == 0) return current;
            var hash = RoundProjectionSupport.EventHash(@event);
            var version = checked(root.GetProperty("priorStateVersion").GetInt64() + 1);
            return current with { Rounds = current.Rounds.SetItem(hash, new RoundEntry("collecting", null, version, null)) };
        }

        var result = root.GetProperty("resultHash").GetString()!;
        if (!current.Rounds.TryGetValue(result, out var resultRound)) return current;

        if (@event.ActionKind == CampaignHostAcceptedStep.ContactPostAssaultRetreatKind)
            return current with { Rounds = current.Rounds.SetItem(result, resultRound with { Status = "completed" }) };

        if (root.GetProperty("reason").GetString() == "retreat-unavailable")
            return current with { Rounds = current.Rounds.Remove(result) };
        return current with
        {
            Rounds = current.Rounds.SetItem(result, resultRound with
            {
                Status = "timed-out",
                ConfigurationHash = root.GetProperty("configurationHash").GetString(),
            }),
        };
    }
}
