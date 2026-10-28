using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedAssaultContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-assault-v1.json"));
        return fixture.RootElement.GetProperty("cases").EnumerateArray().Select((c, ordinal) => new object[] { ordinal, c.GetProperty("name").GetString()! }).ToArray();
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryRetainedCutHasExactSourceControlProofAndEventParity(int _, string name)
    {
        var context = new Context(name);
        var current = context.Replay();
        for (var index = 0; index <= context.Inputs.Length; index++)
        {
            var cut = context.Item.GetProperty("cuts")[index];
            Check(cut, "source", current.SourceBytes); Check(cut, "control", current.ControlBytes); Check(cut, "proof", current.ProofBytes);
            if (index == context.Inputs.Length) break;
            current = context.Step(index);
            Assert.Equal(Encoding.UTF8.GetBytes(context.Item.GetProperty("eventCanonicalUtf8")[index].GetString()!), current.EventBytes);
        }
        Assert.True(current.Control.GetProperty("closed").GetBoolean());
        Assert.Equal("reserve-entry", current.Control.GetProperty("phase").GetString());
        Assert.Equal(current.ProofBytes, context.Replay().ProofBytes);
        var read = CampaignCombatActualMovedAssault.ReadProof(current.ProofBytes, context.Source, context.Request,
            context.Prefix, context.Lifecycle, context.Breakdown, context.Selection, context.Round, context.Paid, Dependency);
        Assert.Equal(current.ProofBytes, read.ProofBytes);
    }
    [Theory]
    [InlineData("axis-player-decline-attacker-selected")]
    [InlineData("commonwealth-player-decline-attacker-selected")]
    public void LegacyRevisionReplaysEveryRetainedCutWithoutIdentityMigration(string name)
    {
        using var legacy = new LegacyBundle();
        var context = new Context(name, legacy.Read);
        var current = context.Replay();
        for (var index = 0; index <= context.Inputs.Length; index++)
        {
            var cut = context.Item.GetProperty("cuts")[index];
            Check(cut, "source", current.SourceBytes); Check(cut, "control", current.ControlBytes); Check(cut, "proof", current.ProofBytes);
            if (index == context.Inputs.Length) break;
            current = context.Step(index);
            Assert.Equal(Encoding.UTF8.GetBytes(context.Item.GetProperty("eventCanonicalUtf8")[index].GetString()!), current.EventBytes);
        }
        Assert.True(current.Control.GetProperty("closed").GetBoolean());
        Assert.Equal(current.ProofBytes, context.Replay().ProofBytes);
        var refreshed = context.Replay(Dependency);
        Assert.Equal(current.ControlBytes, refreshed.ControlBytes);
        Assert.NotEqual(current.ProofBytes, refreshed.ProofBytes);
        var mismatch = Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.ReadProof(current.ProofBytes,
            context.Source, context.Request, context.Prefix, context.Lifecycle, context.Breakdown, context.Selection, context.Round, context.Paid, Dependency));
        Assert.Equal("CMB-MAS-006", mismatch.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void LegacyRepeatedBreakdownPreservesCommandEventProofAndRetryIdentity(int index)
    {
        using var legacy = new LegacyBundle();
        using var compressed = new MemoryStream(Dependency("docs/specs/fixtures/combat-evidence-legacy-r03-v1.json.gz"));
        using var gzip = new System.IO.Compression.GZipStream(compressed, System.IO.Compression.CompressionMode.Decompress);
        using var fixture = JsonDocument.Parse(gzip);
        var row = fixture.RootElement[index]; var vectors = row.GetProperty("vectors");
        using var retained = JsonDocument.Parse(legacy.Read("docs/specs/fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json"));
        var original = retained.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("rowKey").GetString() == row.GetProperty("rowKey").GetString());
        var summary = JsonNode.Parse(row.GetRawText())!.AsObject(); summary.Remove("vectors");
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(original.GetRawText()), summary));
        var parents = vectors.GetProperty("parentInputLedgersCanonicalUtf8").EnumerateArray().Skip(1).Select(Bytes).ToArray();
        var request = new Context("axis-player-decline-attacker-selected", legacy.Read).Request;
        byte[] Vector(string field) => Encoding.ASCII.GetBytes(vectors.GetProperty(field).GetString()!);
        var packet = JsonNode.Parse(Vector("sourceCanonicalUtf8"))!.AsObject();
        packet["breakdownEventCanonicalUtf8"] = new JsonArray();
        var beforeSource = CampaignCombatActualMovedRepeatBreakdownCompletionCodec.Canonical(packet, "Source");
        CombatActualMovedRepeatBreakdownCompletionResult Replay(byte[] source, byte[][] own) =>
            CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source, request,
                parents[0], parents[1], parents[2], parents[3], parents[4], parents[5], parents[6], parents[7], parents[8], parents[9], own, legacy.Read);
        CombatActualMovedRepeatBreakdownCompletionResult Apply(byte[] source, byte[][] own) =>
            CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(source, request,
                parents[0], parents[1], parents[2], parents[3], parents[4], parents[5], parents[6], parents[7], parents[8], parents[9], own, Vector("inputCanonicalUtf8"), legacy.Read);
        var before = Replay(beforeSource, []);
        Assert.Equal(Vector("beforeControlCanonicalUtf8"), before.ControlBytes);
        Assert.Equal(Vector("beforeProofCanonicalUtf8"), before.ProofBytes);
        var after = Apply(beforeSource, []);
        Assert.Equal(Vector("sourceCanonicalUtf8"), after.SourceBytes);
        Assert.Equal(Vector("eventCanonicalUtf8"), after.EventBytes);
        Assert.Equal(Vector("afterControlCanonicalUtf8"), after.ControlBytes);
        Assert.Equal(Vector("afterProofCanonicalUtf8"), after.ProofBytes);
        Assert.Equal(row.GetProperty("receiptId").GetString(), after.ReceiptId);
        var retry = Apply(after.SourceBytes, [Vector("inputCanonicalUtf8")]);
        Assert.True(retry.Duplicate); Assert.Null(retry.EventBytes);
        Assert.Equal(after.ReceiptId, retry.ReceiptId); Assert.Equal(after.ProofBytes, retry.ProofBytes);
        Assert.Equal(after.ProofBytes, Replay(after.SourceBytes, [Vector("inputCanonicalUtf8")]).ProofBytes);
        var incompatible = Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(
            after.SourceBytes, request, parents[0], parents[1], parents[2], parents[3], parents[4], parents[5], parents[6], parents[7], parents[8], parents[9],
            [Vector("inputCanonicalUtf8")], Dependency));
        Assert.DoesNotContain("-009", incompatible.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyAndRefreshedFilesCannotBeMixedAfterSuccessfulReplay()
    {
        using var legacy = new LegacyBundle();
        const string path = "docs/specs/fixtures/combat-cycle-sequence-v1.json";
        Assert.NotEqual(legacy.Read(path), Dependency(path));
        var context = new Context("axis-player-decline-attacker-selected", legacy.Read);
        var expected = context.Replay().ProofBytes;
        byte[] Mixed(string name) => name == path ? Dependency(name) : legacy.Read(name);
        var error = Assert.Throws<JsonException>(() => context.Replay(Mixed));
        Assert.Equal("CMB-MAS-009", error.Message);
        Assert.Equal(expected, context.Replay().ProofBytes);
    }

    [Fact]
    public void MergedPr334RevisionRetainsExactReplayAndRejectsMixedSnapshotDependencies()
    {
        const string archivePath = "docs/specs/fixtures/combat-evidence-pr334-v1.zip";
        Assert.Equal("d96e0fc8d0afc59a6a9849a88bb37ac49cc25bd0ddccd119e99b7bbe47408040",
            Convert.ToHexStringLower(SHA256.HashData(Dependency(archivePath))));
        using var retained = new LegacyBundle(archivePath);
        var context = new Context("axis-player-decline-attacker-selected", retained.Read);
        var current = context.Replay();
        for (var index = 0; index <= context.Inputs.Length; index++)
        {
            var cut = context.Item.GetProperty("cuts")[index];
            Check(cut, "source", current.SourceBytes);
            Check(cut, "control", current.ControlBytes);
            Check(cut, "proof", current.ProofBytes);
            if (index == context.Inputs.Length) break;
            current = context.Step(index);
            Assert.Equal(Encoding.UTF8.GetBytes(context.Item.GetProperty("eventCanonicalUtf8")[index].GetString()!), current.EventBytes);
        }
        Assert.Equal(current.ProofBytes, context.Replay().ProofBytes);
        const string changed = "docs/specs/combat-actual-moved-assault-v1.schema.json";
        Assert.NotEqual(retained.Read(changed), Dependency(changed));
        byte[] Mixed(string path) => path == changed ? Dependency(path) : retained.Read(path);
        Assert.Equal("CMB-MAS-009", Assert.Throws<JsonException>(() => context.Replay(Mixed)).Message);
        Assert.Equal(current.ProofBytes, context.Replay().ProofBytes);
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.ReadProof(current.ProofBytes,
            context.Source, context.Request, context.Prefix, context.Lifecycle, context.Breakdown,
            context.Selection, context.Round, context.Paid, Dependency));
    }

    private sealed class LegacyBundle(string archivePath = "docs/specs/fixtures/combat-evidence-legacy-v1.zip") : IDisposable
    {
        private readonly System.IO.Compression.ZipArchive archive = new(new MemoryStream(Dependency(archivePath)));
        private readonly Dictionary<string, byte[]> cache = new(StringComparer.Ordinal);
        public byte[] Read(string path)
        {
            if (!cache.TryGetValue(path, out var bytes))
            {
                var entry = archive.GetEntry(path);
                if (entry is null) bytes = Dependency(path);
                else
                {
                    using var input = entry.Open(); using var output = new MemoryStream();
                    input.CopyTo(output); bytes = output.ToArray();
                }
                cache.Add(path, bytes);
            }
            return bytes.ToArray();
        }
        public void Dispose() => archive.Dispose();
    }

    private static void Check(JsonElement cut, string kind, byte[] actual)
    {
        Assert.Equal(cut.GetProperty(kind).GetProperty("bytes").GetInt32(), actual.Length);
        Assert.Equal(cut.GetProperty(kind).GetProperty("sha256").GetString(), "sha256:" + Convert.ToHexStringLower(SHA256.HashData(actual)));
    }
    [Fact]
    public void ForgedClaimsAndCrossOwnerClaimsCannotAuthorizeState()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        var result = context.Replay();
        var forged = JsonNode.Parse(result.ControlBytes)!;
        forged["stateVersion"] = 19;
        var claim = CampaignCombatActualMovedAssaultCodec.Canonical(forged, "FamilyControl");
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.ReadControl(claim, context.Source, context.Request,
            context.Prefix, context.Lifecycle, context.Breakdown, [], [], [], Dependency));
        var other = new Context("commonwealth-player-decline-attacker-selected").Replay();
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.ReadProof(other.ProofBytes, context.Source, context.Request,
            context.Prefix, context.Lifecycle, context.Breakdown, [], [], [], Dependency));
        forged = JsonNode.Parse(result.ProofBytes)!; forged["lastEventHash"] = "sha256:" + new string('0', 64);
        claim = CampaignCombatActualMovedAssaultCodec.Canonical(forged, "FamilyProof");
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.ReadProof(claim, context.Source, context.Request,
            context.Prefix, context.Lifecycle, context.Breakdown, [], [], [], Dependency));
    }
    [Fact]
    public void SourceGrammarRejectsExtraDuplicateNoncanonicalAndOversizeBytesBeforeDependencies()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        var text = Encoding.ASCII.GetString(context.Source);
        var malformed = new[] { Encoding.ASCII.GetBytes(text.Insert(1, "\"extra\":1,")), Encoding.ASCII.GetBytes(text.Insert(1, "\"contractVersion\":1,")),
            Encoding.ASCII.GetBytes(" " + text), new byte[8_388_609] };
        foreach (var bytes in malformed)
        {
            var calls = 0;
            Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.ReplayTrustedSource(bytes, context.Request,
                context.Prefix, context.Lifecycle, context.Breakdown, [], [], [], p => { calls++; return Dependency(p); }));
            Assert.Equal(0, calls);
        }
    }
    [Fact]
    public void StructuralMaximumWireShapeAndJustOverLimitsAreIndependentOfTrustedAdmission()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        var packet = JsonNode.Parse(context.Source)!.AsObject();
        packet["reactionEntrySourceCanonicalUtf8"] = new string('\\', 1_048_576);
        foreach (var (field, count) in new[] { ("selectionEventCanonicalUtf8", 16), ("roundEventCanonicalUtf8", 16), ("paidEventCanonicalUtf8", 11) })
            packet[field] = new JsonArray(Enumerable.Range(0, count).Select(_ => (JsonNode?)JsonValue.Create(new string('\\', 65_536))).ToArray());
        var encoded = CampaignCombatActualMovedAssaultCodec.Canonical(packet, "Source");
        Assert.InRange(encoded.Length, 7_700_000, 8_388_608);
        CampaignCombatActualMovedAssaultCodec.SourceBounds(CampaignCombatActualMovedAssaultCodec.Object(encoded, "Source"));
        packet["paidEventCanonicalUtf8"]!.AsArray().Add(new string('x', 65_536));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssaultCodec.SourceBounds(packet));
        packet["paidEventCanonicalUtf8"]!.AsArray().RemoveAt(11);
        packet["reactionEntrySourceCanonicalUtf8"] = new string('x', 1_048_577);
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssaultCodec.SourceBounds(packet));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssaultCodec.Copy(new byte[65_537], "PaidEvent"));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssaultCodec.Copy(new byte[4_097], "Envelope"));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssaultCodec.Copy(new byte[524_289], "FamilyControl"));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssaultCodec.Copy(new byte[2_097_153], "FamilyProof"));
    }
    [Fact]
    public void FreshAdmissionDisabledPreservesAcceptedRetryAndAlreadyPaidCompletion()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        Assert.Throws<JsonException>(() => Disabled(context, context.Inputs[0]));
        context.Step(0);
        var retry = Disabled(context, context.Inputs[0]); Assert.True(retry.Duplicate);
        while (context.Paid.Count == 0 && context.Selection.Count + context.Round.Count < 12) context.Step(context.Selection.Count + context.Round.Count);
        var index = context.Selection.Count + context.Round.Count;
        Assert.Equal(12, index);
        Assert.Throws<JsonException>(() => Disabled(context, context.Inputs[index]));
        var commit = context.Step(index++); var before = context.Source.ToArray();
        Assert.Equal(commit.ProofBytes, Disabled(context, context.Inputs[index - 1]).ProofBytes);
        Assert.Equal(before, context.Source);
        var completion = Disabled(context, context.Inputs[index]);
        Assert.NotNull(completion.EventBytes);
    }
    private static CombatActualMovedAssaultResult Disabled(Context c, byte[] input) => CampaignCombatActualMovedAssault.ApplyTrustedSource(c.Source, c.Request,
        c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, input, Dependency, false);
    [Fact]
    public void MissingHandlerCapacityOrCatalogueRowBlocksWithoutChangingPreparedWorld()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        for (var i = 0; i < 12; i++) context.Step(i);
        var control = JsonNode.Parse(context.Replay().ControlBytes)!;
        var frame = new PaidAssaultFrame(new JsonObject(), control["round"]!.AsObject(), control["selection"]!["selection"]!.AsObject(),
            new JsonObject(), control["cycle"]!.AsObject(), context.Request.Context.Configuration,
            JsonNode.Parse(Dependency("docs/specs/fixtures/combat-content-v7.canonical.json"))!.AsObject());
        var original = frame.Prior.ToJsonString();
        var handlers = CampaignCombatActualPaidAssaultSupport.SupportedHandlers().ToHashSet(StringComparer.Ordinal);
        handlers.Remove(handlers.First());
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, handlers: handlers));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, capacities: new Dictionary<string, int>()));
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, records: []));
        var rows = CampaignCombatActualPaidAssaultSupport.Catalogue().Select(r => r.DeepClone().AsObject()).ToArray();
        rows[0]["ordinal"] = 1;
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, records: rows));
        var bounds = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["PaidInput"] = 4096,
            ["PaidEvent"] = 65536,
            ["PaidControl"] = 524288,
            ["PaidProof"] = 2097152,
            ["AssaultResult"] = 65536,
            ["World"] = 524288,
            ["PaidSource"] = 7733760,
            ["PaidLedger"] = 45056,
            ["acceptedEvents"] = 11,
            ["acceptedReceipts"] = 11,
            ["draws"] = 9,
            ["consumedBytesPerDraw"] = 512,
            ["routes"] = 9
        };
        foreach (var kind in bounds.Keys)
        {
            var shortByOne = new Dictionary<string, int>(bounds, StringComparer.Ordinal); shortByOne[kind]--;
            Assert.Throws<JsonException>(() => CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, capacities: shortByOne));
        }
        CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, capacities: bounds);
        var larger = bounds.ToDictionary(p => p.Key, p => p.Value + 1, StringComparer.Ordinal);
        CampaignCombatActualMovedAssault.CertifyMovedSupport(frame, capacities: larger);
        Assert.Equal(original, frame.Prior.ToJsonString());
        Assert.Equal(2UL, context.Replay().Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(context.Item.GetProperty("cuts")[12].GetProperty("source").GetProperty("sha256").GetString(),
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(context.Source)));
    }
    [Fact]
    public void AllSixLedgersAreOwnedBeforeDependencyCallbacksAndResultBuffersAreIndependent()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        for (var i = 0; i < 13; i++) context.Step(i);
        var before = context.Replay(); var input = context.Inputs[13].ToArray();
        var ledgers = new[] { context.Prefix, context.Lifecycle, context.Breakdown,
            context.Selection.ToArray(), context.Round.ToArray(), context.Paid.ToArray() };
        var originals = ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray();
        var called = false;
        var result = CampaignCombatActualMovedAssault.ApplyTrustedSource(context.Source, context.Request,
            context.Prefix, context.Lifecycle, context.Breakdown, context.Selection, context.Round, context.Paid, input, path =>
            {
                if (!called) { called = true; foreach (var ledger in ledgers) ledger[0][0] = 0; context.Source[0] = 0; input[0] = 0; }
                return Dependency(path);
            });
        Assert.NotNull(result.EventBytes);
        var returned = new[] { result.PrefixInputs, result.LifecycleInputs, result.BreakdownInputs, result.SelectionInputs, result.RoundInputs, result.PaidInputs };
        for (var i = 0; i < 6; i++) for (var j = 0; j < originals[i].Length; j++) Assert.Equal(originals[i][j], returned[i][j]);
        var source = result.SourceBytes; var proof = result.ProofBytes; var control = result.ControlBytes; var accepted = result.EventBytes!;
        source[0] = 0; proof[0] = 0; control[0] = 0; accepted[0] = 0; returned[0][0][0] = 0;
        Assert.NotEqual((byte)0, result.SourceBytes[0]); Assert.NotEqual((byte)0, result.ProofBytes[0]);
        Assert.NotEqual((byte)0, result.ControlBytes[0]); Assert.NotEqual((byte)0, result.EventBytes![0]);
        Assert.Equal(originals[0][0], result.PrefixInputs[0]);
        Assert.Equal(31, before.Control.GetProperty("stateVersion").GetInt64());
    }
    [Theory]
    [InlineData("docs/specs/combat-actual-moved-assault-v1.schema.json")]
    [InlineData("docs/specs/verify-combat-actual-moved-assault-v1.py")]
    public void OwnedDeliveryBytesAreRevalidatedOnWarmReplay(string path)
    {
        var context = new Context("axis-player-decline-attacker-selected"); context.Replay();
        Assert.Throws<JsonException>(() => context.Replay(p => { var bytes = Dependency(p); if (p == path) bytes[0] ^= 1; return bytes; }));
        context.Replay();
    }
    [Fact]
    public void MalformedRetryTimeAndActorCannotBypassPrimitiveChecks()
    {
        var context = new Context("axis-player-decline-attacker-selected"); context.Step(0);
        foreach (var actor in new[] { "foreign", "axis" })
        {
            var retry = JsonNode.Parse(context.Inputs[0])!; retry["input"]!["actor"] = actor;
            Assert.Throws<JsonException>(() => context.Apply(CampaignCombatActualMovedAssaultCodec.Canonical(retry, "Envelope")));
        }
        var malformed = JsonNode.Parse(context.Inputs[0])!; malformed["input"]!["admittedAt"] = -1;
        Assert.ThrowsAny<JsonException>(() => context.Apply(Encoding.UTF8.GetBytes(malformed.ToJsonString())));
        var valid = JsonNode.Parse(context.Inputs[0])!; valid["input"]!["admittedAt"] = 9999; valid["input"]!["clockAvailable"] = false;
        Assert.True(context.Apply(CampaignCombatActualMovedAssaultCodec.Canonical(valid, "Envelope")).Duplicate);
    }
    [Theory]
    [InlineData("attacker", true, "player")]
    [InlineData("attacker", true, "deadline")]
    [InlineData("attacker", true, "unavailable")]
    [InlineData("attacker", false, "player")]
    [InlineData("attacker", false, "deadline")]
    [InlineData("attacker", false, "unavailable")]
    [InlineData("defender", false, "player")]
    [InlineData("defender", false, "deadline")]
    [InlineData("defender", false, "unavailable")]
    public void IsolatedOptionalHandlerSupportPreservesCumulativeCostsAndWindowRecovery(string capturedRole, bool retreat, string reason)
    {
        var context = new Context("axis-player-decline-attacker-selected");
        for (var i = 0; i < 12; i++) context.Step(i);
        var prepared = JsonNode.Parse(context.Replay().ControlBytes)!;
        var packet = JsonNode.Parse(context.Source)!;
        var entryHash = CampaignCombatActualMovedAssaultCodec.Hash(Encoding.ASCII.GetBytes(packet["reactionEntrySourceCanonicalUtf8"]!.GetValue<string>()));
        var selectionPacket = packet.DeepClone(); selectionPacket["roundEventCanonicalUtf8"] = new JsonArray();
        var basis = new JsonObject
        {
            ["contractVersion"] = 1,
            ["entrySourceHash"] = entryHash,
            ["selectionSourceHash"] = CampaignCombatActualMovedAssaultCodec.Hash(CampaignCombatActualMovedAssaultCodec.Canonical(selectionPacket, "Source")),
            ["preparedSourceHash"] = CampaignCombatActualMovedAssaultCodec.Hash(context.Source),
            ["preparedWorldHash"] = CampaignCombatActualMovedAssaultCodec.Hash(CampaignCombatActualMovedAssaultCodec.Canonical(prepared["round"]!["world"]!, "World")),
            ["configurationHash"] = prepared["cycle"]!["admittedPolicyBundleDigest"]!.DeepClone()
        };
        var parent = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(Encoding.ASCII.GetBytes(packet["reactionEntrySourceCanonicalUtf8"]!.GetValue<string>()),
            context.Request, context.Prefix, context.Lifecycle, context.Breakdown, Dependency);
        var boundary = new JsonObject { ["cycleId"] = prepared["cycleId"]!.DeepClone(), ["position"] = JsonNode.Parse(parent.ControlBytes)!["sequencePosition"]!.DeepClone() };
        var frame = new PaidAssaultFrame(basis, prepared["round"]!.AsObject(), prepared["selection"]!["selection"]!.AsObject(),
            boundary, prepared["cycle"]!.AsObject(), context.Request.Context.Configuration,
            JsonNode.Parse(Dependency("docs/specs/fixtures/combat-content-v7.canonical.json"))!.AsObject());
        context.Step(12); context.Step(13);
        var state = JsonNode.Parse(context.Replay().ControlBytes)!["paid"]!.AsObject();
        // Only isolated arithmetic/handler state: never encoded into an ingress Source or claimed as authentic RNG history.
        var row = CampaignCombatActualPaidAssaultSupport.Catalogue().First(r => r["kind"]!.GetValue<string>() == "capture-refusal" &&
            r["facts"]!["capturedRole"]?.GetValue<string>() == capturedRole && r["facts"]!["captureShare"]!.GetValue<int>() > 0 &&
            (r["facts"]!["requiredRetreat"]!.GetValue<int>() > 0) == retreat);
        state["result"]!["facts"] = row["facts"]!.DeepClone();
        var random = state["randomState"]!.ToJsonString();
        var windows = new HashSet<string>(StringComparer.Ordinal);
        for (var steps = 0; steps < 11 && !state["closed"]!.GetValue<bool>(); steps++)
        {
            var window = state["window"]; var status = state["status"]!.GetValue<string>();
            var kind = window is null ? "advance" : reason == "player" ? "choose" : reason == "deadline" ? "expire" : "unavailable";
            var command = new JsonObject
            {
                ["contractVersion"] = 1,
                ["kind"] = kind,
                ["baseHash"] = state["baseHash"]!.DeepClone(),
                ["roundId"] = frame.Prior["roundId"]!.DeepClone(),
                ["commitmentId"] = state["commitmentId"]!.DeepClone(),
                ["expectedPriorVersion"] = window is null ? state["stateVersion"]!.DeepClone() : null,
                ["decisionId"] = window?["decisionId"]?.DeepClone(),
                ["choice"] = null
            };
            string actor = "system"; long? now = null; var available = true;
            if (window is not null)
            {
                var windowKind = window["kind"]!.GetValue<string>(); windows.Add(windowKind);
                Assert.Equal(10000, window["timing"]!["openedAtUnixMilliseconds"]!.GetValue<long>());
                if (kind == "choose") { actor = window["owner"]!.GetValue<string>(); command["choice"] = windowKind == "retreat" ? "retreat" : "relocate-and-guard"; now = 10001; }
                if (kind == "expire") now = window["timing"]!["deadlineUnixMilliseconds"]!.GetValue<long>();
                if (kind == "unavailable") available = false;
            }
            else if (status == "resolved" && retreat || status == "retreat") now = 10000;
            var input = new JsonObject { ["command"] = command, ["actor"] = actor, ["admittedAt"] = now, ["clockAvailable"] = available };
            var (after, accepted, _) = CampaignCombatActualMovedAssault.PaidTransition(frame, state, input, false);
            Assert.NotNull(accepted); Assert.Equal(random, after["randomState"]!.ToJsonString());
            var changed = input.DeepClone().AsObject(); changed["admittedAt"] = 20000L; changed["clockAvailable"] = false;
            var (retry, repeated, _) = CampaignCombatActualMovedAssault.PaidTransition(frame, after, changed, false);
            Assert.Null(repeated); Assert.Equal(after.ToJsonString(), retry.ToJsonString());
            state = after;
        }
        Assert.True(state["closed"]!.GetValue<bool>()); Assert.Contains("custody", windows);
        Assert.Equal(retreat, windows.Contains("retreat")); Assert.InRange(JsonSerializer.SerializeToElement(state["stateVersion"]).GetInt64(), 39, 41);
        var elements = state["world"]!["elements"]!.AsArray();
        var attacker = elements.Single(e => e!["elementId"]!.GetValue<string>() == frame.Selection["attacker"]!["unit"]!["elementId"]!.GetValue<string>())!;
        var defender = elements.Single(e => e!["elementId"]!.GetValue<string>() == frame.Selection["defender"]!["unit"]!["elementId"]!.GetValue<string>())!;
        Assert.Equal(9, JsonSerializer.SerializeToElement(attacker["operationalState"]!["capabilityPointsExpended"]!["numerator"]).GetInt32());
        Assert.InRange(JsonSerializer.SerializeToElement(defender["operationalState"]!["capabilityPointsExpended"]!["numerator"]).GetInt32(), 3, 10);
        Assert.Equal(0, attacker["ammunition"]!["points"]!.GetValue<int>()); Assert.Equal(0, defender["ammunition"]!["points"]!.GetValue<int>());
        Assert.NotNull(state["roundClosureReceiptId"]); Assert.NotNull(state["caCompletionReceiptId"]);
        var world = state["world"]!; var settlement = world["settlements"]![0]!;
        var lot = Assert.Single(world["custodyLots"]!.AsArray());
        var guarded = reason == "player"; Assert.Equal(guarded ? "guarded" : "escaped", lot!["status"]!.GetValue<string>());
        Assert.Equal(guarded ? 1 : 0, world["guards"]!.AsArray().Count);
        Assert.Equal(guarded ? 0 : 1, world["replacementEntitlements"]!.AsArray().Count);
        var obligation = Assert.Single(world["futureObligations"]!.AsArray());
        Assert.Equal("retained-unimplemented", obligation!["status"]!.GetValue<string>());
        Assert.Equal(guarded ? "guard-priority-upkeep" : "replacement-training-gate", obligation["kind"]!.GetValue<string>());
        if (guarded)
        {
            var custody = settlement["custody"]!;
            Assert.Equal(1, JsonSerializer.SerializeToElement(custody["donorToeBefore"]).GetInt32() - JsonSerializer.SerializeToElement(custody["donorToeAfter"]).GetInt32());
            Assert.Equal(0, world["guards"]![0]!["ammunition"]!["points"]!.GetValue<int>());
        }
        else
        {
            var entitlement = world["replacementEntitlements"]![0]!;
            Assert.Equal(12, entitlement["delayOperationStages"]!.GetValue<int>());
            Assert.Equal(5, JsonSerializer.SerializeToElement(entitlement["eligibleScope"]!["gameTurn"]).GetInt32());
            Assert.Equal(1, JsonSerializer.SerializeToElement(entitlement["eligibleScope"]!["operationStage"]).GetInt32());
        }
        var surviving = elements.Sum(e => JsonSerializer.SerializeToElement(e!["components"]![0]!["currentToe"]).GetInt32());
        var guardToe = world["guards"]!.AsArray().Sum(g => JsonSerializer.SerializeToElement(g!["toe"]).GetInt32());
        var removed = settlement["losses"]!["roles"]!.AsArray().Sum(r => JsonSerializer.SerializeToElement(r!["lossToe"]).GetInt32());
        Assert.Equal(20, surviving + guardToe + removed);
    }
    private sealed class Context
    {
        public JsonElement Item { get; }
        public CampaignCombatCreationRequest Request { get; }
        public byte[][] Prefix { get; }
        public byte[][] Lifecycle { get; }
        public byte[][] Breakdown { get; }
        public List<byte[]> Selection { get; } = [];
        public List<byte[]> Round { get; } = [];
        public List<byte[]> Paid { get; } = [];
        public byte[] Source { get; private set; }
        public byte[][] Inputs { get; }

        private readonly Func<string, byte[]> read;

        public Context(string name, Func<string, byte[]>? dependencies = null)
        {
            read = dependencies ?? Dependency;
            using var fixture = JsonDocument.Parse(read("docs/specs/fixtures/combat-actual-moved-assault-v1.json"));
            Item = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name).Clone();
            using var authority = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
            using var created = JsonDocument.Parse(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
            var artifact = Cna1979CombatContentCatalog.Artifact;
            var scenario = Assert.Single(artifact.Definition.Scenarios);
            var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
            var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
            Request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 1,
                new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
            Prefix = Bytes(Item.GetProperty("prefixInputCanonicalUtf8"));
            Lifecycle = Bytes(Item.GetProperty("lifecycleInputCanonicalUtf8"));
            Breakdown = Bytes(Item.GetProperty("breakdownInputCanonicalUtf8"));
            Inputs = Bytes(Item.GetProperty("inputCanonicalUtf8"));
            Source = Encoding.UTF8.GetBytes(Item.GetProperty("initialSourceCanonicalUtf8").GetString()!);
        }

        public CombatActualMovedAssaultResult Replay(Func<string, byte[]>? dependency = null) =>
            CampaignCombatActualMovedAssault.ReplayTrustedSource(Source, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, dependency ?? read);

        public CombatActualMovedAssaultResult Apply(byte[] input) =>
            CampaignCombatActualMovedAssault.ApplyTrustedSource(Source, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, input, read);

        public CombatActualMovedAssaultResult Step(int index)
        {
            var result = Apply(Inputs[index]);
            using var envelope = JsonDocument.Parse(Inputs[index]);
            var phase = envelope.RootElement.GetProperty("phase").GetString();
            (phase == "selection" ? Selection : phase == "round" ? Round : Paid).Add(Inputs[index].ToArray());
            Source = result.SourceBytes;
            return result;
        }
    }

    private static byte[][] Bytes(JsonElement values) => values.EnumerateArray().Select(v => Encoding.UTF8.GetBytes(v.GetString()!)).ToArray();
    private static byte[] Dependency(string path)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Sandtable.slnx"))) root = root.Parent;
        return File.ReadAllBytes(Path.Combine(root!.FullName, path));
    }
    private static readonly JsonSerializerOptions Options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
