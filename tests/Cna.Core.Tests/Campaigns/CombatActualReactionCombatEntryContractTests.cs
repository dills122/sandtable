using System.Text.Json;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualReactionCombatEntryContractTests
{
    [Fact]
    public void SixReferenceTracesMatchAllNativeCutsEventsProofsRetriesAndRejectForgedAuthority()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-reaction-combat-entry-v1.json")));
        var cases = fixture.RootElement.GetProperty("cases");
        Assert.Equal(6, cases.GetArrayLength());
        foreach (var item in cases.EnumerateArray()) CombatActualReactionCombatEntryTests.VerifyCanonicalCase(item);
    }
}
