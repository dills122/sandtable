using System.Collections.Immutable;
using System.Text.Json;
using JasperFx.Events;

namespace Cna.OrleansHost.Persistence;

/// <summary>
/// One retained round row, exactly as <c>CampaignDurableAuthorityJournal</c> wrote it into one of
/// its seven round tables (<c>force_rounds</c>, <c>rba_rounds</c>, ...). <see cref="Key"/> is the
/// table's own round-identity hash column (<c>decline_event_hash</c>, <c>barrage_event_hash</c>,
/// <c>release_event_hash</c>, <c>settlement_hash</c>, <c>result_hash</c> or <c>entry_hash</c>).
/// </summary>
public sealed record RetainedRoundRow(
    string Key,
    string? DecisionActionId,
    long? EntryStateVersion,
    string ConfigurationHash,
    long OpenedMs,
    long DeadlineMs,
    long HighWaterMs,
    string Status,
    string RecordHash);

/// <summary>One round-row transition carried by an event header: which table, and the row's state.</summary>
public sealed record RoundRowChange(string Table, RetainedRoundRow Row);

/// <summary>
/// Phase C3b of issue #439 (design spike &#167;3.3): the write-once round-timing columns
/// (<c>opened_ms</c>/<c>deadline_ms</c>/<c>configuration_hash</c>) and every round status
/// transition ride as a per-event header (<see cref="HeaderKey"/>) on the accepted step that the
/// transition happened after, inside the same save. The seven C2 round projections fold these
/// headers into their documents' <c>Retained</c> maps (<see cref="FoldRetained"/>), so the
/// projections -- not an ad-hoc side table -- own all write-once round state. This formally
/// reverses C2's "drop the timing columns" narrowing
/// (<c>docs/work/plans/2026-11-19-phase-c2-round-table-projection-fork.md</c>) and fills its
/// "configuration_hash only on timeout" gap: every retained round now carries it.
/// </summary>
/// <remarks>
/// The values are captured once, at write time, from the journal's injected <c>TimeProvider</c>
/// (<c>DET-002</c>), so a projection rebuild reproduces them exactly -- <c>Apply</c> never reads a
/// clock. They sit outside the canonical event bytes and outside <c>StepHash</c> (<c>EVT-001</c>/the
/// hash chain are untouched); each row still carries the journal's own <c>record_hash</c> self-hash,
/// exactly as the SQLite row did. Only the open round's mutable high-water mark lives elsewhere
/// (<see cref="CampaignRoundHighWaterDocument"/>).
/// </remarks>
public static class CampaignRoundRowHeader
{
    public const string HeaderKey = "round_rows";

    /// <summary>Round table name &#8594; that table's round-identity column.</summary>
    public static readonly ImmutableDictionary<string, string> KeyColumns = new Dictionary<string, string>
    {
        ["force_rounds"] = "decline_event_hash",
        ["rba_rounds"] = "barrage_event_hash",
        ["cycle_rounds"] = "release_event_hash",
        ["reserve_rounds"] = "release_event_hash",
        ["custody_rounds"] = "settlement_hash",
        ["retreat_rounds"] = "result_hash",
        ["selection_rounds"] = "entry_hash",
    }.ToImmutableDictionary(StringComparer.Ordinal);

    /// <summary>Tables keyed by a decision action id (the others carry an entry state version).</summary>
    public static bool HasDecisionActionId(string table) => table is "force_rounds" or "rba_rounds";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IReadOnlyList<RoundRowChange> changes) =>
        JsonSerializer.Serialize(changes, Options);

    public static IReadOnlyList<RoundRowChange> Read(IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (@event.Headers is not { } headers || !headers.TryGetValue(HeaderKey, out var value) || value is null)
            return [];
        var text = value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString()!,
            _ => value.ToString()!,
        };
        return JsonSerializer.Deserialize<RoundRowChange[]>(text, Options) ?? [];
    }

    /// <summary>
    /// Folds every <paramref name="table"/> row change carried by <paramref name="metadata"/> into
    /// <paramref name="retained"/> (later transitions replace earlier ones for the same round key).
    /// </summary>
    public static ImmutableDictionary<string, RetainedRoundRow> FoldRetained(
        ImmutableDictionary<string, RetainedRoundRow> retained, string table, IEvent metadata)
    {
        foreach (var change in Read(metadata))
            if (change.Table == table) retained = retained.SetItem(change.Row.Key, change.Row);
        return retained;
    }
}
