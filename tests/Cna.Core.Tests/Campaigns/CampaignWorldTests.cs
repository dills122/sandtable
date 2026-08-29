using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignWorldTests
{
    [Fact]
    public void SnapshotDefensivelyCopiesSortsAndComparesElementsStructurally()
    {
        var input = new List<CampaignElementState>
        {
            Element("commonwealth-element-a", "east"),
            Element("axis-element-a", "west"),
        };
        var first = World(input);
        var equivalent = World(input.AsEnumerable().Reverse().ToArray());

        input.Clear();

        Assert.Equal(
            ["axis-element-a", "commonwealth-element-a"],
            first.Elements.Select(element => element.ElementId));
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void WorldV4CanonicallyRoundTripsEveryDefinedReserveStatus()
    {
        var world = World(
            [
                Element(
                    "axis-element-a",
                    "west",
                    CampaignElementReserveStatus.None),
                Element(
                    "axis-element-b",
                    "north-west",
                    CampaignElementReserveStatus.ReserveI),
                Element(
                    "commonwealth-element-a",
                    "east",
                    CampaignElementReserveStatus.ReserveII),
            ]);

        var canonical = SerializeWorld(world);
        var parsed = ParseWorld(canonical);

        Assert.Contains("\"contractVersion\":4", Encoding.UTF8.GetString(canonical));
        Assert.Contains("\"operationalState\":{", Encoding.UTF8.GetString(canonical));
        Assert.Contains("\"representations\":[", Encoding.UTF8.GetString(canonical));
        Assert.Equal(world, parsed);
        Assert.Equal(CampaignElementReserveStatus.ReserveI, parsed.Elements[1].ReserveStatus);
    }

    [Fact]
    public void SnapshotRejectsInvalidContractValuesAndDuplicateElements()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CampaignWorldSnapshot(2, [], []));
        Assert.Throws<ArgumentException>(
            () => new CampaignElementState(
                "Invalid ID",
                "west",
                CampaignElementReserveStatus.None,
                Operational()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignElementState(
            "axis-element-a",
            "west",
            (CampaignElementReserveStatus)99,
            Operational()));
        Assert.Throws<ArgumentException>(
            () => new CampaignWorldSnapshot(
                4,
                [
                    Element("axis-element-a", "west"),
                    Element("axis-element-a", "east"),
                ],
                []));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("ReserveI")]
    [InlineData("reserve_i")]
    public void WorldV4ParserRejectsUnknownOrNoncanonicalReserveStatus(string reserveStatus)
    {
        var canonical = SerializeWorld(World([Element("axis-element-a", "west")]));
        var changed = Encoding.UTF8.GetString(canonical).Replace(
            "\"reserveStatus\":\"none\"",
            $"\"reserveStatus\":\"{reserveStatus}\"",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ParseWorld(Encoding.UTF8.GetBytes(changed)));
    }

    [Fact]
    public void InitialProjectionExactlyMapsAndOrdersScenarioPlacements()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");

        var world = CampaignWorldFactory.CreateInitial(artifact, scenario);

        Assert.Equal(
            [
                ("axis-element-a", "west", CampaignElementReserveStatus.None),
                ("axis-element-b", "north-west", CampaignElementReserveStatus.None),
                ("commonwealth-element-a", "east", CampaignElementReserveStatus.None),
                ("commonwealth-element-b", "south-east", CampaignElementReserveStatus.None),
            ],
            world.Elements.Select(element => (
                element.ElementId,
                element.CurrentLocationId,
                element.ReserveStatus)));
        Assert.True(CampaignWorldValidator.IsValidInitial(world, artifact, scenario));
    }

    [Fact]
    public void InitialValidationRejectsMissingUnknownRelocatedAndInvalidLocationState()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");
        var baseline = CampaignWorldFactory.CreateInitial(artifact, scenario);

        var missing = new CampaignWorldSnapshot(
            4,
            baseline.Elements.Skip(1).ToArray(),
            baseline.Representations);
        var unknown = new CampaignWorldSnapshot(
            4,
            baseline.Elements.Append(Element("unknown-element", "west")).ToArray(),
            baseline.Representations);
        var relocated = Replace(
            baseline,
            Element("axis-element-a", "east"));
        var invalidLocation = Replace(
            baseline,
            Element("axis-element-a", "unknown-location"));
        var reserveI = Replace(
            baseline,
            Element(
                "axis-element-a",
                "west",
                CampaignElementReserveStatus.ReserveI));
        var reserveII = Replace(
            baseline,
            Element(
                "axis-element-a",
                "west",
                CampaignElementReserveStatus.ReserveII));

        Assert.False(CampaignWorldValidator.IsValidInitial(missing, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(unknown, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(relocated, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(invalidLocation, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(reserveI, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(reserveII, artifact, scenario));
    }

    [Fact]
    public void InitialValidationRejectsAttachmentOnlyElementsAndForeignScenarios()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var attachment = new ContentCombatElement(
            "axis-attachment",
            "axis",
            baseline.Formations[0].FormationId,
            "land.organization.battalion",
            Cna.Core.Rules.Cna1979Movement.NonMotorizedMobilityId,
            10,
            ContentPlacementMode.AttachmentOnly,
            ContentTestData.Origin("content.element.attachment"));
        var artifact = ContentPackArtifact.Create(ContentTestData.Copy(
            baseline,
            elements: baseline.Elements.Append(attachment)));
        var scenario = artifact.Definition.Scenarios[0];
        var initial = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var withAttachment = new CampaignWorldSnapshot(
            4,
            initial.Elements.Append(Element("axis-attachment", "west")).ToArray(),
            initial.Representations);
        var foreignScenario = Cna1979SyntheticContentCatalog.Artifact.Definition.Scenarios[0];

        Assert.False(CampaignWorldValidator.IsValidInitial(
            withAttachment,
            artifact,
            scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(
            initial,
            artifact,
            foreignScenario));
        Assert.Throws<ArgumentException>(
            () => CampaignWorldFactory.CreateInitial(artifact, foreignScenario));
    }

    private static CampaignWorldSnapshot Replace(
        CampaignWorldSnapshot world,
        CampaignElementState replacement) => new(
            4,
            world.Elements
                .Where(element => element.ElementId != replacement.ElementId)
                .Append(replacement)
                .ToArray(),
            world.Representations);

    private static CampaignElementOperationalState Operational() => new(
        1,
        1,
        CapabilityPointAmount.Zero,
        0);

    private static CampaignElementState Element(
        string elementId,
        string locationId,
        CampaignElementReserveStatus status = CampaignElementReserveStatus.None) => new(
            elementId,
            locationId,
            status,
            Operational());

    private static CampaignWorldSnapshot World(IEnumerable<CampaignElementState> elements)
    {
        var ordered = elements.OrderBy(element => element.ElementId, StringComparer.Ordinal).ToArray();
        return new CampaignWorldSnapshot(
            4,
            ordered,
            ordered.Select((element, index) => new CampaignMapRepresentationState(
                $"map-representation.{index + 1:D4}",
                element.CurrentLocationId,
                CampaignMapRepresentationBindingKind.IndependentElement,
                [element.ElementId])).ToArray());
    }

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
