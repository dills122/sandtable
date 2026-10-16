using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
using static Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementTests;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedRepeatMovementCompletionTests
{
    [Theory]
    [InlineData(0, 1, "paid")]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 1, "armed")]
    [InlineData(12, 6, "armed")]
    public void AuthenticPublishedMovementCompletesLifecycle(int rootOrdinal, int movementCut, string profile)
    {
        var c = new CompletionContext(rootOrdinal, movementCut, profile); var current = c.Replay();
        Assert.Equal("movement", current.Control.GetProperty("phase").GetString());
        Assert.Equal(2, current.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
        Assert.Equal(movementCut - 1, current.Control.GetProperty("actualProgressRefs").GetArrayLength());
        foreach (var input in c.Inputs) { current = Apply(c, input); Retain(c, current, input); }
        Assert.True(current.Control.GetProperty("closed").GetBoolean());
        Assert.Equal("breakdown", current.Control.GetProperty("phase").GetString());
        Assert.Equal(2, current.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32());
        Assert.Equal(current.ReceiptId, current.Control.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString());
        Assert.Equal(1, current.Control.GetProperty("originalMovementEnd").GetProperty("ordinal").GetInt32());
    }
    internal const string FixturePath = "docs/specs/fixtures/combat-actual-moved-repeat-movement-completion-v1.json";
    internal sealed class CompletionContext
    {
        public RepeatContext Parent { get; }
        public JsonElement Case { get; }
        public byte[] Source { get; set; }
        public byte[][] Inputs { get; }
        public List<byte[]> Completion { get; } = [];
        public CompletionContext(int rootOrdinal, int movementCut, string profile)
        {
            Parent = new RepeatContext(rootOrdinal); CombatActualMovedRepeatMovementContractTests.AtCut(Parent, movementCut);
            using var fixture = JsonDocument.Parse(Dependency(FixturePath));
            Case = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("rootOrdinal").GetInt32() == rootOrdinal && c.GetProperty("movementCut").GetInt32() == movementCut && c.GetProperty("profile").GetString() == profile).Clone();
            Source = CampaignCombatActualMovedRepeatMovementCompletionCodec.Canonical(new JsonObject { ["contractVersion"] = 1, ["movementSourceCanonicalUtf8"] = Encoding.ASCII.GetString(Parent.Source), ["lifecycleEventCanonicalUtf8"] = new JsonArray() }, "Source");
            Inputs = Bytes(Case.GetProperty("inputCanonicalUtf8"));
        }
        public CombatActualMovedRepeatMovementCompletionResult Replay(Func<string, byte[]>? dependencies = null) => ReplayLedgers(this, Ledgers(this), dependencies);
    }
    internal static byte[][][] Ledgers(CompletionContext c) => [c.Parent.Ancestor.Prefix, c.Parent.Ancestor.Lifecycle, c.Parent.Ancestor.Breakdown, c.Parent.Ancestor.Selection.ToArray(), c.Parent.Ancestor.Round.ToArray(), c.Parent.Ancestor.Paid.ToArray(), c.Parent.Ancestor.Release.ToArray(), c.Parent.Control, c.Parent.Movement.ToArray(), c.Completion.ToArray()];
    internal static CombatActualMovedRepeatMovementCompletionResult ReplayLedgers(CompletionContext c, byte[][][] l, Func<string, byte[]>? dependency = null) => CampaignCombatActualMovedRepeatMovementCompletion.ReplayTrustedSource(c.Source, c.Parent.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], dependency ?? Dependency);
    internal static CombatActualMovedRepeatMovementCompletionResult Apply(CompletionContext c, byte[] input, Func<string, byte[]>? dependency = null, bool admission = true)
    { var l = Ledgers(c); return CampaignCombatActualMovedRepeatMovementCompletion.ApplyTrustedSource(c.Source, c.Parent.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], input, dependency ?? Dependency, admission); }
    internal static void Retain(CompletionContext c, CombatActualMovedRepeatMovementCompletionResult result, byte[] input) { c.Source = result.SourceBytes; c.Completion.Add(input.ToArray()); }
    internal static void AtCut(CompletionContext c, int cut)
    {
        var packet = JsonNode.Parse(c.Source)!; packet["lifecycleEventCanonicalUtf8"] = new JsonArray(c.Case.GetProperty("eventCanonicalUtf8").EnumerateArray().Take(cut).Select(e => (JsonNode?)JsonValue.Create(e.GetString())).ToArray());
        c.Source = CampaignCombatActualMovedRepeatMovementCompletionCodec.Canonical(packet, "Source"); c.Completion.Clear(); c.Completion.AddRange(c.Inputs.Take(cut).Select(b => b.ToArray()));
    }
    internal static byte[] Wire(JsonNode input) => CampaignCombatActualMovedRepeatMovementCompletionCodec.Canonical(input, "Input");
    internal static byte[] Command(JsonElement state, string kind) => Wire(CampaignCombatActualMovedRepeatMovementCompletion.Command(JsonNode.Parse(state.GetRawText())!.AsObject(), kind));
    internal static void Code(int code, Action action) => Assert.Equal($"CMB-MRC-{code:000}", Assert.Throws<JsonException>(action).Message);
    internal static CombatActualMovedRepeatMovementCompletionResult Claim(CompletionContext c, byte[] claim, bool proof, Func<string, byte[]>? dependency = null)
    {
        var l = Ledgers(c); return proof ? CampaignCombatActualMovedRepeatMovementCompletion.ReadProof(claim, c.Source, c.Parent.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], dependency ?? Dependency) : CampaignCombatActualMovedRepeatMovementCompletion.ReadControl(claim, c.Source, c.Parent.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], dependency ?? Dependency);
    }
    internal static void Certify(CompletionContext c, IReadOnlySet<string> handlers, IReadOnlyDictionary<string, int> capacities)
    { var l = Ledgers(c); CampaignCombatActualMovedRepeatMovementCompletion.CertifyTrustedSource(c.Source, c.Parent.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], Dependency, handlers, capacities); }
}
