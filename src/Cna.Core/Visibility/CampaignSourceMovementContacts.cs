using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// Adjacent contacts for one accepted source-grouped movement. Source map
// geometry and observer identities must be established before an offer is made.
internal static class CampaignSourceMovementContacts
{
    internal static IReadOnlyList<CampaignContactTransition> Derive(CampaignSourcePlayableWorld before,
        CampaignSourcePlayableWorld after, string movedRepresentationId, long version, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (!ReferenceEquals(before.Source, after.Source) || before.MapIdentityBound != after.MapIdentityBound || before.CdSeamBound != after.CdSeamBound
            || knowledge.CampaignId != before.Source.Request.CampaignId)
            throw new InvalidOperationException("Source movement contacts do not bind their campaign.");
        return DeriveCounters(CampaignSourcePolishArrivalContacts.EffectiveCounters(before),
            CampaignSourcePolishArrivalContacts.EffectiveCounters(after), movedRepresentationId, version, knowledge,
            before.MapIdentityBound, before.CdSeamBound);
    }

    internal static IReadOnlyList<CampaignContactTransition> DeriveCounters(
        IReadOnlyList<CampaignSourceContactCounter> before, IReadOnlyList<CampaignSourceContactCounter> after,
        string movedRepresentationId, long version, CampaignContactKnowledge knowledge, bool useHistoricalMap = true, bool useCdSeam = false)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentException.ThrowIfNullOrWhiteSpace(movedRepresentationId);
        if (version < 2 || before.Select(value => value.RepresentationId).Distinct(StringComparer.Ordinal).Count() != before.Count
            || after.Select(value => value.RepresentationId).Distinct(StringComparer.Ordinal).Count() != after.Count
            || before.Count != after.Count || before.Any(value => value.SideId is not ("axis" or "commonwealth"))
            || after.Any(value => value.SideId is not ("axis" or "commonwealth")))
            throw new InvalidOperationException("Source movement contact identities are invalid.");
        var old = before.ToDictionary(value => value.RepresentationId, StringComparer.Ordinal);
        var current = after.ToDictionary(value => value.RepresentationId, StringComparer.Ordinal);
        if (!old.TryGetValue(movedRepresentationId, out var priorMove)
            || !current.TryGetValue(movedRepresentationId, out var nextMove)
            || priorMove.SideId != nextMove.SideId || priorMove.ObserverFormationId != nextMove.ObserverFormationId
            || priorMove.ObserverKind != nextMove.ObserverKind
            || priorMove.LocationId == nextMove.LocationId
            || old.Any(pair => pair.Key != movedRepresentationId
                && (!current.TryGetValue(pair.Key, out var value) || value != pair.Value)))
            throw new InvalidOperationException("Source movement must change exactly one counter location.");
        var ownSide = knowledge.Side == LandSide.Axis ? "axis" : "commonwealth";
        var own = after.Where(value => value.SideId == ownSide).ToArray();
        var targets = after.Where(value => value.SideId != ownSide).OrderBy(value => value.RepresentationId, StringComparer.Ordinal).ToArray();
        var working = knowledge.Copy();
        var result = new List<CampaignContactTransition>();
        foreach (var unknown in own.Where(value => value.ObserverFormationId is null))
            foreach (var target in targets)
            {
                if (unknown.RepresentationId != movedRepresentationId && target.RepresentationId != movedRepresentationId) continue;
                if (!CampaignSourceInitialContacts.TryAdjacent(unknown.LocationId, target.LocationId, out var adjacent, useHistoricalMap, useCdSeam) || adjacent
                    || !CampaignSourceInitialContacts.TryAdjacent(old[unknown.RepresentationId].LocationId,
                        old[target.RepresentationId].LocationId, out var priorAdjacent, useHistoricalMap, useCdSeam) || priorAdjacent)
                    throw new InvalidOperationException("Source movement observer identity is not established.");
            }
        foreach (var observer in own.Where(value => value.ObserverFormationId is not null)
            .GroupBy(value => (value.ObserverKind, Id: value.ObserverFormationId!))
            .OrderBy(value => value.Key.ObserverKind).ThenBy(value => value.Key.Id, StringComparer.Ordinal))
            foreach (var target in targets)
            {
                var observers = observer.ToArray();
                if (target.RepresentationId != movedRepresentationId
                    && !observers.Any(value => value.RepresentationId == movedRepresentationId)) continue;
                var visible = AnyAdjacent(observers, target, current, useHistoricalMap, useCdSeam);
                var wasVisible = AnyAdjacent(observers, target, old, useHistoricalMap, useCdSeam);
                var previous = working.FindPrivateContact(target.RepresentationId, observer.Key.Id, observer.Key.ObserverKind);
                if (!visible && !(wasVisible && previous?.IsCurrent == true)) continue;
                var kind = visible ? previous is null ? CampaignContactTransitionKind.Observed
                    : previous.IsCurrent ? CampaignContactTransitionKind.Updated : CampaignContactTransitionKind.Rediscovered
                    : CampaignContactTransitionKind.Lost;
                var independent = observer.Key.ObserverKind == CampaignContactObserverKind.IndependentCounter;
                var transition = new CampaignContactTransition(independent ? CampaignContactKnowledge.IndependentObserverContractVersion
                        : CampaignContactKnowledge.CurrentContractVersion,
                    knowledge.CampaignId, knowledge.Side, version,
                    $"source-move/{version}/{observer.Key.Id}/{target.RepresentationId}", CampaignContactAdjacencySource.RuleId,
                    kind, previous?.Handle ?? (independent ? working.NextIndependentCounterHandle(observer.Key.Id)
                        : working.NextHandle(observer.Key.Id)), target.RepresentationId, observer.Key.Id,
                    visible ? target.LocationId : previous!.ApparentLocationId,
                    CampaignContactIdentification.Presence, CampaignContactAccess.Observer, observer.Key.ObserverKind);
                working.Apply(transition);
                result.Add(transition);
            }
        return result.AsReadOnly();
    }

    private static bool AnyAdjacent(IEnumerable<CampaignSourceContactCounter> observers,
        CampaignSourceContactCounter target, Dictionary<string, CampaignSourceContactCounter> world, bool useHistoricalMap, bool useCdSeam)
    {
        var unknown = false;
        foreach (var observer in observers)
        {
            if (!CampaignSourceInitialContacts.TryAdjacent(world[observer.RepresentationId].LocationId,
                    world[target.RepresentationId].LocationId, out var adjacent, useHistoricalMap, useCdSeam)) unknown = true;
            else if (adjacent) return true;
        }
        if (unknown) throw new InvalidOperationException("Source movement adjacency is not established.");
        return false;
    }
}
