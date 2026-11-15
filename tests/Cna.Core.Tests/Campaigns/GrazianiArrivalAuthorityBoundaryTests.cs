using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiArrivalAuthorityBoundaryTests
{
    [Fact]
    public void NoObligationCannotSkipDueOrOverdueGrazianiArrival()
    {
        var nz = Cna1979GrazianiSourceFacts.NzFieldArtilleryArrival;
        var indian = Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival;
        var source = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.47")]);
        var scenario = new ContentScenario("graziani-excerpt", new(1, 1), new(6, 3), [],
            [nz, indian], source);

        Assert.False(CampaignV11Preamble.HasDueOrOverdueArrival(scenario, 2, 2));
        Assert.True(CampaignV11Preamble.HasDueOrOverdueArrival(scenario, 2, 3));
        Assert.True(CampaignV11Preamble.HasDueOrOverdueArrival(scenario, 3, 1));
        Assert.True(CampaignV11Preamble.HasDueOrOverdueArrival(scenario, 5, 1));
        var indianOnly = new ContentScenario("graziani-indian-excerpt", new(1, 1), new(6, 3), [],
            [indian], source);
        Assert.False(CampaignV11Preamble.HasDueOrOverdueArrival(indianOnly, 4, 3));
        Assert.True(CampaignV11Preamble.HasDueOrOverdueArrival(indianOnly, 5, 1));
    }
}
