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

    private CampaignContactJournal(string campaignId, long authorityVersion,
        CampaignContactKnowledge axis, CampaignContactKnowledge commonwealth, byte[][] records)
    {
        CampaignId = campaignId;
        AuthorityVersion = authorityVersion;
        this.axis = axis;
        this.commonwealth = commonwealth;
        this.records = records;
    }

    public string CampaignId { get; }
    public long AuthorityVersion { get; }
    public IReadOnlyList<byte[]> Records =>
        Array.AsReadOnly(records.Select(record => record.ToArray()).ToArray());

    public static CampaignContactJournal Empty(string campaignId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        return new CampaignContactJournal(campaignId, 0,
            new CampaignContactKnowledge(campaignId, LandSide.Axis),
            new CampaignContactKnowledge(campaignId, LandSide.Commonwealth), []);
    }

    public static CampaignContactJournal Initialize(CampaignSnapshotV11 created,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(created);
        var empty = Empty(created.CampaignId);
        return empty.ApplySources(
            CampaignContactAdjacencySource.OnCreation(created, context, empty.axis),
            CampaignContactAdjacencySource.OnCreation(created, context, empty.commonwealth))
            .AtAuthorityVersion(created.StateVersion);
    }

    internal static CampaignContactJournal FromEvents(CampaignContentContext context,
        IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(events);
        CampaignSnapshotV11? snapshot = null;
        CampaignContactJournal? journal = null;
        foreach (var value in events)
        {
            var after = CampaignCurrentProjector.Apply(snapshot, value, context);
            journal = snapshot is null
                ? Initialize(after, context)
                : value switch
                {
                    ElementMovedV3 movement => journal!.AppendMovement(snapshot, after, movement, context),
                    ReactingElementMovedV2 movement => journal!.AppendMovement(snapshot, after, movement, context),
                    _ => journal!.AdvanceNonMovement(snapshot, after),
                };
            snapshot = after;
        }
        return journal ?? throw new InvalidCampaignHistoryException("Contact replay requires creation authority.");
    }

    /// <summary>Derive both audiences' sight from one accepted movement cut.</summary>
    public CampaignContactJournal AppendMovement(CampaignSnapshotV11 before,
        CampaignSnapshotV11 after, ElementMovedV3 movement, CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(movement);
        if (before.CampaignId != CampaignId || after.CampaignId != CampaignId)
            throw new ArgumentException("Movement belongs to another campaign.", nameof(movement));
        RequireNextAuthority(before, after);
        return ApplySources(
            CampaignContactAdjacencySource.OnMovement(before, after, movement, context, axis),
            CampaignContactAdjacencySource.OnMovement(before, after, movement, context, commonwealth))
            .AtAuthorityVersion(after.StateVersion);
    }

    public CampaignContactJournal AppendMovement(CampaignSnapshotV11 before,
        CampaignSnapshotV11 after, ReactingElementMovedV2 movement, CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(movement);
        if (before.CampaignId != CampaignId || after.CampaignId != CampaignId)
            throw new ArgumentException("Movement belongs to another campaign.", nameof(movement));
        RequireNextAuthority(before, after);
        return ApplySources(
            CampaignContactAdjacencySource.OnMovement(before, after, movement, context, axis),
            CampaignContactAdjacencySource.OnMovement(before, after, movement, context, commonwealth))
            .AtAuthorityVersion(after.StateVersion);
    }

    public CampaignContactJournal AdvanceNonMovement(
        CampaignSnapshotV11 before, CampaignSnapshotV11 after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        RequireNextAuthority(before, after);
        return AtAuthorityVersion(after.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatSelection(
        CampaignNativeCombatDecisionStateV12 selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        if (selected.CampaignId != CampaignId
            || selected.PriorSnapshot.StateVersion != AuthorityVersion
            || selected.StateVersion != checked(AuthorityVersion + 1))
            throw new ArgumentException("Combat selection does not bind the contact authority cut.", nameof(selected));
        return AtAuthorityVersion(selected.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatNoAttackCompletion(
        CampaignNativeCombatNoAttackCompletionV13 completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if (completion.CampaignId != CampaignId
            || completion.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("No-attack completion does not bind contact authority.",
                nameof(completion));
        return AtAuthorityVersion(completion.StateVersion);
    }

    private void RequireNextAuthority(CampaignSnapshotV11 before, CampaignSnapshotV11 after)
    {
        if (before.CampaignId != CampaignId || after.CampaignId != CampaignId
            || before.StateVersion != AuthorityVersion
            || after.StateVersion != checked(before.StateVersion + 1))
            throw new ArgumentException("Contact journal does not bind the accepted authority cut.");
    }

    private CampaignContactJournal AtAuthorityVersion(long version) =>
        new(CampaignId, version, axis, commonwealth, records);

    private CampaignContactJournal ApplySources(
        IReadOnlyList<CampaignContactTransition> axisTransitions,
        IReadOnlyList<CampaignContactTransition> commonwealthTransitions)
    {
        var nextAxis = axis.Copy();
        var nextCommonwealth = commonwealth.Copy();
        if (axisTransitions.Count + commonwealthTransitions.Count > MaximumRecords - records.Length)
            throw new JsonException("Contact journal exceeds its record limit.");

        var appended = new byte[records.Length + axisTransitions.Count + commonwealthTransitions.Count][];
        Array.Copy(records, appended, records.Length);
        var index = records.Length;
        foreach (var (knowledge, transitions) in new[]
                 {
                     (nextAxis, axisTransitions), (nextCommonwealth, commonwealthTransitions),
                 })
            foreach (var transition in transitions)
            {
                var canonical = CampaignContactTransitionCodec.SerializeCanonical(transition);
                if (canonical.Length > MaximumRecordBytes)
                    throw new JsonException("Contact record exceeds its byte limit.");
                knowledge.Apply(transition);
                appended[index++] = canonical;
            }
        return new CampaignContactJournal(CampaignId, AuthorityVersion,
            nextAxis, nextCommonwealth, appended);
    }

    /// <summary>
    /// Cold readback requires the independently replayed, ordered authority events. A contact
    /// record alone cannot establish sight or cause an owner-visible transition.
    /// </summary>
    public static CampaignContactJournal Replay(CampaignSnapshotV11 initial,
        CampaignContentContext context, IReadOnlyList<ElementMovedV3> committedMovements,
        IReadOnlyList<byte[]> retainedRecords)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(committedMovements);
        ArgumentNullException.ThrowIfNull(retainedRecords);
        if (retainedRecords.Count > MaximumRecords)
            throw new JsonException("Contact journal exceeds its record limit.");
        foreach (var record in retainedRecords)
            if (record is not { Length: > 0 and <= MaximumRecordBytes })
                throw new JsonException("Missing or oversized retained contact record.");

        var journal = Initialize(initial, context);
        var before = initial;
        foreach (var movement in committedMovements)
        {
            var after = CampaignCurrentProjector.Apply(before, movement, context);
            journal = journal.AppendMovement(before, after, movement, context);
            before = after;
        }
        if (journal.records.Length != retainedRecords.Count)
            throw new JsonException("Contact records do not match committed movements.");
        for (var index = 0; index < journal.records.Length; index++)
            if (!journal.records[index].AsSpan().SequenceEqual(retainedRecords[index]))
                throw new JsonException("Contact record differs from its committed movement source.");
        return journal;
    }

    public CampaignContactObservationV1 ObserveFor(LandSide side, string actingFormationId) =>
        CampaignContactObservationV1.Project(Knowledge(side), actingFormationId);

    public byte[] ObserveCanonical(LandSide side, string actingFormationId) =>
        ObserveFor(side, actingFormationId).SerializeCanonical();

    public IReadOnlyList<CampaignContactTargetCandidate> CurrentTargetsFor(
        LandSide side, string actingFormationId) => Knowledge(side).CurrentTargetsFor(actingFormationId);

    internal string ResolvePrivateTarget(LandSide side, string actingFormationId,
        string contactHandle) => Knowledge(side).ResolvePrivateTarget(actingFormationId, contactHandle);

    private CampaignContactKnowledge Knowledge(LandSide side) => side switch
    {
        LandSide.Axis => axis,
        LandSide.Commonwealth => commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
