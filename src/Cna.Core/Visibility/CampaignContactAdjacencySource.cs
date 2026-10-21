using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

/// <summary>
/// Permanent adjacent-contact rule. Core reads truth only while applying an authoritative
/// creation or movement event; player queries consume the resulting side knowledge.
/// </summary>
internal static class CampaignContactAdjacencySource
{
    public const string RuleId = "sandtable.visibility.adjacent-contact.v1";

    public static IReadOnlyList<CampaignContactTransition> OnCreation(
        CampaignSnapshotV11 created, CampaignContentContext context,
        CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(created);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(knowledge);
        RequireAuthority(created, context, knowledge);
        return Derive(null, created.World, created.CampaignId, created.StateVersion,
            context.ArtifactV6!, knowledge, null);
    }

    public static IReadOnlyList<CampaignContactTransition> OnMovement(
        CampaignSnapshotV11 before, CampaignSnapshotV11 after, ElementMovedV3 movement,
        CampaignContentContext context, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(movement);
        return OnMovement(before, after, movement.CampaignId, movement.PriorStateVersion,
            movement.StateVersion, movement.RepresentationId, movement.DestinationLocationId,
            context, knowledge);
    }

    public static IReadOnlyList<CampaignContactTransition> OnMovement(
        CampaignSnapshotV11 before, CampaignSnapshotV11 after, ReactingElementMovedV2 movement,
        CampaignContentContext context, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(movement);
        return OnMovement(before, after, movement.CampaignId, movement.PriorStateVersion,
            movement.StateVersion, movement.RepresentationId, movement.DestinationLocationId,
            context, knowledge);
    }

    public static IReadOnlyList<CampaignContactTransition> OnNativeRetreatMove(
        CampaignWorldSnapshotV6 before, CampaignWorldSnapshotV6 after,
        CampaignNativeCombatRetreatMoveV16 movement, string representationId,
        CampaignContentContext context, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (context.ArtifactV6 is null || movement.CampaignId != knowledge.CampaignId
            || movement.PriorStateVersion < 1
            || !before.Representations.Any(value => value.RepresentationId == representationId
                && value.CurrentLocationId == movement.OriginLocationId)
            || !after.Representations.Any(value => value.RepresentationId == representationId
                && value.CurrentLocationId == movement.DestinationLocationId))
            throw new ArgumentException("Retreat movement does not bind both world cuts.", nameof(movement));
        return Derive(before, after, movement.CampaignId, movement.StateVersion,
            context.ArtifactV6, knowledge, representationId);
    }

    public static IReadOnlyList<CampaignContactTransition> OnNativeSettlement(
        CampaignWorldSnapshotV6 before, CampaignWorldSnapshotV6 after,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignContentContext context, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(retreat);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (context.ArtifactV6 is null || settlement.CampaignId != knowledge.CampaignId
            || settlement.PriorStateVersion != retreat.StateVersion
            || settlement.PaidWorldHash != CampaignNativeCombatPaidWorldV22.Hash(before)
            || settlement.SettledWorldHash != CampaignNativeCombatPaidWorldV22.Hash(after))
            throw new ArgumentException("Settlement contact source lacks both World cuts.",
                nameof(settlement));
        if (retreat.Disposition != "moved") return [];
        var representation = before.Representations.Single(value =>
            value.BoundElementIds.Count == 1
            && value.BoundElementIds[0] == retreat.DefenderElementId);
        if (after.Representations.Single(value =>
                value.RepresentationId == representation.RepresentationId)
                .CurrentLocationId != retreat.DestinationLocationId)
            throw new ArgumentException("Settlement moved a different representation.",
                nameof(after));
        return Derive(before, after, settlement.CampaignId, settlement.StateVersion,
            context.ArtifactV6, knowledge, representation.RepresentationId);
    }

    private static List<CampaignContactTransition> OnMovement(
        CampaignSnapshotV11 before, CampaignSnapshotV11 after, string campaignId,
        long priorStateVersion, long stateVersion, string representationId,
        string destinationLocationId, CampaignContentContext context,
        CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(knowledge);
        RequireAuthority(before, context, knowledge);
        RequireAuthority(after, context, knowledge);
        if (campaignId != before.CampaignId || campaignId != after.CampaignId
            || priorStateVersion != before.StateVersion
            || stateVersion != after.StateVersion
            || !after.World.Representations.Any(value =>
                value.RepresentationId == representationId
                && value.CurrentLocationId == destinationLocationId))
            throw new ArgumentException("Movement does not match its committed authority cut.", nameof(representationId));
        return Derive(before.World, after.World, after.CampaignId, after.StateVersion,
            context.ArtifactV6!, knowledge, representationId);
    }

