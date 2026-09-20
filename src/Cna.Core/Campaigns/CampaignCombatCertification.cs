using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Only inherited-selection-v1 empty entry admission; positive profile certification remains separate.</summary>
internal static class CampaignCombatCertification
{
    public static CampaignCombatAdmissionBoundary Admit(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> retainedCreated, IReadOnlyList<byte[]> events)
    {
        var result = CampaignCombatHistoryReplay.Replay(request, retainedCreated, events);
        if (result.Projection is not CampaignCombatHistoryProjection.BreakdownCompletion { State: var entry } ||
            entry.Completion is null || entry.BreakdownCompletionReceiptId is null ||
            entry.SequencePosition.PositionId != "land.position.operation-1.first-player.movement-and-combat.combat.position-determination" ||
            entry.SequencePosition.ActiveSide is not null || entry.Lifecycle.BreakdownFlow is not CampaignBreakdownFlow.Idle ||
            entry.Lifecycle.InterruptContext is not null || entry.Lifecycle.MovementEnd is null)
            throw new JsonException("Inherited Combat assessment requires actual completed G2 authority.");
        var movement = entry.Lifecycle.Movement;
        var cycle = movement.Opening.Cycle!;
        if (cycle.GameTurn != 1 || cycle.OperationStage != 1 || cycle.PlayerPhaseSlot != "first-acting-side" || cycle.Ordinal != 1 ||
            movement.Events.Count is not (6 or 7))
            throw new JsonException("Unsupported inherited Combat assessment scope.");
        var actingSide = CampaignSnapshotSerializer.FormatSide(cycle.ActingSide);
        var defendingSide = actingSide == "axis" ? "commonwealth" : "axis";
        var content = request.Context.Setup.Artifact.Definition;
        var acting = BindParticipant(request, movement.World, new CampaignCombatUnitKey(request.CreationBinding,
            actingSide, content.Elements.Single(element => element.SideId == actingSide).ElementId));
        var defending = BindParticipant(request, movement.World, new CampaignCombatUnitKey(request.CreationBinding,
            defendingSide, content.Elements.Single(element => element.SideId == defendingSide).ElementId));
        var actingCp = movement.World.Elements.Single(element => element.ElementId == acting.Unit.ElementId).OperationalState.CapabilityPointsExpended;
        var defendingCp = movement.World.Elements.Single(element => element.ElementId == defending.Unit.ElementId).OperationalState.CapabilityPointsExpended;
        var weather = movement.Opening.Predecessor.Stage.Weather.Weather;
        var normal = weather.Count > 0 && weather.All(value => value.Kind == WeatherKind.Normal);
        var adjacent = content.Edges.Any(edge => edge.FirstLocationId == acting.LocationId && edge.SecondLocationId == defending.LocationId ||
            edge.FirstLocationId == defending.LocationId && edge.SecondLocationId == acting.LocationId);
        var actingWithin = actingCp.Denominator == 1 && actingCp.Numerator <= 5;
        var defendingWithin = defendingCp.Denominator == 1 && defendingCp.Numerator <= 7;
        // Supported absence is proven by replay and all frozen predicates, never inferred from an
        // unsupported candidate implementation or a caller-supplied empty collection.
        if (actingCp.Denominator != 1 || actingCp.Numerator is not (12 or 14) ||
            !normal || adjacent || actingWithin || !defendingWithin)
            throw new JsonException("History is outside inherited-selection-v1 empty admission.");
        return new(result.History, entry, new(acting, defending, normal, adjacent, actingCp, defendingCp, actingWithin, defendingWithin));
    }

    /// <summary>Bind current identity in caller-trusted World; this does not authenticate history or admit combat.</summary>
    public static CampaignCombatParticipant BindParticipant(CampaignCombatCreationRequest request,
        CampaignWorldSnapshotV7 world, CampaignCombatUnitKey unit)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(unit);
        var content = request.Context.Setup.Artifact.Definition;
        var source = content.Elements.SingleOrDefault(element => element.ElementId == unit.ElementId);
        var element = world.Elements.SingleOrDefault(value => value.ElementId == unit.ElementId);
        if (unit.CreationBinding != request.CreationBinding || world.CreationBinding != request.CreationBinding ||
            source is null || element is null || source.SideId != unit.OriginalSide ||
            source.ParentFormationId != element.SourceParentFormationId ||
            !element.Components.Select(component => component.ComponentId).SequenceEqual(source.Components.Select(component => component.ComponentId), StringComparer.Ordinal) ||
            !content.Locations.Any(location => location.LocationId == element.CurrentLocationId))
            throw new JsonException("Participant unit/component provenance differs from trusted current context.");
        var representations = world.Representations.Where(value => value.BoundElementIds.Contains(unit.ElementId, StringComparer.Ordinal)).ToArray();
        if (representations.Length != 1 || representations[0].BindingKind != CampaignMapRepresentationBindingKind.IndependentElement ||
            representations[0].BoundElementIds.Count != 1 || representations[0].CurrentLocationId != element.CurrentLocationId)
            throw new JsonException("Participant requires one exact current independent representation.");
        return new(unit, representations[0].RepresentationId, element.CurrentLocationId, element.Components.Select(component => component.ComponentId).ToArray());
    }
}
