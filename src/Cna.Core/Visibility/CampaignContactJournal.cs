using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

/// <summary>
/// Core-owned contact stream. Authority events and contact records are retained together by
/// the campaign host; cold replay re-derives every contact record from those authority events.
/// </summary>
internal sealed class CampaignContactJournal
{
    private const int MaximumRecords = 65_536;
    private const int MaximumRecordBytes = 4_096;
    private readonly CampaignContactKnowledge axis;
    private readonly CampaignContactKnowledge commonwealth;
    private readonly byte[][] records;

    private CampaignContactJournal(string campaignId, CampaignContactKnowledge axis,
        CampaignContactKnowledge commonwealth, byte[][] records)
    {
        CampaignId = campaignId;
        this.axis = axis;
        this.commonwealth = commonwealth;
        this.records = records;
    }

    public string CampaignId { get; }
    public IReadOnlyList<byte[]> Records =>
        Array.AsReadOnly(records.Select(record => record.ToArray()).ToArray());

    public static CampaignContactJournal Empty(string campaignId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        return new CampaignContactJournal(campaignId,
            new CampaignContactKnowledge(campaignId, LandSide.Axis),
            new CampaignContactKnowledge(campaignId, LandSide.Commonwealth), []);
    }

    internal static CampaignContactJournal FromEvents(string campaignId, IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var journal = Empty(campaignId);
        foreach (var value in events)
            if (value is ElementMovedV3 movement)
                journal = journal.Append(movement);
        return journal;
    }

    /// <summary>Derive contacts from a committed movement, leaving the prior journal unchanged.</summary>
    public CampaignContactJournal Append(ElementMovedV3 movement)
    {
        ArgumentNullException.ThrowIfNull(movement);
        if (!string.Equals(CampaignId, movement.CampaignId, StringComparison.Ordinal))
            throw new ArgumentException("Movement belongs to another campaign.", nameof(movement));
        if (movement.OpenedReactionWindow is null) return this;

        var nextAxis = axis.Copy();
        var nextCommonwealth = commonwealth.Copy();
        var side = movement.OpenedReactionWindow.ReactingSide;
        var knowledge = side switch
        {
            LandSide.Axis => nextAxis,
            LandSide.Commonwealth => nextCommonwealth,
            _ => throw new ArgumentException("Movement has an invalid reacting side.", nameof(movement)),
        };
        var transitions = CampaignContactReactionSource.Derive(movement, knowledge);
        if (transitions.Count > MaximumRecords - records.Length)
            throw new JsonException("Contact journal exceeds its record limit.");

        var appended = new byte[records.Length + transitions.Count][];
        Array.Copy(records, appended, records.Length);
        for (var index = 0; index < transitions.Count; index++)
        {
            var canonical = CampaignContactTransitionCodec.SerializeCanonical(transitions[index]);
            if (canonical.Length > MaximumRecordBytes)
                throw new JsonException("Contact record exceeds its byte limit.");
            knowledge.Apply(transitions[index]);
            appended[records.Length + index] = canonical;
        }

        return new CampaignContactJournal(CampaignId, nextAxis, nextCommonwealth, appended);
    }

    /// <summary>
    /// Cold readback requires the independently replayed, ordered authority events. A contact
    /// record alone cannot establish sight or cause an owner-visible transition.
    /// </summary>
    public static CampaignContactJournal Replay(string campaignId,
        IReadOnlyList<ElementMovedV3> committedMovements, IReadOnlyList<byte[]> retainedRecords)
    {
        ArgumentNullException.ThrowIfNull(committedMovements);
        ArgumentNullException.ThrowIfNull(retainedRecords);
        if (retainedRecords.Count > MaximumRecords)
            throw new JsonException("Contact journal exceeds its record limit.");
        foreach (var record in retainedRecords)
            if (record is not { Length: > 0 and <= MaximumRecordBytes })
                throw new JsonException("Missing or oversized retained contact record.");

        var journal = Empty(campaignId);
        foreach (var movement in committedMovements)
            journal = journal.Append(movement);
        if (journal.records.Length != retainedRecords.Count)
            throw new JsonException("Contact records do not match committed movements.");
        for (var index = 0; index < journal.records.Length; index++)
            if (!journal.records[index].AsSpan().SequenceEqual(retainedRecords[index]))
                throw new JsonException("Contact record differs from its committed movement source.");
        return journal;
    }

    public byte[] ObserveCanonical(LandSide side, string actingFormationId) =>
        CampaignContactObservationV1.Project(Knowledge(side), actingFormationId).SerializeCanonical();

    public IReadOnlyList<CampaignContactTargetCandidate> CurrentTargetsFor(
        LandSide side, string actingFormationId) => Knowledge(side).CurrentTargetsFor(actingFormationId);

    private CampaignContactKnowledge Knowledge(LandSide side) => side switch
    {
        LandSide.Axis => axis,
        LandSide.Commonwealth => commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
