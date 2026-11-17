using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

internal sealed record CampaignSourceContactCounter(string RepresentationId, string SideId,
    string? ObserverFormationId, string LocationId,
    CampaignContactObserverKind ObserverKind = CampaignContactObserverKind.Formation);

// Bounded contact projection for the source-backed Polish arrival. Geometry is
// from the Map E scan (PDF p.5, sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030).
internal static class CampaignSourcePolishArrivalContacts
{
    // The arrival hex and its immediate historical neighborhood (the only
    // locations the Map E geometry needs to resolve for this event). Neighbor
    // sets themselves come from Cna1979HistoricalMap.NeighborsOf, not a
    // second hand-copy of that transcription - verified byte-identical to
    // the map for all five of these hexes before this table was retired.
    private static readonly string[] KnownArrivalHexes =
        ["e1730", "e1829", "e1830", "e1930", "e1931"];

    internal static bool TryCairoAdjacent(string first, string second, out bool adjacent)
    {
        if (KnownArrivalHexes.Contains(first, StringComparer.Ordinal))
        {
            if (second.Length != 5 || second[0] != 'e') { adjacent = false; return false; }
            adjacent = first == second || Cna1979HistoricalMap.Instance.NeighborsOf(first).Contains(second, StringComparer.Ordinal);
            return true;
        }
        if (KnownArrivalHexes.Contains(second, StringComparer.Ordinal))
        {
            if (first.Length != 5 || first[0] != 'e') { adjacent = false; return false; }
            adjacent = first == second || Cna1979HistoricalMap.Instance.NeighborsOf(second).Contains(first, StringComparer.Ordinal);
            return true;
        }
        adjacent = false;
        return false;
    }

