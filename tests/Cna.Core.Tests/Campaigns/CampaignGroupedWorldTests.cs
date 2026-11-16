using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignGroupedWorldTests
{
    [Fact]
    public void SourceCireneAndMarmaricaRetainTheirDistinctRosterAndComponents()
    {
        var cirene = Assert.Single(CampaignSourceCireneMarmaricaGroupedDefinition.Groups,
            value => value.Id == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId);
        var marmarica = Assert.Single(CampaignSourceCireneMarmaricaGroupedDefinition.Groups,
            value => value.Id == CampaignSourceCireneMarmaricaGroupedDefinition.MarmaricaId);
        Assert.Equal("c4120", cirene.LocationId); Assert.Equal(50, cirene.MotorPoints); Assert.Equal(15, cirene.Members.Count);
        Assert.Equal("c3918", marmarica.LocationId); Assert.Equal(45, marmarica.MotorPoints); Assert.Equal(14, marmarica.Members.Count);
        Assert.Equal("63-cirene-hq", Assert.Single(cirene.Members, value => value.Id == "lxiii-light-tank").ParentId);
        Assert.Equal("62-marmarica-hq", Assert.Single(marmarica.Members, value => value.Id == "lxii-light-tank").ParentId);
        Assert.Null(Assert.Single(cirene.Members, value => value.Id == "63-engineer").Components);
        var artillery = Assert.Single(cirene.Members, value => value.Id == "45-artillery");
        Assert.Contains(new CampaignGroupedComponent("75-18-gun-howitzer", 6), artillery.Components!);
        Assert.Contains(new CampaignGroupedComponent("100-17-howitzer", 3), artillery.Components!);
        Assert.Contains(new CampaignGroupedComponent("light-aa", 3), artillery.Components!);
        Assert.Equal(1, Assert.Single(Assert.Single(cirene.Members, value => value.Id == "157-infantry-hq").Components!).Toe);
    }

    [Fact]
    public void MixedLibyanArtilleryRetainsBothSourceComponentsImmutably()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
            new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        var extras = new List<CampaignGroupedComponent> { new("100-17-howitzer", 3) };
        var members = new[]
        {
            new CampaignGroupedMember("2-libyan-division-hq", null, null, 0, false, null, null, 30, CapabilityPointAmount.Zero, 0),
            new CampaignGroupedMember("2-libyan-artillery", "2-libyan-division-hq", null, 0, false,
                "75-27-gun", 9, 15, CapabilityPointAmount.Zero, 0, extras),
        };
        var group = new CampaignGroupedFormation("2-libyan-division", "axis", "2-libyan-division-hq", "c3920", null, 15, members, false);
        var world = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);
        var before = world.Hash();
        extras.Clear();
        var artillery = Assert.Single(group.Members, member => member.ElementId == "2-libyan-artillery");
        Assert.Equal(9, artillery.ComponentToe);
        Assert.Equal(new CampaignGroupedComponent("100-17-howitzer", 3), Assert.Single(artillery.AdditionalComponents!));
        Assert.Equal(before, world.Hash());
    }

    [Fact]
    public void DetachedMalettiReservationSurvivesPromotionAndSharesParentPool()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None);
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId, initialization);
        const string id = "1-1-libyan-motor-infantry";
        var group = new CampaignGroupedFormation("maletti", "axis", "maletti-hq", "c3617", 5, 35,
        [
            new CampaignGroupedMember("maletti-hq", null, null, 0, false, null, null, 30, CapabilityPointAmount.Zero, 0),
            new CampaignGroupedMember(id, "maletti-hq", 5, 0, true, "infantry", 5, 10, CapabilityPointAmount.Zero, 0),
        ]);
        var world = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);
        var detached = world.DetachMember(group.GroupId, id, 1, null).AssignMotorization(group.GroupId, id, 5);
        Assert.Equal(30, detached.RemainingMotorPoints(group.GroupId));
        Assert.Equal(5, Assert.Single(detached.Detached).Member.AssignedMotorPoints);
        var movementCosts = detached.Groups.Single().Members.ToDictionary(value => value.ElementId,
            value => new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var moved = detached.MoveFormation(group.GroupId, "c3618", movementCosts, 4, 1, 1);
        Assert.Equal("c3617", Assert.Single(moved.Detached).LocationId);
        Assert.Equal("c3618", Assert.Single(moved.Groups).LocationId);
        Assert.Equal(30, moved.RemainingMotorPoints(group.GroupId));
        Assert.Throws<InvalidOperationException>(() => moved.AssignMotorization(group.GroupId, id, -1));
        Assert.Equal("c3617", Assert.Single(detached.Groups).LocationId);
        var element = new CampaignElementStateV5(id, "c3617", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, null),
            [new CampaignComponentToeState(id + ".infantry", 5, ContentSourceCertifiedMalettiPackArtifact.Origin)]);
        var representation = new CampaignMapRepresentationState("certified-maletti-setup." + id, "c3617",
            CampaignMapRepresentationBindingKind.IndependentElement, [id]);
        var next6 = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(element),
            prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var next8 = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, initialization, next6, prior.PendingArrivals);
        var promoted = detached.Rebind(next8, 1, 1);
        Assert.Empty(promoted.Detached);
        Assert.Equal(5, Assert.Single(promoted.ExternalReservations).AssignedMotorPoints);
        Assert.Equal(30, promoted.RemainingMotorPoints(group.GroupId));
        var movedPromoted = promoted.MoveFormation(group.GroupId, "c3618", movementCosts, 4, 1, 1);
        Assert.Equal("c3617", movedPromoted.Prior.World.Elements.Single(value => value.ElementId == id).CurrentLocationId);
        Assert.Equal(30, movedPromoted.RemainingMotorPoints(group.GroupId));
        Assert.Throws<InvalidOperationException>(() => movedPromoted.AssignMotorization(group.GroupId, id, -1));
        var imported = CampaignGroupedWorld.Create(next8, CampaignSourceFourthIndianDefinition.Hash(), detached.Groups,
            externalReservations: [new CampaignGroupedExternalReservation(group.GroupId, id, 5, 5)]);
        Assert.Equal(30, imported.RemainingMotorPoints(group.GroupId));
        Assert.Throws<ArgumentException>(() => CampaignGroupedWorld.Create(next8, CampaignSourceFourthIndianDefinition.Hash(), detached.Groups,
            externalReservations: [new CampaignGroupedExternalReservation(group.GroupId, id, 4, 4)]));
        Assert.Throws<ArgumentException>(() => promoted.AssignMotorization(group.GroupId, id, 1));
        Assert.Equal(35, promoted.AssignMotorization(group.GroupId, id, -5).RemainingMotorPoints(group.GroupId));
    }

    [Fact]
    public void MatruhDetachmentRemainsOneOccupantThroughWorld8Promotion()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None);
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId, initialization);
        var members = CampaignSourceMatruhFormation.Members.Select(value => new CampaignGroupedMember(value.ElementId,
            CampaignSourceMatruhFormation.HeadquartersId, 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0))
            .Append(new CampaignGroupedMember(CampaignSourceMatruhFormation.HeadquartersId, null, null, 0, false,
                null, null, 30, CapabilityPointAmount.Zero, 0));
        var group = new CampaignGroupedFormation("matruh-garrison", "commonwealth", CampaignSourceMatruhFormation.HeadquartersId,
            "d3714", 0, 0, members);
        var world = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);
        var detached = world.DetachMember(group.GroupId, "1-essex", 1, 0);
        Assert.Equal(2, detached.OccupantsAt("d3714").Count);
        Assert.Equal(1, detached.StackAt("commonwealth", "d3714", _ => null));
        Assert.DoesNotContain(detached.Groups.Single().Members, value => value.ElementId == "1-essex");
        Assert.Equal("1-essex", Assert.Single(detached.Detached).Member.ElementId);
        var element = new CampaignElementStateV5("1-essex", "d3714", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, null),
            [new CampaignComponentToeState("1-essex.infantry", 6, ContentSourceMatruhDetachedPackArtifact.Origin)]);
        var representation = new CampaignMapRepresentationState("matruh-detached.1-essex", "d3714",
            CampaignMapRepresentationBindingKind.IndependentElement, ["1-essex"]);
        var next6 = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(element),
            prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var next8 = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, initialization, next6, prior.PendingArrivals);
        var promoted = detached.Rebind(next8, 1, 1);
        Assert.Empty(promoted.Detached);
        Assert.Equal(2, promoted.OccupantsAt("d3714").Count);
        Assert.Equal(1, promoted.StackAt("commonwealth", "d3714", _ => 1));
        Assert.Equal(prior.PendingArrivals, promoted.Prior.PendingArrivals);
        Assert.Throws<InvalidOperationException>(() => world.Rebind(next8, 1, 1));
        var changedElement = new CampaignElementStateV5("1-essex", "d3714", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, null),
            [new CampaignComponentToeState("1-essex.infantry", 5, ContentSourceMatruhDetachedPackArtifact.Origin)]);
        var changed6 = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(changedElement),
            prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var changed8 = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, initialization, changed6, prior.PendingArrivals);
        Assert.Throws<InvalidOperationException>(() => detached.Rebind(changed8, 1, 1));
        var futureLedger = new CampaignElementStateV5("1-essex", "d3714", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(2, 1, CapabilityPointAmount.Zero, 0, null, null),
            [new CampaignComponentToeState("1-essex.infantry", 6, ContentSourceMatruhDetachedPackArtifact.Origin)]);
        var futureWorld = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(futureLedger),
            prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var future = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, initialization, futureWorld, prior.PendingArrivals);
        Assert.Throws<InvalidOperationException>(() => detached.Rebind(future, 1, 1));
        var laterStage = new CampaignElementStateV5("1-essex", "d3714", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 2, CapabilityPointAmount.Zero, 0, null, null),
            [new CampaignComponentToeState("1-essex.infantry", 6, ContentSourceMatruhDetachedPackArtifact.Origin)]);
        var laterStageWorld = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(laterStage),
            prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var later = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, initialization, laterStageWorld, prior.PendingArrivals);
        Assert.Throws<InvalidOperationException>(() => detached.Rebind(later, 1, 1));
    }

    [Fact]
    public void RealFourthIndianGroupConservesPoolAndDoesNotAssumeStackOrUnknownToe()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
            new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        var members = CampaignSourceFourthIndianDefinition.Members.Select(member => new CampaignGroupedMember(
            member.Id, member.ParentId, member.Kind is "infantry" or "motor-infantry" ? member.Toe : null,
            0, member.Id == "central-india-horse", member.Toe is null ? null : member.Kind, member.Toe,
            member.CapabilityPoints, prior.Initialization.CapabilityPointsExpended, prior.Initialization.CohesionLevel)).ToArray();
        var group = new CampaignGroupedFormation("4-indian-division", "commonwealth", "4-indian-division-hq",
            "d3615", null, 22, members);
        var world = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);
        Assert.Equal(11, Assert.Single(world.Groups).Members.Count);
        var artillery = Assert.Single(group.Members, member => member.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", artillery.ComponentType);
        Assert.Equal(6, artillery.ComponentToe);
        Assert.Equal(20, artillery.CapabilityPointAllowance);
        Assert.Throws<ArgumentException>(() => new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId,
            group.LocationId, null, group.MotorPool, members.Select(member => member.ElementId == artillery.ElementId
                ? member with { CapabilityPointsExpended = new CapabilityPointAmount(21, 1) } : member)));
        Assert.Null(Assert.Single(group.Members, member => member.ElementId == "4-indian-division-hq").ComponentToe);
        Assert.Equal(group, Assert.Single(world.OccupantsAt("d3615")).Formation);
        Assert.Null(world.StackAt("commonwealth", "d3615", _ => null));
        // Exercise known-value arithmetic without certifying this source counter's shell status.
        var knownStack = new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId,
            group.LocationId, 5, group.MotorPool, members);
        var knownStackWorld = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [knownStack]);
        Assert.Equal(5, knownStackWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(0, knownStackWorld.StackAt("axis", "d3615", _ => null));
        var occupiedLocation = prior.World.Representations[0].CurrentLocationId;
        var overlapping = new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId,
            occupiedLocation, null, group.MotorPool, members);
        var overlappingWorld = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [overlapping]);
        Assert.True(overlappingWorld.OccupantsAt(occupiedLocation).Count > 1);
        Assert.Null(overlappingWorld.StackAt("commonwealth", occupiedLocation, _ => null));
        Assert.Equal(2, world.Prior.PendingArrivals.Count);
        var assigned = world.AssignMotorization(group.GroupId, "central-india-horse", 6);
        Assert.Equal(16, assigned.RemainingMotorPoints(group.GroupId));
        Assert.Equal(22, world.RemainingMotorPoints(group.GroupId));
        Assert.Throws<ArgumentException>(() => assigned.AssignMotorization(group.GroupId, "central-india-horse", 1));
        Assert.Throws<InvalidOperationException>(() => world.AssignMotorization(group.GroupId, "1-royal-fusiliers", 1));
        Assert.Equal(world.Hash(), assigned.AssignMotorization(group.GroupId, "central-india-horse", -6).Hash());
        Assert.Throws<ArgumentException>(() => CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group, group]));
    }
}
