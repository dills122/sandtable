using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// A single source-identity-bound index over retained scenario evidence. Entries
// tagged source-evidence are not yet playable units or proof of control.
internal sealed class CampaignSourceBattlefield
{
    internal const string PolicyId = "graziani.source-battlefield.evidence-index.v1";
    internal const string InitialHash = "sha256:5004b5cd07299c479c6fc77bd2421fa751b5b3081e3e95ae1657d27a047a70b8";
    private readonly ReadOnlyCollection<CampaignSourceBattlefieldEntry> entries;
    private readonly ReadOnlyCollection<CampaignSourcePendingGroup> pending;
    private readonly bool cairoSetupIncluded;
    private readonly string? cairoSetupOriginHash;
    private readonly string? axisFreeArtilleryOriginHash;
    private readonly string? axisCorpsArtilleryOriginHash;
    private readonly string? matruhOriginHash;
    private readonly string? cwFreeGunsOriginHash;

    private CampaignSourceBattlefield(IEnumerable<CampaignSourceBattlefieldEntry> entries,
        IEnumerable<CampaignSourcePendingGroup> pending, bool cairoSetupIncluded = false,
        string? cairoSetupOriginHash = null, string? axisFreeArtilleryOriginHash = null,
        string? axisCorpsArtilleryOriginHash = null, string? matruhOriginHash = null,
        string? cwFreeGunsOriginHash = null)
    {
        this.cairoSetupIncluded = cairoSetupIncluded;
        this.cairoSetupOriginHash = cairoSetupOriginHash;
        this.axisFreeArtilleryOriginHash = axisFreeArtilleryOriginHash;
        this.axisCorpsArtilleryOriginHash = axisCorpsArtilleryOriginHash;
        this.matruhOriginHash = matruhOriginHash;
        this.cwFreeGunsOriginHash = cwFreeGunsOriginHash;
        this.entries = Array.AsReadOnly(entries.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
        this.pending = Array.AsReadOnly(pending.OrderBy(value => value.GroupId, StringComparer.Ordinal).ToArray());
        var ids = this.entries.Select(value => value.ElementId).ToArray();
        var deferred = this.pending.SelectMany(value => value.ElementIds).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || deferred.Distinct(StringComparer.Ordinal).Count() != deferred.Length
            || ids.Intersect(deferred, StringComparer.Ordinal).Any()
            || this.pending.Select(value => value.GroupId).Distinct(StringComparer.Ordinal).Count() != this.pending.Count)
            throw new InvalidOperationException("Source battlefield contains duplicate, already-placed or overlapping pending units.");
    }

    internal IReadOnlyList<CampaignSourceBattlefieldEntry> Entries => entries;
    internal IReadOnlyList<CampaignSourcePendingGroup> Pending => pending;

