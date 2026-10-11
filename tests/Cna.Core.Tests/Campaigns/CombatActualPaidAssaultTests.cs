using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualPaidAssaultTests
{
    private static readonly int[] ZeroCosts = [0, 0];
    private static readonly int[] PaidCp = [5, 3];
    private static readonly int[] InitialAmmo = [10, 10];
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GenuinePreparedCommitPaysBothUnits(int index)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases")[index];
        var packet = JsonNode.Parse(row.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject();
        packet["paidEventCanonicalUtf8"] = new JsonArray();
        var source = Encode(packet);
        var selection = Ledger(row, "selectionLedger");
        var rounds = Ledger(row, "roundLedger");
        var context = Context();
        using var admission = JsonDocument.Parse(packet["admissionSourceCanonicalUtf8"]!.GetValue<string>());
        var prepared = CampaignCombatActualRoundEntry.ReplayTrustedSource(
            Encoding.ASCII.GetBytes(admission.RootElement.GetProperty("actualRoundSourceCanonicalUtf8").GetString()!),
            context, selection, rounds, Dependency);
        Assert.Equal(25, prepared.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal(2UL, prepared.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.All(prepared.Control.GetProperty("world").GetProperty("elements").EnumerateArray(), element =>
        {
            Assert.Equal(0, element.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt64());
            Assert.Equal(10, element.GetProperty("ammunition").GetProperty("points").GetInt32());
        });
        var input = Encode(row.GetProperty("paidLedger")[0]);

        var result = CampaignCombatActualPaidAssault.ApplyTrustedSource(source, context, selection, rounds, [], input, Dependency);

        Assert.Equal(26, result.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal(2UL, result.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Single(result.PaidInputs);
        Assert.Equal(input, result.PaidInputs[0]);
        Assert.StartsWith("cmb.", result.ReceiptId);
        Assert.StartsWith("cmt.", result.Control.GetProperty("commitmentId").GetString());
        using var accepted = JsonDocument.Parse(result.EventBytes!);
        var effect = accepted.RootElement.GetProperty("effect");
        Assert.Equal("attack-committed", effect.GetProperty("kind").GetString());
        Assert.Equal(prepared.Control.GetProperty("prefix").GetString(), accepted.RootElement.GetProperty("priorPrefix").GetString());
        var costs = effect.GetProperty("costs").EnumerateArray().ToArray();
        Assert.Equal(ZeroCosts, costs.Select(x => x.GetProperty("beforeCp").GetInt32()));
        Assert.Equal(PaidCp, costs.Select(x => x.GetProperty("afterCp").GetInt32()));
        Assert.Equal(InitialAmmo, costs.Select(x => x.GetProperty("beforeAmmo").GetInt32()));
        Assert.Equal(ZeroCosts, costs.Select(x => x.GetProperty("afterAmmo").GetInt32()));
        Assert.Equal(prepared.Control.GetProperty("randomState").GetRawText(), effect.GetProperty("preResultRandomState").GetRawText());
    }

    internal static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-actual-paid-assault-v1.json")));
    internal static byte[] Encode(JsonNode value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    internal static byte[] Encode(JsonElement value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    internal static byte[][] Ledger(JsonElement row, string field) => row.GetProperty(field).EnumerateArray().Select(Encode).ToArray();
    internal static byte[] Dependency(string path) => File.ReadAllBytes(Path.Combine(Repository(), path));
    internal static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx"))) directory = directory.Parent;
        return directory!.FullName;
    }
    internal static CampaignCombatCreationContext Context()
    {
        using var creation = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(creation.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        return new(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config);
    }
}
