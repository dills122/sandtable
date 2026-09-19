using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatWorldTests
{
    [Fact]
    public void CertifiedContentCreatesExactDormantWorldAndCopiesSeedProvenance()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var initialization = InitialPolicy(scenario);

        var world = CampaignWorldV7Factory.CreateInitial(
            artifact,
            scenario,
            initialization,
            "fixture.creation-001");

        Assert.Equal(7, world.ContractVersion);
        Assert.Equal("fixture.creation-001", world.CreationBinding);
        Assert.Equal(2, world.Elements.Count);
        Assert.Equal(2, world.Representations.Count);
        Assert.Empty(world.BrokenVehicleLots);
        Assert.Empty(world.CohesionCauses);
        Assert.Empty(world.Relationships);
        Assert.Empty(world.CustodyLots);
        Assert.Empty(world.Guards);
        Assert.Empty(world.ReplacementEntitlements);
        Assert.Empty(world.FutureObligations);
        Assert.Empty(world.Settlements);

        foreach (var element in world.Elements)
        {
            var source = Assert.Single(artifact.Definition.Elements, value => value.ElementId == element.ElementId);
            var placement = Assert.Single(scenario.InitialPlacements, value => value.ElementId == element.ElementId);
            Assert.Equal(placement.LocationId, element.CurrentLocationId);
            Assert.Equal(source.ParentFormationId, element.SourceParentFormationId);
            Assert.Equal(source.ParentFormationId, element.CurrentParentFormationId);
            Assert.Equal(placement.InitialAmmunition.Points, element.Ammunition.Points);
            Assert.Equal(placement.InitialAmmunition.Origin, element.Ammunition.InitialAmmunitionOrigin);
            Assert.Equal(placement.InitialReadiness.Origin, element.Readiness.InitialReadinessOrigin);
            Assert.Equal(placement.InitialReadiness.WaterStatus, element.Readiness.WaterStatus);
            Assert.Equal(placement.InitialReadiness.StoresStatus, element.Readiness.StoresStatus);
            Assert.Equal(placement.InitialComponentToes[0].Origin, Assert.Single(element.Components).InitialToeOrigin);
            Assert.Equal(10, Assert.Single(element.Components).CurrentToe);
            Assert.Equal(CapabilityPointAmount.Zero, element.OperationalState.CapabilityPointsExpended);
            Assert.Equal(0, element.OperationalState.CohesionLevel);
            Assert.Equal(initialization.Origin, element.OperationalState.InitialLedgerOrigin);
            Assert.Null(element.OperationalState.VehicleBreakdownState);
            Assert.Null(element.OperationalState.MovementEnded);
        }
    }

    [Fact]
    public void CreationRejectsForeignScenarioAndMismatchedInitialization()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var initialization = InitialPolicy(scenario);

        Assert.Throws<ArgumentException>(() => CampaignWorldV7Factory.CreateInitial(
            artifact,
            new ContentCombatScenario(
                "different-scenario",
                scenario.Start,
                scenario.End,
                scenario.InitialPlacements,
                scenario.RetreatSupplyAnchors,
                scenario.Origin),
            initialization,
            "fixture.creation-001"));
        Assert.Throws<ArgumentException>(() => CampaignWorldV7Factory.CreateInitial(
            artifact,
            scenario,
            new CampaignCombatInitializationPolicy(
                1,
                scenario.Start.GameTurn,
                scenario.Start.OperationStage,
                new CapabilityPointAmount(1, 1),
                0,
                CampaignElementReserveStatus.None,
                initialization.Origin),
            "fixture.creation-001"));
    }

    [Fact]
    public void OrdinaryAndMandatorySpendingUseDistinctCeilingsAndImmediateExcessDp()
    {
        var origin = InitialPolicy(Assert.Single(Cna1979CombatContentCatalog.Artifact.Definition.Scenarios)).Origin;
        var atFive = new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(5, 1),
            0, null, null, origin);
        var atNine = new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(9, 1),
            0, null, null, origin);
        var atTen = new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(10, 1),
            0, null, null, origin);

        var eleven = CampaignCombatSpending.ChargeOrdinary(atFive, new CapabilityPointAmount(6, 1), 10,
            CampaignCombatSpendCeiling.Ordinary, "axis-assault-battalion", "move.11", []);
        Assert.Equal(new CapabilityPointAmount(11, 1), eleven.State.CapabilityPointsExpended);
        Assert.Equal(-1, eleven.State.CohesionLevel);
        Assert.Equal(1, Assert.IsType<CampaignCombatCohesionCause>(eleven.Cause).Points);

        var fifteen = CampaignCombatSpending.ChargeOrdinary(atNine, new CapabilityPointAmount(6, 1), 10,
            CampaignCombatSpendCeiling.Ordinary, "axis-assault-battalion", "move.15", []);
        Assert.Equal(new CapabilityPointAmount(15, 1), fifteen.State.CapabilityPointsExpended);
        Assert.Equal(-5, fifteen.State.CohesionLevel);
        Assert.Equal(5, Assert.IsType<CampaignCombatCohesionCause>(fifteen.Cause).Points);

        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatSpending.ChargeOrdinary(
            atTen, new CapabilityPointAmount(6, 1), 10, CampaignCombatSpendCeiling.Ordinary,
            "axis-assault-battalion", "move.16", []));

        var mandatory = CampaignCombatSpending.ChargeMandatoryRetreat(atTen, 10,
            "axis-assault-battalion", "settlement.001.retreat",
            "settlement.001.retreat-excess-dp.axis", []);
        Assert.Equal(new CapabilityPointAmount(11, 1), mandatory.State.CapabilityPointsExpended);
        Assert.Equal(-1, mandatory.State.CohesionLevel);
        Assert.Equal(1, Assert.IsType<CampaignCombatCohesionCause>(mandatory.Cause).Points);

        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatSpending.ChargeOrdinary(
            atNine, new CapabilityPointAmount(2, 1), 10, CampaignCombatSpendCeiling.ReleasedReserveI,
            "axis-assault-battalion", "move.reserve-i", []));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatSpending.ChargeOrdinary(
            atFive, new CapabilityPointAmount(1, 1), 10, CampaignCombatSpendCeiling.ReleasedReserveII,
            "axis-assault-battalion", "move.reserve-ii", []));

        Assert.Throws<OverflowException>(() => CampaignCombatSpending.ChargeMandatoryRetreat(
            new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(long.MaxValue, 1),
                0, null, null, origin), 10, "axis-assault-battalion", "retreat.overflow",
            "retreat.overflow.dp", []));
    }

    [Fact]
    public void GuardTransferConservesToeAndRetainsDistinctOrigins()
    {
        var world = InitialWorld();
        var donor = Assert.Single(world.Elements, value => value.ElementId == "commonwealth-assault-battalion");
        var original = Assert.Single(world.Elements, value => value.ElementId == "axis-assault-battalion");
        var sourceComponent = Assert.Single(donor.Components);
        var lot = new CampaignCombatCustodyLot("fixture.settlement-001.captives",
            "fixture.settlement-001.losses",
            new CampaignCombatComponentKey(new CampaignCombatUnitKey(world.CreationBinding, "axis", original.ElementId),
                Assert.Single(original.Components).ComponentId),
            new CampaignCombatUnitKey(world.CreationBinding, "commonwealth", donor.ElementId),
            3, original.CurrentLocationId, original.CurrentLocationId, "pending", null, null);

        var transfer = CampaignCombatGuardFormation.Transfer(donor, lot, sourceComponent.ComponentId,
            "fixture.settlement-001.custody", "fixture.settlement-001.guard");

        Assert.Equal(9, Assert.Single(transfer.Donor.Components).CurrentToe);
        Assert.Equal(1, transfer.Guard.Toe);
        Assert.Equal(sourceComponent.InitialToeOrigin, Assert.Single(transfer.Donor.Components).InitialToeOrigin);
        Assert.Equal(sourceComponent.ComponentId, transfer.Guard.OriginComponent.ComponentId);
        Assert.Equal(donor.OperationalState, transfer.Guard.OperationalState);
        Assert.Equal(donor.Readiness, transfer.Guard.Readiness);
        Assert.Equal(0, transfer.Guard.Ammunition.Points);
        Assert.Equal("guarded", transfer.Lot.Status);
        Assert.Equal(transfer.Guard.GuardId, transfer.Lot.GuardId);
        Assert.Equal(donor.CurrentLocationId, transfer.Lot.CurrentLocationId);
    }

    [Fact]
    public void MalformedCustodyLotsRejectBeforeWorldConstruction()
    {
        var binding = "fixture.creation-001";
        var component = new CampaignCombatComponentKey(
            new CampaignCombatUnitKey(binding, "axis", "axis-assault-battalion"),
            "axis-assault-battalion.toe.infantry");
        var captor = new CampaignCombatUnitKey(binding, "commonwealth", "commonwealth-assault-battalion");
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignCombatCustodyLot(
            "lot.bad", "losses.bad", component, captor, 4, "assault-west", "assault-west",
            "pending", null, null));
        Assert.Throws<ArgumentException>(() => new CampaignCombatCustodyLot(
            "lot.bad", "losses.bad", component, captor, 1, "assault-west", "assault-west",
            "guarded", null, null));
        Assert.Throws<ArgumentException>(() => new CampaignCombatCustodyLot(
            "lot.bad", "losses.bad", component,
            new CampaignCombatUnitKey(binding, "axis", "other-axis"), 1,
            "assault-west", "assault-west", "pending", null, null));
    }

    [Fact]
    public void WorldRejectsDanglingGuardAndDuplicateLots()
    {
        var world = InitialWorld();
        var original = Assert.Single(world.Elements, value => value.ElementId == "axis-assault-battalion");
        var captor = Assert.Single(world.Elements, value => value.ElementId == "commonwealth-assault-battalion");
        var lot = new CampaignCombatCustodyLot("fixture.settlement-001.captives",
            "fixture.settlement-001.losses",
            new CampaignCombatComponentKey(new CampaignCombatUnitKey(world.CreationBinding, "axis", original.ElementId),
                Assert.Single(original.Components).ComponentId),
            new CampaignCombatUnitKey(world.CreationBinding, "commonwealth", captor.ElementId),
            3, original.CurrentLocationId, captor.CurrentLocationId, "guarded",
            "fixture.settlement-001.guard", null);
        Assert.Throws<ArgumentException>(() => WithLots(world, [lot], []));
        Assert.Throws<ArgumentException>(() => WithLots(world, [lot, lot], []));
    }

    [Fact]
    public void ReplacementEligibilityUsesFutureOperationStagesWithoutClamping()
    {
        var original = new CampaignCombatComponentKey(
            new CampaignCombatUnitKey("fixture.creation-001", "axis", "axis-assault-battalion"),
            "axis-assault-battalion.toe.infantry");
        var entitlement = new CampaignCombatReplacementEntitlement("replacement.001", "escape.001",
            "lot.001", original, 3, "assault-west", new CampaignCombatScope(111, 3), 12,
            new CampaignCombatScope(115, 3, true), "awaiting-eligibility-and-training");
        Assert.Equal(115, entitlement.EligibleScope.GameTurn);
        Assert.Throws<ArgumentException>(() => new CampaignCombatReplacementEntitlement("replacement.001", "escape.001",
            "lot.001", original, 3, "assault-west", new CampaignCombatScope(111, 3), 12,
            new CampaignCombatScope(111, 3), "awaiting-eligibility-and-training"));
    }

    [Fact]
    public void SpendingRetainsPreexistingBreakdownAndInitialLedgerProvenance()
    {
        var origin = InitialPolicy(Assert.Single(Cna1979CombatContentCatalog.Artifact.Definition.Scenarios)).Origin;
        var breakdown = new CampaignVehicleBreakdownState("cohort.001", BreakdownPointAmount.Zero,
            BreakdownPointAmount.Zero, null, 1, 0);
        var state = new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(9, 1),
            0, breakdown, null, origin);

        var charged = CampaignCombatSpending.ChargeOrdinary(state, new CapabilityPointAmount(2, 1),
            10, CampaignCombatSpendCeiling.Ordinary, "axis-assault-battalion", "move.breakdown", []);

        Assert.Same(breakdown, charged.State.VehicleBreakdownState);
        Assert.Same(origin, charged.State.InitialLedgerOrigin);
        Assert.Equal(new CapabilityPointAmount(9, 1), state.CapabilityPointsExpended);
    }

    [Fact]
    public void SettlementReceiptsKeepOrderedRouteAndRejectForgedPredecessor()
    {
        var world = InitialWorld();
        var axis = Assert.Single(world.Elements, value => value.ElementId == "axis-assault-battalion");
        var commonwealth = Assert.Single(world.Elements, value => value.ElementId == "commonwealth-assault-battalion");
        var result = new CampaignCombatResultFacts(-2, 11, 36, 6, 25, 5, false, 1, "attacker", 75);
        var route = new[] { "assault-east", "commonwealth-rear" };
        var disposition = new CampaignCombatRetreatDisposition("settlement.001.disposition", "settlement.001.result",
            "retreat", 1, 1, 0, route);
        route[0] = "forged";
        Assert.Equal("assault-east", disposition.Route[0]);
        var attacker = new CampaignCombatUnitKey(world.CreationBinding, "axis", axis.ElementId);
        var defender = new CampaignCombatUnitKey(world.CreationBinding, "commonwealth", commonwealth.ElementId);
        Assert.Throws<ArgumentException>(() => new CampaignCombatSettlementState("settlement.001",
            "settlement.001.commit", "settlement.001.result", 1, 1, attacker, defender,
            world.Elements, result,
            new CampaignCombatRetreatDisposition("settlement.001.disposition", "wrong.result",
                "retreat", 1, 1, 0, ["assault-east", "commonwealth-rear"]),
            null, null, null, null));
        var settlement = new CampaignCombatSettlementState("settlement.001", "settlement.001.commit",
            "settlement.001.result", 1, 1, attacker, defender, world.Elements, result,
            disposition, null, null, null, null);
        Assert.Equal("assault-east", settlement.Disposition!.Route[0]);
    }

    [Fact]
    public void WorldRejectsNoncontiguousCohesionHistory()
    {
        var world = InitialWorld();
        var cause = new CampaignCombatCohesionCause("cause.002", 2, "move.002",
            "axis-assault-battalion", 1, 1, "ordinary-movement-excess-cp-dp", 1, 0, -1);
        Assert.Throws<ArgumentException>(() => new CampaignWorldSnapshotV7(7, world.CreationBinding,
            world.Elements, world.Representations, [], [cause], [], [], [], [], [], []));
    }

    [Fact]
    public void ReceiptValuesRejectDistanceAndToeConservationForgery()
    {
        var component = new CampaignCombatComponentKey(
            new CampaignCombatUnitKey("fixture.creation-001", "axis", "axis-assault-battalion"),
            "axis-assault-battalion.toe.infantry");
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatDisposition(
            "settlement.001.disposition", "settlement.001.result", "retreat", 1, 1, 0,
            ["assault-east"]));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("attacker", component,
            10, 25, 0, 3, 3, 0, 8, 3));
        Assert.Throws<ArgumentException>(() => new CampaignCombatCustodyReceipt(
            "settlement.001.custody", "settlement.001.retreat", "lot.001",
            "relocate-and-guard", ["a", "b", "c", "d", "e"], "guard.001", null, 10, 9));
    }

    [Fact]
    public void EndedRelationshipRequiresBothReceiptAndCause()
    {
        var world = InitialWorld();
        var attacker = new CampaignCombatUnitKey(world.CreationBinding, "axis", "axis-assault-battalion");
        var defender = new CampaignCombatUnitKey(world.CreationBinding, "commonwealth", "commonwealth-assault-battalion");

        Assert.Throws<ArgumentException>(() => new CampaignCombatRelationship("relation.001", "settlement.001.relationships",
            "contact", attacker, defender, 1, 1, false, "move.001", null));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRelationship("relation.001", "settlement.001.relationships",
            "contact", attacker, defender, 1, 1, false, null, "ordinary-movement-breakoff"));

        var ended = new CampaignCombatRelationship("relation.001", "settlement.001.relationships",
            "contact", attacker, defender, 1, 1, false, "move.001", "ordinary-movement-breakoff");
        Assert.False(ended.Active);
    }

    [Fact]
    public void RoleLossRequiresSelectedToeAndThirtyPercentDpThreshold()
    {
        var component = new CampaignCombatComponentKey(
            new CampaignCombatUnitKey("fixture.creation-001", "axis", "axis-assault-battalion"),
            "axis-assault-battalion.toe.infantry");

        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("attacker", component,
            10, 20, 0, 2, 0, 2, 8, 3));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("attacker", component,
            10, 30, 0, 3, 0, 3, 7, 0));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("attacker", component,
            9, 30, 0, 3, 0, 3, 6, 3));

        Assert.Equal(0, new CampaignCombatRoleLoss("attacker", component,
            10, 20, 0, 2, 0, 2, 8, 0).LossDp);
        Assert.Equal(3, new CampaignCombatRoleLoss("attacker", component,
            10, 30, 0, 3, 0, 3, 7, 3).LossDp);
    }

    [Fact]
    public void ResultFactsMustMatchSelectedRulesTablesAndEffects()
    {
        var valid = new CampaignCombatResultFacts(-2, 11, 36, 6, 25, 5, false, 1, "attacker", 75);
        Assert.Equal(25, valid.AttackerPercent);

        Assert.Throws<ArgumentException>(() => new CampaignCombatResultFacts(
            -2, 11, 36, 6, 0, 5, false, 1, "attacker", 75));
        Assert.Throws<ArgumentException>(() => new CampaignCombatResultFacts(
            -2, 11, 36, 6, 25, 0, false, 1, "attacker", 75));
        Assert.Throws<ArgumentException>(() => new CampaignCombatResultFacts(
            -2, 11, 36, 6, 25, 5, true, 1, "attacker", 75));
        Assert.Throws<ArgumentException>(() => new CampaignCombatResultFacts(
            -2, 11, 36, 6, 25, 5, false, 0, "attacker", 75));
        Assert.Throws<ArgumentException>(() => new CampaignCombatResultFacts(
            -2, 11, 36, null, 25, 5, false, 1, null, 0));
        Assert.Throws<ArgumentException>(() => new CampaignCombatResultFacts(
            -2, 11, 36, 6, 25, 5, false, 1, "attacker", 50));
    }

    [Fact]
    public void RoleLossMustUseRoleSpecificRoundingAndRefusalBound()
    {
        var component = new CampaignCombatComponentKey(
            new CampaignCombatUnitKey("fixture.creation-001", "axis", "axis-assault-battalion"),
            "axis-assault-battalion.toe.infantry");
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("attacker", component,
            10, 20, 0, 0, 0, 0, 10, 0));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("defender", component,
            10, 20, 0, 3, 0, 3, 7, 3));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("attacker", component,
            10, 20, 10, 3, 0, 3, 7, 3));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRoleLoss("defender", component,
            10, 0, 20, 2, 0, 2, 8, 0));

        Assert.Equal(3, new CampaignCombatRoleLoss("defender", component,
            10, 20, 10, 3, 0, 3, 7, 3).LossToe);
    }

    [Fact]
    public void RetreatReceiptMustChargeExactCpAndImmediateEffects()
    {
        var before = new CapabilityPointAmount(10, 1);
        var after = new CapabilityPointAmount(11, 1);
        var valid = new CampaignCombatRetreatReceipt("settlement.001.retreat", "settlement.001.losses",
            "retreat", ["assault-east", "commonwealth-rear"], 1, before, after, 1, 3);
        Assert.Equal(1, valid.ExcessCpDp);

        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatReceipt(
            "settlement.001.retreat", "settlement.001.losses", "retreat",
            ["assault-east", "commonwealth-rear"], 1, before, before, 0, 3));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatReceipt(
            "settlement.001.retreat", "settlement.001.losses", "retreat",
            ["assault-east", "commonwealth-rear"], 1, before, after, 0, 3));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatReceipt(
            "settlement.001.retreat", "settlement.001.losses", "retreat",
            ["assault-east", "commonwealth-rear"], 1, before, after, 1, 0));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatReceipt(
            "settlement.001.retreat", "settlement.001.losses", "not-required",
            ["assault-east"], 0, before, after, 1, 0));
    }

    [Fact]
    public void UnguardedEscapeRouteUsesClearTerrainCpNotEdgeCount()
    {
        var fourEdges = new CampaignCombatCustodyReceipt("settlement.001.custody", "settlement.001.retreat",
            "lot.001", "leave-unguarded", ["a", "b", "c", "d", "e"], null, "entitlement.001", 10, 10);
        Assert.Equal(5, fourEdges.Route.Count);

        Assert.Throws<ArgumentException>(() => new CampaignCombatCustodyReceipt(
            "settlement.001.custody", "settlement.001.retreat", "lot.001", "leave-unguarded",
            ["a", "b", "c", "d", "e", "f"], null, "entitlement.001", 10, 10));
    }

    [Fact]
    public void VictoryRecoveryCauseAddsCohesionAndCapsAtTen()
    {
        var recovered = new CampaignCombatCohesionCause("settlement.001.assault-victory-rp.axis", 1,
            "settlement.001.retreat", "axis-assault-battalion", 1, 1,
            "assault-victory-rp", 3, -3, 0);
        var capped = new CampaignCombatCohesionCause("settlement.002.assault-victory-rp.axis", 2,
            "settlement.002.retreat", "axis-assault-battalion", 1, 1,
            "assault-victory-rp", 3, 9, 10);
        var alreadyCapped = new CampaignCombatCohesionCause("settlement.003.assault-victory-rp.axis", 3,
            "settlement.003.retreat", "axis-assault-battalion", 1, 1,
            "assault-victory-rp", 3, 10, 10);

        Assert.Equal(0, recovered.After);
        Assert.Equal(10, capped.After);
        Assert.Equal(10, alreadyCapped.After);
        Assert.Throws<ArgumentException>(() => new CampaignCombatCohesionCause(
            "settlement.003.assault-victory-rp.axis", 3, "settlement.003.retreat",
            "axis-assault-battalion", 1, 1, "assault-victory-rp", 3, 9, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignCombatCohesionCause(
            "settlement.003.assault-victory-rp.axis", 3, "settlement.003.retreat",
            "axis-assault-battalion", 1, 1, "assault-victory-rp", 3, 9, 12));
    }

    [Fact]
    public void SettlementRequiresEveryReceiptPrefixEvenWhenRetreatIsNotRequired()
    {
        var world = InitialWorld();
        var attacker = new CampaignCombatUnitKey(world.CreationBinding, "axis", "axis-assault-battalion");
        var defender = new CampaignCombatUnitKey(world.CreationBinding, "commonwealth", "commonwealth-assault-battalion");
        var result = new CampaignCombatResultFacts(0, 44, 44, null, 5, 5, false, 0, null, 0);
        var losses = new CampaignCombatLossReceipt("settlement.001.losses", "settlement.001.disposition",
            [new CampaignCombatRoleLoss("attacker", new CampaignCombatComponentKey(attacker,
                "axis-assault-battalion.toe.infantry"), 10, 5, 0, 1, 0, 1, 9, 0),
             new CampaignCombatRoleLoss("defender", new CampaignCombatComponentKey(defender,
                "commonwealth-assault-battalion.toe.infantry"), 10, 5, 0, 0, 0, 0, 10, 0)]);
        var disposition = new CampaignCombatRetreatDisposition("settlement.001.disposition",
            "settlement.001.result", "not-required", 0, 0, 0, ["assault-east"]);
        var relationships = new CampaignCombatRelationshipsReceipt("settlement.001.relationships",
            "settlement.001.retreat", null, null);

        var lossesWithoutDisposition = new CampaignCombatLossReceipt("settlement.001.losses",
            "settlement.001.result", losses.Roles);
        Assert.Throws<ArgumentException>(() => new CampaignCombatSettlementState("settlement.001",
            "settlement.001.commit", "settlement.001.result", 1, 1, attacker, defender,
            world.Elements, result, null, lossesWithoutDisposition, null, null, null));
        var relationshipsWithoutRetreat = new CampaignCombatRelationshipsReceipt(
            "settlement.001.relationships", "settlement.001.losses", null, null);
        Assert.Throws<ArgumentException>(() => new CampaignCombatSettlementState("settlement.001",
            "settlement.001.commit", "settlement.001.result", 1, 1, attacker, defender,
            world.Elements, result, disposition, losses, null, null, relationshipsWithoutRetreat));

        var retreat = new CampaignCombatRetreatReceipt("settlement.001.retreat",
            "settlement.001.losses", "not-required", ["assault-east"], 0,
            CapabilityPointAmount.Zero, CapabilityPointAmount.Zero, 0, 0);
        var complete = new CampaignCombatSettlementState("settlement.001",
            "settlement.001.commit", "settlement.001.result", 1, 1, attacker, defender,
            world.Elements, result, disposition, losses, retreat, null, relationships);
        Assert.NotNull(complete.Relationships);
    }

    [Fact]
    public void SettlementLossesMustBindResultCaptureAndRetreatRefusal()
    {
        var world = InitialWorld();
        var attacker = new CampaignCombatUnitKey(world.CreationBinding, "axis", "axis-assault-battalion");
        var defender = new CampaignCombatUnitKey(world.CreationBinding, "commonwealth", "commonwealth-assault-battalion");
        var attackerComponent = new CampaignCombatComponentKey(attacker, "axis-assault-battalion.toe.infantry");
        var defenderComponent = new CampaignCombatComponentKey(defender, "commonwealth-assault-battalion.toe.infantry");
        var result = new CampaignCombatResultFacts(-2, 11, 36, 6, 25, 5, false, 1, "attacker", 75);
        var disposition = new CampaignCombatRetreatDisposition("settlement.001.disposition",
            "settlement.001.result", "retreat", 1, 1, 0, ["assault-east", "commonwealth-rear"]);
        var attackerLoss = new CampaignCombatRoleLoss("attacker", attackerComponent, 10, 25, 0,
            3, 3, 0, 7, 3);
        var defenderLoss = new CampaignCombatRoleLoss("defender", defenderComponent, 10, 5, 0,
            0, 0, 0, 10, 0);
        CampaignCombatLossReceipt Receipt(CampaignCombatRoleLoss first, CampaignCombatRoleLoss second) =>
            new("settlement.001.losses", "settlement.001.disposition", [first, second]);
        CampaignCombatSettlementState Settlement(CampaignCombatRetreatDisposition choice, CampaignCombatLossReceipt losses) =>
            new("settlement.001", "settlement.001.commit", "settlement.001.result", 1, 1,
                attacker, defender, world.Elements, result, choice, losses, null, null, null);

        Assert.NotNull(Settlement(disposition, Receipt(attackerLoss, defenderLoss)).Losses);
        Assert.Throws<ArgumentException>(() => Settlement(disposition, Receipt(
            new CampaignCombatRoleLoss("attacker", attackerComponent, 10, 25, 0, 3, 2, 1, 7, 3),
            defenderLoss)));
        Assert.Throws<ArgumentException>(() => Settlement(disposition, Receipt(
            new CampaignCombatRoleLoss("attacker", attackerComponent, 10, 20, 0, 2, 2, 0, 8, 0),
            defenderLoss)));

        var refusal = new CampaignCombatRetreatDisposition("settlement.001.disposition",
            "settlement.001.result", "refuse-retreat", 1, 0, 1, ["assault-east"]);
        Assert.Throws<ArgumentException>(() => Settlement(refusal, Receipt(attackerLoss, defenderLoss)));
        var refusalLoss = new CampaignCombatRoleLoss("defender", defenderComponent, 10, 5, 10,
            1, 0, 1, 9, 0);
        Assert.NotNull(Settlement(refusal, Receipt(attackerLoss, refusalLoss)).Losses);
    }

    [Fact]
    public void RetreatDispositionKindDeterminesTheOnlyAllowedDistances()
    {
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatDisposition(
            "settlement.001.disposition", "settlement.001.result", "not-required", 1, 1, 0,
            ["assault-east", "commonwealth-rear"]));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatDisposition(
            "settlement.001.disposition", "settlement.001.result", "refuse-retreat", 1, 1, 0,
            ["assault-east", "commonwealth-rear"]));
        Assert.Throws<ArgumentException>(() => new CampaignCombatRetreatDisposition(
            "settlement.001.disposition", "settlement.001.result", "retreat", 1, 0, 1,
            ["assault-east"]));
    }

    [Fact]
    public void ConsecutiveExcessSpendsAppendDistinctOrderedCauses()
    {
        var world = InitialWorld();
        var element = Assert.Single(world.Elements, value => value.ElementId == "axis-assault-battalion");
        var atTen = new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(10, 1),
            0, null, null, element.OperationalState.InitialLedgerOrigin);

        var first = CampaignCombatSpending.ChargeOrdinary(atTen, new CapabilityPointAmount(1, 1),
            10, CampaignCombatSpendCeiling.Ordinary, element.ElementId, "move.11", []);
        var suppliedHistory = first.Causes.ToArray();
        var second = CampaignCombatSpending.ChargeOrdinary(first.State, new CapabilityPointAmount(1, 1),
            10, CampaignCombatSpendCeiling.Ordinary, element.ElementId, "move.12", suppliedHistory);
        suppliedHistory[0] = second.Cause!;

        Assert.Equal(1, Assert.IsType<CampaignCombatCohesionCause>(first.Cause).Ordinal);
        Assert.Equal(2, Assert.IsType<CampaignCombatCohesionCause>(second.Cause).Ordinal);
        Assert.Equal("move.11.dp", first.Cause!.CauseId);
        Assert.Equal("move.12.dp", second.Cause!.CauseId);
        Assert.Equal("ordinary-movement-excess-cp-dp", second.Cause.Kind);
        Assert.Equal(2, second.Causes.Count);
        Assert.Equal("move.11.dp", second.Causes[0].CauseId);
        Assert.Throws<ArgumentException>(() => CampaignCombatSpending.ChargeOrdinary(first.State,
            new CapabilityPointAmount(1, 1), 10, CampaignCombatSpendCeiling.Ordinary,
            element.ElementId, "move.11", first.Causes));

        var updated = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId,
            element.ReserveStatus, second.State, element.Components,
            element.SourceParentFormationId, element.CurrentParentFormationId,
            element.Ammunition, element.Readiness);
        var replay = new CampaignWorldSnapshotV7(7, world.CreationBinding,
            world.Elements.Select(value => value.ElementId == element.ElementId ? updated : value),
            world.Representations, [], second.Causes, [], [], [], [], [], []);
        Assert.Equal(2, replay.CohesionCauses.Count);
    }

    private static CampaignWorldSnapshotV7 WithLots(CampaignWorldSnapshotV7 world,
        IEnumerable<CampaignCombatCustodyLot> lots, IEnumerable<CampaignCombatGuardAsset> guards) => new(
        7, world.CreationBinding, world.Elements, world.Representations, [], [], [], lots, guards, [], [], []);

    private static CampaignWorldSnapshotV7 InitialWorld()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        return CampaignWorldV7Factory.CreateInitial(artifact, scenario, InitialPolicy(scenario),
            "fixture.creation-001");
    }

    private static CampaignCombatInitializationPolicy InitialPolicy(ContentCombatScenario scenario) => new(
        1,
        scenario.Start.GameTurn,
        scenario.Start.OperationStage,
        CapabilityPointAmount.Zero,
        0,
        CampaignElementReserveStatus.None,
        new ContentOrigin(ContentOriginKind.Synthetic,
            [new RuleReference("sandtable-rules-lab", "combat.close-assault-positive.v1:initial-ledger")]));
}
