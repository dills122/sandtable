using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Cna.Core.Campaigns;

/// <summary>Owned additive source syntax; only strict replay establishes authority.</summary>
internal static class CampaignCombatActualReactionCombatEntryCodec
{
    internal sealed record Packet(byte[] Created, byte[][] Prefix, byte[][] Lifecycle, byte[][] Breakdown);
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    internal static byte[] Encode(JsonNode node) => JsonSerializer.SerializeToUtf8Bytes(node, Options);
    internal static string Hash(ReadOnlySpan<byte> bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    internal static byte[] Copy(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("CMB-RCE-001");
        return bytes.ToArray();
    }
    internal static JsonObject Object(byte[] bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("CMB-RCE-001");
        using var doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
        Check(doc.RootElement);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("CMB-RCE-001");
        return JsonNode.Parse(bytes)!.AsObject();
    }
    private static void Check(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            { if (!seen.Add(property.Name)) throw new JsonException("CMB-RCE-001"); Check(property.Value); }
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 512) throw new JsonException("CMB-RCE-001");
            foreach (var item in value.EnumerateArray()) Check(item);
        }
    }
    internal static Packet Parse(ReadOnlySpan<byte> bytes)
    {
        var copy = Copy(bytes);
        try
        {
            var obj = Object(copy);
            string[] fields = ["contractVersion", "createdCanonicalUtf8", "prefixEventCanonicalUtf8", "lifecycleEventCanonicalUtf8", "breakdownEventCanonicalUtf8"];
            if (!obj.Select(x => x.Key).SequenceEqual(fields) || obj["contractVersion"]?.GetValue<int>() != 1)
                throw new JsonException("CMB-RCE-001");
            var packet = new Packet(Text(obj["createdCanonicalUtf8"]), Array(obj["prefixEventCanonicalUtf8"]),
                Array(obj["lifecycleEventCanonicalUtf8"]), Array(obj["breakdownEventCanonicalUtf8"]));
            if (packet.Prefix.Length != 13 || packet.Lifecycle.Length > 3 || packet.Breakdown.Length > 1 ||
                packet.Breakdown.Length > 0 && packet.Lifecycle.Length != 3) throw new JsonException("CMB-RCE-001");
            if (!copy.AsSpan().SequenceEqual(Write(packet))) throw new JsonException("CMB-RCE-008");
            return packet;
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or FormatException or OverflowException)
        { throw new JsonException("CMB-RCE-001", e); }
    }
    private static byte[] Text(JsonNode? node)
    {
        var text = node?.GetValue<string>() ?? throw new JsonException("CMB-RCE-001");
        if (text.Length is 0 or > 1_048_576 || text.Any(c => c > 127)) throw new JsonException("CMB-RCE-001");
        return Encoding.ASCII.GetBytes(text);
    }
    private static byte[][] Array(JsonNode? node) => (node as JsonArray ?? throw new JsonException("CMB-RCE-001")).Select(Text).ToArray();
    internal static byte[] Write(Packet packet)
    {
        JsonArray Strings(byte[][] values) => new(values.Select(v => (JsonNode?)JsonValue.Create(Encoding.ASCII.GetString(v))).ToArray());
        var bytes = Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["createdCanonicalUtf8"] = Encoding.ASCII.GetString(packet.Created),
            ["prefixEventCanonicalUtf8"] = Strings(packet.Prefix),
            ["lifecycleEventCanonicalUtf8"] = Strings(packet.Lifecycle),
            ["breakdownEventCanonicalUtf8"] = Strings(packet.Breakdown)
        });
        if (bytes.Length > 1_048_576) throw new JsonException("CMB-RCE-001");
        return bytes;
    }
    internal static byte[][] Inputs(IReadOnlyList<byte[]> inputs, int count)
    {
        if (inputs is null || inputs.Count != count) throw new JsonException("CMB-RCE-004");
        return inputs.Select(i =>
        {
            if (i is null || i.Length is 0 or > 1_048_576) throw new JsonException("CMB-RCE-001");
            var owned = i.ToArray(); _ = Object(owned); return owned;
        }).ToArray();
    }
    internal static void CompareInput(byte[] ev, byte[] trusted)
    {
        using var doc = JsonDocument.Parse(ev);
        if (!doc.RootElement.TryGetProperty("input", out var input) ||
            !Encoding.UTF8.GetBytes(input.GetRawText()).AsSpan().SequenceEqual(trusted)) throw new JsonException("CMB-RCE-004");
    }
    // The base is produced by strict actual Reaction replay, never a caller projection.
    internal static byte[] SerializeControl(byte[] closureControl, CampaignCombatBreakdownCompletionState state)
    {
        var obj = Object(closureControl);
        obj.Remove("currentPosition"); obj.Remove("reactionWindow");
        obj["stateVersion"] = state.StateVersion; obj["prefix"] = state.Prefix;
        obj["interruptContext"] = null; obj["movementEnd"] = null; obj["breakdownCompletionReceiptId"] = state.BreakdownCompletionReceiptId;
        foreach (var ev in state.Lifecycle.Events)
        {
            var node = Object(CampaignCombatMovementLifecycleCodec.SerializeEvent(ev));
            obj["sequencePosition"] = node["sequencePosition"]!.DeepClone();
            obj["breakdownFlow"] = node["breakdownFlowAfter"]!.DeepClone();
            obj["interruptContext"] = node["interruptContextAfter"]?.DeepClone();
            if (ev.Input.Command is CampaignCombatMovementLifecycleCommand.Complete)
            {
                var cycle = obj["cycle"]!;
                obj["movementEnd"] = new JsonObject
                {
                    ["scope"] = new JsonObject
                    {
                        ["gameTurn"] = cycle["gameTurn"]!.DeepClone(),
                        ["operationStage"] = cycle["operationStage"]!.DeepClone(),
                        ["playerPhaseSlot"] = cycle["playerPhaseSlot"]!.DeepClone(),
                        ["actingSide"] = cycle["actingSide"]!.DeepClone()
                    },
                    ["ordinal"] = 1,
                    ["completionReceiptId"] = ev.ReceiptId,
                    ["endLocations"] = node["endLocations"]!.DeepClone(),
                    ["excludedBefore"] = node["excludedUnits"]!.DeepClone()
                };
            }
        }
        if (state.Completion is not null)
            obj["sequencePosition"] = Object(CampaignCombatBreakdownCompletionCodec.SerializeEvent(state.Completion))["sequencePosition"]!.DeepClone();
        obj["receipts"] = new JsonArray(state.Receipts.Select(r => (JsonNode?)new JsonObject
        {
            ["commandHash"] = r.CommandHash,
            ["eventHash"] = r.EventHash,
            ["receiptId"] = r.ReceiptId,
            ["actor"] = r.Actor.ToString().ToLowerInvariant(),
            ["stateVersion"] = r.StateVersion
        }).ToArray());
        using var doc = JsonDocument.Parse(Encode(obj));
        return CampaignCombatPositiveEntryCodec.Canonical(doc.RootElement, "CombatEntryState");
    }
    private static readonly IReadOnlyDictionary<string, string> Pins = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["src/Cna.Core/Campaigns/CampaignCombatReactionClosure.cs"] = "fad532a53a58918ced14f7e38898aeb8dc8161e68e79a9c327de55000ee86360",
        ["src/Cna.Core/Campaigns/CampaignCombatReactionClosureCodec.cs"] = "3db05ef3a20b2f3cd0527ec4c4ba11c3d515a6731ec99305bb1399b3c9768e36",
        ["src/Cna.Core/Campaigns/CampaignCombatReactionTrigger.cs"] = "f85f23507429d8dcbddc23d9ae2d835571d64844b71d712083cf633a72f60218",
        ["src/Cna.Core/Campaigns/CampaignCombatMovementLifecycle.cs"] = "e63a4d0225f5347ce6a70a855c34272110ce75a3b258591b50115c3ca5fb07aa",
        ["src/Cna.Core/Campaigns/CampaignCombatMovementLifecycleCodec.cs"] = "d0f5d587c7ce9fe33e20acc670d65ac525f1c6214ef9db959a36c642621c9171",
        ["src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletion.cs"] = "fe24d5a524383c1e5137b3d9e8a0050fc21fd381c4dc7e59812d68dc3a24dc42",
        ["src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletionCodec.cs"] = "e1160fcf932f938ba43aabbc2bc52b97b973228145a30b15ac6b747f00e359c4",
        ["src/Cna.Core/Campaigns/CampaignCombatCertification.cs"] = "2d3859c248783d07772753b5105848c4555cd7b7e146ea7cd9d740316cb705da",
        ["docs/specs/verify-combat-inherited-reaction-closure-v1.py"] = "9f07f00f8b749c2b3d86f6d3a2e40edbe0eee6af0a147e25d4d87b1ad287355c",
        ["docs/specs/verify-combat-inherited-movement-lifecycle-v1.py"] = "f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920",
        ["docs/specs/verify-combat-inherited-breakdown-completion-v1.py"] = "db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba",
        ["docs/specs/fixtures/combat-content-v7.canonical.json"] = "ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
    };
    internal static void Dependencies(Func<string, byte[]> retained)
    {
        ArgumentNullException.ThrowIfNull(retained);
        foreach (var (path, digest) in Pins)
        {
            byte[] data;
            try { data = retained(path)?.ToArray() ?? throw new JsonException("CMB-RCE-009"); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException)
            { throw new JsonException("CMB-RCE-009", e); }
            if (Hash(data) != "sha256:" + digest) throw new JsonException("CMB-RCE-009");
        }
    }
}
