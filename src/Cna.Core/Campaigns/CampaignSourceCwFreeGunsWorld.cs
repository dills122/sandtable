using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Activates owner-placed CW source gun counters in the retained World8.</summary>
internal sealed class CampaignSourceCwFreeGunsWorld
{
    internal const string PolicyId = "graziani.cw-free-guns-active-world.v1";
    private readonly Lazy<string> cachedHash;
    private CampaignSourceCwFreeGunsWorld(ContentSourceCwFreeGunsPackArtifact content, CampaignWorldSnapshotV8 world,
        IEnumerable<string> added, string predecessorHash, string setupHash,
        ContentSourceAxisMinorGarrisonsPackArtifact? minorGarrisonContent = null,
        ContentSourceAxisGarrisonRemainderPackArtifact? garrisonRemainderContent = null,
        ContentSourceGiarabubMotorMachineGunArtifact? motorMachineGunContent = null,
        ContentSourceCwFrontlineArtifact? cwFrontlineContent = null,
        ContentSourceTobrukBardiaGarrisonPackArtifact? tobrukBardiaGarrisonContent = null,
        ContentSourceBardiaGarrisonRemainderPackArtifact? bardiaGarrisonRemainderContent = null,
        ContentSourceTripoliTankRegimentPackArtifact? tripoliTankRegimentContent = null,
        ContentSourceCwAttachedArtilleryPackArtifact? cwAttachedArtilleryContent = null)
    {
        Content = content; World = world; PredecessorHash = predecessorHash; SetupHash = setupHash;
        cachedHash = new Lazy<string>(ComputeHash, LazyThreadSafetyMode.ExecutionAndPublication);
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
        MinorGarrisonContent = minorGarrisonContent;
        GarrisonRemainderContent = garrisonRemainderContent;
        MotorMachineGunContent = motorMachineGunContent;
        CwFrontlineContent = cwFrontlineContent;
        TobrukBardiaGarrisonContent = tobrukBardiaGarrisonContent;
        BardiaGarrisonRemainderContent = bardiaGarrisonRemainderContent;
        TripoliTankRegimentContent = tripoliTankRegimentContent;
        CwAttachedArtilleryContent = cwAttachedArtilleryContent;
    }
    internal ContentSourceCwFreeGunsPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal string PredecessorHash { get; }
    internal string SetupHash { get; }
    internal ContentSourceAxisMinorGarrisonsPackArtifact? MinorGarrisonContent { get; }
    internal ContentSourceAxisGarrisonRemainderPackArtifact? GarrisonRemainderContent { get; }
    internal ContentSourceGiarabubMotorMachineGunArtifact? MotorMachineGunContent { get; }
    internal ContentSourceCwFrontlineArtifact? CwFrontlineContent { get; }
    internal ContentSourceTobrukBardiaGarrisonPackArtifact? TobrukBardiaGarrisonContent { get; }
    internal ContentSourceBardiaGarrisonRemainderPackArtifact? BardiaGarrisonRemainderContent { get; }
    internal ContentSourceTripoliTankRegimentPackArtifact? TripoliTankRegimentContent { get; }
    internal ContentSourceCwAttachedArtilleryPackArtifact? CwAttachedArtilleryContent { get; }

