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
            CampaignCombatSpendCeiling.Ordinary, "axis-assault-battalion", "move.11");
        Assert.Equal(new CapabilityPointAmount(11, 1), eleven.State.CapabilityPointsExpended);
        Assert.Equal(-1, eleven.State.CohesionLevel);
        Assert.Equal(1, Assert.IsType<CampaignCombatCohesionCause>(eleven.Cause).Points);

        var fifteen = CampaignCombatSpending.ChargeOrdinary(atNine, new CapabilityPointAmount(6, 1), 10,
            CampaignCombatSpendCeiling.Ordinary, "axis-assault-battalion", "move.15");
        Assert.Equal(new CapabilityPointAmount(15, 1), fifteen.State.CapabilityPointsExpended);
        Assert.Equal(-5, fifteen.State.CohesionLevel);
        Assert.Equal(5, Assert.IsType<CampaignCombatCohesionCause>(fifteen.Cause).Points);

        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatSpending.ChargeOrdinary(
            atTen, new CapabilityPointAmount(6, 1), 10, CampaignCombatSpendCeiling.Ordinary,
            "axis-assault-battalion", "move.16"));

        var mandatory = CampaignCombatSpending.ChargeMandatoryRetreat(atTen, 10,
            "axis-assault-battalion", "retreat.11");
        Assert.Equal(new CapabilityPointAmount(11, 1), mandatory.State.CapabilityPointsExpended);
        Assert.Equal(-1, mandatory.State.CohesionLevel);
        Assert.Equal(1, Assert.IsType<CampaignCombatCohesionCause>(mandatory.Cause).Points);

        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatSpending.ChargeOrdinary(
            atNine, new CapabilityPointAmount(2, 1), 10, CampaignCombatSpendCeiling.ReleasedReserveI,
            "axis-assault-battalion", "move.reserve-i"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatSpending.ChargeOrdinary(
            atFive, new CapabilityPointAmount(1, 1), 10, CampaignCombatSpendCeiling.ReleasedReserveII,
            "axis-assault-battalion", "move.reserve-ii"));

        Assert.Throws<OverflowException>(() => CampaignCombatSpending.ChargeMandatoryRetreat(
            new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(long.MaxValue, 1),
                0, null, null, origin), 10, "axis-assault-battalion", "retreat.overflow"));
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
            10, CampaignCombatSpendCeiling.Ordinary, "axis-assault-battalion", "move.breakdown");

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
            "axis-assault-battalion", 1, 1, "excess-cp-dp", 1, 0, -1);
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
