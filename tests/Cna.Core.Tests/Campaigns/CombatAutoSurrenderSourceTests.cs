using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatAutoSurrenderSourceTests
{
    [Fact]
    public void FrozenTripletReadsOnlyApprovedOneOrientationSource()
    {
        var source = CampaignCombatAutoSurrenderSourceV1.Read(
            Fixture("content-v8"), Fixture("rules-source-v1"), Fixture("setup-v8"));
        Assert.Equal("axis-assault-battalion", source.AttackerElementId);
        Assert.Equal("commonwealth-assault-battalion", source.DefenderElementId);
        Assert.Equal(10, source.AttackerInitialAmmunition);
        Assert.Equal(0, source.DefenderInitialAmmunition);
        Assert.Equal("sha256:f7e25087a3f892304907e03640df258203eda6a2dee59fc669468200ef8d1595",
            source.ContentHash);
        Assert.Equal("sha256:68ecae6ce2b688622842c3ed581c0cbd7e263a36c8e82c9b89e51ebb079b134d",
            source.RulesSourceHash);
        Assert.Equal("sha256:253f6e302851d127a5cfaf735f377f9748091915a06e3b12f47acac1239bfa0d",
            source.SetupHash);
        Assert.False(ContentPackV7Serializer.Deserialize(Fixture("content-v8")).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OneByteTamperInAnyArtifactFailsClosed(int index)
    {
        var artifacts = new[] { Fixture("content-v8"), Fixture("rules-source-v1"), Fixture("setup-v8") };
        artifacts[index] = (byte[])artifacts[index].Clone();
        artifacts[index][artifacts[index].Length / 2] ^= 1;
        Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderSourceV1.Read(
            artifacts[0], artifacts[1], artifacts[2]));
    }

    [Fact]
    public void AlternateCanonicalBytesOrOriginalContent7CannotMasqueradeAsContent8()
    {
        var content = Fixture("content-v8");
        var rules = Fixture("rules-source-v1");
        var setup = Fixture("setup-v8");
        Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderSourceV1.Read(
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(content)), rules, setup));
        var oldContent = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Content", "Fixtures", "combat-content-v7.canonical.json"));
        Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderSourceV1.Read(oldContent, rules, setup));
    }

    private static byte[] Fixture(string kind) => File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Campaigns", "Fixtures", $"combat-auto-surrender-{kind}.canonical.json"));
}
