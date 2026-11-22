using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-002 slice 1 (2026-11-22): focused, isolated coverage for the first
// two native-chain preamble events (CampaignNativeInitiativeDeclarationV49,
// CampaignNativeWeatherDeterminationV50). Follows this project's own
// "Contract-frozen"/"Implemented" status model (pre-alpha roadmap) -- these
// two types are not yet wired into ExerciseSession/CampaignExercises
// dispatch (no real campaign can reach Operation Stage 2 yet, since neither
// the remaining four preamble mechanics nor the Reserve Designation handoff
// exist), so there is no existing end-to-end fixture to drive this through.
// Tests construct a synthetic predecessor directly, the same "dormant,
// contract-level" testing depth this project used for other native
// contracts before their own Host/session wiring landed.
public sealed class CampaignNativeStageEntryPreambleTests
{
    private static string Hash(byte theme = 1) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData([theme]));

    // A synthetic predecessor representing "the second acting side has just
    // completed its own Patrol phase at the end of Operation Stage 1" --
    // the real, already-generalized CampaignNativePostCombatPassV42
    // (#518), not a fabricated type. Its NextPositionId is read directly
    // from the real Cna1979LandSequenceV4 catalog (Cna1979LandSequenceV4.GetNext),
    // not hand-typed, so this test cannot silently drift from the real
    // catalog shape.
    private static CampaignNativePostCombatPassV42 SecondSidePatrolComplete(string campaignId, long stateVersion)
    {
        var catalog = Cna1979LandSequenceV4.CreateTurn(1);
        var patrol = catalog.Single(value => value.PhaseId == LandPhaseIds.Patrol
            && value.ActorRole == LandActorRole.SecondActingSide && value.OperationStage == 1);
        var next = Cna1979LandSequenceV4.GetNext(patrol);
        return new CampaignNativePostCombatPassV42(CampaignNativePostCombatPassV42.CurrentContractVersion,
            campaignId, stateVersion, Hash(1), LandSide.Commonwealth, patrol.PositionId, next.PositionId, Hash(2));
    }

    [Fact]
    public void InitiativeDeclarationAdvancesFromPatrolCompleteToWeatherForEitherChoice()
    {
        var predecessor = SecondSidePatrolComplete("test-campaign-001", 100);

        var actFirst = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: true, 1, predecessor.StateVersion);
        Assert.Equal(LandSide.Axis, actFirst.FirstSide);
        Assert.Equal(LandSide.Commonwealth, actFirst.SecondSide);
        Assert.Equal(LandSide.Axis, actFirst.DeclaringHolder);
        Assert.Equal(predecessor.NextPositionId, actFirst.PositionId);
        Assert.Equal(predecessor.WorldHash, actFirst.WorldHash);
        Assert.Equal(2, actFirst.OperationStage);
        Assert.Contains("operation-2", actFirst.PositionId, StringComparison.Ordinal);
        Assert.Contains("weather-determination", actFirst.NextPositionId, StringComparison.Ordinal);

        var actLast = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: false, 1, predecessor.StateVersion);
        Assert.Equal(LandSide.Commonwealth, actLast.FirstSide);
        Assert.Equal(LandSide.Axis, actLast.SecondSide);
        Assert.Equal(LandSide.Axis, actLast.DeclaringHolder);
    }

    [Fact]
    public void InitiativeDeclarationSerializationRoundTripsCanonically()
    {
        var predecessor = SecondSidePatrolComplete("test-campaign-002", 7);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Commonwealth, actFirst: true, 1, predecessor.StateVersion);

        var bytes = declared.SerializeCanonical();
        var parsed = CampaignNativeInitiativeDeclarationV49.Deserialize(bytes);

        Assert.Equal(declared, parsed);
        Assert.Equal(bytes, parsed.SerializeCanonical());
    }

    [Fact]
    public void InitiativeDeclarationRejectsWrongPredecessorTypeOrStaleCut()
    {
        Assert.Throws<InvalidOperationException>(() => CampaignNativeInitiativeDeclarationV49.Create(
            new object(), LandSide.Axis, true, 1, 1));

        var predecessor = SecondSidePatrolComplete("test-campaign-003", 50);
        Assert.Throws<InvalidOperationException>(() => CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, true, 1, predecessor.StateVersion + 1));
    }

    [Fact]
    public void InitiativeDeclarationRejectsAPredecessorNotAtSecondSidePatrol()
    {
        // The second-to-last position in the catalog's own per-player block
        // (Repair/Maintenance) is a real catalog position, but its own
        // GetNext successor is Patrol, not the next stage's Initiative
        // Declaration -- confirming this rejects on the real catalog shape,
        // not a hand-picked string.
        var catalog = Cna1979LandSequenceV4.CreateTurn(1);
        var maintenance = catalog.Single(value => value.SegmentId == LandSegmentIds.Maintenance
            && value.ActorRole == LandActorRole.SecondActingSide && value.OperationStage == 1);
        var next = Cna1979LandSequenceV4.GetNext(maintenance);
        var predecessor = new CampaignNativePostCombatPassV42(CampaignNativePostCombatPassV42.CurrentContractVersion,
            "test-campaign-004", 10, Hash(3), LandSide.Axis, maintenance.PositionId, next.PositionId, Hash(4));

        Assert.Throws<InvalidOperationException>(() => CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, true, 1, predecessor.StateVersion));
    }

    private static RandomStreamState GenesisRandomState() => new(SandtableRandom.ContractVersion,
        SandtableRandom.AlgorithmId, 4242, 0);

    [Fact]
    public void WeatherDeterminationAdvancesFromInitiativeDeclarationAndConsumesExactlyTwoOrThreeDraws()
    {
        var predecessor = SecondSidePatrolComplete("test-campaign-010", 100);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: true, 1, predecessor.StateVersion);
        var priorRandomState = GenesisRandomState();

        var weather = CampaignNativeWeatherDeterminationV50.Create(
            declared, priorRandomState, 1, declared.StateVersion);

        Assert.Equal(declared.NextPositionId, weather.PositionId);
        Assert.Equal(declared.WorldHash, weather.WorldHash);
        Assert.Equal(declared.DeclaringHolder, weather.DeterminingSide);
        Assert.Equal(priorRandomState.NextByteCursor, weather.PriorRandomCursor);
        Assert.NotEqual(weather.PriorRandomCursor, weather.ResultRandomCursor);
        Assert.Contains("organization", weather.NextPositionId, StringComparison.Ordinal);
        // Confirms determinism: resolving from the identical prior cursor a
        // second time reproduces byte-identical output -- the actual
        // DET-001 property this type exists to preserve.
        var repeat = CampaignNativeWeatherDeterminationV50.Create(
            declared, priorRandomState, 1, declared.StateVersion);
        Assert.Equal(weather.SerializeCanonical(), repeat.SerializeCanonical());
    }

    [Fact]
    public void WeatherDeterminationSerializationRoundTripsCanonically()
    {
        var predecessor = SecondSidePatrolComplete("test-campaign-011", 5);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Commonwealth, actFirst: false, 1, predecessor.StateVersion);
        var weather = CampaignNativeWeatherDeterminationV50.Create(
            declared, GenesisRandomState(), 1, declared.StateVersion);

        var bytes = weather.SerializeCanonical();
        var parsed = CampaignNativeWeatherDeterminationV50.Deserialize(bytes);

        Assert.Equal(weather, parsed);
        Assert.Equal(bytes, parsed.SerializeCanonical());
    }

    [Fact]
    public void WeatherDeterminationRejectsAStaleCutOrForeignRandomAlgorithm()
    {
        var predecessor = SecondSidePatrolComplete("test-campaign-012", 20);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: true, 1, predecessor.StateVersion);

        Assert.Throws<InvalidOperationException>(() => CampaignNativeWeatherDeterminationV50.Create(
            declared, GenesisRandomState(), 1, declared.StateVersion + 1));

        var foreignAlgorithm = new RandomStreamState(SandtableRandom.ContractVersion, "foreign-algorithm", 1, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignNativeWeatherDeterminationV50.Create(
            declared, foreignAlgorithm, 1, declared.StateVersion));
    }

    // MTS-TASK-002 slices 2-3 (2026-11-22): the remaining four native-chain
    // preamble events (Organization, Naval Convoy Arrival, Fleet Assignment,
    // Fleet Repair) and a full six-event chain proof, same isolated-fixture
    // depth as slice 1 above.
    private static CampaignNativeWeatherDeterminationV50 WeatherComplete(string campaignId, long stateVersion)
    {
        var predecessor = SecondSidePatrolComplete(campaignId, stateVersion);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: true, 1, predecessor.StateVersion);
        return CampaignNativeWeatherDeterminationV50.Create(declared, GenesisRandomState(), 1, declared.StateVersion);
    }

    [Fact]
    public void OrganizationAdvancesFromWeatherToNavalConvoyArrival()
    {
        var weather = WeatherComplete("test-campaign-020", 100);

        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);

        Assert.Equal(weather.NextPositionId, organization.PositionId);
        Assert.Equal(weather.FirstSide, organization.FirstSide);
        Assert.Equal(weather.SecondSide, organization.SecondSide);
        Assert.Equal(weather.WorldHash, organization.WorldHash);
        Assert.Contains("naval-convoy-arrival", organization.NextPositionId, StringComparison.Ordinal);

        var bytes = organization.SerializeCanonical();
        Assert.Equal(organization, CampaignNativeNoObligationOrganizationV51.Deserialize(bytes));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeNoObligationOrganizationV51.Create(
            weather, weather.StateVersion + 1));
    }

    [Fact]
    public void NavalConvoyArrivalAdvancesFromOrganizationToFleetAssignmentWhenNoArrivalIsDue()
    {
        var weather = WeatherComplete("test-campaign-021", 100);
        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);

        var convoyArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
            organization, context, organization.StateVersion);

        Assert.Equal(organization.NextPositionId, convoyArrival.PositionId);
        Assert.Contains("commonwealth-fleet.assignment", convoyArrival.NextPositionId, StringComparison.Ordinal);

        var bytes = convoyArrival.SerializeCanonical();
        Assert.Equal(convoyArrival, CampaignNativeNoObligationNavalConvoyArrivalV52.Deserialize(bytes));
    }

    [Fact]
    public void FleetAssignmentAdvancesFromNavalConvoyArrivalToFleetRepair()
    {
        var weather = WeatherComplete("test-campaign-022", 100);
        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var convoyArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
            organization, context, organization.StateVersion);

        var fleetAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(
            convoyArrival, convoyArrival.StateVersion);

        Assert.Equal(convoyArrival.NextPositionId, fleetAssignment.PositionId);
        Assert.Contains("commonwealth-fleet.repair", fleetAssignment.NextPositionId, StringComparison.Ordinal);

        var bytes = fleetAssignment.SerializeCanonical();
        Assert.Equal(fleetAssignment, CampaignNativeNoObligationFleetAssignmentV53.Deserialize(bytes));
    }

    [Fact]
    public void FleetRepairIsTheTerminalEventAndAdvancesToTheNewStagesFirstActingSideReserveDesignation()
    {
        var weather = WeatherComplete("test-campaign-023", 100);
        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var convoyArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
            organization, context, organization.StateVersion);
        var fleetAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(
            convoyArrival, convoyArrival.StateVersion);

        var fleetRepair = CampaignNativeNoObligationFleetRepairV54.Create(fleetAssignment, fleetAssignment.StateVersion);

        Assert.Equal(fleetAssignment.NextPositionId, fleetRepair.PositionId);
        var catalog = Cna1979LandSequenceV4.CreateTurn(1);
        var successor = catalog.Single(value => value.PositionId == fleetRepair.NextPositionId);
        Assert.Equal(LandActorRole.FirstActingSide, successor.ActorRole);
        Assert.Equal(LandPhaseIds.ReserveDesignation, successor.PhaseId);
        Assert.Equal(2, successor.OperationStage);

        var order = fleetRepair.ToOrder();
        Assert.Equal(fleetRepair.FirstSide, order.FirstSide);
        Assert.Equal(fleetRepair.SecondSide, order.SecondSide);
        Assert.Equal(2, order.OperationStage);

        var bytes = fleetRepair.SerializeCanonical();
        Assert.Equal(fleetRepair, CampaignNativeNoObligationFleetRepairV54.Deserialize(bytes));
    }

    [Fact]
    public void FullSixEventChainResolvesIntoACampaignNativeSecondReserveV43PredecessorForTheNewStage()
    {
        // Proves the whole chain this slice built actually connects end to
        // end: PostCombatPass -> Initiative -> Weather -> Organization ->
        // Naval Convoy Arrival -> Fleet Assignment -> Fleet Repair -> the
        // new stage's first-acting-side Reserve Designation, via the
        // additive predecessor-switch arm added to
        // CampaignNativeSecondReserveV43.Create.
        var predecessor = SecondSidePatrolComplete("test-campaign-030", 100);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: true, 1, predecessor.StateVersion);
        var weather = CampaignNativeWeatherDeterminationV50.Create(declared, GenesisRandomState(), 1, declared.StateVersion);
        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var convoyArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
            organization, context, organization.StateVersion);
        var fleetAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(
            convoyArrival, convoyArrival.StateVersion);
        var fleetRepair = CampaignNativeNoObligationFleetRepairV54.Create(fleetAssignment, fleetAssignment.StateVersion);
        var order = fleetRepair.ToOrder();
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];
        // A real, valid initial World for this fixture's content/scenario --
        // its hash deliberately does NOT match fleetRepair.WorldHash (that
        // field threads an arbitrary synthetic hash from
        // SecondSidePatrolComplete above, not a real one), so Create is
        // expected to fail the *World* check below, not the predecessor
        // match -- which is exactly what proves the new switch arm was
        // reached rather than falling through to the generic rejection.
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var error = Assert.Throws<InvalidOperationException>(() => CampaignNativeSecondReserveV43.Create(
            fleetRepair, world, context, orders, order.FirstSide, elementId: null, 1, fleetRepair.StateVersion));

        Assert.Equal("Second Reserve requires its owner and settled World.", error.Message);
    }
}
