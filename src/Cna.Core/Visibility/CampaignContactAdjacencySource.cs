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
        return Derive(null, created, context.ArtifactV6!, knowledge, null);
    }

    public static IReadOnlyList<CampaignContactTransition> OnMovement(
        CampaignSnapshotV11 before, CampaignSnapshotV11 after, ElementMovedV3 movement,
        CampaignContentContext context, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(knowledge);
        RequireAuthority(before, context, knowledge);
        RequireAuthority(after, context, knowledge);
        if (movement.CampaignId != before.CampaignId || movement.CampaignId != after.CampaignId
            || movement.PriorStateVersion != before.StateVersion
            || movement.StateVersion != after.StateVersion
            || !after.World.Representations.Any(value =>
                value.RepresentationId == movement.RepresentationId
                && value.CurrentLocationId == movement.DestinationLocationId))
            throw new ArgumentException("Movement does not match its committed authority cut.", nameof(movement));
        return Derive(before, after, context.ArtifactV6!, knowledge, movement.RepresentationId);
    }

    private static List<CampaignContactTransition> Derive(
        CampaignSnapshotV11? before, CampaignSnapshotV11 after, ContentPackV6Artifact artifact,
        CampaignContactKnowledge knowledge, string? changedRepresentationId)
    {
        var definitions = artifact.Definition.LegacyDefinition;
        var sides = definitions.Elements.ToDictionary(value => value.ElementId,
            value => value.SideId, StringComparer.Ordinal);
        var ownSide = CampaignSnapshotSerializer.FormatSide(knowledge.Side);
        var own = after.World.Representations.Where(value =>
            value.BoundElementIds.Count > 0
            && value.BoundElementIds.All(id => sides[id] == ownSide)).ToArray();
        var opposing = after.World.Representations.Where(value =>
            value.BoundElementIds.Count > 0
            && value.BoundElementIds.All(id => sides[id] != ownSide)).ToArray();
        var beforeLocations = before?.World.Representations.ToDictionary(value =>
            value.RepresentationId, value => value.CurrentLocationId, StringComparer.Ordinal);
        var working = knowledge.Copy();
        var transitions = new List<CampaignContactTransition>();

        foreach (var observer in own.OrderBy(value => value.RepresentationId, StringComparer.Ordinal))
            foreach (var observerId in observer.BoundElementIds.OrderBy(value => value, StringComparer.Ordinal))
                foreach (var target in opposing.OrderBy(value => value.RepresentationId, StringComparer.Ordinal))
                {
                    if (changedRepresentationId is not null
                        && observer.RepresentationId != changedRepresentationId
                        && target.RepresentationId != changedRepresentationId)
                        continue;
                    var currentlyVisible = Adjacent(definitions, observer.CurrentLocationId, target.CurrentLocationId);
                    var previouslyVisible = beforeLocations is not null
                        && beforeLocations.TryGetValue(observer.RepresentationId, out var observerBefore)
                        && beforeLocations.TryGetValue(target.RepresentationId, out var targetBefore)
                        && Adjacent(definitions, observerBefore, targetBefore);
                    var prior = working.FindPrivateContact(target.RepresentationId, observerId);
                    if (!currentlyVisible && !(previouslyVisible && prior?.IsCurrent == true))
                        continue;
                    var kind = currentlyVisible
                        ? prior is null ? CampaignContactTransitionKind.Observed
                            : prior.IsCurrent ? CampaignContactTransitionKind.Updated
                            : CampaignContactTransitionKind.Rediscovered
                        : CampaignContactTransitionKind.Lost;
                    var transition = new CampaignContactTransition(
                        CampaignContactKnowledge.CurrentContractVersion, after.CampaignId, knowledge.Side,
                        after.StateVersion,
                        changedRepresentationId is null
                            ? $"creation/{observerId}/{target.RepresentationId}"
                            : $"move/{after.StateVersion}/{observerId}/{target.RepresentationId}",
                        RuleId, kind, prior?.Handle ?? working.NextHandle(observerId), target.RepresentationId,
                        observerId,
                        currentlyVisible ? target.CurrentLocationId : prior!.ApparentLocationId,
                        CampaignContactIdentification.Presence, CampaignContactAccess.Observer);
                    working.Apply(transition);
                    transitions.Add(transition);
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
