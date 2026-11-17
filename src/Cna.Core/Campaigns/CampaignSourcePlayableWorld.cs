using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Carries the actual source setup into a turn authority without flattening
// grouped counters or treating unmaterialized source evidence as empty hexes.
internal sealed class CampaignSourcePlayableWorld
{
    private CampaignSourcePlayableWorld(CampaignSourceAuthorityState source,
        CampaignGroupedWorld groupedWorld, CampaignSourceBattlefield battlefield, bool mapIdentityBound = false,
        bool cdSeamBound = false)
    {
        Source = source;
        GroupedWorld = groupedWorld;
        Battlefield = battlefield;
        MapIdentityBound = mapIdentityBound;
        if (cdSeamBound && !mapIdentityBound) throw new ArgumentException("C/D seam policy requires the historical map.");
        CdSeamBound = cdSeamBound;
        if (CdSeamBound && Cna1979HistoricalMap.Instance.EdgeBetween("c3533", "d3501", useCdSeam: true) is null)
            throw new InvalidOperationException("The approved C/D crossing is unavailable.");
        if (MapIdentityBound)
            RequireKnownMapLocations(battlefield.Entries.Select(value => value.LocationId));
        var entries = battlefield.Entries.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        foreach (var group in groupedWorld.Groups)
            foreach (var member in group.Members)
                if (!entries.TryGetValue(member.ElementId, out var entry)
                    || entry.SideId != group.SideId || entry.LocationId != group.LocationId)
                    throw new InvalidOperationException("Grouped member differs from the retained battlefield.");
        foreach (var member in groupedWorld.Detached)
            if (!entries.TryGetValue(member.Member.ElementId, out var entry)
                || entry.SideId != member.SideId || entry.LocationId != member.LocationId)
                throw new InvalidOperationException("Detached member differs from the retained battlefield.");
        foreach (var element in groupedWorld.Prior.World.Elements)
            if (!entries.TryGetValue(element.ElementId, out var entry) || entry.LocationId != element.CurrentLocationId)
                throw new InvalidOperationException("Active member differs from the retained battlefield.");
        var activeIds = groupedWorld.Prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        if (groupedWorld.Prior.World.Representations.Any(value => value.BoundElementIds.Any(id => !activeIds.Contains(id))))
            throw new InvalidOperationException("Active counter references a missing source member.");
        // Reject opposing materialized counters at the same printed location.
        // This does not prove different section IDs are different physical
        // hexes: unresolved seams separately block source gameplay. Evidence-
        // only entries remain unresolved, not materialized or disclosed.
        var locations = groupedWorld.Prior.World.Representations.Select(value => value.CurrentLocationId)
            .Concat(groupedWorld.Groups.Select(value => value.LocationId))
            .Concat(groupedWorld.Detached.Select(value => value.LocationId)).Distinct(StringComparer.Ordinal);
        foreach (var location in locations)
        {
            var sides = groupedWorld.OccupantsAt(location).Select(value => value.Formation?.SideId
                ?? value.Detached?.SideId
                ?? entries[value.Independent!.BoundElementIds.Single()].SideId).Distinct(StringComparer.Ordinal);
            if (sides.Skip(1).Any()) throw new InvalidOperationException("Opposing active counters cannot share a source hex.");
        }
        var retained = groupedWorld.Prior.PendingArrivals;
        if (retained.Count != battlefield.Pending.Count || retained.Any(arrival =>
        {
            var indexed = battlefield.Pending.SingleOrDefault(value => value.GroupId == arrival.GroupId);
            return indexed is null || indexed.SideId != arrival.SideId
                || indexed.GameTurn != arrival.Arrival.GameTurn
                || indexed.OperationStage != arrival.Arrival.OperationStage
                || indexed.EntryAreaId != arrival.EntryAreaId
                || !indexed.ElementIds.SequenceEqual(arrival.ElementIds);
        }))
            throw new InvalidOperationException("Pending arrival index differs from the retained source world.");
    }

    internal CampaignSourceAuthorityState Source { get; }
    internal bool MapIdentityBound { get; }
    internal bool CdSeamBound { get; }
    internal CampaignGroupedWorld GroupedWorld { get; }
    internal CampaignWorldSnapshotV8 World8 => GroupedWorld.Prior;
    internal CampaignSourceBattlefield Battlefield { get; }
    internal RandomStreamState RandomState => Source.RandomState;

    internal static CampaignSourcePlayableWorld Create(CampaignSourceAuthorityState source, bool mapIdentityBound = false,
        bool cdSeamBound = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        var grouped = source.GroupedWorld ?? throw new InvalidOperationException("Grouped source world is missing.");
        var battlefield = source.Battlefield ?? throw new InvalidOperationException("Source battlefield is missing.");
        if (grouped.Prior != source.World)
            throw new InvalidOperationException("Grouped world does not retain the active source world.");
        return new(source, grouped, battlefield, mapIdentityBound, cdSeamBound);
    }

