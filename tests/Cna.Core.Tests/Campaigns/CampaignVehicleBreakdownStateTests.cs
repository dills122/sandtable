using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignVehicleBreakdownStateTests
{
    [Fact]
    public void StateRequiresExactCoherentBreakdownContinuity()
    {
        var state = new CampaignVehicleBreakdownState(
            "axis-element-a.truck-cohort",
            new BreakdownPointAmount(21, 1),
            BreakdownPointAmount.Zero,
            "land.breakdown.band.4-10",
            9,
            1);

        Assert.Equal("axis-element-a.truck-cohort", state.CohortId);
        Assert.Equal(new BreakdownPointAmount(21, 1), state.CumulativeBreakdownPoints);
        Assert.Equal(
            BreakdownPointAmount.Zero,
            state.SandstormAttributedBreakdownPoints);
        Assert.Equal("land.breakdown.band.4-10", state.HighestEffectiveCheckedBandId);
        Assert.Equal(9, state.WorkingPointCount);
        Assert.Equal(1, state.BrokenPointCount);
    }

    [Fact]
    public void StateRejectsMalformedOrIncoherentContinuity()
    {
        Assert.Throws<ArgumentException>(() => State(cohortId: "Invalid ID"));
        Assert.Throws<ArgumentOutOfRangeException>(() => State(
            cumulative: new BreakdownPointAmount(-1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => State(
            sandstorm: new BreakdownPointAmount(-1, 1)));
        Assert.Throws<ArgumentException>(() => State(
            cumulative: new BreakdownPointAmount(1, 2),
            sandstorm: new BreakdownPointAmount(1, 1)));
        Assert.Throws<ArgumentException>(() => State(highestBandId: "unknown-band"));
        Assert.Throws<ArgumentException>(() => State(
            highestBandId: "land.breakdown.band.0-3"));
        Assert.Throws<ArgumentOutOfRangeException>(() => State(workingPointCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => State(brokenPointCount: -1));
    }

    [Fact]
    public void WorldV4CanonicallyRoundTripsOptionalBreakdownState()
    {
        var world = World(new CampaignVehicleBreakdownState(
            "axis-element-a.truck-cohort",
            new BreakdownPointAmount(21, 1),
            BreakdownPointAmount.Zero,
            "land.breakdown.band.4-10",
            9,
            1));

        var canonical = SerializeWorld(world);
        var parsed = ParseWorld(canonical);

        Assert.Equal(
            "{\"world\":{\"contractVersion\":4,\"elements\":[{" +
            "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"," +
            "\"reserveStatus\":\"none\",\"operationalState\":{" +
            "\"ledgerGameTurn\":1,\"ledgerOperationStage\":1," +
            "\"capabilityPointsExpended\":{\"numerator\":0,\"denominator\":1}," +
            "\"cohesionLevel\":0,\"vehicleBreakdownState\":{" +
            "\"cohortId\":\"axis-element-a.truck-cohort\"," +
            "\"cumulativeBreakdownPoints\":{\"numerator\":21,\"denominator\":1}," +
            "\"sandstormAttributedBreakdownPoints\":{\"numerator\":0,\"denominator\":1}," +
            "\"highestEffectiveCheckedBandId\":\"land.breakdown.band.4-10\"," +
            "\"workingPointCount\":9,\"brokenPointCount\":1}}" +
            "}],\"representations\":[{" +
            "\"representationId\":\"map-representation.0001\"," +
            "\"currentLocationId\":\"west\",\"bindingKind\":\"independent-element\"," +
            "\"boundElementIds\":[\"axis-element-a\"]}]}}",
            Encoding.UTF8.GetString(canonical));
        Assert.Equal(world, parsed);
    }

    [Fact]
    public void WorldV4WritesCanonicalNullForAnElementWithoutAVehicleCohort()
    {
        var canonical = SerializeWorld(World(null));

        Assert.Contains("\"vehicleBreakdownState\":null", Encoding.UTF8.GetString(canonical));
        Assert.Equal(World(null), ParseWorld(canonical));
    }

    [Fact]
    public void WorldReaderRejectsLegacyMalformedUnknownAndNoncanonicalBreakdownState()
    {
        var canonical = Encoding.UTF8.GetString(SerializeWorld(World(
            new CampaignVehicleBreakdownState(
                "axis-element-a.truck-cohort",
                new BreakdownPointAmount(21, 1),
                BreakdownPointAmount.Zero,
                "land.breakdown.band.4-10",
                9,
                1))));

        var legacy = canonical.Replace(
            "\"contractVersion\":4",
            "\"contractVersion\":3",
            StringComparison.Ordinal);
        var missing = canonical.Replace(
            ",\"vehicleBreakdownState\":{",
            ",\"missingVehicleBreakdownState\":{",
            StringComparison.Ordinal);
        var extra = canonical.Replace(
            "\"cohortId\":",
            "\"extra\":true,\"cohortId\":",
            StringComparison.Ordinal);
        var noncanonicalRational = canonical.Replace(
            "\"numerator\":21,\"denominator\":1",
            "\"numerator\":42,\"denominator\":2",
            StringComparison.Ordinal);
        var subtotalAboveTotal = canonical.Replace(
            "\"sandstormAttributedBreakdownPoints\":{\"numerator\":0,\"denominator\":1}",
            "\"sandstormAttributedBreakdownPoints\":{\"numerator\":22,\"denominator\":1}",
            StringComparison.Ordinal);
        var unknownBand = canonical.Replace(
            "\"land.breakdown.band.4-10\"",
            "\"breakdown-band.unknown\"",
            StringComparison.Ordinal);

        Assert.ThrowsAny<Exception>(() => ParseWorld(Encoding.UTF8.GetBytes(legacy)));
        Assert.Throws<JsonException>(() => ParseWorld(Encoding.UTF8.GetBytes(missing)));
        Assert.Throws<JsonException>(() => ParseWorld(Encoding.UTF8.GetBytes(extra)));
        Assert.Throws<JsonException>(() => ParseWorld(Encoding.UTF8.GetBytes(noncanonicalRational)));
        Assert.Throws<JsonException>(() => ParseWorld(Encoding.UTF8.GetBytes(subtotalAboveTotal)));
        Assert.Throws<JsonException>(() => ParseWorld(Encoding.UTF8.GetBytes(unknownBand)));
    }

    [Fact]
    public void CreationSeedsContentCohortsAndLeavesNonmotorizedElementsCanonicalNull()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");

        var world = CampaignWorldFactory.CreateInitial(artifact, scenario);

        foreach (var contentElement in artifact.Definition.Elements)
        {
            var state = world.Elements.Single(
                element => element.ElementId == contentElement.ElementId)
                .OperationalState.VehicleBreakdownState;
            var cohort = contentElement.BreakdownVehicleCohort;

            if (cohort is null)
            {
                Assert.Null(state);
                continue;
            }

            Assert.NotNull(state);
            Assert.Equal(cohort.CohortId, state.CohortId);
            Assert.Equal(BreakdownPointAmount.Zero, state.CumulativeBreakdownPoints);
            Assert.Equal(BreakdownPointAmount.Zero, state.SandstormAttributedBreakdownPoints);
            Assert.Null(state.HighestEffectiveCheckedBandId);
            Assert.Equal(cohort.WorkingPointCount, state.WorkingPointCount);
            Assert.Equal(0, state.BrokenPointCount);
        }

        Assert.True(CampaignWorldValidator.IsValidInitial(world, artifact, scenario));
    }

    [Fact]
    public void ContextAdmissionRejectsMissingExtraMismatchedAndCountForgedCohorts()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");
        var initial = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var cohortElement = initial.Elements.First(
            element => element.OperationalState.VehicleBreakdownState is not null);
        var noCohortElement = initial.Elements.First(
            element => element.OperationalState.VehicleBreakdownState is null);
        var state = cohortElement.OperationalState.VehicleBreakdownState!;

        var missing = ReplaceBreakdown(initial, cohortElement, null);
        var extra = ReplaceBreakdown(initial, noCohortElement, State());
        var mismatchedId = ReplaceBreakdown(
            initial,
            cohortElement,
            State(
                cohortId: "foreign.truck-cohort",
                workingPointCount: state.WorkingPointCount));
        var mismatchedCount = ReplaceBreakdown(
            initial,
            cohortElement,
            State(
                cohortId: state.CohortId,
                workingPointCount: state.WorkingPointCount + 1));

        Assert.False(CampaignWorldValidator.IsValidInitial(missing, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(extra, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(mismatchedId, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(mismatchedCount, artifact, scenario));
    }

    [Fact]
    public void CurrentPreMovementCheckpointsRejectUnauthorizedBreakdownMutation()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");
        var initial = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var cohortElement = initial.Elements.First(
            element => element.OperationalState.VehicleBreakdownState is not null);
        var initialState = cohortElement.OperationalState.VehicleBreakdownState!;
        var afterCheck = ReplaceBreakdown(
            initial,
            cohortElement,
            new CampaignVehicleBreakdownState(
                initialState.CohortId,
                new BreakdownPointAmount(21, 1),
                BreakdownPointAmount.Zero,
                "land.breakdown.band.4-10",
                0,
                1));

        Assert.False(CampaignWorldValidator.IsValidInitial(afterCheck, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidReserveDesignation(
            afterCheck,
            artifact,
            scenario,
            LandSide.Axis));
    }

    [Fact]
    public void CreationReplayRejectsAContentCohortMismatch()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var creation = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-breakdown-forgery",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));
        var created = Assert.IsType<CampaignCreated>(Assert.Single(creation.Events));
        var cohortElement = created.InitialWorld.Elements.First(
            element => element.OperationalState.VehicleBreakdownState is not null);
        var forged = created with
        {
            InitialWorld = ReplaceBreakdown(created.InitialWorld, cohortElement, null),
        };

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([forged]));
    }

    [Fact]
    public void CreationReplayIsByteIdenticalAndTheOpeningPreamblePreservesBreakdownState()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var creation = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-breakdown-continuity",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));
        var created = Assert.IsType<CampaignCreated>(Assert.Single(creation.Events));
        var initial = CampaignTestHarness.Apply(null, created);

        var initiativeDecision = CampaignTestHarness.Decide(
            initial,
            new ResolveInitiative(
                initial.StateVersion,
                initial.SequencePosition.PositionId));
        var initiative = Assert.IsType<InitiativeDetermined>(
            Assert.Single(initiativeDecision.Events));
        var afterInitiative = CampaignTestHarness.Apply(initial, initiative);
        var replayed = CampaignTestHarness.Replay([created, initiative]);

        Assert.Equal(initial.World, afterInitiative.World);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(afterInitiative),
            CampaignSnapshotSerializer.Serialize(replayed));
        Assert.Equal(
            CampaignEventSerializer.Serialize(created),
            CampaignEventSerializer.Serialize(
                CampaignEventSerializer.Deserialize(CampaignEventSerializer.Serialize(created))));
    }

    private static CampaignVehicleBreakdownState State(
        string cohortId = "axis-element-a.truck-cohort",
        BreakdownPointAmount? cumulative = null,
        BreakdownPointAmount? sandstorm = null,
        string? highestBandId = null,
        int workingPointCount = 10,
        int brokenPointCount = 0) => new(
            cohortId,
            cumulative ?? BreakdownPointAmount.Zero,
            sandstorm ?? BreakdownPointAmount.Zero,
            highestBandId,
            workingPointCount,
            brokenPointCount);

    private static CampaignWorldSnapshot World(CampaignVehicleBreakdownState? breakdown) => new(
        CampaignWorldSnapshot.CurrentContractVersion,
        [new CampaignElementState(
            "axis-element-a",
            "west",
            CampaignElementReserveStatus.None,
            new CampaignElementOperationalState(
                1,
                1,
                CapabilityPointAmount.Zero,
                0,
                breakdown))],
        [new CampaignMapRepresentationState(
            "map-representation.0001",
            "west",
            CampaignMapRepresentationBindingKind.IndependentElement,
            ["axis-element-a"])]);

    private static CampaignWorldSnapshot ReplaceBreakdown(
        CampaignWorldSnapshot world,
        CampaignElementState target,
        CampaignVehicleBreakdownState? replacement) => new(
            CampaignWorldSnapshot.CurrentContractVersion,
            world.Elements.Select(element => element.ElementId == target.ElementId
                ? new CampaignElementState(
                    element.ElementId,
                    element.CurrentLocationId,
                    element.ReserveStatus,
                    new CampaignElementOperationalState(
                        element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage,
                        element.OperationalState.CapabilityPointsExpended,
                        element.OperationalState.CohesionLevel,
                        replacement))
                : element).ToArray(),
            world.Representations);

    private static byte[] SerializeWorld(CampaignWorldSnapshot world)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignSnapshotSerializer.WriteWorld(writer, "world", world);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static CampaignWorldSnapshot ParseWorld(ReadOnlyMemory<byte> canonical)
    {
        using var document = JsonDocument.Parse(canonical);
        return CampaignSnapshotSerializer.ParseWorld(document.RootElement.GetProperty("world"));
    }
}