    internal CampaignSourceCwFreeGunsWorld ActivateCwFrontline(CampaignSourceCommonwealthSupplementalForces supplement,
        CampaignSourceBattlefield battlefield, CampaignSourceMotorizationWorld motorization)
    {
        if (MotorMachineGunContent is null || CwFrontlineContent is not null
            || World.ContentIdentity != MotorMachineGunContent.Identity
            || supplement.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash)
            throw new InvalidOperationException("CW frontline requires retained source predecessor.");
        var units = ContentSourceCwFrontlineArtifact.Units;
        var ids = units.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        if (World.World.Elements.Any(value => ids.Contains(value.ElementId))
            || World.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("CW frontline overlaps retained world or arrivals.");
        var init = World.Initialization;
        var elements = units.Select(unit =>
        {
            var source = supplement.Forces.Single(value => value.ElementId == unit.Id);
            var entry = battlefield.Entries.Single(value => value.ElementId == unit.Id);
            var component = ContentSourceCwFrontlineArtifact.Facts(unit.Id).Components.Single();
            var pool = motorization.Pools.Single(value => value.Id == unit.PoolId);
            if (source.LocationId != unit.Location || entry.SideId != "commonwealth" || entry.LocationId != unit.Location
                || entry.AuthorityStatus != "source-evidence" || source.InitialToes is not { Count: 1 }
                || source.InitialToes[0].Toe != unit.Toe || source.InitialToes[0].ComponentId != component.ComponentId
                || pool.Points != unit.PoolPoints || motorization.Remaining(unit.PoolId) != unit.PoolPoints
                || init.CapabilityPointsExpended > new CapabilityPointAmount(unit.CapabilityPoints, 1))
                throw new InvalidOperationException("CW frontline differs from retained source evidence.");
            return new CampaignElementStateV5(unit.Id, unit.Location, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(component.ComponentId, unit.Toe, ContentSourceCwFrontlineArtifact.Origin(unit.Id))]);
        }).ToArray();
        var content = ContentSourceCwFrontlineArtifact.Create(MotorMachineGunContent);
        var reps = units.Select(unit => new CampaignMapRepresentationState("cw-frontline." + unit.Id,
            unit.Location, CampaignMapRepresentationBindingKind.IndependentElement, [unit.Id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            World.World.Elements.Concat(elements), World.World.Representations.Concat(reps), World.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, World.ScenarioId, init, world6, World.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, GarrisonRemainderContent, MotorMachineGunContent, content, TobrukBardiaGarrisonContent,
            BardiaGarrisonRemainderContent, TripoliTankRegimentContent, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateGiarabubMotorMachineGun(CampaignSourceAxisGarrisonForces garrisons,
        CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(garrisons); ArgumentNullException.ThrowIfNull(battlefield);
        if (GarrisonRemainderContent is null || MotorMachineGunContent is not null
            || World.ContentIdentity != GarrisonRemainderContent.Identity
            || garrisons.Hash() != CampaignSourceAxisGarrisonForces.ContentHash)
            throw new InvalidOperationException("Motor MG requires its retained garrison predecessor.");
        var id = ContentSourceGiarabubMotorMachineGunArtifact.ElementId;
        var location = ContentSourceGiarabubMotorMachineGunArtifact.LocationId;
        var source = garrisons.Forces.Single(value => value.ElementId == id);
        var entry = battlefield.Entries.Single(value => value.ElementId == id);
        var init = World.Initialization;
        if (source.LocationId != location || entry.SideId != "axis" || entry.LocationId != location
            || entry.AuthorityStatus != "source-evidence" || source.InitialToes is not { Count: 1 }
            || source.InitialToes[0].Toe != 1 || source.InitialToes[0].ComponentId != id + ".infantry"
            || World.World.Elements.Any(value => value.ElementId == id)
            || World.PendingArrivals.Any(value => value.ElementIds.Contains(id, StringComparer.Ordinal))
            || init.CapabilityPointsExpended != CapabilityPointAmount.Zero)
            throw new InvalidOperationException("Motor MG differs from retained source evidence or has unknown prior expenditure.");
        var content = ContentSourceGiarabubMotorMachineGunArtifact.Create(GarrisonRemainderContent);
        if (ContentSourceGiarabubMotorMachineGunArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Motor MG source content failed canonical validation.");
        var element = new CampaignElementStateV5(id, location, init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(id + ".infantry", 1, ContentSourceAxisGarrisonRemainderPackArtifact.Origin(id))]);
        var rep = new CampaignMapRepresentationState("axis-garrison." + id, location,
            CampaignMapRepresentationBindingKind.IndependentElement, [id]);
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            World.World.Elements.Append(element), World.World.Representations.Append(rep), World.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, World.ScenarioId, init, world6, World.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, GarrisonRemainderContent, content, CwFrontlineContent, TobrukBardiaGarrisonContent,
            BardiaGarrisonRemainderContent, TripoliTankRegimentContent, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateGarrisonRemainder(CampaignSourceAxisGarrisonForces garrisons,
        CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(garrisons); ArgumentNullException.ThrowIfNull(battlefield);
        if (MinorGarrisonContent is null || GarrisonRemainderContent is not null
            || garrisons.Hash() != CampaignSourceAxisGarrisonForces.ContentHash
            || World.ContentIdentity != MinorGarrisonContent.Identity)
            throw new InvalidOperationException("Garrison remainder requires its retained source predecessor.");
        var content = ContentSourceAxisGarrisonRemainderPackArtifact.Create(MinorGarrisonContent);
        if (ContentSourceAxisGarrisonRemainderPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Garrison remainder content failed canonical validation.");
        var prior = World;
        var ids = ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        if (prior.World.Elements.Any(value => ids.Contains(value.ElementId))
            || prior.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("Garrison remainder overlaps the native world or pending arrivals.");
        var init = prior.Initialization;
        var elements = ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(unit =>
        {
            var source = garrisons.Forces.Single(value => value.ElementId == unit.Id);
            var entry = battlefield.Entries.Single(value => value.ElementId == unit.Id);
            var component = ContentSourceAxisGarrisonRemainderPackArtifact.Facts(unit.Id).Components.Single();
            if (source.LocationId != unit.Location || entry.SideId != "axis" || entry.LocationId != unit.Location
                || entry.AuthorityStatus != "source-evidence" || source.InitialToes is not { Count: 1 }
                || source.InitialToes[0].Toe != unit.Toe || source.InitialToes[0].ComponentId != component.ComponentId
                || init.CapabilityPointsExpended > new CapabilityPointAmount(unit.CapabilityPoints, 1))
                throw new InvalidOperationException("Garrison remainder differs from retained source evidence.");
            return new CampaignElementStateV5(unit.Id, unit.Location, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(component.ComponentId, unit.Toe, ContentSourceAxisGarrisonRemainderPackArtifact.Origin(unit.Id))]);
        }).ToArray();
        var reps = ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(unit => new CampaignMapRepresentationState(
            "axis-garrison." + unit.Id, unit.Location, CampaignMapRepresentationBindingKind.IndependentElement, [unit.Id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, content, MotorMachineGunContent, CwFrontlineContent, TobrukBardiaGarrisonContent,
            BardiaGarrisonRemainderContent, TripoliTankRegimentContent, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateMinorGarrisons(CampaignSourceAxisGarrisonForces garrisons,
        CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(garrisons); ArgumentNullException.ThrowIfNull(battlefield);
        if (MinorGarrisonContent is not null || garrisons.Hash() != CampaignSourceAxisGarrisonForces.ContentHash)
            throw new InvalidOperationException("Minor garrisons are already active or lack their source identity.");
        var content = ContentSourceAxisMinorGarrisonsPackArtifact.Create(Content);
        if (ContentSourceAxisMinorGarrisonsPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Minor garrison content failed canonical validation.");
        var prior = World;
        var ids = ContentSourceAxisMinorGarrisonsPackArtifact.Units.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        if (prior.World.Elements.Any(value => ids.Contains(value.ElementId))
            || prior.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("Minor garrisons overlap the native world or pending arrivals.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(15, 1))
            throw new InvalidOperationException("Retained expenditure exceeds garrison allowance.");
        var elements = ContentSourceAxisMinorGarrisonsPackArtifact.Units.Select(unit =>
        {
            var source = garrisons.Forces.Single(value => value.ElementId == unit.Id);
            var entry = battlefield.Entries.Single(value => value.ElementId == unit.Id);
            if (source.LocationId != unit.Location || entry.SideId != "axis" || entry.LocationId != unit.Location
                || entry.AuthorityStatus != "source-evidence"
                || source.InitialToes is not { Count: 1 } || source.InitialToes[0].Toe != unit.Toe
                || source.InitialToes[0].ComponentId != unit.Id + ".infantry")
                throw new InvalidOperationException("Minor garrison differs from retained source evidence.");
            return new CampaignElementStateV5(unit.Id, unit.Location, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(unit.Id + ".infantry", unit.Toe,
                    ContentSourceAxisMinorGarrisonsPackArtifact.Origin(unit.Id))]);
        }).ToArray();
        var reps = ContentSourceAxisMinorGarrisonsPackArtifact.Units.Select(unit => new CampaignMapRepresentationState(
            "axis-minor." + unit.Id, unit.Location, CampaignMapRepresentationBindingKind.IndependentElement, [unit.Id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, content, GarrisonRemainderContent, MotorMachineGunContent, CwFrontlineContent, TobrukBardiaGarrisonContent,
            BardiaGarrisonRemainderContent, TripoliTankRegimentContent, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateTobrukBardiaGarrison(CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        if (TobrukBardiaGarrisonContent is not null)
            throw new InvalidOperationException("Tobruk/Bardia garrison is already active.");
        var content = ContentSourceTobrukBardiaGarrisonPackArtifact.Create(Content);
        if (ContentSourceTobrukBardiaGarrisonPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Tobruk/Bardia garrison content failed canonical validation.");
        var prior = World;
        var ids = ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds;
        if (prior.World.Elements.Any(value => ids.Contains(value.ElementId, StringComparer.Ordinal))
            || prior.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("Tobruk/Bardia garrison overlaps the native world or pending arrivals.");
        var init = prior.Initialization;
        var seeds = ContentSourceTobrukBardiaGarrisonPackArtifact.CreateToeSeeds(prior.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var elements = ids.Select(id =>
        {
            var entry = battlefield.Entries.Single(value => value.ElementId == id);
            var location = ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf(id);
            if (entry.SideId != "axis" || entry.LocationId != location || entry.AuthorityStatus != "source-tobruk-bardia-garrison-admitted")
                throw new InvalidOperationException("Tobruk/Bardia garrison differs from retained source evidence.");
            var toe = seeds[id];
            return new CampaignElementStateV5(id, location, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                toe.InitialComponentToes.Select(component =>
                    new CampaignComponentToeState(component.ComponentId, component.CurrentToe, component.Origin)));
        }).ToArray();
        var reps = ids.Select(id => new CampaignMapRepresentationState("tobruk-bardia-garrison." + id,
            ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf(id), CampaignMapRepresentationBindingKind.IndependentElement, [id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, GarrisonRemainderContent, MotorMachineGunContent, CwFrontlineContent, content,
            BardiaGarrisonRemainderContent, TripoliTankRegimentContent, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateBardiaGarrisonRemainder(CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        if (TobrukBardiaGarrisonContent is null || BardiaGarrisonRemainderContent is not null)
            throw new InvalidOperationException("Bardia garrison remainder requires the admitted Tobruk/Bardia pack and no duplicate.");
        var content = ContentSourceBardiaGarrisonRemainderPackArtifact.Create(TobrukBardiaGarrisonContent);
        if (ContentSourceBardiaGarrisonRemainderPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Bardia garrison remainder content failed canonical validation.");
        var prior = World;
        var ids = ContentSourceBardiaGarrisonRemainderPackArtifact.AllIds;
        if (prior.World.Elements.Any(value => ids.Contains(value.ElementId, StringComparer.Ordinal))
            || prior.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("Bardia garrison remainder overlaps the native world or pending arrivals.");
        var init = prior.Initialization;
        var seeds = ContentSourceBardiaGarrisonRemainderPackArtifact.CreateToeSeeds(prior.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var elements = ids.Select(id =>
        {
            var entry = battlefield.Entries.Single(value => value.ElementId == id);
            if (entry.SideId != "axis" || entry.LocationId != ContentSourceBardiaGarrisonRemainderPackArtifact.LocationId
                || entry.AuthorityStatus != "source-bardia-garrison-remainder-admitted")
                throw new InvalidOperationException("Bardia garrison remainder differs from retained source evidence.");
            var toe = seeds[id];
            return new CampaignElementStateV5(id, ContentSourceBardiaGarrisonRemainderPackArtifact.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                toe.InitialComponentToes.Select(component =>
                    new CampaignComponentToeState(component.ComponentId, component.CurrentToe, component.Origin)));
        }).ToArray();
        var reps = ids.Select(id => new CampaignMapRepresentationState("bardia-garrison-remainder." + id,
            ContentSourceBardiaGarrisonRemainderPackArtifact.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, GarrisonRemainderContent, MotorMachineGunContent, CwFrontlineContent, TobrukBardiaGarrisonContent,
            content, TripoliTankRegimentContent, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateTripoliTankRegiment(CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        if (TripoliTankRegimentContent is not null)
            throw new InvalidOperationException("Tripoli tank regiment is already active.");
        var content = ContentSourceTripoliTankRegimentPackArtifact.Create(Content);
        if (ContentSourceTripoliTankRegimentPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Tripoli tank regiment content failed canonical validation.");
        var prior = World;
        var ids = ContentSourceTripoliTankRegimentPackArtifact.AllIds;
        if (prior.World.Elements.Any(value => ids.Contains(value.ElementId, StringComparer.Ordinal))
            || prior.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("Tripoli tank regiment overlaps the native world or pending arrivals.");
        var init = prior.Initialization;
        var seeds = ContentSourceTripoliTankRegimentPackArtifact.CreateToeSeeds(prior.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var elements = ids.Select(id =>
        {
            var entry = battlefield.Entries.Single(value => value.ElementId == id);
            if (entry.SideId != "axis" || entry.LocationId != ContentSourceTripoliTankRegimentPackArtifact.LocationId
                || entry.AuthorityStatus != "source-tripoli-tank-regiment-admitted")
                throw new InvalidOperationException("Tripoli tank regiment differs from retained source evidence.");
            var toe = seeds[id];
            return new CampaignElementStateV5(id, ContentSourceTripoliTankRegimentPackArtifact.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                toe.InitialComponentToes.Select(component =>
                    new CampaignComponentToeState(component.ComponentId, component.CurrentToe, component.Origin)));
        }).ToArray();
        var reps = ids.Select(id => new CampaignMapRepresentationState("tripoli-tank-regiment." + id,
            ContentSourceTripoliTankRegimentPackArtifact.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, GarrisonRemainderContent, MotorMachineGunContent, CwFrontlineContent, TobrukBardiaGarrisonContent,
            BardiaGarrisonRemainderContent, content, CwAttachedArtilleryContent);
    }

    internal CampaignSourceCwFreeGunsWorld ActivateCwAttachedArtillery(CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        if (CwFrontlineContent is null || CwAttachedArtilleryContent is not null)
            throw new InvalidOperationException("CW attached artillery requires the admitted CW frontline pack and no duplicate.");
        var content = ContentSourceCwAttachedArtilleryPackArtifact.Create(CwFrontlineContent);
        if (ContentSourceCwAttachedArtilleryPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("CW attached artillery content failed canonical validation.");
        var prior = World;
        var ids = ContentSourceCwAttachedArtilleryPackArtifact.AllIds;
        if (prior.World.Elements.Any(value => ids.Contains(value.ElementId, StringComparer.Ordinal))
            || prior.PendingArrivals.Any(value => value.ElementIds.Any(ids.Contains)))
            throw new InvalidOperationException("CW attached artillery overlaps the native world or pending arrivals.");
        var init = prior.Initialization;
        var seeds = ContentSourceCwAttachedArtilleryPackArtifact.CreateToeSeeds(prior.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var elements = ids.Select(id =>
        {
            var entry = battlefield.Entries.Single(value => value.ElementId == id);
            var location = ContentSourceCwAttachedArtilleryPackArtifact.LocationOf(id);
            if (entry.SideId != "commonwealth" || entry.LocationId != location
                || entry.AuthorityStatus != "source-cw-attached-artillery-admitted")
                throw new InvalidOperationException("CW attached artillery differs from retained source evidence.");
            var toe = seeds[id];
            return new CampaignElementStateV5(id, location, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                toe.InitialComponentToes.Select(component =>
                    new CampaignComponentToeState(component.ComponentId, component.CurrentToe, component.Origin)));
        }).ToArray();
        var reps = ids.Select(id => new CampaignMapRepresentationState("cw-attached-artillery." + id,
            ContentSourceCwAttachedArtilleryPackArtifact.LocationOf(id), CampaignMapRepresentationBindingKind.IndependentElement, [id]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(Content, world, NewlyActivatedIds, PredecessorHash, SetupHash, MinorGarrisonContent, GarrisonRemainderContent, MotorMachineGunContent, CwFrontlineContent, TobrukBardiaGarrisonContent,
            BardiaGarrisonRemainderContent, TripoliTankRegimentContent, content);
    }

    internal static CampaignSourceCwFreeGunsWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var setup = state.CwFreeGuns ?? throw new InvalidOperationException("CW free-gun setup missing.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        var basis = state.CwPriorityWorld ?? throw new InvalidOperationException("Normalized priority source world missing.");
        var previous = state.CwFreeGunsWorld;
        var prior = state.World;
        if (prior.ContentIdentity != (previous?.Content.Identity ?? basis.Content.Identity))
            throw new InvalidOperationException("CW free-gun predecessor identity differs.");
        var content = ContentSourceCwFreeGunsPackArtifact.Create(basis.Content);
        if (ContentSourceCwFreeGunsPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("CW free-gun source content failed validation.");
        var existing = prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var candidateIds = CampaignSourceCwFreeGuns.Units.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var previouslyActive = previous is null ? new HashSet<string>(StringComparer.Ordinal)
            : existing.Where(candidateIds.Contains).ToHashSet(StringComparer.Ordinal);
        if (previous is null && existing.Overlaps(candidateIds))
            throw new InvalidOperationException("CW free-gun source overlaps prior world.");
        var placements = setup.Placements.Where(value => !previouslyActive.Contains(value.ElementId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (placements.Length == 0) throw new InvalidOperationException("No placed CW gun awaits activation.");
        foreach (var placement in setup.Placements)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == placement.ElementId);
            if (entry is null || entry.SideId != "commonwealth" || entry.LocationId != placement.LocationId
                || entry.AuthorityStatus is not ("source-cw-free-gun-placed" or "source-cw-free-gun-active"))
                throw new InvalidOperationException("CW free-gun placement disagrees with retained battlefield.");
        }
        if (placements.Any(value => prior.PendingArrivals.Any(group => group.ElementIds.Contains(value.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("CW free gun overlaps deferred arrival.");
        var init = prior.Initialization;
        var elements = placements.Select(placement =>
        {
            var unit = CampaignSourceCwFreeGuns.Units.Single(value => value.ElementId == placement.ElementId);
            if (init.CapabilityPointsExpended > new CapabilityPointAmount(unit.CapabilityPoints, 1))
                throw new InvalidOperationException("Retained expenditure exceeds source gun allowance.");
            return new CampaignElementStateV5(unit.ElementId, placement.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(unit.ElementId + "." + unit.ComponentSuffix, unit.Toe,
                    ContentSourceCwFreeGunsPackArtifact.Origin(unit.ElementId))]);
        }).ToArray();
        var reps = placements.Select(placement => new CampaignMapRepresentationState("cw-free-gun." + placement.ElementId,
            placement.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [placement.ElementId]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, placements.Select(value => value.ElementId), previous?.Hash() ?? basis.Hash(), setup.Hash());
    }

    internal static CampaignSourceCwFreeGunsWorld Rebind(CampaignSourceCwFreeGunsWorld previous,
        CampaignSourceCertifiedMalettiWorld successor) => Rebind(previous, successor.World, successor.PredecessorHash, successor.Hash());
    internal static CampaignSourceCwFreeGunsWorld Rebind(CampaignSourceCwFreeGunsWorld previous,
        CampaignSourceMatruhDetachedWorld successor) => Rebind(previous, successor.World, successor.PredecessorHash, successor.Hash());
    private static CampaignSourceCwFreeGunsWorld Rebind(CampaignSourceCwFreeGunsWorld previous,
        CampaignWorldSnapshotV8 successor, string predecessorHash, string successorHash)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(successor);
        if (predecessorHash != previous.Hash()
            || !previous.World.World.Elements.All(value => successor.World.Elements.Contains(value))
            || !previous.World.World.Representations.All(value => successor.World.Representations.Contains(value))
            || !previous.World.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !previous.World.PendingArrivals.SequenceEqual(successor.PendingArrivals))
            throw new InvalidOperationException("CW free-gun rebind must preserve its actual predecessor.");
        var world = new CampaignWorldSnapshotV8(previous.CwAttachedArtilleryContent?.Identity ?? previous.TripoliTankRegimentContent?.Identity
            ?? previous.BardiaGarrisonRemainderContent?.Identity ?? previous.TobrukBardiaGarrisonContent?.Identity ?? previous.CwFrontlineContent?.Identity
            ?? previous.MotorMachineGunContent?.Identity ?? previous.GarrisonRemainderContent?.Identity ?? previous.MinorGarrisonContent?.Identity ?? previous.Content.Identity, successor.ScenarioId, successor.Initialization,
            successor.World, successor.PendingArrivals);
        return new(previous.Content, world, previous.NewlyActivatedIds, successorHash, previous.SetupHash, previous.MinorGarrisonContent, previous.GarrisonRemainderContent, previous.MotorMachineGunContent, previous.CwFrontlineContent, previous.TobrukBardiaGarrisonContent,
            previous.BardiaGarrisonRemainderContent, previous.TripoliTankRegimentContent, previous.CwAttachedArtilleryContent);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        WriteCanonical(stream);
        return stream.ToArray();
    }
    private void WriteCanonical(Stream stream)
    {
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("setupHash", SetupHash); writer.WriteString("contentHash", Content.Hash());
            writer.WriteBase64String("content", Content.CanonicalBytes); writer.WriteString("scenarioId", World.ScenarioId);
            if (MinorGarrisonContent is not null)
            {
                writer.WriteString("minorGarrisonContentHash", MinorGarrisonContent.Identity.Hash);
                writer.WriteBase64String("minorGarrisonContent", MinorGarrisonContent.CanonicalBytes);
            }
            if (GarrisonRemainderContent is not null)
            {
                writer.WriteString("garrisonRemainderContentHash", GarrisonRemainderContent.Identity.Hash);
                writer.WriteBase64String("garrisonRemainderContent", GarrisonRemainderContent.CanonicalBytes);
            }
            if (MotorMachineGunContent is not null)
            {
                writer.WriteString("motorMachineGunContentHash", MotorMachineGunContent.Identity.Hash);
                writer.WriteBase64String("motorMachineGunContent", MotorMachineGunContent.CanonicalBytes);
            }
            if (CwFrontlineContent is not null)
            {
                writer.WriteString("cwFrontlineContentHash", CwFrontlineContent.Identity.Hash);
                writer.WriteBase64String("cwFrontlineContent", CwFrontlineContent.CanonicalBytes);
            }
            if (TobrukBardiaGarrisonContent is not null)
            {
                writer.WriteString("tobrukBardiaGarrisonContentHash", TobrukBardiaGarrisonContent.Identity.Hash);
                writer.WriteBase64String("tobrukBardiaGarrisonContent", TobrukBardiaGarrisonContent.CanonicalBytes);
            }
            if (BardiaGarrisonRemainderContent is not null)
            {
                writer.WriteString("bardiaGarrisonRemainderContentHash", BardiaGarrisonRemainderContent.Identity.Hash);
                writer.WriteBase64String("bardiaGarrisonRemainderContent", BardiaGarrisonRemainderContent.CanonicalBytes);
            }
            if (TripoliTankRegimentContent is not null)
            {
                writer.WriteString("tripoliTankRegimentContentHash", TripoliTankRegimentContent.Identity.Hash);
                writer.WriteBase64String("tripoliTankRegimentContent", TripoliTankRegimentContent.CanonicalBytes);
            }
            if (CwAttachedArtilleryContent is not null)
            {
                writer.WriteString("cwAttachedArtilleryContentHash", CwAttachedArtilleryContent.Identity.Hash);
                writer.WriteBase64String("cwAttachedArtilleryContent", CwAttachedArtilleryContent.CanonicalBytes);
            }
            writer.WriteBoolean("worldCoverageComplete", false); writer.WriteString("mobilityStatus", "unresolved-no-native-movement");
            writer.WriteStartArray("newlyActivatedIds"); foreach (var id in NewlyActivatedIds) writer.WriteStringValue(id); writer.WriteEndArray();
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", World.World); writer.WriteStartArray("pendingArrivals");
            foreach (var arrival in World.PendingArrivals)
            {
                writer.WriteStartObject(); writer.WriteString("groupId", arrival.GroupId); writer.WriteString("sideId", arrival.SideId);
                writer.WriteNumber("gameTurn", arrival.Arrival.GameTurn); writer.WriteNumber("operationStage", arrival.Arrival.OperationStage);
                writer.WriteString("entryAreaId", arrival.EntryAreaId); writer.WriteStartArray("elementIds");
                foreach (var id in arrival.ElementIds) writer.WriteStringValue(id);
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
    }
    internal string Hash() => cachedHash.Value;

    private string ComputeHash()
    {
        using var hash = SHA256.Create();
        using var sink = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write, leaveOpen: true);
        WriteCanonical(sink);
        sink.FlushFinalBlock();
        return "sha256:" + Convert.ToHexStringLower(hash.Hash!);
    }
}
