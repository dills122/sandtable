using System.Globalization;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// Builds source-turn creation contacts from the effective grouped world.
// Unknown adjacency or observer identity fails closed; no inferred empty hexes.
internal static class CampaignSourceInitialContacts
{
    // These exact source locations have visible in-section grid paths to
    // Tobruk C4807; this is not a general Map C coordinate transform.
    private static readonly HashSet<string> TobrukPathSources = new(StringComparer.Ordinal)
    {
        "c3617", "c3918", "c3919", "c3920", "c4020", "c4120", "c4218", "c4307", "c4308",
        "c4407", "c4507", "c4607", "c4707", "c4807",
    };
    private static readonly Dictionary<string, int> CairoPathUpper = new(StringComparer.Ordinal)
    {
        ["e1829"] = 0,
        ["e1730"] = 1,
        ["e1830"] = 1,
        ["e1930"] = 1,
        ["e1931"] = 2,
        // Initial-contact coverage only; Helwan is not a Cairo arrival hex.
        ["e1430"] = 4,
    };
    private static readonly Dictionary<string, int> MatruhPathUpper = new(StringComparer.Ordinal)
    {
        ["d3714"] = 0,
        ["d3715"] = 1,
        ["d3615"] = 2,
        ["d3612"] = 4,
    };
    // Real same-sheet shortest-path distances from Ft. Maddalena C3019 (a
    // named Air/Logistics 37.42 chart anchor) to every other Map C hex,
    // computed once from Cna1979HistoricalMap's own transcribed hexes and
    // hexsides (BFS over NeighborsOf) - not a hand count off a map image and
    // not an invented figure. Used by TrySoluchMechiliToFtMaddalenaBound.
    private static readonly Lazy<Dictionary<string, int>> FtMaddalenaMapDistances = new(() =>
    {
        var map = Cna1979HistoricalMap.Instance;
        var distances = new Dictionary<string, int>(StringComparer.Ordinal) { ["c3019"] = 0 };
        if (map.TerrainAt("c3019") is null) return distances;
        var queue = new Queue<string>();
        queue.Enqueue("c3019");
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in map.NeighborsOf(current))
            {
                if (distances.ContainsKey(next)) continue;
                distances[next] = distances[current] + 1;
                queue.Enqueue(next);
            }
        }
        return distances;
    });
    internal static bool CanProject(CampaignSourcePlayableWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        try
        {
            var id = world.Source.Request.CampaignId;
            _ = Derive(world, 1, new CampaignContactKnowledge(id, LandSide.Axis));
            _ = Derive(world, 1, new CampaignContactKnowledge(id, LandSide.Commonwealth));
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (ArgumentException) { return false; }
    }

    internal static IReadOnlyList<CampaignContactTransition> Derive(CampaignSourcePlayableWorld world,
        long version, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (knowledge.CampaignId != world.Source.Request.CampaignId)
            throw new InvalidOperationException("Source contact campaign identity differs.");
        return DeriveCounters(CampaignSourcePolishArrivalContacts.EffectiveCounters(world), version, knowledge, world.MapIdentityBound, world.CdSeamBound);
    }

    internal static IReadOnlyList<CampaignContactTransition> DeriveCounters(
        IReadOnlyList<CampaignSourceContactCounter> counters, long version, CampaignContactKnowledge knowledge,
        bool useHistoricalMap = true, bool useCdSeam = false)
    {
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (version < 1 || counters.Any(value => value.SideId is not ("axis" or "commonwealth"))
            || counters.Select(value => value.RepresentationId).Distinct(StringComparer.Ordinal).Count() != counters.Count)
            throw new InvalidOperationException("Source contact identities are invalid.");
        var ownSide = knowledge.Side == LandSide.Axis ? "axis" : "commonwealth";
        var own = counters.Where(value => value.SideId == ownSide).ToArray();
        var targets = counters.Where(value => value.SideId != ownSide).OrderBy(value => value.RepresentationId, StringComparer.Ordinal).ToArray();
        // An unassigned observer can be ignored only when source geometry
        // proves it cannot observe any opposing counter.
        foreach (var unknownObserver in own.Where(value => value.ObserverFormationId is null))
            foreach (var target in targets)
                if (!TryAdjacent(unknownObserver.LocationId, target.LocationId, out var adjacent, useHistoricalMap, useCdSeam) || adjacent)
                    throw new InvalidOperationException("Source counter observer identity is not established.");
        var working = knowledge.Copy();
        var transitions = new List<CampaignContactTransition>();
        foreach (var observer in own.Where(value => value.ObserverFormationId is not null)
            .GroupBy(value => (value.ObserverKind, Id: value.ObserverFormationId!))
            .OrderBy(value => value.Key.ObserverKind).ThenBy(value => value.Key.Id, StringComparer.Ordinal))
            foreach (var target in targets)
            {
                var visible = false;
                var unknown = false;
                foreach (var counter in observer)
                {
                    if (!TryAdjacent(counter.LocationId, target.LocationId, out var adjacent, useHistoricalMap, useCdSeam)) unknown = true;
                    else if (adjacent) visible = true;
                }
                if (!visible && unknown) throw new InvalidOperationException("Source map adjacency is not established.");
                if (!visible) continue;
                var prior = working.FindPrivateContact(target.RepresentationId, observer.Key.Id, observer.Key.ObserverKind);
                var kind = prior is null ? CampaignContactTransitionKind.Observed
                    : prior.IsCurrent ? CampaignContactTransitionKind.Updated : CampaignContactTransitionKind.Rediscovered;
                var independent = observer.Key.ObserverKind == CampaignContactObserverKind.IndependentCounter;
                var transition = new CampaignContactTransition(independent ? CampaignContactKnowledge.IndependentObserverContractVersion
                        : CampaignContactKnowledge.CurrentContractVersion,
                    knowledge.CampaignId, knowledge.Side, version, $"source-start/{version}/{observer.Key.Id}/{target.RepresentationId}",
                    CampaignContactAdjacencySource.RuleId, kind, prior?.Handle ?? (independent
                        ? working.NextIndependentCounterHandle(observer.Key.Id) : working.NextHandle(observer.Key.Id)),
                    target.RepresentationId, observer.Key.Id, target.LocationId,
                    CampaignContactIdentification.Presence, CampaignContactAccess.Observer, observer.Key.ObserverKind);
                working.Apply(transition);
                transitions.Add(transition);
            }
        return transitions.AsReadOnly();
    }

    internal static bool TryAdjacent(string first, string second, out bool adjacent, bool useHistoricalMap = true, bool useCdSeam = false)
    {
        if (useCdSeam && useHistoricalMap && Cna1979HistoricalMap.IsCdSeamCrossing(first, second)
            && Cna1979HistoricalMap.Instance.EdgeBetween(first, second, useCdSeam: true) is not null)
        {
            adjacent = true;
            return true;
        }
        // A provisional near-seam candidate can veto a separation proof; a
        // far or unknown candidate never certifies adjacency or separation.
        var seam = CampaignSourceSeamCandidate.Classify(first, second);
        if (seam.Certainty == CampaignSourceSeamCertainty.ProvisionalNear || seam.PublishedDistanceConflict)
        {
            adjacent = false;
            return false;
        }
        // Chart entries are shortest hex distances (Air/Logistics 37.42).
        // Exact raw IDs only; absent entries and any chart/candidate conflict
        // remain unresolved rather than establishing an alias or a contact.
        if (CampaignSourceSeamCandidate.TryPublishedDistance(first, second, out var published) && published > 1)
        {
            adjacent = false;
            return true;
        }
        // Exact Map C hexsides, not a complete neighborhood or a ZOC rule.
        if ((first == "c3419" && second == "c3520") || (first == "c3520" && second == "c3419")
            || (first == "c3019" && second == "c3020") || (first == "c3020" && second == "c3019")
            || (first == "c1014" && second == "c1015") || (first == "c1015" && second == "c1014"))
        {
            adjacent = true;
            return true;
        }
        if (TryChartAnchorToCairoBound(first, second) || TryChartAnchorToCairoBound(second, first)
            || TryElGreinToCairoBound(first, second) || TryElGreinToCairoBound(second, first)
            || TryScheferzenToCairoBound(first, second) || TryScheferzenToCairoBound(second, first)
            || TryAxisToMatruhBound(first, second) || TryAxisToMatruhBound(second, first)
            || TryTobrukToCairoBound(first, second) || TryTobrukToCairoBound(second, first)
            || (useHistoricalMap && (TrySoluchMechiliToFtMaddalenaBound(first, second)
                || TrySoluchMechiliToFtMaddalenaBound(second, first))))
        {
            adjacent = false;
            return true;
        }
        if (CampaignSourcePolishArrivalContacts.TryCairoAdjacent(first, second, out adjacent)) return true;
        if ((first is "c3320" or "c3617") && second.Length == 5 && second[0] == 'c')
        {
            adjacent = first == second || ContentSourceLocalMapPackArtifact.Neighbors(first).Contains(second, StringComparer.Ordinal);
            return true;
        }
        if ((second is "c3320" or "c3617") && first.Length == 5 && first[0] == 'c')
        {
            adjacent = first == second || ContentSourceLocalMapPackArtifact.Neighbors(second).Contains(first, StringComparer.Ordinal);
            return true;
        }
        if (first.Length == 5 && second.Length == 5 && first[0] == second[0]
            && int.TryParse(first.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var firstRow)
            && int.TryParse(first.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var firstColumn)
            && int.TryParse(second.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var secondRow)
            && int.TryParse(second.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var secondColumn)
            && Math.Max(Math.Abs(firstRow - secondRow), Math.Abs(firstColumn - secondColumn)) > 1)
        {
            adjacent = false;
            return true;
        }
        // Same-sheet grid geometry has no seam ambiguity. Resolve only pairs
        // the pinned transcription actually contains; retain every established
        // legacy proof above so existing successful contact replay is unchanged.
        if (useHistoricalMap && first.Length == 5 && second.Length == 5 && first[0] == second[0])
        {
            var map = Cna1979HistoricalMap.Instance;
            if (map.TerrainAt(first) is not null && map.TerrainAt(second) is not null)
            {
                adjacent = first == second || map.EdgeBetween(first, second) is not null;
                return true;
            }
        }
        // Air/Logistics 37.42 states that identical numbered hexes on
        // adjacent map sections are 33 hexes apart. Triangle inequality and
        // a conservative grid-distance upper bound can prove separation;
        // they cannot prove adjacency or any particular seam crossing.
        if (TryCoordinates(first, out var section1, out var row1, out var column1)
            && TryCoordinates(second, out var section2, out var row2, out var column2))
        {
            if (section1 == 'd' && section2 == 'c')
                (section1, section2, row1, row2, column1, column2) = (section2, section1, row2, row1, column2, column1);
            // Only same-number C/D pairs whose two physical cells are
            // independently visible are admitted from the chart statement.
            if (section1 == 'c' && section2 == 'd'
                && (row2, column2) is (36, 15) or (37, 14) or (37, 15)
                && row1 == row2 && column1 == column2)
            {
                adjacent = false;
                return true;
            }
            // The chart also gives Tobruk C4807 to Matruh D3714 as 45.
            // The C4407–C4807 run is visible on Map C's Tobruk sheet.
            if (section1 == 'c' && section2 == 'd' && row1 is >= 44 and <= 48 && column1 == 7
                && (row2, column2) is (36, 15) or (37, 15) or (37, 14)
                && !CampaignSourceSeamCandidate.Classify("c4807", "d3714").PublishedDistanceConflict
                && 45 - (Math.Abs(row1 - 48) + 1)
                    - (Math.Abs(row2 - 37) + Math.Abs(column2 - 14) + 1) > 1)
            {
                adjacent = false;
                return true;
            }
            // The same source chart gives Derna B5925 to Matruh D3714 as
            // 66 hexes; this bounds B/D pairs without assuming map seams.
            if (section1 == 'd' && section2 == 'b')
                (section1, section2, row1, row2, column1, column2) = (section2, section1, row2, row1, column2, column1);
            if (section1 == 'b' && row1 == 59 && column1 == 25 && section2 == 'd'
                && (row2, column2) is (36, 15) or (37, 15) or (37, 14)
                && !CampaignSourceSeamCandidate.Classify("b5925", "d3714").PublishedDistanceConflict
                && 66
                    - (Math.Abs(row2 - 37) + Math.Abs(column2 - 14) + 1) > 1)
            {
                adjacent = false;
                return true;
            }
        }
        adjacent = false;
        return false;
    }

    private static bool TryCoordinates(string id, out char section, out int row, out int column)
    {
        section = '\0'; row = 0; column = 0;
        if (id.Length != 5 || id[0] is < 'a' or > 'e'
            || !int.TryParse(id.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out row)
            || !int.TryParse(id.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out column)) return false;
        section = id[0];
        return true;
    }

    private static bool TryTobrukToCairoBound(string mapC, string mapE)
    {
        if (!TobrukPathSources.Contains(mapC) || !CairoPathUpper.TryGetValue(mapE, out var cairoSteps)
            || !CampaignSourceSeamCandidate.TryPublishedDistance("c4807", "e1829", out var anchor)
            || CampaignSourceSeamCandidate.Classify("c4807", "e1829").PublishedDistanceConflict
            || !TryCoordinates(mapC, out _, out var row, out var column)) return false;
        // §37.42 measures 103 hexes C4807→E1829. Map C's printed
        // in-section paths use at most this coordinate upper bound; Map E's
        // independently checked Cairo neighbor graph supplies cairoSteps.
        var mapCSteps = Math.Abs(row - 48) + Math.Abs(column - 7) + 1;
        return anchor - mapCSteps - cairoSteps > 1;
    }

    private static bool TryChartAnchorToCairoBound(string location, string cairo)
    {
        if (!CairoPathUpper.TryGetValue(cairo, out var cairoSteps)
            || !CampaignSourceSeamCandidate.TryPublishedDistance(location, "e1829", out var anchor)
            || CampaignSourceSeamCandidate.Classify(location, "e1829").PublishedDistanceConflict)
            return false;
        return anchor - cairoSteps > 1;
    }

    private static bool TryElGreinToCairoBound(string location, string cairo)
    {
        if (location != "c1715" || !CairoPathUpper.TryGetValue(cairo, out var cairoSteps)
            || !CampaignSourceSeamCandidate.TryPublishedDistance("c1014", "e1829", out var anchor)
            || CampaignSourceSeamCandidate.Classify("c1014", "e1829").PublishedDistanceConflict)
            return false;
        // Map C shows C1715→C1615→...→C1015→C1014 as eight
        // hex steps. The printed chart anchors C1014→E1829 at 85.
        return anchor - 8 - cairoSteps > 1;
    }

    private static bool TryScheferzenToCairoBound(string location, string cairo)
    {
        if (location != "c3419" || !CairoPathUpper.TryGetValue(cairo, out var cairoSteps)
            || !CampaignSourceSeamCandidate.TryPublishedDistance("c3019", "e1829", out var anchor)
            || CampaignSourceSeamCandidate.Classify("c3019", "e1829").PublishedDistanceConflict)
            return false;
        // The original Map C shows C3419→C3319→C3219→C3119→C3019.
        // The printed §37.42 chart anchors C3019→E1829 at 82 hexes.
        return anchor - 4 - cairoSteps > 1;
    }

    private static bool TryAxisToMatruhBound(string axisLocation, string matruhLocation)
    {
        if (!MatruhPathUpper.TryGetValue(matruhLocation, out var matruhSteps)) return false;
        string anchorLocation;
        int axisSteps;
        if (TobrukPathSources.Contains(axisLocation) && TryCoordinates(axisLocation, out _, out var row, out var column))
        {
            anchorLocation = "c4807";
            axisSteps = Math.Abs(row - 48) + Math.Abs(column - 7) + 1;
        }
        else if (axisLocation is "c1715" or "c3419")
        {
            anchorLocation = axisLocation == "c1715" ? "c1014" : "c3019";
            axisSteps = axisLocation == "c1715" ? 8 : 4;
        }
        // Soluch, Benghazi and Derna are named chart endpoints. Their
        // printed distances to D3714 apply directly, without guessing a
        // cross-section alias or a traversable route.
        else if (axisLocation is "c1014" or "c3019" or "c4321" or "b4921"
            or "a4130" or "a4827" or "b5925")
        {
            anchorLocation = axisLocation;
            axisSteps = 0;
        }
        else return false;
        if (!CampaignSourceSeamCandidate.TryPublishedDistance(anchorLocation, "d3714", out var anchor)
            || CampaignSourceSeamCandidate.Classify(anchorLocation, "d3714").PublishedDistanceConflict)
            return false;
        return anchor - axisSteps - matruhSteps > 1;
    }

    // Soluch A4130 and Mechili B4921 are named Air/Logistics 37.42 chart
    // anchors with a published distance to Ft. Maddalena C3019 (also a named
    // anchor): 61 and 41 hexes respectively, both free of the seam
    // candidate's own printed/candidate conflict flag. The local leg from
    // C3019 to the forward hex is the real same-sheet graph distance from
    // Cna1979HistoricalMap's own transcribed hexes/hexsides (FtMaddalenaMapDistances),
    // not a hand-counted or invented figure. (a4130,c4321) has a published-
    // distance conflict - Bardia is deliberately not used as an alternate anchor here.
    private static bool TrySoluchMechiliToFtMaddalenaBound(string axisLocation, string forwardLocation)
    {
        if (axisLocation is not ("a4130" or "b4921")) return false;
        if (!CampaignSourceSeamCandidate.TryPublishedDistance(axisLocation, "c3019", out var anchor)
            || CampaignSourceSeamCandidate.Classify(axisLocation, "c3019").PublishedDistanceConflict)
            return false;
        if (!FtMaddalenaMapDistances.Value.TryGetValue(forwardLocation, out var localSteps)) return false;
        return anchor - localSteps > 1;
    }
}
