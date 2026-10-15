using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedRepeatMovementTests
{
    [Theory]
    [InlineData(0, "paid")]
    [InlineData(12, "armed")]
    public void AuthenticPendingIntentPublishesActualOrdinalTwoMovement(int ordinal, string profile)
    {
        using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json"));
        var item = fixture.RootElement.GetProperty("cases")[ordinal]; Assert.Equal(profile, item.GetProperty("profile").GetString());
        using var parents = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json"));
        var parent = parents.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == item.GetProperty("parentName").GetString());
        var c = new Context(profile, parent.GetProperty("parentName").GetString()!);
        var source = Encoding.ASCII.GetBytes(item.GetProperty("initialSourceCanonicalUtf8").GetString()!);
        var inputs = Bytes(parent.GetProperty("inputCanonicalUtf8"));
        var result = CampaignCombatActualMovedRepeatMovement.ApplyTrustedSource(source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown,
            c.Selection, c.Round, c.Paid, c.Release, inputs, [], Encoding.ASCII.GetBytes(item.GetProperty("inputCanonicalUtf8")[0].GetString()!), Dependency);
        Assert.Equal("movement", result.Control.GetProperty("phase").GetString());
        Assert.Equal(2, result.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
    }
}