    private static List<CampaignContactTransition> Derive(
        CampaignWorldSnapshotV6? before, CampaignWorldSnapshotV6 after,
        string campaignId, long stateVersion, ContentPackV6Artifact artifact,
        CampaignContactKnowledge knowledge, string? changedRepresentationId)
    {
        var definitions = artifact.Definition.LegacyDefinition;
        var sides = definitions.Elements.ToDictionary(value => value.ElementId,
            value => value.SideId, StringComparer.Ordinal);
        var formations = definitions.Elements.ToDictionary(value => value.ElementId,
            value => value.ParentFormationId, StringComparer.Ordinal);
        var ownSide = CampaignSnapshotSerializer.FormatSide(knowledge.Side);
        var own = after.Representations.Where(value =>
            value.BoundElementIds.Count > 0
            && value.BoundElementIds.All(id => sides[id] == ownSide)).ToArray();
        var opposing = after.Representations.Where(value =>
            value.BoundElementIds.Count > 0
            && value.BoundElementIds.All(id => sides[id] != ownSide)).ToArray();
        var beforeLocations = before?.Representations.ToDictionary(value =>
            value.RepresentationId, value => value.CurrentLocationId, StringComparer.Ordinal);
        var working = knowledge.Copy();
        var transitions = new List<CampaignContactTransition>();

        var observers = own.SelectMany(representation => representation.BoundElementIds.Select(id =>
                (FormationId: formations[id], Representation: representation)))
            .GroupBy(value => value.FormationId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);
        foreach (var observer in observers)
        {
            var observerRepresentations = observer.Select(value => value.Representation)
                .DistinctBy(value => value.RepresentationId).ToArray();
            foreach (var target in opposing.OrderBy(value => value.RepresentationId, StringComparer.Ordinal))
            {
                if (changedRepresentationId is not null
                    && !observerRepresentations.Any(value => value.RepresentationId == changedRepresentationId)
                    && target.RepresentationId != changedRepresentationId)
                    continue;
                var currentlyVisible = observerRepresentations.Any(value =>
                    Adjacent(definitions, value.CurrentLocationId, target.CurrentLocationId));
                var previouslyVisible = beforeLocations is not null
                    && beforeLocations.TryGetValue(target.RepresentationId, out var targetBefore)
                    && observerRepresentations.Any(value =>
                        beforeLocations.TryGetValue(value.RepresentationId, out var observerBefore)
                        && Adjacent(definitions, observerBefore, targetBefore));
                var prior = working.FindPrivateContact(target.RepresentationId, observer.Key);
                if (!currentlyVisible && !(previouslyVisible && prior?.IsCurrent == true))
                    continue;
                var kind = currentlyVisible
                    ? prior is null ? CampaignContactTransitionKind.Observed
                        : prior.IsCurrent ? CampaignContactTransitionKind.Updated
                        : CampaignContactTransitionKind.Rediscovered
                    : CampaignContactTransitionKind.Lost;
                var transition = new CampaignContactTransition(
                    CampaignContactKnowledge.CurrentContractVersion, campaignId, knowledge.Side,
                    stateVersion,
                    changedRepresentationId is null
                        ? $"creation/{observer.Key}/{target.RepresentationId}"
                        : $"move/{stateVersion}/{observer.Key}/{target.RepresentationId}",
                    RuleId, kind, prior?.Handle ?? working.NextHandle(observer.Key), target.RepresentationId,
                    observer.Key,
                    currentlyVisible ? target.CurrentLocationId : prior!.ApparentLocationId,
                    CampaignContactIdentification.Presence, CampaignContactAccess.Observer);
                working.Apply(transition);
                transitions.Add(transition);
            }
        }
        return transitions;
    }

    private static bool Adjacent(ContentPackDefinition definition, string first, string second) =>
        first == second || CampaignElementMovedV3Factory.FindEdge(definition, first, second) is not null;

    private static void RequireAuthority(CampaignSnapshotV11 snapshot,
        CampaignContentContext context, CampaignContactKnowledge knowledge)
    {
        if (context.ArtifactV6 is null || snapshot.CampaignId != knowledge.CampaignId
            || snapshot.StateVersion < 1)
            throw new ArgumentException("Contact source lacks the campaign's Content 6 authority.");
    }
}
