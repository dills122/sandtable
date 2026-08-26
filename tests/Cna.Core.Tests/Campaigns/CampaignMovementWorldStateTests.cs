using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementWorldStateTests
{
    [Fact]
    public void InitialWorldSeedsExactOperationalLedgerAndOpaqueRepresentations()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");

        var world = CampaignWorldFactory.CreateInitial(artifact, scenario);

        Assert.Equal(3, world.ContractVersion);
        Assert.All(world.Elements, element =>
        {
            Assert.Equal(scenario.Start.GameTurn, element.OperationalState.LedgerGameTurn);
            Assert.Equal(
                scenario.Start.OperationStage,
                element.OperationalState.LedgerOperationStage);
            Assert.Equal(CapabilityPointAmount.Zero, element.OperationalState.CapabilityPointsExpended);
            Assert.Equal(0, element.OperationalState.CohesionLevel);
        });
        Assert.Equal(
            [
                ("map-representation.0001", "west", "axis-element-a"),
                ("map-representation.0002", "north-west", "axis-element-b"),
                ("map-representation.0003", "east", "commonwealth-element-a"),
                ("map-representation.0004", "south-east", "commonwealth-element-b"),
            ],
            world.Representations.Select(representation => (
                representation.RepresentationId,
                representation.CurrentLocationId,
                Assert.Single(representation.BoundElementIds))));
        Assert.All(world.Representations, representation => Assert.Equal(
            CampaignMapRepresentationBindingKind.IndependentElement,
            representation.BindingKind));
        Assert.DoesNotContain(
            world.Representations,
            representation => representation.BoundElementIds.Any(
                elementId => representation.RepresentationId.Contains(
                    elementId,
                    StringComparison.Ordinal)));
        Assert.True(CampaignWorldValidator.IsValidInitial(world, artifact, scenario));
    }

    [Fact]
    public void WorldV3CanonicallyRoundTripsLedgerAndInternalBinding()
    {
        var operational = new CampaignElementOperationalState(
            1,
            1,
            new CapabilityPointAmount(1, 2),
            -1);
        var world = new CampaignWorldSnapshot(
            3,
            [new CampaignElementState(
                "axis-element-a",
                "west",
                CampaignElementReserveStatus.ReserveI,
                operational)],
            [new CampaignMapRepresentationState(
                "map-representation.0001",
                "west",
                CampaignMapRepresentationBindingKind.IndependentElement,
                ["axis-element-a"])]);

        var canonical = SerializeWorld(world);
        var parsed = ParseWorld(canonical);

        Assert.Equal(
            "{\"world\":{\"contractVersion\":3,\"elements\":[{" +
            "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"," +
            "\"reserveStatus\":\"reserve-i\",\"operationalState\":{" +
            "\"ledgerGameTurn\":1,\"ledgerOperationStage\":1," +
            "\"capabilityPointsExpended\":{\"numerator\":1,\"denominator\":2}," +
            "\"cohesionLevel\":-1}}],\"representations\":[{" +
            "\"representationId\":\"map-representation.0001\"," +
            "\"currentLocationId\":\"west\",\"bindingKind\":\"independent-element\"," +
            "\"boundElementIds\":[\"axis-element-a\"]}]}}",
            Encoding.UTF8.GetString(canonical));
        Assert.Equal(world, parsed);
    }

    [Fact]
    public void InitialValidationRejectsForgedLedgerBindingAndRepresentationLocation()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");
        var baseline = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var element = baseline.Elements[0];
        var representation = baseline.Representations.Single(
            candidate => candidate.BoundElementIds.Contains(element.ElementId));

        var wrongLedger = ReplaceElement(
            baseline,
            new CampaignElementState(
                element.ElementId,
                element.CurrentLocationId,
                element.ReserveStatus,
                new CampaignElementOperationalState(
                    2,
                    1,
                    CapabilityPointAmount.Zero,
                    0)));
        var nonzeroExpenditure = ReplaceElement(
            baseline,
            new CampaignElementState(
                element.ElementId,
                element.CurrentLocationId,
                element.ReserveStatus,
                new CampaignElementOperationalState(
                    1,
                    1,
                    new CapabilityPointAmount(1, 2),
                    0)));
        var wrongRepresentationLocation = ReplaceRepresentation(
            baseline,
            new CampaignMapRepresentationState(
                representation.RepresentationId,
                "east",
                representation.BindingKind,
                representation.BoundElementIds));
        var duplicatedBinding = new CampaignWorldSnapshot(
            3,
            baseline.Elements,
            [
                .. baseline.Representations,
                new CampaignMapRepresentationState(
                    "map-representation.9999",
                    element.CurrentLocationId,
                    CampaignMapRepresentationBindingKind.IndependentElement,
                    [element.ElementId]),
            ]);
        var missingRepresentation = new CampaignWorldSnapshot(
            3,
            baseline.Elements,
            baseline.Representations.Skip(1).ToArray());

        Assert.False(CampaignWorldValidator.IsValidInitial(wrongLedger, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(nonzeroExpenditure, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(
            wrongRepresentationLocation,
            artifact,
            scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(duplicatedBinding, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(missingRepresentation, artifact, scenario));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignElementOperationalState(
            1,
            1,
            CapabilityPointAmount.Zero,
            11));
    }

    [Fact]
    public void CreationHistoryAndSnapshotUseTheNewCompleteCanonicalContracts()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var execution = CampaignTestHarness.Execute(
        [
            CampaignTestHarness.Create(
                "campaign-movement-state",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash),
        ]);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(execution.Events));
        var snapshot = Assert.IsType<CampaignSnapshot>(execution.Snapshot);
        var eventBytes = CampaignEventSerializer.Serialize(created);
        var snapshotBytes = CampaignSnapshotSerializer.Serialize(snapshot);

        Assert.Equal(7, created.ContractVersion);
        Assert.Equal(8, snapshot.ContractVersion);
        Assert.Equal(created, CampaignEventSerializer.Deserialize(eventBytes));
        Assert.Equal(snapshot, CampaignSnapshotSerializer.Deserialize(snapshotBytes));
        Assert.Contains("\"representations\":[", Encoding.UTF8.GetString(eventBytes));
        Assert.Contains("\"operationalState\":{", Encoding.UTF8.GetString(snapshotBytes));

        var noncanonicalAmount = Encoding.UTF8.GetString(eventBytes).Replace(
            "\"capabilityPointsExpended\":{\"numerator\":0,\"denominator\":1}",
            "\"capabilityPointsExpended\":{\"numerator\":0,\"denominator\":2}",
            StringComparison.Ordinal);
        using var snapshotDocument = JsonDocument.Parse(snapshotBytes);
        var representationJson = snapshotDocument.RootElement
            .GetProperty("world")
            .GetProperty("representations")
            .GetRawText();
        var missingRepresentations = Encoding.UTF8.GetString(snapshotBytes).Replace(
            $",\"representations\":{representationJson}",
            string.Empty,
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(noncanonicalAmount)));
        Assert.Throws<JsonException>(() => CampaignSnapshotSerializer.Deserialize(
            Encoding.UTF8.GetBytes(missingRepresentations)));
    }

    [Fact]
    public void NewWorldContractsContainOnlyTheFrozenAuthorityFields()
    {
        Assert.Equal(
            [
                "CapabilityPointsExpended",
                "CohesionLevel",
                "LedgerGameTurn",
                "LedgerOperationStage",
            ],
            PropertyNames<CampaignElementOperationalState>());
        Assert.Equal(
            [
                "BindingKind",
                "BoundElementIds",
                "CurrentLocationId",
                "RepresentationId",
            ],
            PropertyNames<CampaignMapRepresentationState>());
    }

    private static string[] PropertyNames<T>() => typeof(T)
        .GetProperties()
        .Select(property => property.Name)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static CampaignWorldSnapshot ReplaceElement(
        CampaignWorldSnapshot world,
        CampaignElementState replacement) => new(
            3,
            world.Elements
                .Where(element => element.ElementId != replacement.ElementId)
                .Append(replacement)
                .ToArray(),
            world.Representations);

    private static CampaignWorldSnapshot ReplaceRepresentation(
        CampaignWorldSnapshot world,
        CampaignMapRepresentationState replacement) => new(
            3,
            world.Elements,
            world.Representations
                .Where(value => value.RepresentationId != replacement.RepresentationId)
                .Append(replacement)
                .ToArray());

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
