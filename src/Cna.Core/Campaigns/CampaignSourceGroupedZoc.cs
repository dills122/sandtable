using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Derives Zones of Control over the real (grouped) Graziani battlefield.
// Land 10.11-10.15 operate on whatever sits in a hex, not on how many map
// counters represent it: a compressed formation counter already carries its
// own stacking point value (10.13 - "a full division is 5 SP even when the
// counters attached to it add up to far more"), so this module sums each
// *counter present* once, never a formation's own StackValue plus its
// members' individual values. Unknown source facts are never assumed zero or
// substituted; a projection that needs one is reported as indeterminate.
internal static class CampaignSourceGroupedZoc
{
    private static readonly RuleReference CommonwealthChart446a =
        new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a");
    private static readonly RuleReference CommonwealthOaIndianP15 =
        new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-indian-p15");
    private static readonly RuleReference CommonwealthOa70InfantryP9 =
        new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-70-infantry-p9");
    private static readonly RuleReference AxisChart446b =
        new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "4.46b");
    private static readonly RuleReference CommonwealthOa7ArmoredP6 =
        new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-7-armored-p6");
    private static readonly RuleReference AxisOaCatanzaroP10 = new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-catanzaro-p10");
    private static readonly RuleReference AxisOaCcnnP17 = new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-ccnn-p17");
    private static readonly RuleReference AxisOaCcnnP18 = new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-ccnn-p18");
    private static readonly RuleReference AxisOaCireneP4 = new(Cna1979GrazianiSourceFacts.ErrataSourceId, "oa-cirene-p4");
    private static readonly RuleReference AxisOaMarmaricaP4 = new(Cna1979GrazianiSourceFacts.ErrataSourceId, "oa-marmarica-p4");
    private static readonly RuleReference AxisOaLibyanP5 = new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-libyan-p5");
    private static readonly RuleReference AxisOaMalettiP7 = new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-maletti-p7");

    internal const string FourthIndianGroupId = "4-indian-division";
    internal const string SeventhArmoredGroupId = "7-armored-division";

    internal readonly record struct DefenseEstimate(long LowerBound, bool AllKnown);

    // Per-member defensive Close Assault rating for a named grouped
    // component, scoped to the chart cells actually verified against the
    // Commonwealth OA (see the agreed rulings this module implements). Any
    // other (groupId, elementId, componentType, toe) combination is unknown;
    // it is never guessed, averaged, or substituted from a different unit.
    private static bool TryDefensiveCloseAssaultRating(string groupId, string elementId, string componentType, int toe,
        out int rating, out IReadOnlyList<RuleReference> sources)
    {
        rating = 0;
        sources = Array.Empty<RuleReference>();
        if (groupId == FourthIndianGroupId)
        {
            if (componentType == "infantry" && toe == 6 && elementId is "1-royal-fusiliers" or "3-1-punjab"
                or "4-6-rajputana" or "2-camerons" or "1-6-rajputana" or "4-7-rajput")
            {
                rating = 2;
                sources = [CommonwealthChart446a, CommonwealthOaIndianP15];
                return true;
            }
            // Agreed ruling: under the approved untrained-Horse interpretation,
            // Central India Horse is deployed under infantry code 'l' (OA p.15,
            // chart 4.46a) - same TOE and defensive rating as the division's
            // rifle battalions, not a separate cavalry or reconnaissance rating.
            if (elementId == "central-india-horse" && componentType == "motor-infantry" && toe == 6)
            {
                rating = 2;
                sources = [CommonwealthChart446a, CommonwealthOaIndianP15];
                return true;
            }
            // 25th Field Artillery's integral 3.7-inch howitzer battery has a
            // known, zero Close Assault defensive rating (OA p.15). It must
            // still be counted at that value, not excluded as an unsupported
            // component class the way the legacy independent-element ZOC path
            // excludes artillery outright.
            if (elementId == "25-field-artillery" && componentType == "3-7-inch-howitzer" && toe == 6)
            {
                rating = 0;
                sources = [CommonwealthOaIndianP15];
                return true;
            }
        }
        if (groupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId && componentType == "infantry" && toe == 6
            && elementId is "2-queens-royal-regiment" or "2-leicesters" or "1-argyll-sutherland")
        {
            rating = 2;
            sources = [CommonwealthChart446a, CommonwealthOa70InfantryP9];
            return true;
        }
        // 7th Armored Division: each cited against OA p.6 with its printed
        // weapon's own defensive rating (Mark VI light vs A10 Cruiser), not a
        // blanket "armor" value.
        if (groupId == SeventhArmoredGroupId)
        {
            if (componentType == "mark-vi-light" && toe == 10 && elementId is "6-rtr" or "7-queens-own-hussars")
            { rating = 2; sources = [CommonwealthOa7ArmoredP6]; return true; }
            if (componentType == "a10-cruiser" && toe == 10 && elementId == "8-hussars")
            { rating = 3; sources = [CommonwealthOa7ArmoredP6]; return true; }
        }
        // Italian/Libyan formations: every cited rifle battalion in these
        // divisions carries the same OA-4.46b "infantry" close-assault
        // rating of 1 regardless of nation-specific TOE (5 for Libyan/CCNN
        // lines, 7 for Catanzaro/Cirene/Marmarica); the division's integral
        // machine-gun company is cited separately at TOE 2, rating 7. Each
        // is still scoped to the exact formation and element roster the
        // cited chart actually covers - no blanket "all Axis infantry" rule.
        if (componentType == "infantry")
        {
            if (groupId == CampaignSourceCatanzaroGroupedDefinition.GroupId)
            {
                if (toe == 7 && elementId is "1-141-infantry" or "2-141-infantry" or "3-141-infantry"
                    or "1-142-infantry" or "2-142-infantry" or "3-142-infantry")
                { rating = 1; sources = [AxisChart446b, AxisOaCatanzaroP10]; return true; }
                if (toe == 2 && elementId == "64-machine-gun") { rating = 7; sources = [AxisChart446b, AxisOaCatanzaroP10]; return true; }
            }
            if (groupId == CampaignSourceCcnnBatch.FirstId)
            {
                if (toe == 5 && elementId is "129-ccnn-infantry" or "133-ccnn-infantry" or "146-ccnn-infantry"
                    or "114-ccnn-infantry" or "116-ccnn-infantry" or "118-ccnn-infantry")
                { rating = 1; sources = [AxisChart446b, AxisOaCcnnP17]; return true; }
                if (toe == 2 && elementId == "201-ccnn-machine-gun") { rating = 7; sources = [AxisChart446b, AxisOaCcnnP17]; return true; }
            }
            if (groupId == CampaignSourceCcnnBatch.FourthId)
            {
                if (toe == 5 && elementId is "70-ccnn-infantry" or "119-ccnn-infantry" or "176-ccnn-infantry"
                    or "3-ccnn-infantry" or "17-ccnn-infantry" or "170-ccnn-infantry")
                { rating = 1; sources = [AxisChart446b, AxisOaCcnnP18]; return true; }
                if (toe == 2 && elementId == "204-ccnn-machine-gun") { rating = 7; sources = [AxisChart446b, AxisOaCcnnP18]; return true; }
            }
            if (groupId == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId)
            {
                if (toe == 7 && elementId is "1-157-infantry" or "2-157-infantry" or "3-157-infantry"
                    or "1-158-infantry" or "2-158-infantry" or "3-158-infantry")
                { rating = 1; sources = [AxisChart446b, AxisOaCireneP4]; return true; }
                if (toe == 2 && elementId == "63-machine-gun") { rating = 7; sources = [AxisChart446b, AxisOaCireneP4]; return true; }
            }
            if (groupId == CampaignSourceCireneMarmaricaGroupedDefinition.MarmaricaId)
            {
                if (toe == 7 && elementId is "1-115-infantry" or "2-115-infantry" or "3-115-infantry"
                    or "1-116-infantry" or "2-116-infantry" or "3-116-infantry")
                { rating = 1; sources = [AxisChart446b, AxisOaMarmaricaP4]; return true; }
                if (toe == 2 && elementId == "62-machine-gun") { rating = 7; sources = [AxisChart446b, AxisOaMarmaricaP4]; return true; }
            }
            if ((groupId == CampaignSourceLibyanGroupedDefinition.FirstId || groupId == CampaignSourceLibyanGroupedDefinition.SecondId)
                && toe == 5 && elementId is "8-1-libyan-infantry" or "9-1-libyan-infantry" or "10-1-libyan-infantry"
                    or "11-2-libyan-infantry" or "12-2-libyan-infantry" or "13-2-libyan-infantry"
                    or "2-3-libyan-infantry" or "6-3-libyan-infantry" or "7-3-libyan-infantry"
                    or "14-4-libyan-infantry" or "15-4-libyan-infantry" or "16-4-libyan-infantry")
            { rating = 1; sources = [AxisChart446b, AxisOaLibyanP5]; return true; }
            // Maletti's Libyan-coded motor infantry and infantry attachments
            // (OA p.7) carry the same rating as the parent Libyan division's
            // own rifle battalions, at the same TOE.
            if (groupId == CampaignSourceMalettiGroupedDefinition.GroupId && toe == 5
                && elementId is "1-1-libyan-motor-infantry" or "3-1-libyan-motor-infantry" or "5-1-libyan-motor-infantry"
                    or "17-5-libyan-infantry" or "19-5-libyan-infantry")
            { rating = 1; sources = [AxisChart446b, AxisOaMalettiP7]; return true; }
        }
        return false;
    }

    // A sound lower bound on raw defensive Close Assault points for the given
    // members. Unknown components never contribute a guessed value; they only
    // clear AllKnown. Because every rating is nonnegative, a lower bound that
    // already clears Land 10.15's ten-point threshold is as good as the exact
    // total for qualification purposes, even while some component remains
    // unknown (see EvaluateSource below).
    internal static DefenseEstimate EstimateRawDefenseLowerBound(string groupId, IEnumerable<CampaignGroupedMember> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        long lowerBound = 0;
        var allKnown = true;
        foreach (var member in members)
        {
            if (member.AdditionalComponents is { Count: > 0 }) { allKnown = false; continue; }
            if (member.ComponentType is not { } componentType || member.ComponentToe is not { } toe) continue;
            if (TryDefensiveCloseAssaultRating(groupId, member.ElementId, componentType, toe, out var rating, out _))
                lowerBound = checked(lowerBound + (long)toe * rating);
            else allKnown = false;
        }
        return new(lowerBound, allKnown);
    }

    // Resolves a defense estimate against Land 10.15's raw ten-point floor:
    // Qualified/NotQualified is returned only when that is sound from the
    // lower bound alone; otherwise null (indeterminate).
    private static long? ResolveAgainstThreshold(DefenseEstimate estimate) =>
        estimate.AllKnown || estimate.LowerBound >= 10 ? estimate.LowerBound : null;

    internal static ZocSourceQualificationResult? EvaluateFormationSource(CampaignGroupedFormation group)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (!group.DispositionConfirmed || group.StackValue is not { } stackValue) return null;
        var cohesionLevels = group.Members.Select(value => value.CohesionLevel).Distinct().ToArray();
        if (cohesionLevels.Length != 1) return null;
        var estimate = EstimateRawDefenseLowerBound(group.GroupId, group.Members);
        if (ResolveAgainstThreshold(estimate) is not { } defense) return null;
        return Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
            Cna1979Combat.CombatUnitClassificationId, stackValue, cohesionLevels[0], defense, true));
    }

    // Enforces, rather than assumes, the uniform-cohesion precondition this
    // module relies on. CanDeriveForSide/EvaluateFormationSource already
    // check this before a formation is treated as a ZOC source; this throws
    // instead of silently reading one unrepresentative member's level for a
    // caller that reaches a formation directly without going through them.
    private static int UniformCohesion(IReadOnlyList<CampaignGroupedMember> members)
    {
        var levels = members.Select(value => value.CohesionLevel).Distinct().ToArray();
        if (levels.Length != 1)
            throw new InvalidOperationException("A formation's members must share one cohesion level to derive ZOC.");
        return levels[0];
    }

    internal static ZocSourceQualificationResult? EvaluateDetachedSource(CampaignDetachedGroupedMember detached)
    {
        ArgumentNullException.ThrowIfNull(detached);
        var estimate = EstimateRawDefenseLowerBound(detached.GroupId, [detached.Member]);
        if (ResolveAgainstThreshold(estimate) is not { } defense) return null;
        return Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
            Cna1979Combat.CombatUnitClassificationId, detached.StackValue, detached.Member.CohesionLevel, defense, true));
    }

    // Real readiness check for one side's share of the grouped-movement
    // domain: every grouped formation and detached member must resolve to a
    // determinate (qualified or not) ZOC source, and no remaining
    // independently represented element may be combat-classified - this
    // module does not yet derive ZOC for independent (ungrouped) elements, so
    // any such element leaves the domain honestly incomplete rather than
    // silently ignored.
    internal static bool CanDeriveForSide(CampaignSourcePlayableWorld world, string sideId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(sideId);
        var grouped = world.GroupedWorld;
        foreach (var group in grouped.Groups.Where(value => value.SideId == sideId))
            if (EvaluateFormationSource(group) is null) return false;
        foreach (var detached in grouped.Detached.Where(value => value.SideId == sideId))
            if (EvaluateDetachedSource(detached) is null) return false;
        var claimed = grouped.Groups.SelectMany(value => value.Members.Select(member => member.ElementId))
            .Concat(grouped.Detached.Select(value => value.Member.ElementId))
            .ToHashSet(StringComparer.Ordinal);
        var facts = world.Source.Content.Definition.ElementCombatFacts.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var elements = world.Source.Content.Definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        foreach (var representation in grouped.Prior.World.Representations)
        {
            if (representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
                || representation.BoundElementIds.Count != 1) continue;
            var id = representation.BoundElementIds[0];
            if (claimed.Contains(id) || !elements.TryGetValue(id, out var element) || element.SideId != sideId) continue;
            if (!facts.TryGetValue(id, out var elementFacts)) return false;
            var classification = Cna1979Combat.FindClassification(elementFacts.CombatClassificationId);
            if (classification?.Kind is ZocCombatClassificationKind.CombatUnit or ZocCombatClassificationKind.Headquarters)
                return false;
        }
        return true;
    }

    private readonly record struct HexOccupant(int StackValue, int CohesionLevel, DefenseEstimate Defense);

    // Every grouped formation and detached member is foot/animal-mobile for
    // the purpose this module exists for (which hexes it may ZOC into):
    // truck attachment changes movement cost, tracked separately, not
    // whether the formation could ever set foot in the destination terrain.
    // Non-motorized enterability is also the more complete of the two
    // columns in Cna1979HistoricalTerrainEffects (it resolves salt marsh,
    // where the motorized column is deliberately left unknown).
    private const string GroundMobilityId = Cna1979Movement.NonMotorizedMobilityId;

    // Maps this project's historical-map hexside feature vocabulary onto
    // Cna1979Zoc's topology feature ids. A feature with no entry here (wadi,
    // minor river, an unfinished railroad) is not yet classified for ZOC
    // projection; the edge is skipped rather than guessed open or closed.
    private static string? MapHexsideFeature(string feature) => feature switch
    {
        "road" => "land.edge.road",
        "track" => "land.edge.track",
        "ridge" => "land.edge.ridge",
        "up-slope" or "down-slope" => "land.edge.slope",
        "major-river" => Cna1979Zoc.MajorRiverHexsideId,
        "up-escarpment" or "down-escarpment" => Cna1979Zoc.EscarpmentHexsideId,
        _ => null,
    };

    // Real grouped ZOC projection over the historical map: every own-side
    // counter at a hex contributes its own stacking and defense once
    // (10.13), summed per hex before qualification; each qualifying hex then
    // projects onto its known, enterable same-sheet neighbors and admitted
    // C/D seam crossings. Returns null when the side's sourcing is not fully
    // determinate (see CanDeriveForSide) or the world is not historical-map
    // bound; it never falls back to the small legacy excerpt map.
    internal static IReadOnlyList<string>? TryDeriveControlledLocationIds(CampaignSourcePlayableWorld world, string sideId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(sideId);
        if (!world.MapIdentityBound || !CanDeriveForSide(world, sideId)) return null;
        return TryDeriveControlledLocationIds(world.GroupedWorld, world.CdSeamBound, sideId);
    }

    // Lower-level core usable with a standalone grouped world (no full
    // CampaignSourcePlayableWorld needed), for focused testing. It assumes
    // the caller already established sourcing determinacy for sideId (see
    // CanDeriveForSide) and that the historical map is in effect.
    internal static IReadOnlyList<string> TryDeriveControlledLocationIds(CampaignGroupedWorld grouped, bool cdSeamBound, string sideId)
    {
        ArgumentNullException.ThrowIfNull(grouped);
        ArgumentException.ThrowIfNullOrWhiteSpace(sideId);
        var byLocation = new Dictionary<string, List<HexOccupant>>(StringComparer.Ordinal);
        void Add(string locationId, int stackValue, int cohesion, DefenseEstimate defense)
        {
            if (!byLocation.TryGetValue(locationId, out var list)) byLocation[locationId] = list = [];
            list.Add(new(stackValue, cohesion, defense));
        }
        foreach (var group in grouped.Groups.Where(value => value.SideId == sideId))
            Add(group.LocationId, group.StackValue!.Value, UniformCohesion(group.Members),
                EstimateRawDefenseLowerBound(group.GroupId, group.Members));
        foreach (var detached in grouped.Detached.Where(value => value.SideId == sideId))
            Add(detached.LocationId, detached.StackValue, detached.Member.CohesionLevel,
                EstimateRawDefenseLowerBound(detached.GroupId, [detached.Member]));

        var map = Cna1979HistoricalMap.Instance;
        var candidates = new List<ZocControlCandidate>();
        foreach (var (locationId, occupants) in byLocation)
        {
            var hexStacking = checked(occupants.Sum(value => value.StackValue));
            var hexDefense = Combine(occupants.Select(value => value.Defense));
            if (ResolveAgainstThreshold(hexDefense) is not { } defense) continue;
            // Any one occupant that individually clears cohesion is enough to
            // exert the hex's (shared) stacking and defense (10.26-10.28:
            // several units controlling the same hex have no extra effect).
            var qualifies = occupants.Any(value => Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
                Cna1979Combat.CombatUnitClassificationId, hexStacking, value.CohesionLevel, defense, true)).IsQualified);
            if (!qualifies) continue;
            foreach (var neighborId in NeighborsOf(locationId, cdSeamBound))
            {
                var terrain = map.TerrainAt(neighborId);
                if (!Cna1979HistoricalTerrainEffects.TryIsEnterable(terrain, GroundMobilityId, out var enterable, out _))
                    continue;
                var featureIds = new List<string>();
                var resolved = true;
                foreach (var feature in EdgeFeaturesBetween(locationId, neighborId, cdSeamBound))
                {
                    if (MapHexsideFeature(feature) is not { } mapped) { resolved = false; break; }
                    featureIds.Add(mapped);
                }
                if (!resolved) continue;
                candidates.Add(new ZocControlCandidate(neighborId,
                    new ZocSourceFacts(Cna1979Combat.CombatUnitClassificationId, hexStacking,
                        occupants.Max(value => value.CohesionLevel), defense, true),
                    new ZocProjectionFacts(featureIds, enterable)));
            }
        }
        return Cna1979Zoc.DeriveControlledLocationIds(candidates);
    }

    private static DefenseEstimate Combine(IEnumerable<DefenseEstimate> estimates)
    {
        long sum = 0;
        var allKnown = true;
        foreach (var estimate in estimates)
        {
            sum = checked(sum + estimate.LowerBound);
            if (!estimate.AllKnown) allKnown = false;
        }
        return new(sum, allKnown);
    }

    private static readonly (string First, string Second, string Feature)[] CdSeamEdges =
    [
        ("c3533", "d3501", "track"),
        ("c3633", "d3601", "unfinished-railroad"),
        ("c4033", "d3901", "track"),
    ];

    private static List<string> NeighborsOf(string locationId, bool cdSeamBound)
    {
        var neighbors = Cna1979HistoricalMap.Instance.NeighborsOf(locationId).ToList();
        if (cdSeamBound)
            foreach (var edge in CdSeamEdges)
            {
                if (string.Equals(edge.First, locationId, StringComparison.OrdinalIgnoreCase)) neighbors.Add(edge.Second);
                else if (string.Equals(edge.Second, locationId, StringComparison.OrdinalIgnoreCase)) neighbors.Add(edge.First);
            }
        return neighbors;
    }

    private static IReadOnlyList<string> EdgeFeaturesBetween(string first, string second, bool cdSeamBound)
    {
        if (cdSeamBound && Cna1979HistoricalMap.IsCdSeamCrossing(first, second))
            return Cna1979HistoricalMap.Instance.FeaturesBetween(first, second, useCdSeam: true) ?? Array.Empty<string>();
        return Cna1979HistoricalMap.Instance.FeaturesBetween(first, second) ?? Array.Empty<string>();
    }
}
