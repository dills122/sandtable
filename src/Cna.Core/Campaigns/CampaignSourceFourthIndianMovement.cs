using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Narrow GT1 Op1 source successor. Source-turn owner must not expose its
// availability until destination and supply-route visibility are owner-safe.
internal static class CampaignSourceFourthIndianMovement
{
    internal const string GroupId = "4-indian-division";

    // Offer only information already known to the Commonwealth owner. The
    // source history fixes the Axis outside this opening route, so no
    // opponent-position filter is used to construct the visible options.
    internal static IReadOnlyList<string> ValidSupplyChoices(CampaignSourcePlayableWorld world,
        CampaignSourceCommonwealthSupplyWorld supply, (int GameTurn, int OperationStage)? paidStage,
        LandSequencePosition position, CampaignOperationStageWeather weather)
    {
        // Both facts are fixed by the admitted source history: no first-side
        // land move has occurred before this first movement cut. Keep the
        // same preconditions as Apply; never offer an unaccepted action.
        if (!world.IsGameplayReady || world.Source.GroupedWorld is null || world.Source.Battlefield is null
            || world.GroupedWorld.Hash() != world.Source.GroupedWorld.Hash()
            || world.Battlefield.Hash() != world.Source.Battlefield.Hash()
            || !OwnerConditionsHold(world, supply, paidStage, position, weather)) return [];
        return supply.Units.Where(unit => !unit.IsDummy && unit.LocationId == CampaignSourceFourthIndianRoad.SupplyLocation && unit.Fuel >= 2)
            .Select(unit => unit.Id).Order(StringComparer.Ordinal).ToArray();
    }

    private static bool OwnerConditionsHold(CampaignSourcePlayableWorld world,
        CampaignSourceCommonwealthSupplyWorld supply, (int GameTurn, int OperationStage)? paidStage,
        LandSequencePosition position, CampaignOperationStageWeather weather)
    {
        if (position.GameTurn != 1 || position.OperationStage != 1 || position.ActiveSide != LandSide.Commonwealth
            || position.ActorRole != LandActorRole.FirstActingSide || position.SegmentId != LandSegmentIds.Movement
            || paidStage is not null || weather.Kind != WeatherKind.Normal || weather.GameTurn != 1 || weather.OperationStage != 1)
            return false;
        var group = world.GroupedWorld.Groups.SingleOrDefault(value => value.GroupId == GroupId);
        if (group is null) return false;
        if (!IsOpeningFormation(group) || world.GroupedWorld.Detached.Any(value => value.GroupId == GroupId)
            || world.GroupedWorld.ExternalReservations.Any(value => value.GroupId == GroupId)
            || world.Source.AttachedInitialLoads is null
            || world.Source.AttachedInitialLoads.Reserved(CampaignSourceFourthIndianDefinition.PoolId) != 0
            || CampaignSourcePolishArrivalContacts.EffectiveCounters(world).Any(value => value.SideId == "commonwealth" && value.LocationId == CampaignSourceFourthIndianRoad.Destination))
            return false;
        return true;

    }

    internal static bool IsOpeningFormation(CampaignGroupedFormation group)
    {
        var roster = CampaignSourceFourthIndianDefinition.Members;
        return group.GroupId == GroupId && group.SideId == "commonwealth"
            && group.LocationId == CampaignSourceFourthIndianRoad.Origin && group.StackValue == 5
            && group.DispositionConfirmed && group.MotorPoolKnown && group.MotorPool == 22
            && group.Members.Count == roster.Count && group.Members.All(member =>
            {
                var source = roster.SingleOrDefault(value => value.Id == member.ElementId);
                return source is not null && source.ParentId == member.ParentId && source.Toe == member.ComponentToe
                    && member.ComponentType == (source.Toe is null ? null : source.Kind)
                    && member.AdditionalComponents is not { Count: > 0 }
                    && member.CapabilityPointAllowance == source.CapabilityPoints
                    && member.CapabilityPointsExpended == CapabilityPointAmount.Zero && member.CohesionLevel > -26
                    && member.AssignedMotorPoints == (member.ElementId == "central-india-horse" ? 6 : 0);
            });
    }

    internal static (CampaignSourcePlayableWorld World, CampaignSourceCommonwealthSupplyWorld Supply,
        (int GameTurn, int OperationStage)? PaidStage) Apply(CampaignSourcePlayableWorld world,
        CampaignSourceCommonwealthSupplyWorld supply, (int GameTurn, int OperationStage)? priorPaidStage,
        LandSequencePosition position, CampaignOperationStageWeather weather, string supplyId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(supply);
        ArgumentNullException.ThrowIfNull(weather);
        Cna1979LandSequenceV4.RequireMaterializedMovement(position);
        CampaignSourceFourthIndianRoad.RequirePinnedSource(world);
        if (!world.IsGameplayReady || position.GameTurn != 1 || position.OperationStage != 1
            || position.ActiveSide != LandSide.Commonwealth || position.ActorRole != LandActorRole.FirstActingSide
            || priorPaidStage is not null
            || weather.GameTurn != 1 || weather.OperationStage != 1 || weather.Kind != WeatherKind.Normal)
            throw new InvalidOperationException("Source movement is unavailable.");
        // Scenario 60.31 places Axis land forces on A/B/C, in off-map
        // boxes, or in Libya. The Map C national boundary and Map D place
        // these three hexes well inside Egypt. First-side GT1 Op1 plus the
        // exact retained setup world exclude intervening Axis land movement.
        if (world.Source.GroupedWorld is null || world.Source.Battlefield is null
            || world.GroupedWorld.Hash() != world.Source.GroupedWorld.Hash()
            || world.Battlefield.Hash() != world.Source.Battlefield.Hash())
            throw new InvalidOperationException("This opening move requires the unchanged initial source battlefield.");
        if (!OwnerConditionsHold(world, supply, priorPaidStage, position, weather)
            || !ValidSupplyChoices(world, supply, priorPaidStage, position, weather).Contains(supplyId, StringComparer.Ordinal))
            throw new InvalidOperationException("Source formation, transport or supply choice is unsupported.");
        var group = world.GroupedWorld.Groups.Single(value => value.GroupId == GroupId);

        // Land 32.24: one fuel for the gun battalion and one for the
        // motorized infantry battalion. Empty motorization points are exempt.
        var nextSupply = supply.SpendFuel(supplyId, CampaignSourceFourthIndianRoad.SupplyLocation, 2);
        var costs = OpeningCosts(group);
        // MoveFormation's fourth argument is the formation's resulting stack,
        // not the road's traversal limit. Land 32.9 removes road/track-only
        // stacking limits in the Land-only game; retain the actual counter.
        var grouped = world.GroupedWorld.MoveFormation(GroupId, CampaignSourceFourthIndianRoad.Destination, costs, group.StackValue!.Value, 1, 1);
        var battlefield = world.Battlefield.MoveFormation(world.GroupedWorld, grouped, GroupId);
        return (world.WithWorld(grouped, battlefield), nextSupply, (1, 1));
    }

    internal static IReadOnlyDictionary<string, CapabilityPointAmount> OpeningCosts(CampaignGroupedFormation group) =>
        group.Members.ToDictionary(value => value.ElementId,
            value => CampaignSourceFourthIndianRoad.Cost(motorized: value.ComponentType != "infantry"), StringComparer.Ordinal);
}
