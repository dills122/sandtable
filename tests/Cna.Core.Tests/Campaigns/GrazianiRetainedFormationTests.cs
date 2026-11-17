using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiRetainedFormationTests
{
    [Fact]
    public void CairoBrigadeUsesOneCounterAndRetainsOriginalWorld()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None);
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId, init);
        var members = prior.World.Elements.Select(element =>
        {
            var component = element.Components.Single();
            var type = component.ComponentId[(element.ElementId.Length + 1)..];
            var root = element.ElementId == CampaignSourceSixteenthBrigadeGroupedDefinition.RootId;
            return new CampaignGroupedMember(element.ElementId, root ? null : CampaignSourceSixteenthBrigadeGroupedDefinition.RootId,
                type == "infantry" ? component.CurrentToe : null, 0, false, type, component.CurrentToe,
                root ? 30 : 10, CapabilityPointAmount.Zero, 0);
        }).ToArray();
        var group = new CampaignGroupedFormation("16-infantry-brigade", "commonwealth",
            CampaignSourceSixteenthBrigadeGroupedDefinition.RootId, "e1829", null, 20, members, false, true);
        var world = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);
        Assert.Same(prior, world.Prior);
        Assert.Equal(4, world.Prior.World.Elements.Count);
        Assert.Equal(4, world.Prior.World.Representations.Count);
        Assert.Same(group, Assert.Single(world.OccupantsAt("e1829")).Formation);
        Assert.Null(world.StackAt("commonwealth", "e1829", _ => 1));
        var confirmed = world.ConfirmDisposition(group.GroupId, 2);
        using var setupJson = System.Text.Json.JsonDocument.Parse(confirmed.SerializeCanonical());
        Assert.Equal(9, setupJson.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal(2, confirmed.StackAt("commonwealth", "e1829", _ => 1));
        Assert.True(confirmed.Groups.Single().UsesRetainedMembers);
        Assert.Throws<InvalidOperationException>(() => confirmed.DetachMember(group.GroupId, "2-leicesters", 1, 2));
        Assert.Equal(prior.PendingArrivals, confirmed.Prior.PendingArrivals);
        Assert.Equal(confirmed.Hash(), confirmed.Rebind(prior, 1, 1).Hash());
        var movementCosts = group.Members.ToDictionary(value => value.ElementId,
            value => value.ElementId == group.RootId ? new CapabilityPointAmount(1, 2) : new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var moved = confirmed.MoveFormation(group.GroupId, "e1830", movementCosts, 2, 1, 1);
        using var movedJson = System.Text.Json.JsonDocument.Parse(moved.SerializeCanonical());
        Assert.Equal(11, movedJson.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Empty(moved.OccupantsAt("e1829"));
        Assert.Single(moved.OccupantsAt("e1830"));
        Assert.All(moved.Prior.World.Elements, value =>
        {
            Assert.Equal("e1830", value.CurrentLocationId);
            Assert.Equal(movementCosts[value.ElementId], value.OperationalState.CapabilityPointsExpended);
        });
        Assert.All(moved.Prior.World.Representations, value => Assert.Equal("e1830", value.CurrentLocationId));
        Assert.Equal(prior.PendingArrivals, moved.Prior.PendingArrivals);
        Assert.Throws<InvalidOperationException>(() => confirmed.MoveFormation(group.GroupId, "e1830", movementCosts, 2, 1, 2));
        var invalidMembers = members.Select(value => value.ElementId == "2-leicesters" ? value with { ComponentToe = 5, InfantryToe = 5 } : value);
        var invalid = new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId, group.LocationId,
            null, group.MotorPool, invalidMembers, false, true);
        Assert.Throws<ArgumentException>(() => CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [invalid]));

        // An unrelated counter in the same hex remains visible to stacking.
        var extra = new CampaignElementStateV5("1-essex", "e1829", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, null),
            [new CampaignComponentToeState("1-essex.infantry", 6, ContentSourceMatruhDetachedPackArtifact.Origin)]);
        var extraRepresentation = new CampaignMapRepresentationState("extra.1-essex", "e1829",
            CampaignMapRepresentationBindingKind.IndependentElement, [extra.ElementId]);
        var augmented6 = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(extra),
            prior.World.Representations.Append(extraRepresentation), prior.World.BrokenVehicleLots);
        var augmented = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, init, augmented6, prior.PendingArrivals);
        var withExtra = CampaignGroupedWorld.Create(augmented, CampaignSourceFourthIndianDefinition.Hash(), [group])
            .ConfirmDisposition(group.GroupId, 2);
        Assert.Equal(2, withExtra.OccupantsAt("e1829").Count);
        Assert.Equal(3, withExtra.StackAt("commonwealth", "e1829", _ => 1));

        // Real Polish source members arrive together while preserving the previous
        // Cairo units and the unrelated 7th Indian pending schedule.
        var arrived = CampaignSourcePolishArrival.Materialize(confirmed, artifact, "e1730");
        var polish = arrived.Groups.Single(value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Equal(3, polish.StackValue);
        Assert.False(polish.MotorPoolKnown);
        Assert.Throws<InvalidOperationException>(() => arrived.RemainingMotorPoints(polish.GroupId));
        Assert.Throws<InvalidOperationException>(() => arrived.AssignMotorization(polish.GroupId, "polish-1-infantry", 1));
        using var arrivalJson = System.Text.Json.JsonDocument.Parse(arrived.SerializeCanonical());
        Assert.Equal(10, arrivalJson.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Contains("\"motorPool\":null", System.Text.Encoding.UTF8.GetString(arrived.SerializeCanonical()), StringComparison.Ordinal);
        Assert.Equal(8, polish.Members.Count);
        Assert.Null(polish.Members.Single(value => value.ElementId == "polish-brigade-hq").ComponentType);
        Assert.Equal(1, polish.Members.Single(value => value.ElementId == "polish-1-antitank").ComponentToe);
        Assert.DoesNotContain(arrived.Prior.PendingArrivals, value => value.GroupId == polish.GroupId);
        Assert.Single(arrived.Prior.PendingArrivals);
        Assert.Equal(12, arrived.Prior.World.Elements.Count);
        Assert.All(prior.World.Elements, value => Assert.Contains(value, arrived.Prior.World.Elements));
        Assert.Single(arrived.OccupantsAt("e1730"));
        Assert.Equal(3, arrived.StackAt("commonwealth", "e1730", _ => null));
        Assert.All(arrived.Prior.World.Elements.Where(value => polish.Members.Any(member => member.ElementId == value.ElementId)),
            value => { Assert.Equal(1, value.OperationalState.LedgerGameTurn); Assert.Equal(3, value.OperationalState.LedgerOperationStage); });
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Materialize(arrived, artifact, "e1730"));
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Materialize(confirmed, artifact, "e0000"));
        Assert.Throws<InvalidOperationException>(() => confirmed.ArriveRetainedFormation(prior, polish, polish.GroupId));

        // The Commonwealth's own stack calculation is identical when an
        // unobserved opposing counter changes elsewhere in the Cairo area.
        var own = prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var enemy = new CampaignElementStateV5("64-catanzaro-hq", "e1930", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, null), []);
        var enemyRepresentation = new CampaignMapRepresentationState("enemy.64-catanzaro-hq", "e1930",
            CampaignMapRepresentationBindingKind.IndependentElement, [enemy.ElementId]);
        var hidden6 = new CampaignWorldSnapshotV6(6, prior.World.Elements.Append(enemy),
            prior.World.Representations.Append(enemyRepresentation), prior.World.BrokenVehicleLots);
        var hidden8 = new CampaignWorldSnapshotV8(prior.ContentIdentity, prior.ScenarioId, init, hidden6, prior.PendingArrivals);
        var hidden = CampaignGroupedWorld.Create(hidden8, CampaignSourceFourthIndianDefinition.Hash(), confirmed.Groups);
        Assert.Equal(CampaignSourcePolishArrival.OwnerKnownStackAt(confirmed, own, "e1829"),
            CampaignSourcePolishArrival.OwnerKnownStackAt(hidden, own, "e1829"));
        Assert.Equal(CampaignSourcePolishArrival.OwnerKnownStackAt(confirmed, own, "e1930"),
            CampaignSourcePolishArrival.OwnerKnownStackAt(hidden, own, "e1930"));
        var arrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var entries = own.Select(id => new CampaignSourceBattlefieldEntry(id, "commonwealth", "e1829", "world8-initial")).ToArray();
        var unresolvedEnemy = new CampaignSourceBattlefieldEntry("catanzaro-source-evidence", "axis", "e1829", "source-evidence");
        var cityHexes = artifact.Definition.LegacyDefinition.Locations
            .Where(value => value.TerrainId == Cna1979SourceContentVocabulary.MajorCityTerrainId)
            .Select(value => value.LocationId).ToHashSet(StringComparer.Ordinal);
        // e1829 has a known friendly occupant; the other four Cairo hexes are
        // known-empty (stack 0), which is also a legal destination under §4.46a.
        Assert.Equal(["e1730", "e1829", "e1830", "e1930", "e1931"],
            CampaignSourcePolishArrival.OwnerKnownDestinations(confirmed, entries, arrivalPosition, true, cityHexes));
        Assert.Equal(CampaignSourcePolishArrival.OwnerKnownDestinations(confirmed, entries, arrivalPosition, true, cityHexes),
            CampaignSourcePolishArrival.OwnerKnownDestinations(hidden, entries.Append(unresolvedEnemy), arrivalPosition, true, cityHexes));
        Assert.Empty(CampaignSourcePolishArrival.OwnerKnownDestinations(confirmed, entries, arrivalPosition, false, cityHexes));
        cityHexes.Remove("e1829");
        Assert.Equal(["e1730", "e1830", "e1930", "e1931"],
            CampaignSourcePolishArrival.OwnerKnownDestinations(confirmed, entries, arrivalPosition, true, cityHexes));

        // Adjacent source counters create observer-scoped contacts for both
        // sides, without changing a retained unrelated contact.
        var axisKnowledge = new CampaignContactKnowledge("graziani-real", LandSide.Axis);
        axisKnowledge.Apply(new CampaignContactTransition(1, "graziani-real", LandSide.Axis, 1,
            "existing-source", CampaignContactAdjacencySource.RuleId, CampaignContactTransitionKind.Observed,
            axisKnowledge.NextHandle("catanzaro-division"), "16-infantry-brigade", "catanzaro-division",
            "c4707", CampaignContactIdentification.Presence, CampaignContactAccess.Observer));
        var commonwealthKnowledge = new CampaignContactKnowledge("graziani-real", LandSide.Commonwealth);
        CampaignSourceContactCounter[] contactCounters =
        [
            new(CampaignSourcePolishArrival.GroupId, "commonwealth", CampaignSourcePolishArrival.GroupId, "e1931"),
            new("catanzaro-division", "axis", "catanzaro-division", "e1932"),
            new("far-axis-counter", "axis", "far-axis-formation", "e1934"),
        ];
        var axisContacts = CampaignSourcePolishArrivalContacts.DeriveCounters(contactCounters,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, axisKnowledge);
        var commonwealthContacts = CampaignSourcePolishArrivalContacts.DeriveCounters(contactCounters,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, commonwealthKnowledge);
        Assert.Equal(CampaignSourcePolishArrival.GroupId, Assert.Single(axisContacts).BoundRepresentationId);
        Assert.Equal("catanzaro-division", Assert.Single(commonwealthContacts).BoundRepresentationId);
        Assert.Single(axisKnowledge.ObserveFor("catanzaro-division"));
        var initialAxis = CampaignSourceInitialContacts.DeriveCounters(contactCounters, 2, axisKnowledge);
        var initialCommonwealth = CampaignSourceInitialContacts.DeriveCounters(contactCounters, 2, commonwealthKnowledge);
        Assert.Equal(CampaignSourcePolishArrival.GroupId, Assert.Single(initialAxis).BoundRepresentationId);
        Assert.Equal("catanzaro-division", Assert.Single(initialCommonwealth).BoundRepresentationId);
        CampaignSourceContactCounter[] unknownGeometry =
        [
            new("axis-counter", "axis", "axis-formation", "e9920"),
            new("cw-counter", "commonwealth", "cw-formation", "e9921"),
        ];
        Assert.Throws<InvalidOperationException>(() => CampaignSourceInitialContacts.DeriveCounters(unknownGeometry, 2, axisKnowledge));
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("d3615", "c3615", out var crossMapAdjacent));
        Assert.False(crossMapAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c4707", "d3615", out crossMapAdjacent));
        Assert.False(crossMapAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("b5925", "d3714", out crossMapAdjacent));
        Assert.False(crossMapAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("d3714", "b5925", out crossMapAdjacent));
        Assert.False(crossMapAdjacent);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c5915", "d2615", out _));
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("e1829", "d5030", out _));
        // Previously unresolved (Task F's gap); now proven separated by
        // CampaignSourceInitialContacts.TrySoluchMechiliToFtMaddalenaBound -
        // Mechili B4921's published §37.42 distance to Ft. Maddalena C3019
        // (41 hexes) exceeds C3617's real same-sheet graph distance from
        // C3019 (computed from Cna1979HistoricalMap's own transcribed hexes/
        // hexsides) by far more than the adjacency threshold of 1.
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3617", "b4921", out var c3617Mechili));
        Assert.False(c3617Mechili);
        Assert.DoesNotContain("e1430", Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds);
        Assert.Equal(CampaignSourceSeamCertainty.ProvisionalNear,
            CampaignSourceSeamCandidate.Classify("b3939", "c3906").Certainty);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("b3939", "c3906", out _));
        Assert.Equal(CampaignSourceSeamCertainty.ProvisionalFar,
            CampaignSourceSeamCandidate.Classify("b4922", "e1829").Certainty);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("b4922", "e1829", out _));
        var unresolvedSeam = contactCounters.Append(new CampaignSourceContactCounter(
            "axis-unresolved-map-counter", "axis", "axis-unresolved-formation", "d5030")).ToArray();
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrivalContacts.DeriveCounters(unresolvedSeam,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, axisKnowledge));
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrivalContacts.DeriveCounters(unresolvedSeam,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, commonwealthKnowledge));
        CampaignSourceContactCounter[] chartProvenFar =
        [
            new(CampaignSourcePolishArrival.GroupId, "commonwealth", CampaignSourcePolishArrival.GroupId, "e1829"),
            new("derna-garrison-source", "axis", "derna-garrison", "b5925"),
            new("maletti-source-counter", "axis", "maletti-division", "c3617"),
            new("el-grein-garrison-source", "axis", "el-grein-garrison", "c1715"),
            new("scheferzen-garrison-source", "axis", "scheferzen-garrison", "c3419"),
        ];
        Assert.Empty(CampaignSourcePolishArrivalContacts.DeriveCounters(chartProvenFar,
            CampaignSourcePolishArrival.GroupId, "e1829", 2, axisKnowledge));
        Assert.Empty(CampaignSourcePolishArrivalContacts.DeriveCounters(chartProvenFar,
            CampaignSourcePolishArrival.GroupId, "e1829", 2, commonwealthKnowledge));
        var withDetached = contactCounters.Append(new CampaignSourceContactCounter(
            "source-detached/catanzaro-division/64-catanzaro-infantry", "axis", null, "e1932")).ToArray();
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrivalContacts.DeriveCounters(withDetached,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, axisKnowledge));
        Assert.Throws<InvalidOperationException>(() => CampaignSourceInitialContacts.DeriveCounters(withDetached, 2, axisKnowledge));
        Assert.Equal(2, CampaignSourcePolishArrivalContacts.DeriveCounters(withDetached,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, commonwealthKnowledge).Count);
        var distantDetached = withDetached.Select(value => value.RepresentationId.StartsWith("source-detached/", StringComparison.Ordinal)
            ? value with { LocationId = "e1934" } : value).ToArray();
        Assert.Single(CampaignSourcePolishArrivalContacts.DeriveCounters(distantDetached,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, axisKnowledge));
        Assert.Single(CampaignSourceInitialContacts.DeriveCounters(distantDetached, 2, axisKnowledge));
        var unknownObserver = contactCounters.Select(value => value.RepresentationId == "catanzaro-division"
            ? value with { ObserverFormationId = null } : value).ToArray();
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrivalContacts.DeriveCounters(unknownObserver,
            CampaignSourcePolishArrival.GroupId, "e1931", 2, axisKnowledge));

        var movementAxis = new CampaignContactKnowledge("graziani-move", LandSide.Axis);
        var movementCw = new CampaignContactKnowledge("graziani-move", LandSide.Commonwealth);
        var movedNear = contactCounters.Where(value => value.RepresentationId != "far-axis-counter").ToArray();
        var movedFar = movedNear.Select(value => value.RepresentationId == CampaignSourcePolishArrival.GroupId
            ? value with { LocationId = "e1934" } : value).ToArray();
        var sawPolish = Assert.Single(CampaignSourceMovementContacts.DeriveCounters(movedFar, movedNear,
            CampaignSourcePolishArrival.GroupId, 2, movementAxis));
        var sawAxis = Assert.Single(CampaignSourceMovementContacts.DeriveCounters(movedFar, movedNear,
            CampaignSourcePolishArrival.GroupId, 2, movementCw));
        Assert.Equal(CampaignContactTransitionKind.Observed, sawPolish.Kind);
        Assert.Equal(CampaignContactTransitionKind.Observed, sawAxis.Kind);
        movementAxis.Apply(sawPolish); movementCw.Apply(sawAxis);
        var lostPolish = Assert.Single(CampaignSourceMovementContacts.DeriveCounters(movedNear, movedFar,
            CampaignSourcePolishArrival.GroupId, 3, movementAxis));
        Assert.Equal(CampaignContactTransitionKind.Lost, lostPolish.Kind);
        movementAxis.Apply(lostPolish);
        var rediscovered = Assert.Single(CampaignSourceMovementContacts.DeriveCounters(movedFar, movedNear,
            CampaignSourcePolishArrival.GroupId, 4, movementAxis));
        Assert.Equal(CampaignContactTransitionKind.Rediscovered, rediscovered.Kind);
        Assert.Equal(sawPolish.ContactHandle, rediscovered.ContactHandle);
        var extraChanged = movedNear.Select(value => value.SideId == "axis" ? value with { LocationId = "e1930" } : value).ToArray();
        Assert.Throws<InvalidOperationException>(() => CampaignSourceMovementContacts.DeriveCounters(movedFar, extraChanged,
            CampaignSourcePolishArrival.GroupId, 2, movementAxis));
        var unknownBefore = unknownGeometry.Select(value => value.SideId == "commonwealth"
            ? value with { LocationId = "e1522" } : value).ToArray();
        Assert.Throws<InvalidOperationException>(() => CampaignSourceMovementContacts.DeriveCounters(unknownBefore, unknownGeometry,
            "cw-counter", 2, movementAxis));
    }
}
