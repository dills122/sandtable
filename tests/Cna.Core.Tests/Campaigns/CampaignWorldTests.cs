using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignWorldTests
{
    [Fact]
    public void SnapshotDefensivelyCopiesSortsAndComparesElementsStructurally()
    {
        var input = new List<CampaignElementState>
        {
            new("commonwealth-element-a", "east"),
            new("axis-element-a", "west"),
        };
        var first = new CampaignWorldSnapshot(2, input);
        var equivalent = new CampaignWorldSnapshot(2, input.AsEnumerable().Reverse().ToArray());

        input.Clear();

        Assert.Equal(
            ["axis-element-a", "commonwealth-element-a"],
            first.Elements.Select(element => element.ElementId));
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void WorldV2CanonicallyRoundTripsEveryDefinedReserveStatus()
    {
        var world = new CampaignWorldSnapshot(
            2,
            [
                new CampaignElementState(
                    "axis-element-a",
                    "west",
                    CampaignElementReserveStatus.None),
                new CampaignElementState(
                    "axis-element-b",
                    "north-west",
                    CampaignElementReserveStatus.ReserveI),
                new CampaignElementState(
                    "commonwealth-element-a",
                    "east",
                    CampaignElementReserveStatus.ReserveII),
            ]);

        var canonical = SerializeWorld(world);
        var parsed = ParseWorld(canonical);

        Assert.Equal(
            "{\"world\":{\"contractVersion\":2,\"elements\":[" +
            "{\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"," +
            "\"reserveStatus\":\"none\"}," +
            "{\"elementId\":\"axis-element-b\",\"currentLocationId\":\"north-west\"," +
            "\"reserveStatus\":\"reserve-i\"}," +
            "{\"elementId\":\"commonwealth-element-a\",\"currentLocationId\":\"east\"," +
            "\"reserveStatus\":\"reserve-ii\"}]}}",
            Encoding.UTF8.GetString(canonical));
        Assert.Equal(world, parsed);
        Assert.Equal(CampaignElementReserveStatus.ReserveI, parsed.Elements[1].ReserveStatus);
    }

    [Fact]
    public void SnapshotRejectsInvalidContractValuesAndDuplicateElements()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CampaignWorldSnapshot(1, []));
        Assert.Throws<ArgumentException>(
            () => new CampaignElementState("Invalid ID", "west"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignElementState(
            "axis-element-a",
            "west",
            (CampaignElementReserveStatus)99));
        Assert.Throws<ArgumentException>(
            () => new CampaignWorldSnapshot(
                2,
                [
                    new CampaignElementState("axis-element-a", "west"),
                    new CampaignElementState("axis-element-a", "east"),
                ]));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("ReserveI")]
    [InlineData("reserve_i")]
    public void WorldV2ParserRejectsUnknownOrNoncanonicalReserveStatus(string reserveStatus)
    {
        var canonical = SerializeWorld(new CampaignWorldSnapshot(
            2,
            [new CampaignElementState("axis-element-a", "west")]));
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
                new CampaignElementState(
                    "axis-element-a",
                    "west",
                    CampaignElementReserveStatus.None),
                new CampaignElementState(
                    "axis-element-b",
                    "north-west",
                    CampaignElementReserveStatus.None),
                new CampaignElementState(
                    "commonwealth-element-a",
                    "east",
                    CampaignElementReserveStatus.None),
                new CampaignElementState(
                    "commonwealth-element-b",
                    "south-east",
                    CampaignElementReserveStatus.None),
            ],
            world.Elements);
        Assert.True(CampaignWorldValidator.IsValidInitial(world, artifact, scenario));
    }

    [Fact]
    public void InitialValidationRejectsMissingUnknownRelocatedAndInvalidLocationState()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");
        var baseline = CampaignWorldFactory.CreateInitial(artifact, scenario);

        var missing = new CampaignWorldSnapshot(2, baseline.Elements.Skip(1).ToArray());
        var unknown = new CampaignWorldSnapshot(
            2,
            baseline.Elements.Append(new CampaignElementState("unknown-element", "west")).ToArray());
        var relocated = Replace(
            baseline,
            new CampaignElementState("axis-element-a", "east"));
        var invalidLocation = Replace(
            baseline,
            new CampaignElementState("axis-element-a", "unknown-location"));
        var reserveI = Replace(
            baseline,
            new CampaignElementState(
                "axis-element-a",
                "west",
                CampaignElementReserveStatus.ReserveI));
        var reserveII = Replace(
            baseline,
            new CampaignElementState(
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
            10,
            ContentPlacementMode.AttachmentOnly,
            ContentTestData.Origin("content.element.attachment"));
        var artifact = ContentPackArtifact.Create(ContentTestData.Copy(
            baseline,
            elements: baseline.Elements.Append(attachment)));
        var scenario = artifact.Definition.Scenarios[0];
        var initial = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var withAttachment = new CampaignWorldSnapshot(
            2,
            initial.Elements.Append(new CampaignElementState("axis-attachment", "west")).ToArray());
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
            2,
            world.Elements
                .Where(element => element.ElementId != replacement.ElementId)
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
