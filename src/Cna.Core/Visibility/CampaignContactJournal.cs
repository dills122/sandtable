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

    internal CampaignContactJournal AdvanceNativeCombatBarrage(
        CampaignNativeCombatBarrageV14 barrage)
    {
        ArgumentNullException.ThrowIfNull(barrage);
        if (barrage.CampaignId != CampaignId
            || barrage.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Barrage does not bind contact authority.", nameof(barrage));
        return AtAuthorityVersion(barrage.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatRetreatDecline(
        CampaignNativeCombatRetreatDeclineV15 decline)
    {
        ArgumentNullException.ThrowIfNull(decline);
        if (decline.CampaignId != CampaignId
            || decline.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Defender decline does not bind contact authority.",
                nameof(decline));
        return AtAuthorityVersion(decline.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatForceSeal(
        CampaignNativeCombatForceSealV17 seal)
    {
        ArgumentNullException.ThrowIfNull(seal);
        if (seal.CampaignId != CampaignId || seal.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Force seal does not bind contact authority.", nameof(seal));
        return AtAuthorityVersion(seal.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatForceCompletion(
        CampaignNativeCombatForceCompletionV18 completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if (completion.CampaignId != CampaignId
            || completion.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Force completion does not bind contact authority.",
                nameof(completion));
        return AtAuthorityVersion(completion.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatForceCancellation(
        CampaignNativeCombatForceCancellationV19 cancellation)
    {
        ArgumentNullException.ThrowIfNull(cancellation);
        if (cancellation.CampaignId != CampaignId
            || cancellation.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Force cancellation does not bind contact authority.",
                nameof(cancellation));
        return AtAuthorityVersion(cancellation.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatAntiArmor(
        CampaignNativeCombatAntiArmorV20 antiArmor)
    {
        ArgumentNullException.ThrowIfNull(antiArmor);
        if (antiArmor.CampaignId != CampaignId
            || antiArmor.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Anti Armor does not bind contact authority.",
                nameof(antiArmor));
        return AtAuthorityVersion(antiArmor.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatCancelledAssault(
        CampaignNativeCombatCancelledAssaultV21 cancelledAssault)
    {
        ArgumentNullException.ThrowIfNull(cancelledAssault);
        if (cancelledAssault.CampaignId != CampaignId
            || cancelledAssault.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Cancelled assault does not bind contact authority.",
                nameof(cancelledAssault));
        return AtAuthorityVersion(cancelledAssault.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatAssaultResult(
        CampaignNativeCombatAssaultResultV22 result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.CampaignId != CampaignId
            || result.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Assault result does not bind contact authority.",
                nameof(result));
        return AtAuthorityVersion(result.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatRetreatAfterAssault(
        CampaignNativeCombatRetreatAfterAssaultV23 retreat)
    {
        ArgumentNullException.ThrowIfNull(retreat);
        if (retreat.CampaignId != CampaignId
            || retreat.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Post-result retreat does not bind contact authority.",
                nameof(retreat));
        return AtAuthorityVersion(retreat.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatSettlement(
        CampaignWorldSnapshotV6 before, CampaignWorldSnapshotV6 after,
        CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        if (settlement.CampaignId != CampaignId
            || settlement.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Settlement does not bind contact authority.",
                nameof(settlement));
        return ApplySources(
                CampaignContactAdjacencySource.OnNativeSettlement(before, after,
                    settlement, retreat, context, axis),
                CampaignContactAdjacencySource.OnNativeSettlement(before, after,
                    settlement, retreat, context, commonwealth))
            .AtAuthorityVersion(settlement.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatCustodyDisposition(
        CampaignNativeCombatCustodyDispositionV25 disposition)
    {
        ArgumentNullException.ThrowIfNull(disposition);
        if (disposition.CampaignId != CampaignId
            || disposition.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Custody does not bind contact authority.",
                nameof(disposition));
        return AtAuthorityVersion(disposition.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatSuccessor(
        CampaignNativeCombatSuccessorV26 successor)
    {
        ArgumentNullException.ThrowIfNull(successor);
        if (successor.CampaignId != CampaignId
            || successor.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Combat successor does not bind contact authority.",
                nameof(successor));
        return AtAuthorityVersion(successor.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeReserveRelease(
        CampaignNativeReserveReleaseEventV27 reserveEvent)
    {
        ArgumentNullException.ThrowIfNull(reserveEvent);
        if (reserveEvent.CampaignId != CampaignId
            || reserveEvent.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Reserve Release does not bind contact authority.",
                nameof(reserveEvent));
        return AtAuthorityVersion(reserveEvent.StateVersion);
    }

    internal CampaignContactJournal AdvanceNativeCombatRetreatMove(
        CampaignWorldSnapshotV6 before, CampaignWorldSnapshotV6 after,
        CampaignNativeCombatRetreatMoveV16 movement, string representationId,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(movement);
        if (movement.CampaignId != CampaignId
            || movement.PriorStateVersion != AuthorityVersion)
            throw new ArgumentException("Defender movement does not bind contact authority.",
                nameof(movement));
        return ApplySources(
                CampaignContactAdjacencySource.OnNativeRetreatMove(before, after,
                    movement, representationId, context, axis),
                CampaignContactAdjacencySource.OnNativeRetreatMove(before, after,
                    movement, representationId, context, commonwealth))
            .AtAuthorityVersion(movement.StateVersion);
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
