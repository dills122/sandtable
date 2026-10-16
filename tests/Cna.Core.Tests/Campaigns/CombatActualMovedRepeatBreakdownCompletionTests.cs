using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
using static Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementCompletionTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedRepeatBreakdownCompletionTests
{
    private const string FixturePath = "docs/specs/fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json";

    [Fact]
    public void ReplaysAllAuthenticTerminalCasesWithoutChangingPredecessorControl()
    {
        using var fixture = JsonDocument.Parse(Dependency(FixturePath));
        var cases = fixture.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        Assert.Equal(300, cases.Length);
        var eventHashes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var expected in cases)
        {
            var root = expected.GetProperty("rootOrdinal").GetInt32();
            var cut = expected.GetProperty("movementCut").GetInt32();
            var profile = expected.GetProperty("profile").GetString()!;
            var parent = new CompletionContext(root, cut, profile);
            AtCut(parent, parent.Inputs.Length);
            var predecessor = parent.Replay();
            var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
            {
                ["contractVersion"] = 1,
                ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
                ["breakdownEventCanonicalUtf8"] = new JsonArray()
            });
            var initial = Replay(parent, source, []);
            Assert.Equal(Encoding.ASCII.GetString(predecessor.ControlBytes), initial.Control.GetProperty("predecessorControlCanonicalUtf8").GetString());
            AssertCut(expected.GetProperty("cuts")[0], initial);
            var input = Encoding.ASCII.GetBytes(expected.GetProperty("inputCanonicalUtf8")[0].GetString()!);
            var completed = Apply(parent, source, [], input);
            Assert.Equal(expected.GetProperty("eventCanonicalUtf8")[0].GetString(), Encoding.ASCII.GetString(completed.EventBytes!));
            AssertCut(expected.GetProperty("cuts")[1], completed);
            eventHashes.Add("sha256:" + Convert.ToHexStringLower(SHA256.HashData(completed.EventBytes!)));
        }
        Assert.Equal(300, eventHashes.Count);
    }

    [Fact]
    public void RejectsMalformedAncestryOwnLedgerAndDependencyDrift()
    {
        var parent = new CompletionContext(0, 1, "paid");
        AtCut(parent, parent.Inputs.Length);
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        Assert.Throws<JsonException>(() => Replay(parent, source, [Encoding.ASCII.GetBytes("{}")]));
        var changed = JsonNode.Parse(source)!.AsObject();
        changed["movementCompletionSourceCanonicalUtf8"] = "{}";
        Assert.Throws<JsonException>(() => Replay(parent, CampaignCombatActualPaidAssaultCodec.Encode(changed), []));
        var ledgers = Ledgers(parent);
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], [], path =>
                path.EndsWith("combat-cycle-sequence-v1.json", StringComparison.Ordinal) ? [] : Dependency(path)));
    }

    [Fact]
    public void PublicClaimReadbackRemainsClosedWhileContractIsProvisional()
    {
        var parent = new CompletionContext(0, 1, "paid");
        AtCut(parent, parent.Inputs.Length);
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        var privateResult = Replay(parent, source, []);
        var ledgers = Ledgers(parent);
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadSource(source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], [], Dependency));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadControl(privateResult.ControlBytes, source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], [], Dependency));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadProof(privateResult.ProofBytes, source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], [], Dependency));
    }

    [Fact]
    public void RejectsPhysicalDependencyDriftDuringReplay()
    {
        var parent = new CompletionContext(0, 1, "paid");
        AtCut(parent, parent.Inputs.Length);
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        var ledgers = Ledgers(parent);
        var seen = false;
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], [], path =>
            {
                if (path == "docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json")
                {
                    if (seen) return [];
                    seen = true;
                }
                return Dependency(path);
            }));
        Assert.True(seen);
    }

    [Fact]
    public void EveryPhysicalPinRejectsChangedBytes()
    {
        var parent = new CompletionContext(0, 1, "paid");
        AtCut(parent, parent.Inputs.Length);
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        var ledgers = Ledgers(parent);
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json"));
        var paths = schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().Select(x => x.Name)
            .Concat(["docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json",
                "docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"]).ToArray();
        Assert.Equal(108, paths.Length);
        foreach (var broken in paths)
        {
            Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source,
                parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
                ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], Array.Empty<byte[]>(), path =>
                    path == broken ? Array.Empty<byte>() : Dependency(path)));
        }
    }

    [Fact]
    public void MalformedPrimitiveInputsRejectAsContractErrors()
    {
        var parent = new CompletionContext(0, 1, "paid");
        AtCut(parent, parent.Inputs.Length);
        using var fixture = JsonDocument.Parse(Dependency(FixturePath));
        var expected = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(item =>
            item.GetProperty("rootOrdinal").GetInt32() == 0 && item.GetProperty("movementCut").GetInt32() == 1 &&
            item.GetProperty("profile").GetString() == "paid").Clone();
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        var invalid = JsonNode.Parse(expected.GetProperty("inputCanonicalUtf8")[0].GetString()!)!.AsObject();
        invalid["actor"] = 123;
        Assert.Throws<JsonException>(() => Apply(parent, source, [], CampaignCombatActualPaidAssaultCodec.Encode(invalid)));
        var invalidSource = JsonNode.Parse(source)!.AsObject();
        invalidSource["breakdownEventCanonicalUtf8"] = new JsonArray(123);
        Assert.Throws<JsonException>(() => Replay(parent, CampaignCombatActualPaidAssaultCodec.Encode(invalidSource), []));
    }

    [Fact]
    public void FreshAdmissionAndRecordedEventAreBothAuthenticated()
    {
        var parent = new CompletionContext(0, 1, "paid");
        AtCut(parent, parent.Inputs.Length);
        using var fixture = JsonDocument.Parse(Dependency(FixturePath));
        var expected = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(item =>
            item.GetProperty("rootOrdinal").GetInt32() == 0 && item.GetProperty("movementCut").GetInt32() == 1 &&
            item.GetProperty("profile").GetString() == "paid").Clone();
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        var input = Encoding.ASCII.GetBytes(expected.GetProperty("inputCanonicalUtf8")[0].GetString()!);
        Assert.Throws<JsonException>(() => Apply(parent, source, [], input, admissionEnabled: false));
        var completed = Apply(parent, source, [], input);
        var tampered = JsonNode.Parse(completed.SourceBytes)!.AsObject();
        var recorded = tampered["breakdownEventCanonicalUtf8"]![0]!.GetValue<string>();
        tampered["breakdownEventCanonicalUtf8"]![0] = recorded.Replace("breakdown-completed", "breakdown-overridden", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => Replay(parent, CampaignCombatActualPaidAssaultCodec.Encode(tampered), [input]));
    }

    [Theory]
    [InlineData(0, 1, "paid")]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 1, "armed")]
    [InlineData(12, 6, "armed")]
    public void AuthenticOrdinalTwoMovementCompletesOneAdjacentSystemBreakdown(int rootOrdinal, int movementCut, string profile)
    {
        var parent = new CompletionContext(rootOrdinal, movementCut, profile);
        AtCut(parent, parent.Inputs.Length);
        var predecessor = parent.Replay();
        Assert.Equal("breakdown", predecessor.Control.GetProperty("phase").GetString());
        Assert.True(predecessor.Control.GetProperty("closed").GetBoolean());

        using var fixture = JsonDocument.Parse(Dependency(FixturePath));
        var expected = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(item =>
            item.GetProperty("rootOrdinal").GetInt32() == rootOrdinal &&
            item.GetProperty("movementCut").GetInt32() == movementCut &&
            item.GetProperty("profile").GetString() == profile).Clone();
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parent.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        });
        var initial = Replay(parent, source, []);
        Assert.Equal("breakdown", initial.Control.GetProperty("phase").GetString());
        Assert.Equal(2, initial.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
        AssertCut(expected.GetProperty("cuts")[0], initial);

        var input = Encoding.ASCII.GetBytes(expected.GetProperty("inputCanonicalUtf8")[0].GetString()!);
        var completed = Apply(parent, source, [], input);
        Assert.Equal(expected.GetProperty("eventCanonicalUtf8")[0].GetString(), Encoding.ASCII.GetString(completed.EventBytes!));
        Assert.Equal("combat", completed.Control.GetProperty("phase").GetString());
        Assert.True(completed.Control.GetProperty("closed").GetBoolean());
        Assert.Equal(2, completed.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
        Assert.Equal(initial.Control.GetProperty("predecessorControlCanonicalUtf8").GetString(),
            completed.Control.GetProperty("predecessorControlCanonicalUtf8").GetString());
        AssertCut(expected.GetProperty("cuts")[1], completed);

        var ledger = new byte[][] { input };
        var replayed = Replay(parent, completed.SourceBytes, ledger);
        Assert.Equal(completed.ControlBytes, replayed.ControlBytes);
        Assert.Equal(completed.ProofBytes, replayed.ProofBytes);
        var retry = Apply(parent, completed.SourceBytes, ledger, input, admissionEnabled: false);
        Assert.True(retry.Duplicate);
        Assert.Equal(completed.SourceBytes, retry.SourceBytes);
        Assert.Equal(completed.ReceiptId, retry.ReceiptId);
    }

    private static void AssertCut(JsonElement expected, CombatActualMovedRepeatBreakdownCompletionResult actual)
    {
        AssertArtifact(expected.GetProperty("source"), actual.SourceBytes);
        AssertArtifact(expected.GetProperty("control"), actual.ControlBytes);
        AssertArtifact(expected.GetProperty("proof"), actual.ProofBytes);
    }

    private static void AssertArtifact(JsonElement expected, byte[] actual)
    {
        Assert.Equal(expected.GetProperty("bytes").GetInt32(), actual.Length);
        Assert.Equal(expected.GetProperty("sha256").GetString(),
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(actual)));
    }

    private static CombatActualMovedRepeatBreakdownCompletionResult Replay(
        CompletionContext parent, byte[] source, IReadOnlyList<byte[]> own)
    {
        var ledgers = Ledgers(parent);
        return CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], own, Dependency);
    }

    private static CombatActualMovedRepeatBreakdownCompletionResult Apply(
        CompletionContext parent, byte[] source, IReadOnlyList<byte[]> own, byte[] input, bool admissionEnabled = true)
    {
        var ledgers = Ledgers(parent);
        return CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(source,
            parent.Parent.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4],
            ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], own, input, Dependency, admissionEnabled);
    }
}
