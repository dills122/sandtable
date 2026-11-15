using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceInitialWrecksTests
{
    [Fact]
    public void InitialStockConservesCountsAndRequiresExplicitLegalCityPlacement()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-initial-wrecks", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var stock = CampaignSourceInitialWrecks.Create(CampaignPregameSourceState.Create(request, artifact, init));
        Assert.Equal(CampaignSourceInitialWrecks.InitialHash, stock.Hash());
        Assert.Equal(3, stock.PendingIds.Count);
        Assert.Equal(2, CampaignSourceInitialWrecks.Stocks.Count(value => value.VehicleTypeId == "a9-cruiser"));
        Assert.Single(CampaignSourceInitialWrecks.Stocks, value => value.VehicleTypeId == "a10-cruiser");
        var first = stock.Place("cw-alexandria-a9-1", "e3613");
        Assert.Equal(2, first.PendingIds.Count);
        Assert.Empty(first.ValidDestinations("cw-alexandria-a9-1"));
        Assert.Throws<InvalidOperationException>(() => first.Place("cw-alexandria-a9-1", "e3714"));
        Assert.Throws<InvalidOperationException>(() => first.Place("cw-alexandria-a9-2", "e1930"));
        Assert.NotEqual(stock.Hash(), first.Hash());
        var finished = first.Place("cw-alexandria-a9-2", "e3714").Place("cw-alexandria-a10-1", "e3714");
        Assert.Empty(finished.PendingIds);
        Assert.Equal(3, finished.Placements.Count);
    }
}
