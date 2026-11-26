using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class CombatTurnBoundaryCatalogTests
{
    [Fact]
    public void DedicatedCombatVariantRetainsFullTurnPolicy()
    {
        var setup = Assert.Single(Cna1979BreakdownSetupCatalog.Definitions,
            value => value.SetupId == "rules-lab.breakdown.turn-boundary.combat.gt1.v1");
        var artifact = Cna1979SyntheticContentCatalog.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash).Artifact!;
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        Assert.Equal(1, scenario.Start.GameTurn);
        var combat = Assert.IsType<ContentCombatSourceV1>(ContentCombatSourceV1.Resolve(artifact));
        var retreat = Assert.IsType<ContentCombatRetreatSourceV1>(ContentCombatRetreatSourceV1.Resolve(artifact));
        Assert.Equal("sha256:b8cb69a32cb7569142e8a9ea4f5bba4735cf1d15c59a6fa9eda562c748235c13", combat.Hash);
        Assert.Equal("sha256:ed09187e7409862c81336463006c4d0660ff6010bdbf6dd9c6ea045f62479aed", retreat.Hash);
        Assert.Equal(artifact.Identity.Hash, combat.ContentHash);
        Assert.Equal(artifact.Identity.Hash, retreat.ContentHash);
        Assert.Equal("sha256:f5e5b4a48ca9da4f7bbfa48e56183a9ca3f22b1acb735a8500a78a0b241083d4", setup.Hash);
        Assert.Equal("sha256:37724a4ae5e69acdd71e30e458029a9d74bb93df90caca7d922c259014cdde04", artifact.Identity.Hash);
        Assert.Equal(2, scenario.End.GameTurn);
        Assert.True(Cna1979BreakdownSetupCatalog.AdmitsLaterTurnPasses(setup.Snapshot));
        Assert.Equal(setup.Snapshot, CampaignSetupV6Codec.Deserialize(CampaignSetupV6Codec.Serialize(setup.Snapshot)));
        Assert.Equal(artifact.GetCanonicalBytes(), ContentPackV6Serializer.SerializeCanonical(artifact.Definition));
    }
}
