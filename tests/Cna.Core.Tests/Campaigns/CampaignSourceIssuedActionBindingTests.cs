using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSourceIssuedActionBindingTests
{
    [Fact]
    public void IssuedIdentityRequiresExactVersionFamilyChoiceAndOrdinal()
    {
        const string visible = """{"choices":[{"family":"axis-dump","choice":{"kind":"place","locationId":"c4120"}},{"family":"axis-dump","choice":{"kind":"place","locationId":"c3918"}}]}""";
        using var document = JsonDocument.Parse(visible);
        var first = document.RootElement.GetProperty("choices")[0];
        var action = new CampaignSourceSetupAction(7, first.GetProperty("family").GetString()!, "", first.GetProperty("choice"));
        var bytes = Encoding.UTF8.GetBytes(visible);
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(8, bytes, 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 1, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, -1, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 2, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 0, "sha256:" + new string('0', 64)));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7,
            Encoding.UTF8.GetBytes(visible.Replace("axis-dump", "axis-supply", StringComparison.Ordinal)), 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7,
            Encoding.UTF8.GetBytes(visible.Replace("c4120", "c4121", StringComparison.Ordinal)), 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, "{"u8.ToArray(), 0, action.ActionId));
    }
}
