using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownCreationTests
{
    [Fact]
    public void ProfileRejectionAppendsWithoutRenumbering()
    {
        Assert.Equal(10, (int)CampaignCreationRejectionReason.InvalidState);
        Assert.Equal("UnsupportedCapabilityProfile", Enum.GetName(typeof(CampaignCreationRejectionReason), 11));
    }

    [Fact]
    public void CreationBindsCertifiedInitialWorldAndProjectsSnapshot11()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var created = Create();
        Assert.Equal(10, created.ContractVersion);
        Assert.Equal(1, created.StateVersion);
        Assert.Equal(6, created.Setup.SchemaVersion);
        Assert.Equal(6, created.InitialWorld.ContractVersion);
        Assert.Equal(4, created.SequencePosition.ContractVersion);
        Assert.IsType<CampaignBreakdownFlow.Idle>(created.BreakdownFlow);
        Assert.Empty(created.InitialWorld.BrokenVehicleLots);
        var bytes = CampaignCreatedV10Serializer.Serialize(created, artifact, scenario);
        Assert.Equal(created, CampaignCreatedV10Serializer.Deserialize(bytes, artifact, scenario));
        var snapshot = CampaignCreationV10Factory.CreateSnapshot(created, artifact, scenario);
        Assert.Equal(11, snapshot.ContractVersion);
        Assert.Equal(created.InitialWorld, snapshot.World);
        Assert.Equal(created.RandomState, snapshot.RandomState);
        Assert.Null(snapshot.ReactionWindow);
        Assert.Null(snapshot.InitiativeHolder);
        Assert.Empty(snapshot.OperationStageOrders);
        Assert.Empty(snapshot.OperationStageWeather);
    }

    [Theory]
    [InlineData("ruleset")]
    [InlineData("rng-cursor")]
    [InlineData("rng-algorithm")]
    [InlineData("rng-version")]
    [InlineData("position-version")]
    [InlineData("position")]
    public void FactoryRejectsUnboundInitialTruth(string mutation)
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var rng = new RandomStreamState(mutation == "rng-version" ? 2 : 1,
            mutation == "rng-algorithm" ? "unknown-rng" : SandtableRandom.AlgorithmId,
            12345, mutation == "rng-cursor" ? 1UL : 0UL);
        var position = mutation == "position-version" ? Cna1979LandSequence.CreateTurn(1)[0]
            : Cna1979LandSequenceV4.CreateTurn(1)[mutation == "position" ? 1 : 0];
        Assert.Throws<ArgumentException>(() => CampaignCreationV10Factory.Create("breakdown-campaign",
            mutation == "ruleset" ? Cna1979Ruleset.HistoricalManifestV8.Hash : Cna1979BreakdownRuleset.Manifest.Hash,
            BreakdownSetupTests.CreateSetup(), artifact, scenario, rng, position));
    }

    [Theory]
    [InlineData("created-version")]
    [InlineData("world-version")]
    [InlineData("flow")]
    [InlineData("working-count")]
    [InlineData("location")]
    [InlineData("toe")]
    [InlineData("unknown")]
    public void StrictReadRejectsForgedInitialEvidence(string mutation)
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var root = JsonNode.Parse(CampaignCreatedV10Serializer.Serialize(Create(), artifact, scenario))!.AsObject();
        switch (mutation)
        {
            case "created-version": root["contractVersion"] = 9; break;
            case "world-version": root["initialWorld"]!["contractVersion"] = 5; break;
            case "flow": root["breakdownFlow"]!["kind"] = "moving"; break;
            case "working-count": root["initialWorld"]!["elements"]![1]!["operationalState"]!["vehicleBreakdownState"]!["workingPointCount"] = 11; break;
            case "location": root["initialWorld"]!["elements"]![0]!["currentLocationId"] = "center"; break;
            case "toe": root["initialWorld"]!["elements"]![0]!["components"]![0]!["currentToe"] = 4; break;
            case "unknown": root["callerCertified"] = true; break;
            default: throw new InvalidOperationException(mutation);
        }

        Assert.ThrowsAny<Exception>(() => CampaignCreatedV10Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString()), artifact, scenario));
    }

    [Fact]
    public void CreationRejectsNoncanonicalBytesAndDuplicateFields()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var json = Encoding.UTF8.GetString(CampaignCreatedV10Serializer.Serialize(Create(), artifact, scenario));
        Assert.ThrowsAny<Exception>(() => CampaignCreatedV10Serializer.Deserialize(Encoding.UTF8.GetBytes(json + "\n"), artifact, scenario));
        Assert.ThrowsAny<Exception>(() => CampaignCreatedV10Serializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(
            "\"contractVersion\":10", "\"contractVersion\":10,\"contractVersion\":10", StringComparison.Ordinal)), artifact, scenario));
    }

    [Fact]
    public void FactoryRejectsRehashedSetupBoundToDifferentContent()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var setup = BreakdownSetupTests.CreateSetup();
        var wrongIdentity = new ContentPackV6Identity(6, ContentPackV6Definition.CanonicalFormatId,
            artifact.Identity.PackId, artifact.Identity.RulesetId, "sha256:" + new string('0', 64));
        var rehashed = new CampaignSetupDefinitionV6(6, setup.SetupId, "Forged selection", true,
            setup.CapabilityProfileId, setup.InitialGameTurn, setup.InitialInitiative, setup.OpeningPreamble,
            setup.Weather, setup.StageEntry, new CampaignContentV6Selection(wrongIdentity, scenario.ScenarioId), setup.Sources);
        Assert.Throws<ArgumentException>(() => CampaignCreationV10Factory.Create("breakdown-campaign",
            Cna1979BreakdownRuleset.Manifest.Hash, CampaignSetupSnapshotV6.FromDefinition(rehashed), artifact, scenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0), Cna1979LandSequenceV4.CreateTurn(1)[0]));
    }

    [Fact]
    public void SerializationRejectsConstructedEventWithForgedWorld()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var created = Create();
        var first = created.InitialWorld.Elements[0];
        var moved = new CampaignElementStateV5(first.ElementId, "center", first.ReserveStatus,
            first.OperationalState, first.Components);
        var world = new CampaignWorldSnapshotV6(6, created.InitialWorld.Elements.Select(element =>
            element.ElementId == first.ElementId ? moved : element), created.InitialWorld.Representations, []);
        var forged = new CampaignCreatedV10(created.CampaignId, 1, created.RulesetHash, created.Setup,
            world, created.RandomState, created.SequencePosition, created.BreakdownFlow);
        Assert.ThrowsAny<Exception>(() => CampaignCreatedV10Serializer.Serialize(forged, artifact, scenario));
    }

    internal static CampaignCreatedV10 Create()
    {
        var artifact = BreakdownContentFixture.Artifact();
        return CampaignCreationV10Factory.Create("breakdown-campaign", Cna1979BreakdownRuleset.Manifest.Hash,
            BreakdownSetupTests.CreateSetup(), artifact, artifact.Definition.LegacyDefinition.Scenarios.Single(),
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0), Cna1979LandSequenceV4.CreateTurn(1)[0]);
    }
}
