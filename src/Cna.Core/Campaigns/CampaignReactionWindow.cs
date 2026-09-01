using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignReactionTriggerAuthority
{
    public CampaignReactionTriggerAuthority(
        int moveContractVersion,
        string elementId,
        CampaignMapRepresentationState triggeringRepresentation,
        string originLocationId,
        string destinationLocationId)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            moveContractVersion,
            ElementMovedV2.CurrentContractVersion);
        ArgumentNullException.ThrowIfNull(triggeringRepresentation);
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        OriginLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (string.Equals(OriginLocationId, DestinationLocationId, StringComparison.Ordinal)
            || !string.Equals(
                triggeringRepresentation.CurrentLocationId,
                DestinationLocationId,
                StringComparison.Ordinal)
            || !triggeringRepresentation.BoundElementIds.Contains(
                ElementId,
                StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Reaction trigger authority must bind the committed element move.");
        }

        MoveContractVersion = moveContractVersion;
        TriggeringRepresentation = triggeringRepresentation;
    }

    public int MoveContractVersion { get; }

    public string ElementId { get; }

    public CampaignMapRepresentationState TriggeringRepresentation { get; }

    public string OriginLocationId { get; }

    public string DestinationLocationId { get; }
}

internal sealed record CampaignApparentReactionTrigger
{
    public CampaignApparentReactionTrigger(
        string apparentRepresentationId,
        string originLocationId,
        string destinationLocationId)
    {
        ApparentRepresentationId = ContentContractGuards.RequireStableId(
            apparentRepresentationId,
            nameof(apparentRepresentationId));
        OriginLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (string.Equals(OriginLocationId, DestinationLocationId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An apparent Reaction trigger must change location.",
                nameof(destinationLocationId));
        }
    }

    public string ApparentRepresentationId { get; }

    public string OriginLocationId { get; }

    public string DestinationLocationId { get; }
}

internal sealed record CampaignReactionAdjacencyEvidence
{
    public CampaignReactionAdjacencyEvidence(
        string triggerLocationId,
        string committedDestinationLocationId,
        bool isAdjacent,
        IEnumerable<RuleReference> sources)
    {
        TriggerLocationId = ContentContractGuards.RequireStableId(
            triggerLocationId,
            nameof(triggerLocationId));
        CommittedDestinationLocationId = ContentContractGuards.RequireStableId(
            committedDestinationLocationId,
            nameof(committedDestinationLocationId));
        if (!isAdjacent
            || string.Equals(
                TriggerLocationId,
                CommittedDestinationLocationId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Frozen Reaction evidence must retain a positive local adjacency result.");
        }

        IsAdjacent = isAdjacent;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string TriggerLocationId { get; }

    public string CommittedDestinationLocationId { get; }

    public bool IsAdjacent { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignReactionAdjacencyEvidence? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(TriggerLocationId, other.TriggerLocationId,
                StringComparison.Ordinal)
            && string.Equals(CommittedDestinationLocationId,
                other.CommittedDestinationLocationId, StringComparison.Ordinal)
            && IsAdjacent == other.IsAdjacent
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TriggerLocationId, StringComparer.Ordinal);
        hash.Add(CommittedDestinationLocationId, StringComparer.Ordinal);
        hash.Add(IsAdjacent);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }
}

internal sealed record CampaignFrozenReactionOpportunity
{
    public CampaignFrozenReactionOpportunity(
        CampaignReactionOpportunityId opportunityId,
        CampaignMapRepresentationState reactingRepresentation,
        CampaignReactionAdjacencyEvidence adjacencyEvidence)
    {
        ArgumentNullException.ThrowIfNull(opportunityId);
        ArgumentNullException.ThrowIfNull(reactingRepresentation);
        ArgumentNullException.ThrowIfNull(adjacencyEvidence);
        if (!string.Equals(
            reactingRepresentation.CurrentLocationId,
            adjacencyEvidence.TriggerLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Frozen Reaction evidence must belong to the reacting representation's trigger-time location.",
                nameof(adjacencyEvidence));
        }

        OpportunityId = opportunityId;
        ReactingRepresentation = reactingRepresentation;
        AdjacencyEvidence = adjacencyEvidence;
    }

    public CampaignReactionOpportunityId OpportunityId { get; }

    public CampaignMapRepresentationState ReactingRepresentation { get; }

