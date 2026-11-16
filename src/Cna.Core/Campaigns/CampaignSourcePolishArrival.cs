using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Source-backed GT1 operation-stage 3 arrival. The owning turn authority must
// journal and re-adjudicate this transition; it is not a setup-time placement.
internal static class CampaignSourcePolishArrival
{
    internal const string GroupId = "graziani-polish-brigade";
    private const string RootId = "polish-brigade-hq";
    private const int BrigadeStack = 3;
    private const int CityStackLimit = 8;
    private static readonly ContentScheduledPackIdentity SourceIdentity = Cna1979GrazianiCairoSourceExcerpt.Build().Identity;

    internal static IReadOnlyList<string> ValidDestinations(CampaignSourcePlayableWorld world,
        LandSequencePosition position)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(position);
        if (world.World8.Initialization.CohesionLevel != 0) return [];
        // Turn authority proves world readiness before entering this phase.
        // Offers must not depend on hidden source evidence or opposing counters.
        var knownCityHexes = Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds
            .Where(location => world.KnownTerrainAt(location) == Cna1979SourceContentVocabulary.MajorCityTerrainId)
            .ToHashSet(StringComparer.Ordinal);
        if (knownCityHexes.Any(location => Cna1979HistoricalMap.Instance.TerrainAt(location) != "major-city"))
            throw new InvalidOperationException("Cairo entry terrain differs from the pinned historical map.");
        return OwnerKnownDestinations(world.GroupedWorld, world.Battlefield.Entries, position,
            world.World8.PendingArrivals.Any(value => value.GroupId == GroupId), knownCityHexes);
    }

    internal static CampaignSourcePlayableWorld Apply(CampaignSourcePlayableWorld world,
        LandSequencePosition position, LandSide actor, string destination)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (actor != LandSide.Commonwealth || !world.IsGameplayReady || world.UnresolvedAt(destination).Count != 0
            || !ValidDestinations(world, position).Contains(destination, StringComparer.Ordinal))
            throw new InvalidOperationException("Polish arrival is not legal at this position or destination.");
        var grouped = Materialize(world.GroupedWorld, world.Source.Content, destination);
        var battlefield = world.Battlefield.ArriveRetainedFormation(world.GroupedWorld, grouped, GroupId, destination);
        return world.WithWorld(grouped, battlefield);
    }

    // Creates the source-defined successor; legality and journal authority belong to Apply.
    internal static CampaignGroupedWorld Materialize(CampaignGroupedWorld priorGrouped,
        ContentSourceScheduledCombatPackArtifact source, string destination)
    {
        ArgumentNullException.ThrowIfNull(priorGrouped);
        ArgumentNullException.ThrowIfNull(source);
        if (source.Identity != SourceIdentity || priorGrouped.Prior.ScenarioId != Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
            throw new InvalidOperationException("Polish arrival requires the pinned Graziani source excerpt.");
        if (!Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds.Contains(destination, StringComparer.Ordinal))
            throw new InvalidOperationException("Polish arrival destination is outside the source entry area.");
        var prior = priorGrouped.Prior;
        var scheduled = prior.PendingArrivals.Single(value => value.GroupId == GroupId);
        if (!scheduled.Equals(Cna1979GrazianiSourceFacts.PolishBrigadeArrival)
            || scheduled.Trucks is not null || scheduled.SideId != "commonwealth")
            throw new InvalidOperationException("Polish arrival differs from the retained source schedule.");
        var scenario = source.Definition.LegacyDefinition.Scenarios.Single(value => value.ScenarioId == prior.ScenarioId);
        var published = scenario.ScheduledArrivals.Single(value => value.GroupId == GroupId);
        if (!published.Equals(scheduled)) throw new InvalidOperationException("Polish arrival differs from pinned source content.");
        var ids = scheduled.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var facts = source.Definition.InitialPlacementCombatFacts.Where(value => value.ScenarioId == scenario.ScenarioId
            && ids.Contains(value.ElementId, StringComparer.Ordinal)).ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var allowances = Cna1979GrazianiCounterFacts.CairoBrigadeCandidates.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (facts.Count != ids.Length || ids.Any(id => !allowances.ContainsKey(id)))
            throw new InvalidOperationException("Polish arrival is missing source component or allowance facts.");
        var init = prior.Initialization;
        // Land 6.2 defines zero as normal cohesion. A separate arrival policy
        // is needed before extrapolating a non-normal setup value to new units.
        if (init.CohesionLevel != 0)
            throw new InvalidOperationException("Polish arrival requires the provisional normal-cohesion profile.");
        var elements = ids.Select(id => new CampaignElementStateV5(id, destination, CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 3, CapabilityPointAmount.Zero, init.CohesionLevel, null, null),
            facts[id].InitialComponentToes.Select(toe => new CampaignComponentToeState(toe.ComponentId, toe.CurrentToe, toe.Origin)))).ToArray();
        var representations = ids.Select(id => new CampaignMapRepresentationState("source-arrival." + id, destination,
            CampaignMapRepresentationBindingKind.IndependentElement, [id])).ToArray();
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(representations), prior.World.BrokenVehicleLots);
        var world8 = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, prior.Initialization,
            world6, prior.PendingArrivals.Where(value => value.GroupId != GroupId));
        var members = elements.Select(element =>
        {
            var component = element.Components.SingleOrDefault();
            var type = component is null ? null : component.ComponentId[(element.ElementId.Length + 1)..];
            return new CampaignGroupedMember(element.ElementId, element.ElementId == RootId ? null : RootId,
                type == "infantry" ? component!.CurrentToe : null, 0, false, type, component?.CurrentToe,
                allowances[element.ElementId].CapabilityPointAllowance, CapabilityPointAmount.Zero, init.CohesionLevel);
        });
        // The OA leaves truck quantities blank. Preserve unknown rather than assigning zero trucks.
        var formation = new CampaignGroupedFormation(GroupId, "commonwealth", RootId, destination, BrigadeStack,
            0, members, dispositionConfirmed: true, usesRetainedMembers: true, motorPoolKnown: false);
        return priorGrouped.ArriveRetainedFormation(world8, formation, GroupId);
    }

    internal static IReadOnlyList<string> OwnerKnownDestinations(CampaignGroupedWorld grouped,
        IEnumerable<CampaignSourceBattlefieldEntry> battlefieldEntries,
        LandSequencePosition position, bool hasPendingArrival, IReadOnlySet<string> knownCityHexes)
    {
        // §4.46a permits any Cairo hex subject to stacking. This bounded offer
        // only uses known friendly occupants, so it cannot reveal a hidden enemy.
        // PlayableWorld independently enforces the provisional §8.13 invariant
        // that materialized opposing counters cannot share a hex.
        if (!hasPendingArrival || position != ArrivalPosition()) return [];
        var owned = battlefieldEntries.Where(value => value.SideId == "commonwealth")
            .Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return Array.AsReadOnly(Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds.Where(location =>
        {
            if (!knownCityHexes.Contains(location)) return false;
            var stack = OwnerKnownStackAt(grouped, owned, location);
            return stack is > 0 && stack + BrigadeStack <= CityStackLimit;
        }).ToArray());
    }

    // Null means a friendly counter's stacking value is unavailable. Opposing
    // counters contribute nothing; their location cannot change this result.
    internal static int? OwnerKnownStackAt(CampaignGroupedWorld grouped, IReadOnlySet<string> owned, string location)
    {
        var total = 0;
        foreach (var occupant in grouped.OccupantsAt(location))
        {
            int? value;
            if (occupant.Formation is { } formation)
            {
                if (formation.SideId != "commonwealth") continue;
                value = formation.StackValue;
            }
            else if (occupant.Detached is { } detached)
            {
                if (detached.SideId != "commonwealth") continue;
                value = detached.StackValue;
            }
            else
            {
                var representation = occupant.Independent!;
                if (representation.BoundElementIds.Count != 1) return null;
                var id = representation.BoundElementIds[0];
                if (!owned.Contains(id)) continue;
                value = CampaignSourceCairoInitialSetup.Units.SingleOrDefault(unit => unit.ElementId == id)?.Stacking;
            }
            if (value is null) return null;
            total = checked(total + value.Value);
        }
        return total;
    }

    private static LandSequencePosition ArrivalPosition() => Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
        value.OperationStage == 3 && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
}