    internal static void RequireKnownMapLocations(IEnumerable<string?> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);
        var map = Cna1979HistoricalMap.Instance;
        if (locations.Any(id => id is { Length: 5 } && id[0] is >= 'a' and <= 'e'
            && id.Skip(1).All(char.IsAsciiDigit) && map.HexAt(id) is null))
            throw new InvalidOperationException("Source battlefield references an unknown historical map hex.");
    }

    // The caller must adjudicate and journal the actual transition; this only
    // checks that the successor preserves matching world and pending indexes.
    internal CampaignSourcePlayableWorld WithWorld(CampaignGroupedWorld groupedWorld, CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(groupedWorld);
        ArgumentNullException.ThrowIfNull(battlefield);
        if (groupedWorld.ContentHash != GroupedWorld.ContentHash
            || groupedWorld.Prior.ContentIdentity != World8.ContentIdentity
            || groupedWorld.Prior.ScenarioId != World8.ScenarioId)
            throw new InvalidOperationException("Successor world changes its retained content identity.");
        return new(Source, groupedWorld, battlefield, MapIdentityBound, CdSeamBound);
    }

    // Exact printed location only; cross-section aliases are not certified.
    internal IReadOnlyList<CampaignWorldOccupant> OccupantsAt(string locationId) => GroupedWorld.OccupantsAt(locationId);

    // The retained source pack identifies Cairo's five major-city hexes.
    // Locations outside that excerpt remain unknown, not clear terrain.
    internal string? KnownTerrainAt(string locationId)
    {
        ArgumentNullException.ThrowIfNull(locationId);
        if (Source.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Source geography requires its pinned Graziani content.");
        return Source.Content.Definition.LegacyDefinition.Locations
            .SingleOrDefault(value => value.LocationId == locationId)?.TerrainId;
    }

    internal IReadOnlyList<CampaignSourceBattlefieldEntry> UnresolvedAt(string locationId)
    {
        var represented = RepresentedIds();
        return Array.AsReadOnly(Battlefield.Entries.Where(value => value.LocationId == locationId
            && !represented.Contains(value.ElementId) && !IsSourceRemovedFromPlay(value)).ToArray());
    }

    internal IReadOnlyList<string> ReadinessBlockers()
    {
        var blockers = new List<string>();
        // Battlefield.SerializeCanonical() still prints these two as fixed
        // constants: real completion/certification criteria for this pair
        // were never established by any existing code or test, and two
        // attempts here to infer them from the five evidence-to-active
        // promotion receipts both broke an existing readiness assertion
        // (both resolved true far earlier than the existing test expects
        // them still blocked). Left as the original unconditional blockers
        // rather than guess a third time; see the session hand-over notes.
        using var battlefield = JsonDocument.Parse(Battlefield.SerializeCanonical());
        if (!battlefield.RootElement.GetProperty("battlefieldComplete").GetBoolean()) blockers.Add("battlefield-incomplete");
        if (!battlefield.RootElement.GetProperty("controlCertified").GetBoolean()) blockers.Add("control-incomplete");
        // CampaignSourceLocalMapWorld wraps the old, small excerpt map and
        // still prints worldCoverageComplete/mobilityStatus as fixed
        // placeholders; it is superseded once MapIdentityBound is in effect,
        // since the constructor already required full historical-map
        // coverage for every currently represented location (see
        // RequireKnownMapLocations) and "source-movement-map-topology-
        // incomplete" below is the real gate for that track.
        if (!MapIdentityBound)
        {
            if (Source.LocalMapWorld is null) blockers.Add("map-missing");
            else
            {
                using var map = JsonDocument.Parse(Source.LocalMapWorld.SerializeCanonical());
                if (!map.RootElement.GetProperty("worldCoverageComplete").GetBoolean()) blockers.Add("map-incomplete");
                if (map.RootElement.GetProperty("mobilityStatus").GetString() != "complete") blockers.Add("mobility-incomplete");
            }
        }
        if (GroupedWorld.Groups.Any(value => !value.DispositionConfirmed || value.StackValue is null))
            blockers.Add("formation-disposition-incomplete");
        if (Source.CanCorrectSeventhShell) blockers.Add("seventh-armored-shell-stack-correction-required");
        if (Source.CwFreeGunsWorld?.CwFrontlineContent is not null
            && (Source.AttachedInitialLoads is not { FrontlineFinished: true } loads
                || loads.Targets.Any(value => CampaignSourceAttachedInitialLoads.FrontlineTargets.Any(target => target.ElementId == value.ElementId)
                    && (value.AmmoPoints != 0 || value.FuelPoints != 0))))
            blockers.Add("cw-frontline-initial-loads-unresolved");
        // Cleared by the formal ruling (CampaignSourceCwFreeGunsWorld.AcceptMotorMachineGunRuling):
        // the source content already states no scenario motor pool or effective
        // allowance exists for this element, and both blockers are two facets of
        // that same underlying fact, so they clear together once accepted.
        if (Source.CwFreeGunsWorld is { MotorMachineGunContent: not null, MotorMachineGunRulingAccepted: false })
        {
            blockers.Add("giarabub-motor-mg-historical-motorization-priority-unresolved");
            blockers.Add("giarabub-motor-mg-capability-and-mobility-unresolved");
        }
        if (GroupedWorld.Groups.Where(value => value.GroupId is CampaignSourceCcnnBatch.FirstId or CampaignSourceCcnnBatch.FourthId)
            .Any(value => value.Members.Any(member => member.CapabilityPointAllowance is null
                || (member.ElementId is "1-ccnn-hq" or "4-ccnn-hq" or "201-ccnn-engineer" or "204-ccnn-engineer"
                    && member.ComponentType is null))))
            blockers.Add("ccnn-characteristics-incomplete");
        if (!Source.OpeningConvoyPlan.IsFinished) blockers.Add("axis-gt1-supply-plan-incomplete");
        if (Source.CommonwealthSupplyPlans is null || Source.CommonwealthSupplyPlans.NextTargetGameTurn is not null)
            blockers.Add("commonwealth-supply-plans-incomplete");
        if (Source.SeptemberPlans?.Entries.Any(value => value.Die is not null && !value.Finished) == true)
            blockers.Add("axis-prestart-supply-plan-incomplete");
        if (Source.AxisUnattachedCargo is { IsFinished: false }
            || Source.CommonwealthUnattachedCargo is { IsFinished: false })
            blockers.Add("initial-cargo-setup-incomplete");
        if (Source.AxisSupplyWorld.PendingDumpIds.Count != 0 || Source.AxisSupplyWorld.PendingDummyIds.Count != 0
            || Source.CommonwealthSupplyWorld.PendingPlacementIds.Count != 0 || Source.CommonwealthSupplyWorld.DummyDecision is null)
            blockers.Add("initial-supply-setup-incomplete");
        if (Source.CairoSetup is null || Source.CairoSetup.PendingIds.Count != 0
            || Source.AxisFreeArtillery is null || Source.AxisFreeArtillery.PendingIds.Count != 0
            || Source.AxisCorpsArtillery is null || Source.AxisCorpsArtillery.PendingIds.Count != 0
            || Source.CwFreeGuns is null || Source.CwFreeGuns.PendingIds.Count != 0)
            blockers.Add("initial-unit-setup-incomplete");
        // This formation is specified by scenario 60.41 but is not currently
        // indexed in the source battlefield, so completeness cannot be inferred
        // merely from the existing entries having been represented.
        if (!Source.IsSixthAustralianGrouped) blockers.Add("sixth-australian-setup-unavailable");
        // The source-pinned provisional classification is visible in the
        // confirmed Fourth Indian disposition; actual training activity and
        // prior progress remain unknown.
        if (!Source.HorseStageOneResolved) blockers.Add("central-india-horse-training-state-unresolved");
        if (Source.FinalizedOwners != 3) blockers.Add("owner-setup-finalization-incomplete");
        if (!CampaignSourceInitialContacts.CanProject(this)) blockers.Add("source-initial-contacts-unavailable");
        // Grouped ZOC requires a determinate source on every grouped
        // formation and detached member of both sides, and no remaining
        // independently represented combat-classified element: this module
        // does not yet derive ZOC for independent (ungrouped) elements.
        if (!CampaignSourceGroupedZoc.CanDeriveForSide(this, "axis")
            || !CampaignSourceGroupedZoc.CanDeriveForSide(this, "commonwealth"))
            blockers.Add("source-grouped-movement-domain-incomplete");
        // The historical map binds hex and terrain coverage for every
        // currently represented location at construction time (see
        // RequireKnownMapLocations above); the legacy, unbound track has no
        // such map at all.
        if (!MapIdentityBound) blockers.Add("source-movement-map-topology-incomplete");
        // Only the user-approved 33-column C/D seam ruling is certified;
        // without it, cross-map-seam adjacency remains unresolved.
        if (!CdSeamBound) blockers.Add("source-cross-map-seams-unresolved");
        var represented = RepresentedIds();
        if (Battlefield.Entries.Any(value => value.LocationId is null || (!represented.Contains(value.ElementId)
            && !IsSourceRemovedFromPlay(value))))
            blockers.Add("forces-incomplete");
        return Array.AsReadOnly(blockers.ToArray());
    }

    internal bool IsGameplayReady => ReadinessBlockers().Count == 0;

    private bool IsSourceRemovedFromPlay(CampaignSourceBattlefieldEntry value) =>
        Source.CwFreeGunsWorld?.GarrisonRemainderContent is not null
        && value.AuthorityStatus == "source-land-only-aa-removed"
        && ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds.Contains(value.ElementId, StringComparer.Ordinal);

    private HashSet<string> RepresentedIds() => GroupedWorld.Prior.World.Representations
        .SelectMany(value => value.BoundElementIds)
        .Concat(GroupedWorld.Groups.SelectMany(value => value.Members.Select(member => member.ElementId)))
        .Concat(GroupedWorld.Detached.Select(value => value.Member.ElementId))
        .ToHashSet(StringComparer.Ordinal);
}