    internal CampaignSourceBattlefield ActivateCwFrontline(CampaignWorldSnapshotV8 prior, CampaignWorldSnapshotV8 successor)
    {
        var units = ContentSourceCwFrontlineArtifact.Units;
        var ids = units.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        if (!prior.World.Elements.All(successor.World.Elements.Contains)
            || !prior.World.Representations.All(successor.World.Representations.Contains)
            || !prior.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !prior.PendingArrivals.SequenceEqual(successor.PendingArrivals)
            || !successor.World.Elements.Except(prior.World.Elements).Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal).SetEquals(ids))
            throw new InvalidOperationException("CW frontline differs from predecessor.");
        foreach (var unit in units)
        {
            var entry = entries.Single(value => value.ElementId == unit.Id);
            var element = successor.World.Elements.Single(value => value.ElementId == unit.Id);
            var rep = successor.World.Representations.Single(value => value.BoundElementIds.Contains(unit.Id));
            if (entry.SideId != "commonwealth" || entry.LocationId != unit.Location || entry.AuthorityStatus != "source-evidence"
                || element.CurrentLocationId != unit.Location || rep.CurrentLocationId != unit.Location
                || rep.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement || !rep.BoundElementIds.SequenceEqual([unit.Id]))
                throw new InvalidOperationException("CW frontline differs from source evidence.");
        }
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-cw-frontline-active-initial-loads-unresolved" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash,
            matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ActivateGiarabubMotorMachineGun(CampaignWorldSnapshotV8 prior,
        CampaignWorldSnapshotV8 successor)
    {
        ArgumentNullException.ThrowIfNull(prior); ArgumentNullException.ThrowIfNull(successor);
        var id = ContentSourceGiarabubMotorMachineGunArtifact.ElementId;
        var location = ContentSourceGiarabubMotorMachineGunArtifact.LocationId;
        var entry = entries.Single(value => value.ElementId == id);
        var element = successor.World.Elements.Single(value => value.ElementId == id);
        var representation = successor.World.Representations.Single(value => value.BoundElementIds.Contains(id));
        if (entry is not { SideId: "axis", AuthorityStatus: "source-evidence" } || entry.LocationId != location
            || element.CurrentLocationId != location || representation.CurrentLocationId != location
            || representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
            || !representation.BoundElementIds.SequenceEqual([id])
            || !prior.World.Elements.All(successor.World.Elements.Contains)
            || !prior.World.Representations.All(successor.World.Representations.Contains)
            || !prior.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !prior.PendingArrivals.SequenceEqual(successor.PendingArrivals)
            || !successor.World.Elements.Except(prior.World.Elements).Select(value => value.ElementId).SequenceEqual([id]))
            throw new InvalidOperationException("Motor MG world differs from source evidence or its predecessor.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == id
            ? value with { AuthorityStatus = "source-axis-hh-active-motorization-unresolved" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash,
            matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ActivateAxisGarrisonRemainder(CampaignWorldSnapshotV8 prior,
        CampaignWorldSnapshotV8 successor)
    {
        ArgumentNullException.ThrowIfNull(prior); ArgumentNullException.ThrowIfNull(successor);
        var units = ContentSourceAxisGarrisonRemainderPackArtifact.Units;
        var ids = units.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        if (!prior.World.Elements.All(successor.World.Elements.Contains)
            || !prior.World.Representations.All(successor.World.Representations.Contains)
            || !prior.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !prior.PendingArrivals.SequenceEqual(successor.PendingArrivals)
            || !successor.World.Elements.Except(prior.World.Elements).Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal).SetEquals(ids))
            throw new InvalidOperationException("Garrison remainder does not preserve its predecessor.");
        foreach (var unit in units)
        {
            var entry = entries.Single(value => value.ElementId == unit.Id);
            var element = successor.World.Elements.Single(value => value.ElementId == unit.Id);
            var representation = successor.World.Representations.Single(value => value.BoundElementIds.Contains(unit.Id));
            if (entry.SideId != "axis" || entry.LocationId != unit.Location || entry.AuthorityStatus != "source-evidence"
                || element.CurrentLocationId != unit.Location || representation.CurrentLocationId != unit.Location
                || representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
                || !representation.BoundElementIds.SequenceEqual([unit.Id]))
                throw new InvalidOperationException("Garrison remainder differs from its retained source entry.");
        }
        var removed = ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds.ToHashSet(StringComparer.Ordinal);
        if (removed.Any(id => entries.Single(value => value.ElementId == id) is not
            { SideId: "axis", AuthorityStatus: "source-evidence" }
                || prior.World.Elements.Any(value => value.ElementId == id)
                || successor.World.Elements.Any(value => value.ElementId == id)))
            throw new InvalidOperationException("Abstractly removed AA differs from source evidence or is active.");
        return new CampaignSourceBattlefield(entries.Select(entry => ids.Contains(entry.ElementId)
            ? entry with { AuthorityStatus = "source-axis-garrison-remainder-active" }
            : removed.Contains(entry.ElementId) ? entry with { AuthorityStatus = "source-land-only-aa-removed" } : entry), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash,
            matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ActivateAxisMinorGarrisons(CampaignWorldSnapshotV8 prior,
        CampaignWorldSnapshotV8 successor)
    {
        ArgumentNullException.ThrowIfNull(prior); ArgumentNullException.ThrowIfNull(successor);
        var units = ContentSourceAxisMinorGarrisonsPackArtifact.Units;
        var ids = units.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        if (!prior.World.Elements.All(successor.World.Elements.Contains)
            || !prior.World.Representations.All(successor.World.Representations.Contains)
            || !prior.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !prior.PendingArrivals.SequenceEqual(successor.PendingArrivals)
            || !successor.World.Elements.Except(prior.World.Elements).Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal).SetEquals(ids))
            throw new InvalidOperationException("Minor garrison world does not preserve its predecessor.");
        foreach (var unit in units)
        {
            var entry = entries.Single(value => value.ElementId == unit.Id);
            var element = successor.World.Elements.Single(value => value.ElementId == unit.Id);
            var representation = successor.World.Representations.Single(value => value.BoundElementIds.Contains(unit.Id));
            if (entry.SideId != "axis" || entry.LocationId != unit.Location || entry.AuthorityStatus != "source-evidence"
                || element.CurrentLocationId != unit.Location || representation.CurrentLocationId != unit.Location
                || representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
                || !representation.BoundElementIds.SequenceEqual([unit.Id]))
                throw new InvalidOperationException("Minor garrison differs from its retained source entry.");
        }
        return new CampaignSourceBattlefield(entries.Select(entry => ids.Contains(entry.ElementId)
            ? entry with { AuthorityStatus = "source-axis-minor-active" } : entry), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash,
            matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield AdmitAresca(CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId);
        var ids = group.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != 2 || ids.Any(id => !entries.Any(entry => entry.ElementId == id && entry.SideId == "axis"
                && entry.LocationId == CampaignSourceArescaGroupedDefinition.LocationId && entry.AuthorityStatus == "source-evidence")))
            throw new InvalidOperationException("Aresca source evidence differs from grouped units.");
        return new CampaignSourceBattlefield(entries.Select(entry => ids.Contains(entry.ElementId)
                ? entry with { AuthorityStatus = "source-aresca-grouped" } : entry), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash,
            matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield AdmitCcnnBatch(CampaignGroupedWorld world)
    {
        var groups = world.Groups.Where(value => value.GroupId is CampaignSourceCcnnBatch.FirstId or CampaignSourceCcnnBatch.FourthId).ToArray();
        if (groups.Length != 2) throw new InvalidOperationException("Both CCNN groups are required.");
        var ids = groups.SelectMany(group => group.Members.Select(member => (member.ElementId, group.LocationId))).ToArray();
        if (ids.Any(item => !entries.Any(entry => entry.ElementId == item.ElementId && entry.SideId == "axis"
                && entry.LocationId == item.LocationId && entry.AuthorityStatus == "source-evidence")))
            throw new InvalidOperationException("CCNN source evidence differs from grouped units.");
        var idSet = ids.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(entry => idSet.Contains(entry.ElementId)
                ? entry with { AuthorityStatus = "source-ccnn-grouped" } : entry), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash,
            matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield AddSixthAustralian(CampaignGroupedWorld world, string location)
    {
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId);
        var ids = group.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        if (group.SideId != "commonwealth" || group.LocationId != location || group.StackValue != 5
            || ids.Count != 10 || entries.Any(value => ids.Contains(value.ElementId))
            || pending.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("Australian formation conflicts with source battlefield.");
        var added = ids.Select(id => new CampaignSourceBattlefieldEntry(id, "commonwealth", location, "source-australian-grouped"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending, cairoSetupIncluded, cairoSetupOriginHash,
            axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ArriveRetainedFormation(CampaignGroupedWorld prior,
        CampaignGroupedWorld successor, string pendingGroupId, string locationId)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(successor);
        var group = pending.Single(value => value.GroupId == pendingGroupId);
        var scheduled = prior.Prior.PendingArrivals.Single(value => value.GroupId == pendingGroupId);
        var actual = successor.Groups.Single(value => value.GroupId == pendingGroupId);
        var ids = group.ElementIds.Order(StringComparer.Ordinal).ToArray();
        if (group.SideId != scheduled.SideId || group.GameTurn != scheduled.Arrival.GameTurn
            || group.OperationStage != scheduled.Arrival.OperationStage || group.EntryAreaId != scheduled.EntryAreaId
            || !ids.SequenceEqual(scheduled.ElementIds.Order(StringComparer.Ordinal))
            || actual.SideId != group.SideId || actual.LocationId != locationId
            || !actual.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || successor.ContentHash != prior.ContentHash
            || !successor.Groups.Where(value => value.GroupId != pendingGroupId).SequenceEqual(prior.Groups)
            || !successor.Detached.SequenceEqual(prior.Detached)
            || !successor.ExternalReservations.SequenceEqual(prior.ExternalReservations)
            || !prior.Prior.World.Elements.All(successor.Prior.World.Elements.Contains)
            || !prior.Prior.World.Representations.All(successor.Prior.World.Representations.Contains)
            || !prior.Prior.World.BrokenVehicleLots.SequenceEqual(successor.Prior.World.BrokenVehicleLots)
            || !successor.Prior.PendingArrivals.SequenceEqual(prior.Prior.PendingArrivals.Where(value => value.GroupId != pendingGroupId))
            || entries.Any(value => ids.Contains(value.ElementId, StringComparer.Ordinal))
            || successor.Prior.PendingArrivals.Any(value => value.GroupId == pendingGroupId))
            throw new InvalidOperationException("Arrival must match the retained battlefield and scheduled group.");
        var added = ids.Select(id => new CampaignSourceBattlefieldEntry(id, group.SideId, locationId, "world8-arrival"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending.Where(value => value.GroupId != pendingGroupId),
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    // Called only after movement authority has validated path and payment. The
    // moved group's entries follow its counter; detached members stay behind.
    internal CampaignSourceBattlefield MoveFormation(CampaignGroupedWorld prior, CampaignGroupedWorld successor, string groupId)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(successor);
        var before = prior.Groups.Single(value => value.GroupId == groupId);
        var after = successor.Groups.Single(value => value.GroupId == groupId);
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        if (!successor.Moved || before.SideId != after.SideId || before.LocationId == after.LocationId
            || !after.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal).SetEquals(ids)
            || !successor.Groups.Where(value => value.GroupId != groupId).SequenceEqual(prior.Groups.Where(value => value.GroupId != groupId))
            || !successor.Detached.SequenceEqual(prior.Detached)
            || !successor.ExternalReservations.SequenceEqual(prior.ExternalReservations)
            || !successor.Prior.PendingArrivals.SequenceEqual(prior.Prior.PendingArrivals)
            || ids.Any(id => entries.Count(value => value.ElementId == id && value.SideId == before.SideId
                && value.LocationId == before.LocationId) != 1))
            throw new InvalidOperationException("Movement successor does not match the retained battlefield.");
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
                ? value with { LocationId = after.LocationId } : value), pending, cairoSetupIncluded, cairoSetupOriginHash,
            axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    // The setup policy is source-bound separately; its unit IDs are added as
    // unplaced evidence and cannot be mistaken for active battlefield pieces.
    internal CampaignSourceBattlefield AddCairoSetup(CampaignSourceCairoInitialSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (cairoSetupIncluded || setup.Placements.Count != 0 || setup.BattlefieldHash != Hash())
            throw new InvalidOperationException("Cairo setup is not bound to this initial battlefield.");
        var added = CampaignSourceCairoInitialSetup.Units.Select(unit => new CampaignSourceBattlefieldEntry(
            unit.ElementId, "commonwealth", null, "source-setup-unplaced"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending, cairoSetupIncluded: true,
            cairoSetupOriginHash: setup.BattlefieldHash, axisFreeArtilleryOriginHash: axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash: axisCorpsArtilleryOriginHash, matruhOriginHash: matruhOriginHash, cwFreeGunsOriginHash: cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PlaceCairo(CampaignSourceCairoInitialSetup setup,
        string elementId, string locationId)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (!cairoSetupIncluded || setup.BattlefieldHash != cairoSetupOriginHash
            || !setup.ValidDestinations(elementId).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Cairo setup destination is unavailable.");
        var recorded = entries.Where(value => value.AuthorityStatus == "source-setup-placed")
            .Select(value => (value.ElementId, value.LocationId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        var choices = setup.Placements.Select(value => (value.ElementId, LocationId: (string?)value.LocationId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (!recorded.SequenceEqual(choices))
            throw new InvalidOperationException("Cairo setup choices differ from the retained battlefield.");
        var unit = entries.SingleOrDefault(value => value.ElementId == elementId);
        if (unit is null || unit.AuthorityStatus != "source-setup-unplaced" || unit.LocationId is not null)
            throw new InvalidOperationException("Cairo setup unit is not awaiting placement.");
        var occupants = entries.Where(value => value.LocationId == locationId).ToArray();
        if (occupants.Any(value => value.AuthorityStatus != "source-setup-placed") || occupants.Length >= 8)
            throw new InvalidOperationException("Cairo setup destination has unknown or full occupancy.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == elementId
            ? value with { LocationId = locationId, AuthorityStatus = "source-setup-placed" } : value),
            pending, cairoSetupIncluded: true, cairoSetupOriginHash: cairoSetupOriginHash,
            axisFreeArtilleryOriginHash: axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash: axisCorpsArtilleryOriginHash,
            matruhOriginHash: matruhOriginHash, cwFreeGunsOriginHash: cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteMaletti(CampaignSourceAxisPriorityMotorization priority,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(priority);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length == 0
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals)
            || added.Any(id => !priority.DetachedTargets.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Active Maletti units require new owner-detached source counters.");
        foreach (var id in added)
        {
            var entry = entries.SingleOrDefault(value => value.ElementId == id);
            var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
            if (entry is null || entry.SideId != "axis" || entry.LocationId != "c3617"
                || entry.AuthorityStatus != "source-evidence" || actual.CurrentLocationId != entry.LocationId)
                throw new InvalidOperationException("Active Maletti unit differs from retained source battlefield.");
        }
        return new CampaignSourceBattlefield(entries.Select(value => added.Contains(value.ElementId, StringComparer.Ordinal)
            ? value with { AuthorityStatus = "world8-active" } : value), pending,
            cairoSetupIncluded: cairoSetupIncluded, cairoSetupOriginHash: cairoSetupOriginHash,
            axisFreeArtilleryOriginHash: axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash: axisCorpsArtilleryOriginHash,
            matruhOriginHash: matruhOriginHash, cwFreeGunsOriginHash: cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteCertifiedMaletti(CampaignSourceMalettiHierarchy hierarchy,
        CampaignSourceAxisPriorityMotorization priority, CampaignWorldSnapshotV8 priorWorld,
        CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(priority);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = activeWorld.World.Elements.Where(value => !previous.Contains(value.ElementId)).ToArray();
        if (!hierarchy.Confirmed || hierarchy.DetachedId is null || added.Length != 1
            || added[0].ElementId != hierarchy.DetachedId
            || priority.DetachedTargets.Count != 1 || priority.DetachedTargets[0] != hierarchy.DetachedId)
            throw new InvalidOperationException("Active Maletti unit requires the retained certified setup choice.");
        var promoted = PromoteMaletti(priority, priorWorld, activeWorld);
        return new CampaignSourceBattlefield(promoted.entries.Select(value => value.ElementId == hierarchy.DetachedId
            ? value with { AuthorityStatus = "source-certified-maletti-active" } : value), promoted.pending,
            promoted.cairoSetupIncluded, promoted.cairoSetupOriginHash, promoted.axisFreeArtilleryOriginHash,
            promoted.axisCorpsArtilleryOriginHash, promoted.matruhOriginHash, promoted.cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteGroupedMaletti(CampaignGroupedWorld world,
        CampaignSourceMalettiHierarchy hierarchy, CampaignSourceAxisPriorityMotorization priority)
    {
        RequireGroupedMaletti(world, hierarchy, priority, "source-evidence");
        var attached = world.Groups.Single(value => value.GroupId == "maletti-division").Members
            .Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => attached.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-maletti-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateGroupedMaletti(CampaignGroupedWorld world,
        CampaignSourceMalettiHierarchy hierarchy, CampaignSourceAxisPriorityMotorization priority) =>
        RequireGroupedMaletti(world, hierarchy, priority, "source-maletti-grouped");

    internal CampaignSourceBattlefield DetachGroupedMaletti(CampaignGroupedWorld priorWorld,
        CampaignGroupedWorld nextWorld, CampaignSourceMalettiHierarchy priorHierarchy,
        CampaignSourceMalettiHierarchy nextHierarchy, CampaignSourceAxisPriorityMotorization nextPriority)
    {
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(nextWorld);
        ArgumentNullException.ThrowIfNull(priorHierarchy);
        ArgumentNullException.ThrowIfNull(nextHierarchy);
        ArgumentNullException.ThrowIfNull(nextPriority);
        var id = nextHierarchy.DetachedId;
        if (!priorHierarchy.Confirmed || priorHierarchy.DetachedId is not null || !nextHierarchy.Confirmed
            || id is null || priorHierarchy.FreeHash != nextHierarchy.FreeHash
            || priorHierarchy.CorpsHash != nextHierarchy.CorpsHash
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash)
            throw new InvalidOperationException("Maletti detachment must preserve its confirmed source world.");
        var prior = priorWorld.Groups.Single(value => value.GroupId == "maletti-division");
        var next = nextWorld.Groups.Single(value => value.GroupId == "maletti-division");
        var selected = prior.Members.SingleOrDefault(value => value.ElementId == id);
        var detached = nextWorld.Detached.SingleOrDefault(value => value.GroupId == "maletti-division"
            && value.Member.ElementId == id);
        if (selected is null || detached is null || detached.Member != (selected with { ParentId = null })
            || !prior.Members.Where(value => value.ElementId != id).SequenceEqual(next.Members)
            || prior.RootId != next.RootId || prior.SideId != next.SideId || prior.LocationId != next.LocationId
            || prior.StackValue != next.StackValue || prior.MotorPool != next.MotorPool
            || prior.DispositionConfirmed != next.DispositionConfirmed
            || !priorWorld.Groups.Where(value => value.GroupId != "maletti-division")
                .SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != "maletti-division"))
            || !priorWorld.Detached.SequenceEqual(nextWorld.Detached.Where(value => value != detached))
            || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations))
            throw new InvalidOperationException("Maletti detachment changed unrelated grouped state.");
        var entry = entries.SingleOrDefault(value => value.ElementId == id);
        if (entry is null || entry.LocationId != "c3617" || entry.SideId != "axis"
            || entry.AuthorityStatus != "source-maletti-grouped")
            throw new InvalidOperationException("Maletti source member is not attached on the battlefield.");
        var successor = new CampaignSourceBattlefield(entries.Select(value => value.ElementId == id
            ? value with { AuthorityStatus = "source-evidence" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
        successor.ValidateGroupedMaletti(nextWorld, nextHierarchy, nextPriority);
        return successor;
    }

    private void RequireGroupedMaletti(CampaignGroupedWorld world, CampaignSourceMalettiHierarchy hierarchy,
        CampaignSourceAxisPriorityMotorization priority, string attachedStatus)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(priority);
        if (!hierarchy.Confirmed || (hierarchy.DetachedId is null ? priority.DetachedTargets.Count != 0
                : priority.DetachedTargets.Count != 1 || priority.DetachedTargets[0] != hierarchy.DetachedId))
            throw new InvalidOperationException("Maletti grouped world requires its confirmed hierarchy and detachment.");
        var ids = CampaignSourceMalettiHierarchy.SourceAttachments.Select(value => value.Id)
            .Append("maletti-hq").Order(StringComparer.Ordinal).ToArray();
        var expectedAttached = ids.Where(value => value != hierarchy.DetachedId).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == "maletti-division");
        var occupants = entries.Where(value => value.LocationId == "c3617").ToArray();
        if (group is null || group.SideId != "axis" || group.RootId != "maletti-hq"
            || group.LocationId != "c3617" || group.StackValue != 5 || group.MotorPool != 35
            || !group.DispositionConfirmed
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(expectedAttached)
            || group.Members.Any(value => value.ElementId == "maletti-hq" ? value.ParentId is not null
                : CampaignSourceMalettiHierarchy.SourceAttachments.Single(source => source.Id == value.ElementId).Parent != value.ParentId)
            || occupants.Length != ids.Length
            || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != "axis"
                || (value.ElementId != hierarchy.DetachedId && value.AuthorityStatus != attachedStatus)))
            throw new InvalidOperationException("Maletti grouped world differs from retained battlefield occupancy.");

        var detached = world.Detached.Where(value => value.GroupId == "maletti-division").ToArray();
        var external = world.ExternalReservations.Where(value => value.GroupId == "maletti-division").ToArray();
        var representations = world.Prior.World.Representations.Where(value => value.CurrentLocationId == "c3617").ToArray();
        if (hierarchy.DetachedId is not { } id)
        {
            if (detached.Length != 0 || external.Length != 0 || representations.Length != 0)
                throw new InvalidOperationException("Unexpected independent counter in Maletti source hex.");
            return;
        }
        var entry = occupants.Single(value => value.ElementId == id);
        var points = priority.AssignedPoints(id);
        if (detached.Length + external.Length != 1
            || (detached.Length == 1 && (entry.AuthorityStatus != "source-evidence" || representations.Length != 0
                || detached[0].Member.ElementId != id || detached[0].LocationId != "c3617"
                || detached[0].SideId != "axis" || detached[0].StackValue != 1
                || detached[0].Member.InfantryToe != 5 || detached[0].Member.AssignedMotorPoints != points))
            || (external.Length == 1 && (entry.AuthorityStatus != "source-certified-maletti-active"
                || external[0].ElementId != id || external[0].InfantryToe != 5 || external[0].AssignedMotorPoints != points
                || representations.Length != 1 || representations[0].BoundElementIds.Count != 1
                || representations[0].BoundElementIds[0] != id)))
            throw new InvalidOperationException("Maletti detached counter or its motor reservation differs.");
    }

    internal CampaignSourceBattlefield PromoteSourceIndependent(
        CampaignSourceCommonwealthSupplementalForces supplement,
        CampaignSourceAxisSupplementalForces axisSupplement,
        CampaignSourceAxisPriorityMotorization axisPriority,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld) =>
        PromoteSourceIndependentCore(supplement, axisSupplement, axisPriority, priorWorld, activeWorld,
            ["1-kings-royal-rifle-corps", "2-rifle-brigade"]);

    internal CampaignSourceBattlefield PromoteSourceIndependentV2(
        CampaignSourceCommonwealthSupplementalForces supplement,
        CampaignSourceAxisSupplementalForces axisSupplement,
        CampaignSourceAxisPriorityMotorization axisPriority,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld) =>
        PromoteSourceIndependentCore(supplement, axisSupplement, axisPriority, priorWorld, activeWorld,
            ["1-kings-royal-rifle-corps", "2-rifle-brigade", "3-coldstream-guards",
                "2-scots-guards", "1-royal-northumberland-fusiliers"]);

    private CampaignSourceBattlefield PromoteSourceIndependentCore(
        CampaignSourceCommonwealthSupplementalForces supplement,
        CampaignSourceAxisSupplementalForces axisSupplement,
        CampaignSourceAxisPriorityMotorization axisPriority,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld, string[] cwEligible)
    {
        ArgumentNullException.ThrowIfNull(supplement);
        ArgumentNullException.ThrowIfNull(axisSupplement);
        ArgumentNullException.ThrowIfNull(axisPriority);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        if (supplement.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash
            || axisSupplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Source evidence differs from its pinned identity.");
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length == 0
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals)
            || added.Any(id => !cwEligible.Contains(id, StringComparer.Ordinal)
                && !axisPriority.DetachedTargets.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Active source units do not preserve their retained predecessor.");
        foreach (var id in added)
        {
            var entry = entries.SingleOrDefault(value => value.ElementId == id);
            var isCommonwealth = cwEligible.Contains(id, StringComparer.Ordinal);
            var source = isCommonwealth
                ? supplement.Forces.SingleOrDefault(value => value.ElementId == id)
                : axisSupplement.Forces.SingleOrDefault(value => value.ElementId == id);
            var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
            if (entry is null || source is null || entry.SideId != (isCommonwealth ? "commonwealth" : "axis")
                || entry.AuthorityStatus != "source-evidence" || entry.LocationId != source.LocationId
                || actual.CurrentLocationId != source.LocationId || source.InitialToes is null
                || actual.Components.Count != source.InitialToes.Count
                || actual.Components.Any(component => !source.InitialToes.Any(seed =>
                    seed.ComponentId == component.ComponentId && seed.Toe == component.CurrentToe)))
                throw new InvalidOperationException("Active Commonwealth unit differs from retained source evidence.");
        }
        return new CampaignSourceBattlefield(entries.Select(value => added.Contains(value.ElementId, StringComparer.Ordinal)
            ? value with { AuthorityStatus = "world8-active" } : value), pending,
            cairoSetupIncluded: cairoSetupIncluded, cairoSetupOriginHash: cairoSetupOriginHash,
            axisFreeArtilleryOriginHash: axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash: axisCorpsArtilleryOriginHash,
            matruhOriginHash: matruhOriginHash, cwFreeGunsOriginHash: cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteCwPriorityWorld(
        CampaignSourceCommonwealthSupplementalForces supplement,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(supplement);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        const string id = "1-french-motor-marines";
        if (supplement.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash
            || entries.Any(value => value.LocationId == "c3926" && value.ElementId != id))
            throw new InvalidOperationException("French company source evidence or occupancy differs from its retained setup.");
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length != 1 || added[0] != id
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals))
            throw new InvalidOperationException("Active French company must preserve its retained predecessor.");
        var entry = entries.SingleOrDefault(value => value.ElementId == id);
        var source = supplement.Forces.SingleOrDefault(value => value.ElementId == id);
        var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
        if (source is null || source.LocationId != "c3926" || source.InitialToes is null
            || source.InitialToes.Count != 1 || source.InitialToes[0].ComponentId != id + ".infantry"
            || source.InitialToes[0].Toe != 1 || entry is null
            || entry.SideId != "commonwealth" || entry.LocationId != "c3926"
            || entry.AuthorityStatus != "source-evidence" || actual.CurrentLocationId != "c3926"
            || actual.Components.Count != 1 || actual.Components[0].ComponentId != id + ".infantry"
            || actual.Components[0].CurrentToe != 1)
            throw new InvalidOperationException("Active French company differs from retained source evidence.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == id
            ? value with { AuthorityStatus = "source-cw-priority-active" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield AddCwFreeGuns(CampaignSourceCwFreeGuns setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (cwFreeGunsOriginHash is not null || setup.Placements.Count != 0 || setup.BattlefieldHash != Hash())
            throw new InvalidOperationException("CW free-gun setup is not bound to this battlefield.");
        var added = CampaignSourceCwFreeGuns.Units.Select(unit => new CampaignSourceBattlefieldEntry(
            unit.ElementId, "commonwealth", null, "source-cw-free-gun-unplaced"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending, cairoSetupIncluded,
            cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, setup.BattlefieldHash);
    }

    internal CampaignSourceBattlefield PlaceCwFreeGun(CampaignSourceCwFreeGuns setup,
        string elementId, string locationId)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (cwFreeGunsOriginHash is null || setup.BattlefieldHash != cwFreeGunsOriginHash
            || !setup.ValidDestinations(elementId, this).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("CW free-gun placement is unavailable.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == elementId
            ? value with { LocationId = locationId, AuthorityStatus = "source-cw-free-gun-placed" } : value),
            pending, cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteCwFreeGuns(CampaignSourceCwFreeGuns setup,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        if (cwFreeGunsOriginHash is null || setup.BattlefieldHash != cwFreeGunsOriginHash)
            throw new InvalidOperationException("CW free-gun source setup is not retained.");
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length == 0
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals))
            throw new InvalidOperationException("Active CW guns must preserve their retained predecessor.");
        foreach (var id in added)
        {
            var unit = CampaignSourceCwFreeGuns.Units.SingleOrDefault(value => value.ElementId == id);
            var placement = setup.Placements.SingleOrDefault(value => value.ElementId == id);
            var entry = entries.SingleOrDefault(value => value.ElementId == id);
            var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
            if (unit is null || placement is null || entry is null || entry.SideId != "commonwealth"
                || entry.AuthorityStatus != "source-cw-free-gun-placed" || entry.LocationId != placement.LocationId
                || actual.CurrentLocationId != placement.LocationId || actual.Components.Count != 1
                || actual.Components[0].ComponentId != id + "." + unit.ComponentSuffix
                || actual.Components[0].CurrentToe != unit.Toe)
                throw new InvalidOperationException("Active CW gun differs from its source placement and TOE.");
        }
        return new CampaignSourceBattlefield(entries.Select(value => added.Contains(value.ElementId, StringComparer.Ordinal)
            ? value with { AuthorityStatus = "source-cw-free-gun-active" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteFourthIndian(CampaignGroupedWorld world)
    {
        RequireFourthIndian(world, "source-evidence");
        if (world.Groups.Single(value => value.GroupId == "4-indian-division").DispositionConfirmed)
            throw new InvalidOperationException("Fourth Indian disposition must be proposed before confirmation.");
        var ids = CampaignSourceFourthIndianDefinition.Members.Select(value => value.Id)
            .ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-fourth-indian-proposed" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmFourthIndian(CampaignGroupedWorld prior, CampaignGroupedWorld confirmed)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(confirmed);
        RequireFourthIndian(prior, "source-fourth-indian-proposed");
        var before = prior.Groups.Single(value => value.GroupId == "4-indian-division");
        var after = confirmed.Groups.Single(value => value.GroupId == "4-indian-division");
        if (before.DispositionConfirmed || !after.DispositionConfirmed || prior.Prior != confirmed.Prior
            || prior.ContentHash != confirmed.ContentHash || prior.Groups.Count != 1 || confirmed.Groups.Count != 1
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.StackValue is not null || after.StackValue != 5 || before.MotorPool != after.MotorPool
            || !before.Members.SequenceEqual(after.Members))
            throw new InvalidOperationException("Fourth Indian confirmation changed its proposed world.");
        var ids = CampaignSourceFourthIndianDefinition.Members.Select(value => value.Id)
            .ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-fourth-indian-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateFourthIndian(CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var group = world.Groups.Single(value => value.GroupId == "4-indian-division");
        RequireFourthIndian(world, group.DispositionConfirmed
            ? "source-fourth-indian-grouped" : "source-fourth-indian-proposed");
    }

    private void RequireFourthIndian(CampaignGroupedWorld world, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var expected = CampaignSourceFourthIndianDefinition.Members.Select(value => value.Id)
            .Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == "4-indian-division");
        var occupants = entries.Where(value => value.LocationId == "d3615").ToArray();
        if (group is null || group.SideId != "commonwealth" || group.LocationId != "d3615"
            || group.RootId != "4-indian-division-hq"
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(expected)
            || occupants.Length != expected.Length
            || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(expected)
            || occupants.Any(value => value.SideId != "commonwealth" || value.AuthorityStatus != status))
            throw new InvalidOperationException("Fourth Indian group differs from retained battlefield occupancy.");
    }

    internal CampaignSourceBattlefield ProposeLibyanGroups(CampaignGroupedWorld world)
    {
        RequireLibyanGroup(world, "1-libyan-division", "source-evidence");
        RequireLibyanGroup(world, "2-libyan-division", "source-evidence");
        if (world.Groups.Where(value => value.GroupId is "1-libyan-division" or "2-libyan-division")
            .Any(value => value.DispositionConfirmed))
            throw new InvalidOperationException("Libyan dispositions must be proposed before owner confirmation.");
        var ids = Cna1979GrazianiSourceFacts.FirstLibyanDivisionChartCandidate.ElementIds
            .Concat(Cna1979GrazianiSourceFacts.SecondLibyanDivisionChartCandidate.ElementIds)
            .ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-libyan-proposed" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmLibyanGroup(CampaignGroupedWorld priorWorld,
        CampaignGroupedWorld nextWorld, string groupId)
    {
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(nextWorld);
        RequireLibyanGroup(priorWorld, groupId, "source-libyan-proposed");
        var before = priorWorld.Groups.Single(value => value.GroupId == groupId);
        var after = nextWorld.Groups.Single(value => value.GroupId == groupId);
        if (before.DispositionConfirmed || !after.DispositionConfirmed || before.StackValue is not null || after.StackValue != 5
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.MotorPool != after.MotorPool || !SameGroupedMembers(before.Members, after.Members)
            || !priorWorld.Groups.Where(value => value.GroupId != groupId).SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != groupId))
            || priorWorld.Detached.Count != nextWorld.Detached.Count
            || priorWorld.Detached.Zip(nextWorld.Detached).Any(pair => pair.First.GroupId != pair.Second.GroupId
                || pair.First.SideId != pair.Second.SideId || pair.First.LocationId != pair.Second.LocationId
                || pair.First.StackValue != pair.Second.StackValue || !SameGroupedMember(pair.First.Member, pair.Second.Member))
            || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations))
            throw new InvalidOperationException("Libyan confirmation changed its proposed world.");
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-libyan-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    private static bool SameGroupedMembers(IReadOnlyList<CampaignGroupedMember> first, IReadOnlyList<CampaignGroupedMember> second) =>
        first.Count == second.Count && first.Zip(second).All(pair => SameGroupedMember(pair.First, pair.Second));

    private static bool SameGroupedMember(CampaignGroupedMember first, CampaignGroupedMember second) =>
        first with { AdditionalComponents = null } == second with { AdditionalComponents = null }
        && (first.AdditionalComponents is null) == (second.AdditionalComponents is null)
        && (first.AdditionalComponents ?? []).SequenceEqual(second.AdditionalComponents ?? []);

    internal void ValidateLibyanGroups(CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        foreach (var id in new[] { "1-libyan-division", "2-libyan-division" })
        {
            var group = world.Groups.Single(value => value.GroupId == id);
            RequireLibyanGroup(world, id, group.DispositionConfirmed
                ? "source-libyan-grouped" : "source-libyan-proposed");
        }
    }

    private void RequireLibyanGroup(CampaignGroupedWorld world, string groupId, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var (source, root) = groupId switch
        {
            "1-libyan-division" => (Cna1979GrazianiSourceFacts.FirstLibyanDivisionChartCandidate, "1-libyan-division-hq"),
            "2-libyan-division" => (Cna1979GrazianiSourceFacts.SecondLibyanDivisionChartCandidate, "2-libyan-division-hq"),
            _ => throw new InvalidOperationException("Unknown Libyan source group."),
        };
        var ids = source.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == groupId);
        var occupants = entries.Where(value => value.LocationId == source.LocationId).ToArray();
        var effective = world.OccupantsAt(source.LocationId);
        if (group is null || group.SideId != "axis" || group.RootId != root || group.LocationId != source.LocationId
            || group.MotorPool != 15 || group.StackValue != (group.DispositionConfirmed ? 5 : null)
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Length != ids.Length
            || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != "axis" || value.AuthorityStatus != status)
            || effective.Count != 1 || effective[0].Formation?.GroupId != groupId)
            throw new InvalidOperationException("Libyan grouped world differs from retained battlefield occupancy.");
    }

    internal CampaignSourceBattlefield ProposeCatanzaro(CampaignGroupedWorld world)
    {
        RequireCatanzaro(world, "source-evidence");
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        if (group.DispositionConfirmed) throw new InvalidOperationException("Catanzaro must first be proposed.");
        var ids = group.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-catanzaro-proposed" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmCatanzaro(CampaignGroupedWorld priorWorld, CampaignGroupedWorld nextWorld)
    {
        RequireCatanzaro(priorWorld, "source-catanzaro-proposed");
        var before = priorWorld.Groups.Single(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        var after = nextWorld.Groups.Single(value => value.GroupId == before.GroupId);
        if (before.DispositionConfirmed || !after.DispositionConfirmed || before.StackValue is not null || after.StackValue != 5
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.MotorPool != after.MotorPool || !SameGroupedMembers(before.Members, after.Members)
            || !priorWorld.Groups.Where(value => value.GroupId != before.GroupId).SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != before.GroupId))
            || !priorWorld.Detached.SequenceEqual(nextWorld.Detached) || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations))
            throw new InvalidOperationException("Catanzaro confirmation changed its proposed world.");
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-catanzaro-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateCatanzaro(CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        RequireCatanzaro(world, group.DispositionConfirmed ? "source-catanzaro-grouped" : "source-catanzaro-proposed");
    }

    private void RequireCatanzaro(CampaignGroupedWorld world, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var source = Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate;
        var ids = source.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        var occupants = entries.Where(value => value.LocationId == source.LocationId).ToArray();
        var effective = world.OccupantsAt(source.LocationId);
        if (group is null || group.SideId != "axis" || group.RootId != CampaignSourceCatanzaroGroupedDefinition.RootId
            || group.LocationId != source.LocationId || group.MotorPool != 35 || group.StackValue != (group.DispositionConfirmed ? 5 : null)
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Length != ids.Length || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != "axis" || value.AuthorityStatus != status)
            || effective.Count != 1 || effective[0].Formation?.GroupId != group.GroupId)
            throw new InvalidOperationException("Catanzaro grouped world differs from retained battlefield occupancy.");
    }

    internal CampaignSourceBattlefield ProposeSeventhArmored(CampaignGroupedWorld world)
    {
        RequireSeventhArmored(world, "source-evidence");
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        if (group.DispositionConfirmed) throw new InvalidOperationException("Seventh Armored must first be proposed.");
        var ids = group.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-seventh-armored-proposed" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmSeventhArmored(CampaignGroupedWorld priorWorld, CampaignGroupedWorld nextWorld)
    {
        RequireSeventhArmored(priorWorld, "source-seventh-armored-proposed");
        var before = priorWorld.Groups.Single(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        var after = nextWorld.Groups.Single(value => value.GroupId == before.GroupId);
        if (before.DispositionConfirmed || !after.DispositionConfirmed || before.StackValue is not null || after.StackValue != 5
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.MotorPool != after.MotorPool || !SameGroupedMembers(before.Members, after.Members)
            || !priorWorld.Groups.Where(value => value.GroupId != before.GroupId).SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != before.GroupId))
            || !priorWorld.Detached.SequenceEqual(nextWorld.Detached) || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations))
            throw new InvalidOperationException("Seventh Armored confirmation changed its proposed world.");
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-seventh-armored-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateSeventhArmored(CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        RequireSeventhArmored(world, group.DispositionConfirmed ? "source-seventh-armored-grouped" : "source-seventh-armored-proposed");
    }

    private void RequireSeventhArmored(CampaignGroupedWorld world, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var source = Cna1979GrazianiSourceFacts.SeventhArmoredDivisionInitialCandidate;
        var ids = source.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        var occupants = entries.Where(value => value.LocationId == source.LocationId).ToArray();
        var effective = world.OccupantsAt(source.LocationId);
        if (group is null || group.SideId != "commonwealth" || group.RootId != CampaignSourceSeventhArmoredGroupedDefinition.RootId
            || group.LocationId != source.LocationId || group.MotorPool != 10
            || (group.DispositionConfirmed ? group.StackValue is not (3 or 5) : group.StackValue is not null)
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Length != ids.Length || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != "commonwealth" || value.AuthorityStatus != status)
            || effective.Count != 1 || effective[0].Formation?.GroupId != group.GroupId)
            throw new InvalidOperationException("Seventh Armored grouped world differs from retained battlefield occupancy.");
    }

    internal CampaignSourceBattlefield ProposeSixteenthBrigade(CampaignGroupedWorld world)
    {
        RequireSixteenthBrigade(world, "world8-initial");
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        if (group.DispositionConfirmed) throw new InvalidOperationException("Sixteenth Brigade must first be proposed.");
        var ids = group.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-sixteenth-brigade-proposed" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmSixteenthBrigade(CampaignGroupedWorld priorWorld, CampaignGroupedWorld nextWorld)
    {
        RequireSixteenthBrigade(priorWorld, "source-sixteenth-brigade-proposed");
        var before = priorWorld.Groups.Single(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        var after = nextWorld.Groups.Single(value => value.GroupId == before.GroupId);
        if (before.DispositionConfirmed || !after.DispositionConfirmed || before.StackValue is not null || after.StackValue != 2
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.MotorPool != after.MotorPool || !SameGroupedMembers(before.Members, after.Members)
            || !priorWorld.Groups.Where(value => value.GroupId != before.GroupId).SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != before.GroupId))
            || !priorWorld.Detached.SequenceEqual(nextWorld.Detached) || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations))
            throw new InvalidOperationException("Sixteenth Brigade confirmation changed its proposed world.");
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-sixteenth-brigade-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateSixteenthBrigade(CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        RequireSixteenthBrigade(world, group.DispositionConfirmed ? "source-sixteenth-brigade-grouped" : "source-sixteenth-brigade-proposed");
    }

    private void RequireSixteenthBrigade(CampaignGroupedWorld world, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var source = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate;
        var ids = source.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        var occupants = entries.Where(value => ids.Contains(value.ElementId, StringComparer.Ordinal)).ToArray();
        var effective = world.OccupantsAt(source.LocationId);
        if (group is null || group.SideId != "commonwealth" || group.RootId != CampaignSourceSixteenthBrigadeGroupedDefinition.RootId
            || group.LocationId != source.LocationId || group.MotorPool != 20 || group.StackValue != (group.DispositionConfirmed ? 2 : null)
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Length != ids.Length || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != "commonwealth" || value.LocationId != source.LocationId || value.AuthorityStatus != status)
            || effective.Count(value => value.Formation?.GroupId == group.GroupId) != 1
            || effective.Any(value => value.Independent?.BoundElementIds.Any(id => ids.Contains(id, StringComparer.Ordinal)) == true))
            throw new InvalidOperationException("Sixteenth Brigade grouped world differs from retained battlefield occupancy.");
    }

    internal CampaignSourceBattlefield ProposeItalianDivisions(CampaignGroupedWorld world)
    {
        RequireItalianDivision(world, "cirene-division", "source-evidence");
        RequireItalianDivision(world, "marmarica-division", "source-evidence");
        if (world.Groups.Where(value => value.GroupId is "cirene-division" or "marmarica-division")
            .Any(value => value.DispositionConfirmed))
            throw new InvalidOperationException("Italian dispositions must be proposed before owner confirmation.");
        var ids = Cna1979GrazianiSourceFacts.CireneDivisionChartCandidate.ElementIds
            .Concat(Cna1979GrazianiSourceFacts.MarmaricaDivisionChartCandidate.ElementIds)
            .ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-italian-division-proposed" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmItalianDivision(CampaignGroupedWorld priorWorld,
        CampaignGroupedWorld nextWorld, string groupId)
    {
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(nextWorld);
        RequireItalianDivision(priorWorld, groupId, "source-italian-division-proposed");
        var before = priorWorld.Groups.Single(value => value.GroupId == groupId);
        var after = nextWorld.Groups.Single(value => value.GroupId == groupId);
        if (before.DispositionConfirmed || !after.DispositionConfirmed || before.StackValue is not null || after.StackValue != 5
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.MotorPool != after.MotorPool || !SameGroupedMembers(before.Members, after.Members)
            || !priorWorld.Groups.Where(value => value.GroupId != groupId).SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != groupId))
            || priorWorld.Detached.Count != nextWorld.Detached.Count
            || priorWorld.Detached.Zip(nextWorld.Detached).Any(pair => pair.First.GroupId != pair.Second.GroupId
                || pair.First.SideId != pair.Second.SideId || pair.First.LocationId != pair.Second.LocationId
                || pair.First.StackValue != pair.Second.StackValue || !SameGroupedMember(pair.First.Member, pair.Second.Member))
            || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations))
            throw new InvalidOperationException("Italian division confirmation changed its proposed world.");
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-italian-division-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateItalianDivisions(CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        foreach (var id in new[] { "cirene-division", "marmarica-division" })
        {
            var group = world.Groups.Single(value => value.GroupId == id);
            RequireItalianDivision(world, id, group.DispositionConfirmed
                ? "source-italian-division-grouped" : "source-italian-division-proposed");
        }
    }

    private void RequireItalianDivision(CampaignGroupedWorld world, string groupId, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var (source, root, motorPool) = groupId switch
        {
            "cirene-division" => (Cna1979GrazianiSourceFacts.CireneDivisionChartCandidate, "63-cirene-hq", 50),
            "marmarica-division" => (Cna1979GrazianiSourceFacts.MarmaricaDivisionChartCandidate, "62-marmarica-hq", 45),
            _ => throw new InvalidOperationException("Unknown Italian source division."),
        };
        var ids = source.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == groupId);
        var occupants = entries.Where(value => value.LocationId == source.LocationId).ToArray();
        var effective = world.OccupantsAt(source.LocationId);
        if (group is null || group.SideId != "axis" || group.RootId != root || group.LocationId != source.LocationId
            || group.MotorPool != motorPool || group.StackValue != (group.DispositionConfirmed ? 5 : null)
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Length != ids.Length
            || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != "axis" || value.AuthorityStatus != status)
            || effective.Count != 1 || effective[0].Formation?.GroupId != groupId)
            throw new InvalidOperationException("Italian division differs from retained battlefield occupancy.");
    }

    internal CampaignSourceBattlefield AddAxisFreeArtillery(CampaignSourceAxisFreeArtillery setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (axisFreeArtilleryOriginHash is not null || setup.Placements.Count != 0 || setup.BattlefieldHash != Hash())
            throw new InvalidOperationException("Axis free artillery setup is not bound to this battlefield.");
        var added = CampaignSourceAxisFreeArtillery.Units.Select(unit => new CampaignSourceBattlefieldEntry(
            unit.ElementId, "axis", null, "source-1ar-unplaced"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending, cairoSetupIncluded,
            cairoSetupOriginHash, setup.BattlefieldHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PlaceAxisFreeArtillery(CampaignSourceAxisFreeArtillery setup,
        string elementId, string locationId)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (axisFreeArtilleryOriginHash is null || setup.BattlefieldHash != axisFreeArtilleryOriginHash
            || !setup.ValidDestinations(elementId, this).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Axis free artillery destination is unavailable.");
        var ids = CampaignSourceAxisFreeArtillery.Units.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var recorded = entries.Where(value => ids.Contains(value.ElementId)
                && value.AuthorityStatus is ("source-1ar-placed" or "source-1ar-active"))
            .Select(value => (value.ElementId, value.LocationId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        var choices = setup.Placements.Select(value => (value.ElementId, LocationId: (string?)value.LocationId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (!recorded.SequenceEqual(choices))
            throw new InvalidOperationException("Axis free artillery choices differ from the retained battlefield.");
        var unit = entries.SingleOrDefault(value => value.ElementId == elementId);
        if (unit is null || unit.AuthorityStatus != "source-1ar-unplaced" || unit.LocationId is not null)
            throw new InvalidOperationException("Axis free artillery is not awaiting placement.");
        var occupants = entries.Where(value => value.LocationId == locationId).ToArray();
        if (occupants.Any(value => !ids.Contains(value.ElementId)
                || value.AuthorityStatus is not ("source-1ar-placed" or "source-1ar-active")) || occupants.Length >= 2)
            throw new InvalidOperationException("Axis free artillery destination has unknown or full occupancy.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == elementId
            ? value with { LocationId = locationId, AuthorityStatus = "source-1ar-placed" } : value),
            pending, cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteAxisFreeArtillery(CampaignSourceAxisFreeArtillery setup,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        if (axisFreeArtilleryOriginHash is null || setup.BattlefieldHash != axisFreeArtilleryOriginHash)
            throw new InvalidOperationException("Free artillery setup is not bound to this battlefield.");
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length == 0
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals))
            throw new InvalidOperationException("Active artillery must preserve its retained predecessor.");
        foreach (var id in added)
        {
            var entry = entries.SingleOrDefault(value => value.ElementId == id);
            var source = CampaignSourceAxisFreeArtillery.Units.SingleOrDefault(value => value.ElementId == id);
            var placement = setup.Placements.SingleOrDefault(value => value.ElementId == id);
            var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
            if (entry is null || source is null || placement is null || entry.SideId != "axis"
                || entry.AuthorityStatus != "source-1ar-placed" || entry.LocationId != placement.LocationId
                || actual.CurrentLocationId != placement.LocationId || actual.Components.Count != 1
                || actual.Components[0].ComponentId != id + "." + source.ComponentSuffix
                || actual.Components[0].CurrentToe != source.Toe)
                throw new InvalidOperationException("Active artillery differs from its source placement and TOE.");
        }
        return new CampaignSourceBattlefield(entries.Select(value => added.Contains(value.ElementId, StringComparer.Ordinal)
            ? value with { AuthorityStatus = "source-1ar-active" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield AddAxisCorpsArtillery(CampaignSourceAxisCorpsArtillery setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (axisCorpsArtilleryOriginHash is not null || setup.Placements.Count != 0 || setup.BattlefieldHash != Hash())
            throw new InvalidOperationException("Axis corps artillery setup is not bound to this battlefield.");
        var added = CampaignSourceAxisCorpsArtillery.Units.Select(unit => new CampaignSourceBattlefieldEntry(
            unit.ElementId, "axis", null, "source-corps-artillery-unplaced"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending, cairoSetupIncluded,
            cairoSetupOriginHash, axisFreeArtilleryOriginHash, setup.BattlefieldHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PlaceAxisCorpsArtillery(CampaignSourceAxisCorpsArtillery setup,
        string elementId, string locationId)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (axisCorpsArtilleryOriginHash is null || setup.BattlefieldHash != axisCorpsArtilleryOriginHash
            || !setup.ValidDestinations(elementId, this).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Axis corps artillery destination is unavailable.");
        var ids = CampaignSourceAxisCorpsArtillery.Units.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var recorded = entries.Where(value => ids.Contains(value.ElementId)
                && value.AuthorityStatus is ("source-corps-artillery-placed" or "source-corps-artillery-active"))
            .Select(value => (value.ElementId, value.LocationId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        var choices = setup.Placements.Select(value => (value.ElementId, LocationId: (string?)value.LocationId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (!recorded.SequenceEqual(choices))
            throw new InvalidOperationException("Axis corps artillery choices differ from the retained battlefield.");
        var unit = entries.SingleOrDefault(value => value.ElementId == elementId);
        if (unit is null || unit.AuthorityStatus != "source-corps-artillery-unplaced" || unit.LocationId is not null)
            throw new InvalidOperationException("Axis corps artillery is not awaiting placement.");
        if (entries.Any(value => value.LocationId == locationId))
            throw new InvalidOperationException("Axis corps artillery destination has unknown or reserved occupancy.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == elementId
            ? value with { LocationId = locationId, AuthorityStatus = "source-corps-artillery-placed" } : value),
            pending, cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteAxisCorpsArtillery(CampaignSourceAxisCorpsArtillery setup,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        if (axisCorpsArtilleryOriginHash is null || setup.BattlefieldHash != axisCorpsArtilleryOriginHash)
            throw new InvalidOperationException("Corps artillery setup is not bound to this battlefield.");
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length == 0
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals))
            throw new InvalidOperationException("Active corps artillery must preserve its retained predecessor.");
        foreach (var id in added)
        {
            var entry = entries.SingleOrDefault(value => value.ElementId == id);
            var source = CampaignSourceAxisCorpsArtillery.Units.SingleOrDefault(value => value.ElementId == id);
            var placement = setup.Placements.SingleOrDefault(value => value.ElementId == id);
            var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
            if (entry is null || source is null || placement is null || entry.SideId != "axis"
                || entry.AuthorityStatus != "source-corps-artillery-placed" || entry.LocationId != placement.LocationId
                || actual.CurrentLocationId != placement.LocationId || actual.Components.Count != source.Components.Count
                || actual.Components.Any(component => !source.Components.Any(expected =>
                    component.ComponentId == id + "." + expected.Suffix && component.CurrentToe == expected.Toe)))
                throw new InvalidOperationException("Active corps artillery differs from its source placement and TOE.");
        }
        return new CampaignSourceBattlefield(entries.Select(value => added.Contains(value.ElementId, StringComparer.Ordinal)
            ? value with { AuthorityStatus = "source-corps-artillery-active" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield AddMatruhFormation(CampaignSourceMatruhFormation setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (matruhOriginHash is not null || setup.Confirmed || setup.DetachedId is not null || setup.BattlefieldHash != Hash())
            throw new InvalidOperationException("Matruh source formation is not bound to this battlefield.");
        var added = CampaignSourceMatruhFormation.UnitIds.Select(id => new CampaignSourceBattlefieldEntry(
            id, "commonwealth", "d3714", "source-matruh-unconfirmed"));
        return new CampaignSourceBattlefield(entries.Concat(added), pending, cairoSetupIncluded,
            cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, setup.BattlefieldHash, cwFreeGunsOriginHash);
    }

    internal void ValidateGroupedMatruh(CampaignGroupedWorld world, CampaignSourceMatruhFormation setup)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(setup);
        if (!setup.Confirmed || matruhOriginHash is null || setup.BattlefieldHash != matruhOriginHash)
            throw new InvalidOperationException("A confirmed Matruh source formation is required.");
        var group = world.Groups.SingleOrDefault(value => value.GroupId == "matruh-garrison");
        var expected = setup.AttachedIds.Append(CampaignSourceMatruhFormation.HeadquartersId)
            .Order(StringComparer.Ordinal).ToArray();
        var occupants = entries.Where(value => value.LocationId == "d3714").ToArray();
        if (group is null || group.SideId != "commonwealth" || group.LocationId != "d3714"
            || group.RootId != CampaignSourceMatruhFormation.HeadquartersId || group.StackValue != 0
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(expected)
            || group.Members.Any(value => value.ElementId == group.RootId
                ? value.ParentId is not null : value.ParentId != group.RootId)
            || occupants.Length != CampaignSourceMatruhFormation.UnitIds.Count
            || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal)
                .SequenceEqual(CampaignSourceMatruhFormation.UnitIds)
            || occupants.Any(value => value.SideId != "commonwealth"
                || (value.ElementId != setup.DetachedId && value.AuthorityStatus != "source-matruh-grouped"))
            || world.Detached.Any(value => value.LocationId == "d3714" && value.Member.ElementId != setup.DetachedId)
            || world.Prior.World.Representations.Any(value => value.CurrentLocationId == "d3714"
                && (setup.DetachedId is null || value.BoundElementIds.Count != 1
                    || value.BoundElementIds[0] != setup.DetachedId)))
            throw new InvalidOperationException("Matruh grouped world differs from retained battlefield occupancy.");

        if (setup.DetachedId is not { } id) return;
        var entry = occupants.Single(value => value.ElementId == id);
        var detached = world.Detached.Where(value => value.Member.ElementId == id).ToArray();
        var active = world.Prior.World.Elements.Where(value => value.ElementId == id).ToArray();
        if (detached.Length + active.Length != 1
            || (detached.Length == 1 && (entry.AuthorityStatus != "source-matruh-detached"
                || detached[0].GroupId != "matruh-garrison" || detached[0].SideId != "commonwealth"
                || detached[0].LocationId != "d3714" || detached[0].StackValue != 1))
            || (active.Length == 1 && (entry.AuthorityStatus != "source-matruh-active"
                || active[0].CurrentLocationId != "d3714"
                || world.Prior.World.Representations.Count(value => value.CurrentLocationId == "d3714"
                    && value.BoundElementIds.Count == 1 && value.BoundElementIds[0] == id) != 1)))
            throw new InvalidOperationException("Matruh detached unit must be represented exactly once.");
    }

    internal CampaignSourceBattlefield ConfirmMatruhFormation(CampaignSourceMatruhFormation setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (matruhOriginHash is null || setup.BattlefieldHash != matruhOriginHash || setup.Confirmed
            || setup.DetachedId is not null || CampaignSourceMatruhFormation.UnitIds.Any(id =>
                !entries.Any(value => value.ElementId == id && value.SideId == "commonwealth"
                    && value.LocationId == "d3714" && value.AuthorityStatus == "source-matruh-unconfirmed")))
            throw new InvalidOperationException("Matruh source formation cannot be confirmed.");
        var ids = CampaignSourceMatruhFormation.UnitIds.ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = "source-matruh-grouped" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield DetachMatruhUnit(CampaignSourceMatruhFormation setup, string elementId)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (matruhOriginHash is null || setup.BattlefieldHash != matruhOriginHash || !setup.Confirmed
            || setup.DetachedId is not null || elementId == CampaignSourceMatruhFormation.HeadquartersId
            || !CampaignSourceMatruhFormation.UnitIds.Contains(elementId, StringComparer.Ordinal))
            throw new InvalidOperationException("Matruh source detachment is unavailable.");
        var entry = entries.SingleOrDefault(value => value.ElementId == elementId);
        if (entry is null || entry.AuthorityStatus != "source-matruh-grouped" || entry.LocationId != "d3714")
            throw new InvalidOperationException("Matruh unit differs from the retained grouped formation.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == elementId
            ? value with { AuthorityStatus = "source-matruh-detached" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield PromoteMatruhDetached(CampaignSourceMatruhFormation setup,
        CampaignWorldSnapshotV8 priorWorld, CampaignWorldSnapshotV8 activeWorld)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(activeWorld);
        if (matruhOriginHash is null || setup.BattlefieldHash != matruhOriginHash || !setup.Confirmed
            || setup.DetachedId is not { } id)
            throw new InvalidOperationException("A retained Matruh detachment is required.");
        var previous = priorWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var current = activeWorld.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var added = current.Except(previous, StringComparer.Ordinal).ToArray();
        if (!previous.IsSubsetOf(current) || added.Length != 1 || added[0] != id
            || !priorWorld.World.Elements.All(value => activeWorld.World.Elements.Contains(value))
            || !priorWorld.World.Representations.All(value => activeWorld.World.Representations.Contains(value))
            || !priorWorld.World.BrokenVehicleLots.SequenceEqual(activeWorld.World.BrokenVehicleLots)
            || !priorWorld.PendingArrivals.SequenceEqual(activeWorld.PendingArrivals))
            throw new InvalidOperationException("Active Matruh unit must preserve its retained predecessor.");
        var entry = entries.SingleOrDefault(value => value.ElementId == id);
        var actual = activeWorld.World.Elements.Single(value => value.ElementId == id);
        if (entry is null || entry.SideId != "commonwealth" || entry.LocationId != "d3714"
            || entry.AuthorityStatus != "source-matruh-detached" || actual.CurrentLocationId != "d3714"
            || actual.Components.Count != 1 || actual.Components[0].ComponentId != id + ".infantry"
            || actual.Components[0].CurrentToe != 6)
            throw new InvalidOperationException("Active Matruh unit differs from its source detachment and TOE.");
        return new CampaignSourceBattlefield(entries.Select(value => value.ElementId == id
            ? value with { AuthorityStatus = "source-matruh-active" } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash, axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal static CampaignSourceBattlefield Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var axisSupplement = state.AxisSupplement ?? throw new InvalidOperationException("Axis supplement is missing.");
        var cwSupplement = state.CommonwealthSupplement ?? throw new InvalidOperationException("Commonwealth supplement is missing.");
        var garrisons = state.AxisGarrisons ?? throw new InvalidOperationException("Axis garrisons are missing.");
        // Version 10 identifies the retained pregame base. Later source world
        // successors may add real units without reinterpreting this index.
        var world = state.Pregame.Source.Session.InitialWorld;
        var placed = world.World.Elements.Select(value => new CampaignSourceBattlefieldEntry(
            value.ElementId, "commonwealth", value.CurrentLocationId, "world8-initial"));
        static IEnumerable<CampaignSourceBattlefieldEntry> Source(IEnumerable<CampaignSourceInitialForce> forces,
            string side) => forces.Select(value => new CampaignSourceBattlefieldEntry(
                value.ElementId, side, value.LocationId, "source-evidence"));
        var result = new CampaignSourceBattlefield(placed
                .Concat(Source(state.AxisInitialForces.Forces, "axis"))
                .Concat(Source(axisSupplement.Forces, "axis"))
                .Concat(Source(garrisons.Forces, "axis"))
                .Concat(Source(state.CommonwealthInitialForces.Forces, "commonwealth"))
                .Concat(Source(cwSupplement.Forces, "commonwealth")),
            world.PendingArrivals.Select(value => new CampaignSourcePendingGroup(value.GroupId,
                value.SideId, value.Arrival.GameTurn, value.Arrival.OperationStage, value.EntryAreaId,
                Array.AsReadOnly(value.ElementIds.Order(StringComparer.Ordinal).ToArray()))));
        if (result.Hash() != InitialHash)
            throw new InvalidOperationException("Source battlefield differs from the pinned version 10 identity.");
        return result;
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteBoolean("battlefieldComplete", false);
            writer.WriteBoolean("controlCertified", false);
            if (cairoSetupIncluded)
            {
                writer.WriteBoolean("cairoSetupIncluded", true);
                writer.WriteString("cairoSetupOriginHash", cairoSetupOriginHash);
            }
            if (axisFreeArtilleryOriginHash is not null)
                writer.WriteString("axisFreeArtilleryOriginHash", axisFreeArtilleryOriginHash);
            if (axisCorpsArtilleryOriginHash is not null)
                writer.WriteString("axisCorpsArtilleryOriginHash", axisCorpsArtilleryOriginHash);
            if (matruhOriginHash is not null)
                writer.WriteString("matruhOriginHash", matruhOriginHash);
            if (cwFreeGunsOriginHash is not null)
                writer.WriteString("cwFreeGunsOriginHash", cwFreeGunsOriginHash);
            writer.WriteStartArray("entries");
            foreach (var entry in entries)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", entry.ElementId);
                writer.WriteString("sideId", entry.SideId);
                writer.WriteString("locationId", entry.LocationId);
                writer.WriteString("authorityStatus", entry.AuthorityStatus);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("pendingGroups");
            foreach (var group in pending)
            {
                writer.WriteStartObject();
                writer.WriteString("groupId", group.GroupId);
                writer.WriteString("sideId", group.SideId);
                writer.WriteNumber("gameTurn", group.GameTurn);
                writer.WriteNumber("operationStage", group.OperationStage);
                writer.WriteString("entryAreaId", group.EntryAreaId);
                writer.WriteStartArray("elementIds");
                foreach (var id in group.ElementIds) writer.WriteStringValue(id);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceBattlefieldEntry(string ElementId, string SideId,
    string? LocationId, string AuthorityStatus);

internal sealed record CampaignSourcePendingGroup(string GroupId, string SideId, int GameTurn,
    int OperationStage, string? EntryAreaId, IReadOnlyList<string> ElementIds);
