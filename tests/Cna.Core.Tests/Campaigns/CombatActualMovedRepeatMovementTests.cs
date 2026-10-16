using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    internal sealed class RepeatContext
    {
        public Context Ancestor { get; }
        public JsonElement Case { get; }
        public JsonElement ParentCase { get; }
        public byte[] Source { get; set; }
        public byte[][] Control { get; }
        public byte[][] Inputs { get; }
        public List<byte[]> Movement { get; } = [];
        public RepeatContext(int ordinal)
        {
            using var f = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json")); Case = f.RootElement.GetProperty("cases")[ordinal].Clone();
            using var parents = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json")); ParentCase = parents.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == Case.GetProperty("parentName").GetString()).Clone();
            Ancestor = new Context(Case.GetProperty("profile").GetString()!, ParentCase.GetProperty("parentName").GetString()!); Source = Encoding.ASCII.GetBytes(Case.GetProperty("initialSourceCanonicalUtf8").GetString()!); Control = Bytes(ParentCase.GetProperty("inputCanonicalUtf8")); Inputs = Bytes(Case.GetProperty("inputCanonicalUtf8"));
        }
        public CombatActualMovedRepeatMovementResult Replay(Func<string, byte[]>? dependencies = null) => CampaignCombatActualMovedRepeatMovement.ReplayTrustedSource(Source, Ancestor.Request, Ancestor.Prefix, Ancestor.Lifecycle, Ancestor.Breakdown, Ancestor.Selection, Ancestor.Round, Ancestor.Paid, Ancestor.Release, Control, Movement, dependencies ?? Dependency);
    }
    internal static CombatActualMovedRepeatMovementResult Apply(RepeatContext c, byte[] input, Func<string, byte[]>? dependencies = null, bool fresh = true)
    { var a = c.Ancestor; return CampaignCombatActualMovedRepeatMovement.ApplyTrustedSource(c.Source, a.Request, a.Prefix, a.Lifecycle, a.Breakdown, a.Selection, a.Round, a.Paid, a.Release, c.Control, c.Movement, input, dependencies ?? Dependency, fresh); }
    internal static void Retain(RepeatContext c, CombatActualMovedRepeatMovementResult result, byte[] input) { c.Source = result.SourceBytes; c.Movement.Add(input); }
    internal static byte[] Wire(JsonNode input) => CampaignCombatActualMovedRepeatMovementCodec.Canonical(input, "Input");
    internal static byte[] Command(JsonElement state, string kind, string? destination = null) => Wire(CampaignCombatActualMovedRepeatMovement.Command(JsonNode.Parse(state.GetRawText())!.AsObject(), kind, destination));
    internal static void AssertCode(string code, Action action) => Assert.Equal(code, Assert.Throws<JsonException>(action).Message);
}
