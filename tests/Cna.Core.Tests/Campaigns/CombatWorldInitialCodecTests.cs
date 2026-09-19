using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatWorldInitialCodecTests
{
    [Fact]
    public void InitialWorldMatchesFrozenCanonicalBytesAndReadsBackFromTrustedInputs()
    {
        var (artifact, scenario, policy, world) = InitialInputs();
        var expected = Golden();

        Assert.Equal(expected, CampaignWorldV7InitialCodec.Serialize(world, artifact, scenario, policy));
        var restored = CampaignWorldV7InitialCodec.Deserialize(expected, artifact, scenario, policy,
            world.CreationBinding);
        Assert.Equal(world, restored);
    }

    [Fact]
    public void InitialReadbackRejectsAlteredAndNoncanonicalBytes()
    {
        var (artifact, scenario, policy, world) = InitialInputs();
        var canonical = Encoding.UTF8.GetString(Golden());
        var changedToe = canonical.Replace("\"currentToe\":10", "\"currentToe\":9", StringComparison.Ordinal);
        var changedBinding = canonical.Replace("fixture.creation-001", "fixture.creation-002", StringComparison.Ordinal);
        var unexpectedField = canonical[..^1] + ",\"unexpected\":0}";

        foreach (var candidate in new[] { " " + canonical, changedToe, changedBinding, unexpectedField, "{}" })
            Assert.Throws<JsonException>(() => CampaignWorldV7InitialCodec.Deserialize(
                Encoding.UTF8.GetBytes(candidate), artifact, scenario, policy, world.CreationBinding));
        Assert.Throws<JsonException>(() => CampaignWorldV7InitialCodec.Deserialize(
            Golden(), artifact, scenario, policy, "fixture.creation-002"));
    }

    [Fact]
    public void InitialWriterRejectsAValidButNoninitialWorld()
    {
        var (artifact, scenario, policy, world) = InitialInputs();
        var element = world.Elements[0];
        var changed = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId,
            element.ReserveStatus, element.OperationalState, element.Components,
            element.SourceParentFormationId, element.CurrentParentFormationId,
            new CampaignElementAmmunitionState(9, element.Ammunition.InitialAmmunitionOrigin),
            element.Readiness);
        var noninitial = new CampaignWorldSnapshotV7(7, world.CreationBinding,
            world.Elements.Select(value => value.ElementId == changed.ElementId ? changed : value),
            world.Representations, [], [], [], [], [], [], [], []);

        Assert.Throws<JsonException>(() => CampaignWorldV7InitialCodec.Serialize(
            noninitial, artifact, scenario, policy));
    }

    private static (ContentPackV7Artifact Artifact, ContentCombatScenario Scenario,
        CampaignCombatInitializationPolicy Policy, CampaignWorldSnapshotV7 World) InitialInputs()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var policy = new CampaignCombatInitializationPolicy(1, scenario.Start.GameTurn,
            scenario.Start.OperationStage, CapabilityPointAmount.Zero, 0,
            CampaignElementReserveStatus.None, new ContentOrigin(ContentOriginKind.Synthetic,
                [new RuleReference("sandtable-rules-lab", "combat.close-assault-positive.v1:initial-ledger")]));
        var world = CampaignWorldV7Factory.CreateInitial(artifact, scenario, policy,
            "fixture.creation-001");
        return (artifact, scenario, policy, world);
    }

    private static byte[] Golden()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-world-settlement-v1.json")));
        var initial = fixture.RootElement.GetProperty("goldens").EnumerateArray()
            .Single(value => value.GetProperty("cut").GetString() == "initial");
        return Encoding.UTF8.GetBytes(initial.GetProperty("canonicalUtf8").GetString()!);
    }
}