    public CampaignReactionAdjacencyEvidence AdjacencyEvidence { get; }
}

internal sealed record CampaignReactionWindow
{
    public CampaignReactionWindow(
        CampaignReactionWindowId windowId,
        long triggerCommittedStateVersion,
        LandSide phasingSide,
        LandSide reactingSide,
        CampaignReactingPosition reactingPosition,
        CampaignReactionTriggerAuthority triggerAuthority,
        CampaignApparentReactionTrigger apparentTrigger,
        IEnumerable<CampaignFrozenReactionOpportunity> frozenOpportunities,
        IEnumerable<CampaignReactionOpportunityId> resolvedOpportunityIds,
        CampaignReactionOpportunityId? activeOpportunityId)
    {
        ArgumentNullException.ThrowIfNull(windowId);
        ArgumentOutOfRangeException.ThrowIfLessThan(triggerCommittedStateVersion, 2);
        if (!Enum.IsDefined(phasingSide)
            || !Enum.IsDefined(reactingSide)
            || phasingSide == reactingSide)
        {
            throw new ArgumentException("A Reaction window must bind opposing valid sides.");
        }

        ArgumentNullException.ThrowIfNull(reactingPosition);
        ArgumentNullException.ThrowIfNull(triggerAuthority);
        ArgumentNullException.ThrowIfNull(apparentTrigger);
        var frozen = ContentContractGuards.CopyValues(
            frozenOpportunities,
            nameof(frozenOpportunities));
        var orderedFrozen = frozen
            .OrderBy(value => value.OpportunityId.Value, StringComparer.Ordinal)
            .ToArray();
        if (orderedFrozen.Select(value => value.OpportunityId.Value)
                .Distinct(StringComparer.Ordinal).Count() != orderedFrozen.Length
            || orderedFrozen.Any(value =>
                !value.AdjacencyEvidence.IsAdjacent
                || !string.Equals(
                    value.AdjacencyEvidence.CommittedDestinationLocationId,
                    triggerAuthority.DestinationLocationId,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Frozen Reaction opportunities must be unique and locally adjacent.",
                nameof(frozenOpportunities));
        }

        ArgumentNullException.ThrowIfNull(resolvedOpportunityIds);
        var resolved = resolvedOpportunityIds.ToArray();
        if (resolved.Any(value => value is null)
            || resolved.Select(value => value.Value)
                .Distinct(StringComparer.Ordinal).Count() != resolved.Length)
        {
            throw new ArgumentException(
                "Resolved Reaction opportunity IDs must be non-null and unique.",
                nameof(resolvedOpportunityIds));
        }

        var frozenIds = orderedFrozen.Select(value => value.OpportunityId.Value)
            .ToHashSet(StringComparer.Ordinal);
        var orderedResolved = resolved
            .OrderBy(value => value.Value, StringComparer.Ordinal)
            .ToArray();
        if (orderedResolved.Any(value => !frozenIds.Contains(value.Value))
            || (activeOpportunityId is not null
                && (!frozenIds.Contains(activeOpportunityId.Value)
                    || orderedResolved.Any(value => value == activeOpportunityId))))
        {
            throw new ArgumentException(
                "Current Reaction participant state must be a coherent subset of the frozen universe.");
        }

        if (reactingPosition.PhasingSide != phasingSide
            || reactingPosition.ReactingSide != reactingSide
            || !string.Equals(apparentTrigger.OriginLocationId,
                triggerAuthority.OriginLocationId, StringComparison.Ordinal)
            || !string.Equals(apparentTrigger.DestinationLocationId,
                triggerAuthority.DestinationLocationId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Reaction window position and trigger projections are incoherent.");
        }

        WindowId = windowId;
        TriggerCommittedStateVersion = triggerCommittedStateVersion;
        PhasingSide = phasingSide;
        ReactingSide = reactingSide;
        ReactingPosition = reactingPosition;
        TriggerAuthority = triggerAuthority;
        ApparentTrigger = apparentTrigger;
        FrozenOpportunities = Array.AsReadOnly(orderedFrozen);
        ResolvedOpportunityIds = Array.AsReadOnly(orderedResolved);
        ActiveOpportunityId = activeOpportunityId;
    }

    public CampaignReactionWindowId WindowId { get; }

    public long TriggerCommittedStateVersion { get; }

    public LandSide PhasingSide { get; }

    public LandSide ReactingSide { get; }

    public CampaignReactingPosition ReactingPosition { get; }

    public CampaignReactionTriggerAuthority TriggerAuthority { get; }

    public CampaignApparentReactionTrigger ApparentTrigger { get; }

    public IReadOnlyList<CampaignFrozenReactionOpportunity> FrozenOpportunities { get; }

    public IReadOnlyList<CampaignReactionOpportunityId> ResolvedOpportunityIds { get; }

    public CampaignReactionOpportunityId? ActiveOpportunityId { get; }

    public void ValidateIdentities(string campaignId, string rulesetHash)
    {
        var expectedWindow = CampaignReactionIdentity.CreateWindow(
            campaignId,
            rulesetHash,
            TriggerAuthority.MoveContractVersion,
            TriggerCommittedStateVersion,
            TriggerAuthority.TriggeringRepresentation,
            TriggerAuthority.OriginLocationId,
            TriggerAuthority.DestinationLocationId,
            ReactingSide);
        if (WindowId != expectedWindow
            || FrozenOpportunities.Any(value =>
                value.OpportunityId != CampaignReactionIdentity.CreateOpportunity(
                    WindowId,
                    value.ReactingRepresentation)))
        {
            throw new ArgumentException(
                "Reaction window or opportunity identity does not match its canonical preimage.");
        }
    }

    public bool Equals(CampaignReactionWindow? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && WindowId == other.WindowId
            && TriggerCommittedStateVersion == other.TriggerCommittedStateVersion
            && PhasingSide == other.PhasingSide
            && ReactingSide == other.ReactingSide
            && ReactingPosition == other.ReactingPosition
            && TriggerAuthority == other.TriggerAuthority
            && ApparentTrigger == other.ApparentTrigger
            && FrozenOpportunities.SequenceEqual(other.FrozenOpportunities)
            && ResolvedOpportunityIds.SequenceEqual(other.ResolvedOpportunityIds)
            && ActiveOpportunityId == other.ActiveOpportunityId);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(WindowId);
        hash.Add(TriggerCommittedStateVersion);
        hash.Add(PhasingSide);
        hash.Add(ReactingSide);
        hash.Add(ReactingPosition);
        hash.Add(TriggerAuthority);
        hash.Add(ApparentTrigger);
        foreach (var opportunity in FrozenOpportunities) hash.Add(opportunity);
        foreach (var id in ResolvedOpportunityIds) hash.Add(id);
        hash.Add(ActiveOpportunityId);
        return hash.ToHashCode();
    }
}