    internal static IReadOnlyList<CampaignContactTransition> Derive(CampaignSourcePlayableWorld before,
        CampaignSourcePlayableWorld after, long version, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (!ReferenceEquals(before.Source, after.Source) || before.MapIdentityBound != after.MapIdentityBound || before.CdSeamBound != after.CdSeamBound
            || knowledge.CampaignId != before.Source.Request.CampaignId
            || version < 2 || before.GroupedWorld.Groups.Any(value => value.GroupId == CampaignSourcePolishArrival.GroupId)
            || !before.World8.PendingArrivals.Any(value => value.GroupId == CampaignSourcePolishArrival.GroupId)
            || after.World8.PendingArrivals.Any(value => value.GroupId == CampaignSourcePolishArrival.GroupId)
            || !after.GroupedWorld.Groups.Where(value => value.GroupId != CampaignSourcePolishArrival.GroupId)
                .SequenceEqual(before.GroupedWorld.Groups)
            || !before.World8.World.Elements.All(after.World8.World.Elements.Contains)
            || !before.World8.World.Representations.All(after.World8.World.Representations.Contains))
            throw new InvalidOperationException("Arrival contact source does not match its prior authority.");
        var arrival = after.GroupedWorld.Groups.Single(value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        if (arrival.SideId != "commonwealth" || !KnownArrivalHexes.Contains(arrival.LocationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Arrival contact geometry is unavailable.");
        var counters = EffectiveCounters(after);
        return DeriveCounters(counters, arrival.GroupId, arrival.LocationId, version, knowledge, before.MapIdentityBound, before.CdSeamBound);
    }

    internal static IReadOnlyList<CampaignContactTransition> DeriveCounters(
        IReadOnlyList<CampaignSourceContactCounter> counters, string arrivingId, string arrivalLocation,
        long version, CampaignContactKnowledge knowledge, bool useHistoricalMap = true, bool useCdSeam = false)
    {
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (version < 2 || !KnownArrivalHexes.Contains(arrivalLocation, StringComparer.Ordinal)
            || counters.Any(value => value.SideId is not ("axis" or "commonwealth")
                || string.IsNullOrWhiteSpace(value.RepresentationId) || string.IsNullOrWhiteSpace(value.LocationId))
            || counters.Select(value => value.RepresentationId).Distinct(StringComparer.Ordinal).Count() != counters.Count)
            throw new InvalidOperationException("Arrival contact identity or geometry is invalid.");
        var arrival = counters.Single(value => value.RepresentationId == arrivingId);
        if (arrival.SideId != "commonwealth" || arrival.LocationId != arrivalLocation
            || arrival.ObserverFormationId != arrivingId
            || counters.Any(value => value.SideId == "axis" && value.LocationId == arrivalLocation))
            throw new InvalidOperationException("Arrival group does not match the contact source.");
        // The Map E neighborhood does not certify absence of a counter from
        // another section while map-section identity remains unresolved.
        var enemies = new List<CampaignSourceContactCounter>();
        foreach (var counter in counters.Where(value => value.SideId == "axis"))
        {
            if (!CampaignSourceInitialContacts.TryAdjacent(arrivalLocation, counter.LocationId, out var adjacent, useHistoricalMap, useCdSeam))
                throw new InvalidOperationException("Arrival contact map-section identity is unresolved.");
            if (adjacent) enemies.Add(counter);
        }
        var working = knowledge.Copy();
        var result = new List<CampaignContactTransition>();
        if (knowledge.Side == LandSide.Axis)
        {
            if (enemies.Any(value => value.ObserverFormationId is null))
                throw new InvalidOperationException("Adjacent source counter lacks a certified observer formation.");
            foreach (var observer in enemies.Select(value => (value.ObserverKind, Id: value.ObserverFormationId!))
                .Distinct().OrderBy(value => value.ObserverKind).ThenBy(value => value.Id, StringComparer.Ordinal))
                Add(observer.Id, observer.ObserverKind, arrival);
        }
        else if (knowledge.Side == LandSide.Commonwealth)
        {
            foreach (var target in enemies.OrderBy(value => value.RepresentationId, StringComparer.Ordinal))
                Add(arrivingId, CampaignContactObserverKind.Formation, target);
        }
        else throw new InvalidOperationException("Unknown contact audience.");
        return result.AsReadOnly();

        void Add(string observer, CampaignContactObserverKind observerKind, CampaignSourceContactCounter target)
        {
            var previous = working.FindPrivateContact(target.RepresentationId, observer, observerKind);
            var kind = previous is null ? CampaignContactTransitionKind.Observed
                : previous.IsCurrent ? CampaignContactTransitionKind.Updated : CampaignContactTransitionKind.Rediscovered;
            var independent = observerKind == CampaignContactObserverKind.IndependentCounter;
            var transition = new CampaignContactTransition(independent ? CampaignContactKnowledge.IndependentObserverContractVersion
                    : CampaignContactKnowledge.CurrentContractVersion,
                knowledge.CampaignId, knowledge.Side, version,
                $"source-arrival/{version}/{observer}/{target.RepresentationId}", CampaignContactAdjacencySource.RuleId,
                kind, previous?.Handle ?? (independent ? working.NextIndependentCounterHandle(observer) : working.NextHandle(observer)),
                target.RepresentationId, observer,
                target.LocationId, CampaignContactIdentification.Presence, CampaignContactAccess.Observer, observerKind);
            working.Apply(transition);
            result.Add(transition);
        }
    }

    internal static System.Collections.ObjectModel.ReadOnlyCollection<CampaignSourceContactCounter> EffectiveCounters(CampaignSourcePlayableWorld world)
    {
        var grouped = world.GroupedWorld;
        var entries = world.Battlefield.Entries.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var definitions = world.Source.Content.Definition.LegacyDefinition.Elements
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var locations = grouped.Prior.World.Representations.Select(value => value.CurrentLocationId)
            .Concat(grouped.Groups.Select(value => value.LocationId)).Concat(grouped.Detached.Select(value => value.LocationId))
            .Distinct(StringComparer.Ordinal);
        var result = new List<CampaignSourceContactCounter>();
        foreach (var location in locations)
            foreach (var occupant in grouped.OccupantsAt(location))
            {
                if (occupant.Formation is { } group)
                    result.Add(new(group.GroupId, group.SideId, group.GroupId, location));
                else if (occupant.Detached is { } detached)
                {
                    // The retained group records former attachment, not necessarily
                    // assignment (Land 19.13). No observer formation is inferred.
                    // Slash cannot occur in stable IDs, so the token is unambiguous.
                    var representationId = $"source-detached/{detached.GroupId}/{detached.Member.ElementId}";
                    result.Add(new(representationId, detached.SideId, null, location));
                }
                else
                {
                    var representation = occupant.Independent!;
                    var elementId = representation.BoundElementIds.Single();
                    var side = entries[elementId].SideId;
                    definitions.TryGetValue(elementId, out var definition);
                    var entry = entries[elementId];
                    if (entry.AuthorityStatus == "source-axis-hh-active-motorization-unresolved"
                        && (side != "axis" || world.Source.CwFreeGunsWorld?.MotorMachineGunContent is null
                            || elementId != ContentSourceGiarabubMotorMachineGunArtifact.ElementId))
                    {
                        result.Add(new(representation.RepresentationId, side, null, location));
                        continue;
                    }
                    var independent = (side == "axis" && entry.AuthorityStatus == "source-axis-minor-active"
                            && world.Source.CwFreeGunsWorld?.MinorGarrisonContent is not null
                            && ContentSourceAxisMinorGarrisonsPackArtifact.Units.Any(value => value.Id == elementId))
                        || (side == "axis" && entry.AuthorityStatus == "source-axis-garrison-remainder-active"
                            && world.Source.CwFreeGunsWorld?.GarrisonRemainderContent is not null
                            && ContentSourceAxisGarrisonRemainderPackArtifact.Units.Any(value => value.Id == elementId))
                        // Active identity and location establish an observer;
                        // unresolved motorization does not change adjacency.
                        || (side == "axis" && entry.AuthorityStatus == "source-axis-hh-active-motorization-unresolved"
                            && world.Source.CwFreeGunsWorld?.MotorMachineGunContent is not null
                            && elementId == ContentSourceGiarabubMotorMachineGunArtifact.ElementId)
                        || (side == "commonwealth" && entry.AuthorityStatus == "source-cw-frontline-active-initial-loads-unresolved"
                            && world.Source.CwFreeGunsWorld?.CwFrontlineContent is not null
                            && ContentSourceCwFrontlineArtifact.Units.Any(value => value.Id == elementId))
                        // Independent Commonwealth artillery/antitank counters attached
                        // to the CW frontline pack (source-cw-attached-artillery.*).
                        || (side == "commonwealth" && entry.AuthorityStatus == "source-cw-attached-artillery-active"
                            && world.Source.CwFreeGunsWorld?.CwAttachedArtilleryContent is not null
                            && ContentSourceCwAttachedArtilleryPackArtifact.Units.Any(value => value.Id == elementId))
                        // The normalized CW priority world only ever activates the
                        // French motor company (cw-priority.1-french-motor-marines);
                        // its catalog is still the citation for that identity.
                        || (side == "commonwealth" && entry.AuthorityStatus == "source-cw-priority-active"
                            && world.Source.CwPriorityWorld is not null
                            && CampaignSourceCwPriorityCatalog.Targets.Any(value => value.ElementId == elementId))
                        // Independent-setup world counters (independent-setup.*).
                        || (side == "commonwealth" && entry.AuthorityStatus == "world8-active"
                            && world.Source.IndependentWorld is not null
                            && ContentSourceIndependentPackArtifact.Units.Any(value => value.ElementId == elementId))
                        // The expanded independent-setup world's three added
                        // battalions (independent-setup.*), same binding kind
                        // and status as the base independent-setup world above.
                        || (side == "commonwealth" && entry.AuthorityStatus == "world8-active"
                            && world.Source.ExpandedIndependentWorld is not null
                            && ContentSourceExpandedIndependentPackArtifact.AddedUnits.Any(value => value.ElementId == elementId));
                    result.Add(independent
                        ? new(representation.RepresentationId, side, representation.RepresentationId, location,
                            CampaignContactObserverKind.IndependentCounter)
                        : new(representation.RepresentationId, side, definition?.ParentFormationId, location));
                }
            }
        return result.AsReadOnly();
    }
}
