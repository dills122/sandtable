using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Cna.Core.Campaigns;

/// <summary>Closed moved-family syntax and owned retained-byte acquisition, never admission by cache.</summary>
internal static class CampaignCombatActualMovedRepeatMovementCompletionCodec
{
    internal static void Require([DoesNotReturnIf(false)] bool condition, int code)
    { if (!condition) throw new JsonException($"CMB-MRC-{code:000}"); }
    internal static int ByteLimit(string kind) => kind switch
    {
        "Source" => 134_217_728,
        "Proof" => 2_097_152,
        "World" => 1_048_576,
        "ParentSource" => 67_108_864,
        "AncestorInput" => 8_388_608,
        "Event" => 65_536,
        "Input" or "Command" => 4_096,
        _ => 1_048_576
    };
    internal static byte[] Encode(JsonNode value) => CampaignCombatActualPaidAssaultCodec.Encode(value);
    internal static string Hash(ReadOnlySpan<byte> data) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(data));
    internal static string HashWithDomain(string domain, ReadOnlySpan<byte> data)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.ASCII.GetBytes(domain)); hash.AppendData([0]); hash.AppendData(data);
        return "sha256:" + Convert.ToHexStringLower(hash.GetHashAndReset());
    }
    internal static string EventPrefix(string prior, ReadOnlySpan<byte> data)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData("sandtable.cycle.prefix.event.v1\0"u8); hash.AppendData(Convert.FromHexString(prior[7..]));
        Span<byte> length = stackalloc byte[8]; BinaryPrimitives.WriteUInt64BigEndian(length, checked((ulong)data.Length)); hash.AppendData(length); hash.AppendData(data);
        return "sha256:" + Convert.ToHexStringLower(hash.GetHashAndReset());
    }
    internal static byte[] Copy(ReadOnlySpan<byte> data, string kind)
    { Require(data.Length > 0 && data.Length <= ByteLimit(kind), 1); return data.ToArray(); }
    internal static JsonObject Object(ReadOnlySpan<byte> bytes, string kind)
    {
        var owned = Copy(bytes, kind); Preparse(owned);
        try
        {
            using var doc = JsonDocument.Parse(owned, new JsonDocumentOptions { MaxDepth = 41 });
            Require(owned.AsSpan().SequenceEqual(Canonical(doc.RootElement, kind)), 8);
            return JsonNode.Parse(owned)!.AsObject();
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-MRC-", StringComparison.Ordinal)) { throw new JsonException("CMB-MRC-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("CMB-MRC-001", e); }
    }
    private static void Preparse(ReadOnlySpan<byte> data)
    {
        Span<byte> starts = stackalloc byte[41]; Span<int> commas = stackalloc int[41]; Span<bool> occupied = stackalloc bool[41];
        var depth = 0; var quoted = false; var escaped = false;
        foreach (var c in data)
        {
            Require(c < 128, 1);
            if (quoted) { if (escaped) escaped = false; else if (c == 92) escaped = true; else if (c == 34) quoted = false; continue; }
            if (c is 9 or 10 or 13 or 32) continue;
            if (c == 34) { if (depth > 0) occupied[depth - 1] = true; quoted = true; continue; }
            if (c is 123 or 91)
            { if (depth > 0) occupied[depth - 1] = true; Require(depth < 41, 1); starts[depth] = c; commas[depth] = 0; occupied[depth] = false; depth++; continue; }
            if (c is 125 or 93)
            {
                Require(depth > 0 && (starts[depth - 1] == 123 && c == 125 || starts[depth - 1] == 91 && c == 93), 1);
                depth--; Require(commas[depth] + (occupied[depth] ? 1 : 0) <= (starts[depth] == 91 ? 512 : 128), 1); continue;
            }
            if (c == 44 && depth > 0) { commas[depth - 1]++; Require(commas[depth - 1] < (starts[depth - 1] == 91 ? 512 : 128), 1); }
            else if (depth > 0) occupied[depth - 1] = true;
        }
        Require(!quoted && depth == 0, 1);
    }
    internal static byte[] Canonical(JsonNode value, string kind) => kind == "Source" ? CanonicalSource(value) : Canonical(JsonSerializer.SerializeToElement(value), kind);
    private static byte[] Canonical(JsonElement value, string kind)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            Write(writer, value, kind, 0);
        var bytes = stream.ToArray(); Require(bytes.Length <= ByteLimit(kind), 1);
        return CampaignCombatActualPaidAssaultCodec.Encode(JsonNode.Parse(bytes)!);
    }
    private static readonly string[] SourceFields = ["contractVersion", "movementSourceCanonicalUtf8", "lifecycleEventCanonicalUtf8"];
    private static byte[] CanonicalSource(JsonNode value)
    {
        Require(value is JsonObject, 1); var fields = value.AsObject().Select(p => p.Key).ToArray();
        Require(fields.Length == 3 && fields.ToHashSet(StringComparer.Ordinal).SetEquals(SourceFields), 1);
        SourceVersion(JsonSerializer.SerializeToElement(value["contractVersion"]));
        Require(value["lifecycleEventCanonicalUtf8"] is JsonArray && value["lifecycleEventCanonicalUtf8"]!.AsArray().Count <= 3, 1);
        return SourceBytes(Carrier(value["movementSourceCanonicalUtf8"]), value["lifecycleEventCanonicalUtf8"]!.AsArray().Select(Carrier).ToArray());
    }
    private static byte[] CanonicalSource(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.Object, 1); var fields = value.EnumerateObject().Select(p => p.Name).ToArray();
        Require(fields.Length == 3 && fields.Distinct(StringComparer.Ordinal).Count() == 3 && fields.ToHashSet(StringComparer.Ordinal).SetEquals(SourceFields), 1);
        SourceVersion(value.GetProperty("contractVersion")); var events = value.GetProperty("lifecycleEventCanonicalUtf8");
        Require(value.GetProperty("movementSourceCanonicalUtf8").ValueKind == JsonValueKind.String && events.ValueKind == JsonValueKind.Array && events.GetArrayLength() <= 3, 1);
        var carriers = events.EnumerateArray().Select(e => { Require(e.ValueKind == JsonValueKind.String, 1); return e.GetString()!; }).ToArray();
        return SourceBytes(value.GetProperty("movementSourceCanonicalUtf8").GetString()!, carriers);
    }
    private static string Carrier(JsonNode? value)
    {
        Require(value is JsonValue, 1); var valid = value.AsValue().TryGetValue<string>(out var text); Require(valid && text is not null, 1); return text;
    }
    private static void SourceVersion(JsonElement value)
    {
        _ = Inherited(() => CampaignCombatActualMovedAssaultCodec.Canonical(JsonNode.Parse(value.GetRawText())!, "int"));
        Require(value.GetInt32() == 1, 3);
    }
    private static byte[] SourceBytes(string parent, string[] events)
    {
        Require(parent.Length is > 0 and <= 67108864 && events.Length <= 3 && events.All(e => e.Length is > 0 and <= 65536), 1);
        // Source carriers are ASCII. Emit Python-compatible escaping directly, without serializing
        // a 128MiB envelope through multiple JsonElement/JsonNode/UTF-16 copies.
        long Cost(string text)
        {
            long count = 2;
            foreach (var c in text) { Require(c < 128, 1); count += c is '"' or '\\' or '\b' or '\t' or '\n' or '\f' or '\r' ? 2 : c < 32 || c == 127 ? 6 : 1; }
            return count;
        }
        const string begin = "{\"contractVersion\":1,\"movementSourceCanonicalUtf8\":";
        const string middle = ",\"lifecycleEventCanonicalUtf8\":[";
        var size = begin.Length + Cost(parent) + middle.Length + 2 + events.Sum(Cost) + Math.Max(0, events.Length - 1);
        Require(size <= ByteLimit("Source"), 1); var bytes = new byte[(int)size]; var offset = 0;
        void Literal(string text) { offset += Encoding.ASCII.GetBytes(text.AsSpan(), bytes.AsSpan(offset)); }
        void Text(string text)
        {
            bytes[offset++] = 34;
            foreach (var c in text)
            {
                if (c is '"' or '\\') { bytes[offset++] = 92; bytes[offset++] = (byte)c; }
                else if (c is '\b' or '\t' or '\n' or '\f' or '\r')
                { bytes[offset++] = 92; bytes[offset++] = (byte)(c switch { '\b' => 'b', '\t' => 't', '\n' => 'n', '\f' => 'f', _ => 'r' }); }
                else if (c < 32 || c == 127) { Literal("\\u00"); bytes[offset++] = (byte)"0123456789abcdef"[c >> 4]; bytes[offset++] = (byte)"0123456789abcdef"[c & 15]; }
                else bytes[offset++] = (byte)c;
            }
            bytes[offset++] = 34;
        }
        Literal(begin); Text(parent); Literal(middle);
        for (var i = 0; i < events.Length; i++) { if (i > 0) bytes[offset++] = 44; Text(events[i]); }
        Literal("]}"); Require(offset == bytes.Length, 1); return bytes;
    }
    private static void Write(Utf8JsonWriter writer, JsonElement value, string kind, int depth, string fieldPath = "")
    {
        Require(depth <= 40, 1);
        if (kind.EndsWith('?')) { if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue(); else Write(writer, value, kind[..^1], depth, fieldPath); return; }
        if (kind == "LegacyBrokenVehicleLot") { Require(false, 1); return; }
        if (kind == "Route")
        { Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= 9, 1); writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) Write(writer, item, "id", depth + 1); writer.WriteEndArray(); return; }
        if (Unions.TryGetValue(kind, out var tags))
        { Require(value.ValueKind == JsonValueKind.Object && tags.ContainsKey(value.GetProperty("kind").GetString()!), 3); Write(writer, value, tags[value.GetProperty("kind").GetString()!], depth); return; }
        if (Grammar.TryGetValue(kind, out var shape))
        {
            Require(value.ValueKind == JsonValueKind.Object, 1); var fields = shape.Split(' ').Select(f => f.Split(':')).ToArray();
            var names = value.EnumerateObject().Select(p => p.Name).ToArray();
            Require(names.Length == fields.Length && names.Distinct(StringComparer.Ordinal).Count() == names.Length &&
                names.ToHashSet(StringComparer.Ordinal).SetEquals(fields.Select(f => f[0])), 1);
            if (kind is "Source" or "Base" or "Command" or "Event" or "Control" or "Proof") Require(value.GetProperty("contractVersion").GetInt32() == 1, 3);
            writer.WriteStartObject();
            foreach (var f in fields) { writer.WritePropertyName(f[0]); Write(writer, value.GetProperty(f[0]), f[1], depth + 1, kind + "." + f[0]); }
            writer.WriteEndObject(); return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal))
        { Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= (ArrayBounds.TryGetValue(fieldPath, out var maximum) ? maximum : 512), 1); writer.WriteStartArray(); foreach (var v in value.EnumerateArray()) Write(writer, v, kind[..^2], depth + 1); writer.WriteEndArray(); return; }
        if (kind is "parentCarrier" or "eventCarrier")
        { var text = value.GetString(); Require(text is not null && text.Length > 0 && text.Length <= (kind == "parentCarrier" ? 67_108_864 : 65_536) && text.All(c => c < 128), 1); writer.WriteStringValue(text); return; }
        if (kind is "lifecycleKind" or "lifecyclePhase" or "profile")
        {
            var text = value.GetString(); Require(kind switch
            {
                "lifecycleKind" => text is "stop-element-movement" or "resolve-breakdown-stop" or "complete-movement-segment",
                "lifecyclePhase" => text is "movement" or "breakdown",
                _ => text is "paid" or "armed"
            }, 3); writer.WriteStringValue(text); return;
        }
        if (kind == "null") { Require(value.ValueKind == JsonValueKind.Null, 1); writer.WriteNullValue(); return; }
        // Compatibility is leaf syntax only: no old Source/Base/Proof reader is invoked.
        writer.WriteRawValue(Inherited(() => CampaignCombatActualMovedAssaultCodec.Canonical(JsonNode.Parse(value.GetRawText())!, kind)));
    }
    internal static byte[][] Own(IReadOnlyList<byte[]> inputs, int count, string kind)
    {
        Require(inputs is not null && inputs.Count == count, 4);
        return inputs.Select(i => { Require(i is not null, 1); var owned = Copy(i, kind); return owned; }).ToArray();
    }
    internal static void SourceBounds(JsonObject source)
    {
        Require(source["contractVersion"]!.GetValue<int>() == 1 && source["movementSourceCanonicalUtf8"]!.GetValue<string>().Length <= 67_108_864, 3);
        var events = source["lifecycleEventCanonicalUtf8"]!.AsArray();
        Require(events.Count <= 3 && events.All(e => e!.GetValue<string>().Length <= 65_536), 1);
    }
    internal static Func<string, byte[]> SnapshotDependencies(Func<string, byte[]> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); var acquired = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (path, hash) in Pins.Concat(Delivery))
        {
            byte[]? data;
            try { data = callback(path); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException) { throw new JsonException("CMB-MRC-009", e); }
            Require(data is not null && data.LongLength <= 8_388_608, 9); var owned = data.ToArray(); Require(Hash(owned) == "sha256:" + hash, 9); acquired[path] = owned;
        }
        return path => { Require(acquired.ContainsKey(path), 9); var bytes = acquired[path].ToArray(); Require(Hash(bytes) == "sha256:" + (Pins.TryGetValue(path, out var hash) ? hash : Delivery[path]), 9); return bytes; };
    }
    internal static string ManifestHash => HashWithDomain("sandtable.combat.actual-moved-repeat-movement-completion.dependency-manifest.v1",
        Encode(new JsonObject(Pins.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => KeyValuePair.Create(p.Key, (JsonNode?)JsonValue.Create(p.Value))))));
    internal static T Inherited<T>(Func<T> action)
    {
        try { return action(); }
        catch (JsonException error)
        {
            var match = System.Text.RegularExpressions.Regex.Match(error.Message, @"CMB-[A-Z]+-(\d{3})");
            if (match.Success) throw new JsonException("CMB-MRC-" + match.Groups[1].Value, error);
            throw;
        }
    }
    private static readonly Dictionary<string, string> Delivery = new(StringComparer.Ordinal)
    {
        ["docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json"] = "3a80a1cd94b41e8a88590d8eadddcc4c821bfefa7b0021489ad4e7cfe50af8ef",
        ["docs/specs/verify-combat-actual-moved-repeat-movement-completion-v1.py"] = "705445d1b061ef2ba6d905382e7a26220e49be643ee0fe0444cfbf12b0989c62",
    };
    private static readonly Dictionary<string, string> Pins = new(StringComparer.Ordinal)
    {
        ["docs/research/fixtures/combat-selected-source-v1.json"] = "e225bc650c4bb942e41245a33da20c21c576ab117807a06805e2558a2c6468df",
        ["docs/research/verify-combat-rng.py"] = "f3c7c468f2be0efa583e44c4d04b8fe6d20eef51aae01554bfa535b0ecc0ea30",
        ["docs/research/verify-combat-source-freeze.py"] = "f21a97150e344337f6ee4c2c1a1752ac7d41b8b8c8bf6a61a06a1fe5a0483e7e",
        ["docs/specs/combat-actual-assault-v1.schema.json"] = "a16d852d5a4463a295e315c824e5298f4ff22b979a7c307187c2a8d926136f0d",
        ["docs/specs/combat-actual-moved-assault-v1.schema.json"] = "21097270c395bd1a3937d6787db2a992b8c145e42e06aec080652800b9e19e4b",
        ["docs/specs/combat-actual-moved-continuation-v1.md"] = "9e91987ba214f0d2fe8adb5ac91af63c8cc018138b51702a9b5348ea293a9956",
        ["docs/specs/combat-actual-moved-continuation-v1.schema.json"] = "0a2a0d8089398246b38c84d043ea343a0e3d72d84378b90f0d6da155c2fe3986",
        ["docs/specs/combat-actual-moved-cycle-control-v1.md"] = "8d8761d98750c52a26fffa471f66622cc588ca56d6d71840988d512cd8f4da4d",
        ["docs/specs/combat-actual-moved-cycle-control-v1.schema.json"] = "91f235388faa66ef397a9f466f503075da8443148489aef1e7ac27f2dd1751e8",
        ["docs/specs/combat-actual-moved-repeat-movement-v1.md"] = "c5b0a1faa5a99d80074281166b367e1b1a78ea8c16ba602f51db1bf1c60ecd57",
        ["docs/specs/combat-actual-moved-repeat-movement-v1.schema.json"] = "eb1cf39788cc78376b6d798a13a4a3272906217c51ff02b90310843654122468",
        ["docs/specs/combat-actual-post-assault-continuation-v1.schema.json"] = "e948daf499acbb365d9be52b083a10d1692ccbb89fd4bf94e0b4a13601f41c36",
        ["docs/specs/combat-actual-reaction-combat-entry-v1.md"] = "d2d18776b17d80e8cf746e4301a15338e39b68104962d2046a3540fa31474af7",
        ["docs/specs/combat-actual-reaction-combat-entry-v1.schema.json"] = "06d586006d8dc08f5c2cd2e07ac123cda60c357a00d4d6ec69b692a24be69d84",
        ["docs/specs/combat-actual-round-entry-v1.schema.json"] = "677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8",
        ["docs/specs/combat-actual-selection-v1.schema.json"] = "da6256deb94bb6061e8e98e2448f915c4474373b7f0de6bb76eded2ae88c39b1",
        ["docs/specs/combat-authority-envelope-v1.schema.json"] = "91f90ba47cdfff088b3d8e5dba55c6c283d41c96381e56c7bba115bfb5390550",
        ["docs/specs/combat-cycle-control-v1.schema.json"] = "8c517db6daa734d415bd1d474280c2cf6e65013490c4a2568795d50e6112f7da",
        ["docs/specs/combat-cycle-sequence-v1.schema.json"] = "6e09dc5bc6ca79d8580db6ed1e76ad5007c26a30df11b4dd9da0a9c7b5f5e0c5",
        ["docs/specs/combat-inherited-breakdown-completion-v1.schema.json"] = "1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3",
        ["docs/specs/combat-inherited-movement-lifecycle-v1.schema.json"] = "aac4e07f9546d457defd9042ae701434683e7c22fab2071fe88f872d8d6bc072",
        ["docs/specs/combat-inherited-movement-v1.schema.json"] = "a9ad67ee9b9302f17582fb5087f6dd6e7e62574741e6cea7b5a3e58bf7fe2069",
        ["docs/specs/combat-inherited-successors-v1.schema.json"] = "474d8b5b22457921082c1799c89a42d9ec448b5c6ea131a3458f364ff980babc",
        ["docs/specs/combat-opening-preamble-v1.schema.json"] = "d01bd4eb60ce3a83a4d119631ac72ccce082ff13cdf92f861d42fb83566e7d59",
        ["docs/specs/combat-ordinary-movement-v1.schema.json"] = "039d5f3cdb908dc761540b4809bfb9c6e77c9ab6efd8c965e69955fbbcf0728a",
        ["docs/specs/combat-positive-entry-v1.schema.json"] = "73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36",
        ["docs/specs/combat-reserve-designation-v1.schema.json"] = "f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9",
        ["docs/specs/combat-reserve-release-v1.schema.json"] = "10ba1ca46e05fbed52c6eef5174c2ac84c4282df0957c44eab6e86079342bc32",
        ["docs/specs/combat-result-settlement-v1.schema.json"] = "f65c13687bb2fe3fbabdbd82d48298430330dab756583fbfda17dae50bc73ab0",
        ["docs/specs/combat-result-settlement-v2.schema.json"] = "719634c4720b4a92669b2e83e60df58cd32d0e556ec9ef66c546aed1f9c7e9d6",
        ["docs/specs/combat-rules-inputs-v1.schema.json"] = "1477a9e755a091bbf4539d00184711b18eab2f6289f643596d841b8c314f4797",
        ["docs/specs/combat-sealed-round-v1.schema.json"] = "5556404caa296ce41020654f8fb64c0349fd13051edabf6b4934e47d675c32db",
        ["docs/specs/combat-selection-steps-v1.schema.json"] = "ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d",
        ["docs/specs/combat-stage-entry-v1.schema.json"] = "5d0f85fec61f219e7fd3575037894c147580ec9fe1570603d621c9d76b38889e",
        ["docs/specs/combat-weather-v1.schema.json"] = "44f2fa44af30bf723ecac88db08fc9efac85e9880624635fc73f4dc52bd9459f",
        ["docs/specs/combat-world-settlement-v1.schema.json"] = "f892545f5dac693c55d1180ad771d95ffeec508176a0d6139b82752ed46bcf0e",
        ["docs/specs/fixtures/combat-actual-assault-v1.json"] = "9a486f77d0463c071c55af9196511ec2f1214763ed2278ba89962b30746aba09",
        ["docs/specs/fixtures/combat-actual-selection-v1.json"] = "019d1a3ff0f121d83f377ddfb19d274b4a8aa89172bad8aeb289c3b98228e604",
        ["docs/specs/fixtures/combat-authority-envelope-v1.json"] = "adf7b05e15f863b08f366809adf91c97b39480e404ce2efff15e838fa4043eaa",
        ["docs/specs/fixtures/combat-content-v7.canonical.json"] = "ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
        ["docs/specs/fixtures/combat-creation-ledger-v1.json"] = "4b7f87f73a5882800e4e4befb4c06c20bf19c419f92edba5028b355e37442684",
        ["docs/specs/fixtures/combat-cycle-sequence-v1.json"] = "de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f",
        ["docs/specs/fixtures/combat-positive-entry-v1.json"] = "eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e",
        ["docs/specs/fixtures/combat-reserve-designation-v1.json"] = "6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2",
        ["docs/specs/fixtures/combat-rules-inputs-v1.json"] = "5401cd9a690be8b81f19795f1f66c0c6fe24f5055a06cda5facd6193b3e0a29a",
        ["docs/specs/verify-combat-actual-assault-v1.py"] = "1ab27601eb209f48febc440ad0f1cfebd08155b9a747e06c003f11a1a16e64a9",
        ["docs/specs/verify-combat-actual-moved-assault-v1.py"] = "ab2901ac6aef75f7f5f074cce7d8d16b0fa14d0802e0b3f56ff88dd3b7542094",
        ["docs/specs/verify-combat-actual-moved-continuation-v1.py"] = "07f9c81d3c804769881068b55a4948b0565812bbccb966c671692deee77d035c",
        ["docs/specs/verify-combat-actual-moved-cycle-control-v1.py"] = "0ab89db7e88b0df7256751c7287efe451cc19176cf7422cbcd062813f1856665",
        ["docs/specs/verify-combat-actual-moved-repeat-movement-v1.py"] = "e07f66f8037d9cc6d44ec40b2fb3df915bbef9e2be87a7576f25a5846e32d97a",
        ["docs/specs/verify-combat-actual-post-assault-continuation-v1.py"] = "dad0b1a09219a21a82236b48da9cc67e99a2082219d8906c789e2ca4b0429a9a",
        ["docs/specs/verify-combat-actual-reaction-combat-entry-v1.py"] = "6e32849af9d9c230f5e464ea8a8dec0fffe1a369c0c455f6edbe454e960da1fc",
        ["docs/specs/verify-combat-actual-round-entry-v1.py"] = "2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c",
        ["docs/specs/verify-combat-actual-selection-v1.py"] = "f37a7cfa26b60469168d9f4424465b14f1fe45666f4222d69c840c2527d2e66f",
        ["docs/specs/verify-combat-authority-envelope-v1.py"] = "a12d8ce1ab142b9a7f1c58e07e5b0d7928c4693d869423aa24ade81cb21e6231",
        ["docs/specs/verify-combat-content-v7.py"] = "eaab770988e8d2b0f11cb9a8a39b45f961977aa9048f95de8bf32f46e361bcbd",
        ["docs/specs/verify-combat-creation-ledger-v1.py"] = "04777d27347b943cbb4ec06efeee6f3006694d1acd88b9bc8e25eb5dcb5841e4",
        ["docs/specs/verify-combat-cycle-control-v1.py"] = "9039f9e1067b2cdd2e0a57ae16e711350b4bbdceb500d22bbbd715f39e8ccc9f",
        ["docs/specs/verify-combat-cycle-sequence-v1.py"] = "d43ae974e1f6e2c30067efe4635df655c6d2e4791968ece00dfd473145c3cc8a",
        ["docs/specs/verify-combat-inherited-breakdown-completion-v1.py"] = "db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba",
        ["docs/specs/verify-combat-inherited-movement-lifecycle-v1.py"] = "f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920",
        ["docs/specs/verify-combat-inherited-movement-v1.py"] = "c10c51cd97fca3f3f04fc0983e90d474a35a6b58f4a2cce396272916910579e1",
        ["docs/specs/verify-combat-inherited-reaction-closure-v1.py"] = "9f07f00f8b749c2b3d86f6d3a2e40edbe0eee6af0a147e25d4d87b1ad287355c",
        ["docs/specs/verify-combat-inherited-selection-v1.py"] = "99e0cdb1cc78089e4af93b8e995ba04483f6bdab86edaf7c6e2e251442cd0bb7",
        ["docs/specs/verify-combat-inherited-successors-v1.py"] = "f9cbf73f2045c304c96fc8dcf4d09ba10026db87ff5446c751ad22e19e4fe5de",
        ["docs/specs/verify-combat-opening-preamble-v1.py"] = "50f75c1032f5b17000accb288107a1e047940cadedd5c8d30c469db167c0e5d0",
        ["docs/specs/verify-combat-ordinary-movement-v1.py"] = "cc75e52a6d160d4b0c6c880dc43bc65742068f83ef85cc7fb4babbf8756e0dff",
        ["docs/specs/verify-combat-positive-entry-v1.py"] = "b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172",
        ["docs/specs/verify-combat-reserve-designation-v1.py"] = "fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465",
        ["docs/specs/verify-combat-reserve-release-v1.py"] = "105ef18d9db6364ce42f9831afd3c33010fccc71b0ae60af78cec7c185da892b",
        ["docs/specs/verify-combat-result-settlement-v1.py"] = "1f7dff5b87ce036c406562686f4dc1ce983a4a8dfa15b2401eae72a0d9879ffd",
        ["docs/specs/verify-combat-result-settlement-v2.py"] = "a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2",
        ["docs/specs/verify-combat-rules-inputs-v1.py"] = "ffb6daf98db7097d307ac7c185bf7d68a2ff1d0ac9a357ea063a746aab42081d",
        ["docs/specs/verify-combat-sealed-round-v1.py"] = "497e295e71c4b4da43539ee9b6c30de6d33f409a2f14b6cc24d9030390525ae0",
        ["docs/specs/verify-combat-sealed-round-v2.py"] = "d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9",
        ["docs/specs/verify-combat-selection-steps-v1.py"] = "dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86",
        ["docs/specs/verify-combat-stage-entry-v1.py"] = "85cd2b32f9ad104ec29034180775e1ac1905e5aa7f78caa3c1a26e6675901435",
        ["docs/specs/verify-combat-weather-v1.py"] = "c5f92de66c8c341553fe204a5ec1b0acffb162e61ee90332c14fd608f986ade8",
        ["docs/specs/verify-combat-world-settlement-v1.py"] = "d5f49e5a4165ec568f0a8896ea491341a639da256bcfd573998f529e80ef9f25",
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedAssault.cs"] = "d2ce7b17a832e8fa8691d5c39935cecd3056be61f86769b98648eaeefc061e35",
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedAssaultCodec.cs"] = "d73751bf1caadb0a643954ad8127b0bddc29d3323d59b7d45507b0ae5150f8c4",
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuation.cs"] = "26363b3d4949bfc502a0c0e8c6093fd1599c4b2e8827b3af7829b3f2012b53a7",
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuationCodec.cs"] = "067660bfab0eef4790e8e4fc94b0fdcaf2f7d13424dcd403612745cb37d74d9d",
        ["src/Cna.Core/Campaigns/CampaignCombatActualPaidAssault.cs"] = "1ef9b054c461d4a2e35514d699bbcfaff86da12278015869454f9424e3590004",
        ["src/Cna.Core/Campaigns/CampaignCombatActualPaidAssaultCodec.cs"] = "3fda57595e4991d4635fa6f6c68316cb66277321c2ae87abe25ec84f20256539",
        ["src/Cna.Core/Campaigns/CampaignCombatActualPaidAssaultSupport.cs"] = "b73a62ed471118bf3470145621bb72190918ed56704d9c460b3ef5c9b927457d",
        ["src/Cna.Core/Campaigns/CampaignCombatActualPostAssaultContinuation.cs"] = "25a01c667976ba05b1c2242910c08eccb83615c2fe39be4f93b44702cd9a69da",
        ["src/Cna.Core/Campaigns/CampaignCombatActualPostAssaultContinuationCodec.cs"] = "790775b6df49aa11451778bd52a9fd41a61af40009018ee87e27f4e22bdd73ba",
        ["src/Cna.Core/Campaigns/CampaignCombatActualReactionCombatEntry.cs"] = "d803ef06c1ebb993444f20b6b93123bd56991c714a32de24f602a257842c8d81",
        ["src/Cna.Core/Campaigns/CampaignCombatActualReactionCombatEntryCodec.cs"] = "4d31a43d90d85b5a8196e16be3aed899af055bb301241ab26430149ded886ab4",
        ["src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs"] = "00f30d4abd24598ebea9e12319aef6ba7c7285e64565ed9eff76ac37f4d04fcf",
        ["src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs"] = "e224bb57183b1b0febf9b90a297744e9ce99cf3b4124ce5d320eb3fb60e48065",
        ["src/Cna.Core/Campaigns/CampaignCombatActualSelection.cs"] = "8a94b2d5f9ecd688a0eb5670bbed4408add89f488ccd070ed53361210b7f3721",
        ["src/Cna.Core/Campaigns/CampaignCombatArmedContinuation.cs"] = "411570db524ff3b706ed3f25fd73a824a43f887df5b17b7ad6955b57530b6d25",
        ["src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletion.cs"] = "fe24d5a524383c1e5137b3d9e8a0050fc21fd381c4dc7e59812d68dc3a24dc42",
        ["src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletionCodec.cs"] = "e1160fcf932f938ba43aabbc2bc52b97b973228145a30b15ac6b747f00e359c4",
        ["src/Cna.Core/Campaigns/CampaignCombatCertification.cs"] = "2d3859c248783d07772753b5105848c4555cd7b7e146ea7cd9d740316cb705da",
        ["src/Cna.Core/Campaigns/CampaignCombatContinuation.cs"] = "3904ec56845b5e6b27e55117514a4e9dfacd3385084aadb0f3da4e82cf289224",
        ["src/Cna.Core/Campaigns/CampaignCombatMovementLifecycle.cs"] = "e63a4d0225f5347ce6a70a855c34272110ce75a3b258591b50115c3ca5fb07aa",
        ["src/Cna.Core/Campaigns/CampaignCombatMovementLifecycleCodec.cs"] = "d0f5d587c7ce9fe33e20acc670d65ac525f1c6214ef9db959a36c642621c9171",
        ["src/Cna.Core/Campaigns/CampaignCombatReactionClosure.cs"] = "fad532a53a58918ced14f7e38898aeb8dc8161e68e79a9c327de55000ee86360",
        ["src/Cna.Core/Campaigns/CampaignCombatReactionClosureCodec.cs"] = "3db05ef3a20b2f3cd0527ec4c4ba11c3d515a6731ec99305bb1399b3c9768e36",
        ["src/Cna.Core/Campaigns/CampaignCombatReactionTrigger.cs"] = "f85f23507429d8dcbddc23d9ae2d835571d64844b71d712083cf633a72f60218",
        ["tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.weather-tables.v1.golden.json"] = "7f134b9c5dcc356d48a9dfc51976a166b60f02fa93e73b4c0ccfdd354380c182",
    };
    private static readonly Dictionary<string, string> Grammar = new(StringComparer.Ordinal)
    {
        ["Source"] = "contractVersion:int movementSourceCanonicalUtf8:parentCarrier lifecycleEventCanonicalUtf8:eventCarrier[]",
        ["Base"] = "contractVersion:int movementSourceHash:hash movementControlHash:hash movementProofHash:hash dependencyManifestHash:hash publicationReceiptId:id cycleId:hash configurationHash:hash",
        ["Command"] = "contractVersion:int kind:lifecycleKind baseHash:hash cycleId:hash expectedPositionId:id expectedPriorVersion:long capabilityId:hash",
        ["Input"] = "command:Command actor:actor",
        ["PublishEffect"] = "kind:id intentReceiptId:id priorCycleId:hash nextCycleId:hash nextCycle:Authority successorPositionId:id worldHash:hash assessmentHash:hash witnessesHash:hash",
        ["MovementCosts"] = "terrainCost:int breakOffCost:int totalCost:int baseCpa:int voluntaryCeiling:int beforeCp:long afterCp:long excessCpDp:int beforeCohesion:signedInt afterCohesion:signedInt",
        ["EndedMembership"] = "relationId:id creationReceiptId:id kind:id attacker:UnitKey defender:UnitKey endingCause:id",
        ["MoveEffect"] = "kind:id unit:UnitKey representationId:id originLocationId:id destinationLocationId:id costs:MovementCosts endedMemberships:EndedMembership[]",
        ["Event"] = "contractVersion:int eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash profile:profile movementSourceHash:hash baseHash:hash cycleId:hash positionId:id priorVersion:long stateVersion:long priorPrefix:hash input:Input effect:Effect receiptId:id",
        ["Track"] = "unit:UnitKey route:id[]",
        ["Control"] = "contractVersion:int profile:profile parentSourceHash:hash movementBaseHash:hash baseHash:hash movementSourceHash:hash phase:lifecyclePhase stateVersion:long prefix:hash sequencePosition:Position originalCycle:Authority activeCycle:Authority cycleId:hash world:World randomState:Random originalMovementEnd:MovementEndProof priorCycleProgress:ActualProgressRef[] actualProgressRefs:ActualProgressRef[] members:ReserveMember[] attackHistory:AttackHistory[] priorCycleTargetUses:TargetUse[] targetUses:TargetUse[] assessment:Assessment pendingRepeatIntent:Intent pendingIntentReceiptId:id publicationReceiptId:id? closure:Closure? tracks:Track[] receipts:PreambleReceipt[] movementReceipts:Receipt[] flow:Flow interruptContext:Context? movementEnd:MovementEndProof? lifecycleReceipts:Receipt[] closed:bool",
        ["Proof"] = "contractVersion:int sourceId:id sourceHash:hash base:Base control:Control",
        ["Position"] = "contractVersion:int positionId:id gameTurn:int operationStage:int stageId:id phaseId:id segmentId:id? stepId:id? actorRole:id activeSide:id? sources:Reference[]",
        ["Authority"] = "contractVersion:int campaignId:id rulesetHash:rawHash setupId:id setupHash:hash contentPackId:id contentHash:hash scenarioId:id gameTurn:int operationStage:int playerPhaseSlot:id actingSide:id ordinal:int openedAuthorityVersion:long openingPrefix:hash admittedPolicyBundleDigest:hash",
        ["World"] = "contractVersion:int creationBinding:id elements:Element[] representations:Representation[] brokenVehicleLots:LegacyBrokenVehicleLot[] cohesionCauses:Cause[] relationships:Relationship[] custodyLots:Lot[] guards:Guard[] replacementEntitlements:Entitlement[] futureObligations:Obligation[] settlements:Settlement[]",
        ["Random"] = "contractVersion:int algorithmId:id seed:ulong nextByteCursor:ulong",
        ["MovementEndProof"] = "scope:ReleaseScope ordinal:int completionReceiptId:id endLocations:EndLocation[] excludedBefore:UnitKey[]",
        ["ActualProgressRef"] = "eventType:id receiptId:id eventHash:hash",
        ["ReserveMember"] = "unit:UnitKey status:id baseCpa:int spentCp:Cp history:ReleaseHistory",
        ["AttackHistory"] = "commitmentId:id cycleId:hash segmentId:id attacker:UnitKey defender:UnitKey targetLocationId:id gameTurn:int operationStage:int",
        ["TargetUse"] = "commitmentId:id segmentId:id targetLocationId:id",
        ["Assessment"] = "contractVersion:int mode:profile sourceOccurrence:hash sourceOrdinal:int nextOrdinal:int releaseCompletionReceiptId:id movementCompletionReceiptId:id worldHash:hash membersHash:hash progress:ActualProgressRef[] witnesses:ContinuationWitness[] unsupportedWitnesses:UnsupportedWitness[] combatAssessment:id witnessOutcome:id unsupportedReason:id? executionAvailable:bool supportCertificate:ProspectiveCertificate",
        ["Intent"] = "contractVersion:int kind:id sourceOccurrence:hash sourceOrdinal:int expectedNextOrdinal:int parentSourceHash:hash worldHash:hash assessmentHash:hash configurationHash:hash decisionId:id witnessesHash:hash executionAvailable:bool",
        ["Closure"] = "cycleId:hash ordinal:int outcome:id receiptId:id",
        ["PreambleReceipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["Receipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["ContinuationWitness"] = "kind:id unit:UnitKey destinationLocationId:id terrainCost:int breakOffCost:int afterCp:int excessCpDp:int usesReleaseException:bool",
        ["UnsupportedWitness"] = "witness:ContinuationWitness reason:id",
        ["ProspectiveCertificate"] = "contractVersion:int profile:profile sourceOccurrence:hash sourceOrdinal:int nextOrdinal:int worldHash:hash configurationHash:hash selectedRulesHash:hash catalogueHash:hash moralePairs:int assaultPairs:int captureRefusalVariants:int supportHandlersHash:hash capacitiesHash:hash",
        ["UnitKey"] = "creationBinding:id originalSide:side elementId:id",
        ["Cp"] = "numerator:long denominator:int",
        ["ReleaseHistory"] = "scope:ReleaseScope designationReceiptId:id? conversionReceiptId:id? releasedType:id? releaseReceiptId:id? releaseCycle:int? cpaBasis:int? voluntaryCeiling:int? offensiveCommitmentId:id? nextMovement:MovementException?",
        ["ReleaseScope"] = "gameTurn:int operationStage:int playerPhaseSlot:id actingSide:side",
        ["MovementException"] = "scope:ReleaseScope ordinal:int status:id completionReceiptId:id?",
        ["EndLocation"] = "unit:UnitKey locationId:id",
        ["Element"] = "elementId:id currentLocationId:id reserveStatus:string operationalState:Operational components:Component[] sourceParentFormationId:id currentParentFormationId:id ammunition:Ammunition readiness:Readiness",
        ["Representation"] = "representationId:id currentLocationId:id bindingKind:string boundElementIds:id[]",
        ["Cause"] = "causeId:id ordinal:int receiptId:id elementId:id gameTurn:int operationStage:int kind:string points:int before:signedInt after:signedInt",
        ["Relationship"] = "relationId:id creationReceiptId:id kind:string attacker:UnitKey defender:UnitKey gameTurn:int operationStage:int active:bool endedByReceiptId:id? endingCause:string?",
        ["Lot"] = "lotId:id lossReceiptId:id originalComponent:ComponentKey captor:UnitKey quantity:int originLocationId:id currentLocationId:id? status:string guardId:id? escapeReceiptId:id?",
        ["Guard"] = "guardId:id formationReceiptId:id lotId:id originComponent:ComponentKey currentLocationId:id toe:int baseCapabilityPointAllowance:int offensiveCloseAssaultRating:int defensiveCloseAssaultRating:int operationalState:Operational ammunition:Ammunition readiness:Readiness",
        ["Entitlement"] = "entitlementId:id escapeReceiptId:id lotId:id originalComponent:ComponentKey quantity:int reunionLocationId:id earnedScope:Scope delayOperationStages:int eligibleScope:FutureScope status:string",
        ["Obligation"] = "obligationId:id receiptId:id kind:string subjectId:id earnedScope:Scope eligibleScope:FutureScope? activationGate:string status:string",
        ["Settlement"] = "settlementId:id commitmentId:id resultId:id gameTurn:int operationStage:int attacker:UnitKey defender:UnitKey preLossElements:Element[] result:Result disposition:Disposition? losses:Losses? retreat:Retreat? custody:Custody? relationships:RelationsReceipt?",
        ["Result"] = "differential:differential attackerCoordinate:int defenderCoordinate:int captureDie:int? attackerPercent:int defenderPercent:int rawEngaged:bool requiredRetreat:int capturedRole:string? captureShare:int",
        ["Disposition"] = "receiptId:id predecessorReceiptId:id kind:string requiredDistance:int plannedDistance:int unfulfilledDistance:int route:Route",
        ["Losses"] = "receiptId:id predecessorReceiptId:id roles:RoleLoss[]",
        ["Retreat"] = "receiptId:id predecessorReceiptId:id kind:string route:Route completedDistance:int beforeCp:Cp afterCp:Cp excessCpDp:int attackerVictoryRp:int",
        ["Custody"] = "receiptId:id predecessorReceiptId:id lotId:id kind:string route:Route guardId:id? entitlementId:id? donorToeBefore:int donorToeAfter:int",
        ["RelationsReceipt"] = "receiptId:id predecessorReceiptId:id relationshipId:id? kind:string?",
        ["RoleLoss"] = "role:string component:ComponentKey committedToe:int tablePercent:int refusalPercent:int lossToe:int capturedToe:int otherLossToe:int remainingToe:int lossDp:int",
        ["ComponentKey"] = "unit:UnitKey componentId:id",
        ["Scope"] = "gameTurn:int operationStage:int",
        ["FutureScope"] = "gameTurn:futureTurn operationStage:int",
        ["Operational"] = "ledgerGameTurn:int ledgerOperationStage:int capabilityPointsExpended:Cp cohesionLevel:signedInt vehicleBreakdownState:null movementEnded:null initialLedgerOrigin:Origin",
        ["Ammunition"] = "points:int initialAmmunitionOrigin:Origin",
        ["Readiness"] = "gameTurn:int operationStage:int waterStatus:string storesStatus:string pinned:bool initialReadinessOrigin:Origin",
        ["Origin"] = "kind:string references:Reference[]",
        ["Reference"] = "sourceId:id locator:locator",
        ["Component"] = "componentId:id currentToe:int initialToeOrigin:Origin",
        ["IdleFlow"] = "kind:id",
        ["MovingFlow"] = "kind:id track:Track lastMoveReceiptId:id",
        ["PendingFlow"] = "kind:id stop:Stop",
        ["Stop"] = "stopId:id recordedStateVersion:long track:Track lastMoveReceiptId:id reason:id weatherKind:id cohortInputs:UnitKey[]",
        ["Context"] = "cycle:Authority cycleId:hash suspendedPosition:Position",
        ["StopEffect"] = "kind:id stop:Stop interruptContext:Context successorPosition:Position",
        ["ResolutionEffect"] = "kind:id stopId:id beforeRandomState:Random afterRandomState:Random checks:UnitKey[] createdLots:id[] sources:id[] successorPosition:Position",
        ["CompletionEffect"] = "kind:id endLocations:EndLocation[] excludedUnits:UnitKey[] progress:ActualProgressRef[] successorPosition:Position",
    };
    private static readonly Dictionary<string, int> ArrayBounds = new(StringComparer.Ordinal)
    {
        ["Source.lifecycleEventCanonicalUtf8"] = 3,
        ["Control.lifecycleReceipts"] = 3,
        ["Control.members"] = 1,
        ["Control.tracks"] = 1,
        ["Control.movementReceipts"] = 6,
        ["Control.actualProgressRefs"] = 5,
        ["Control.priorCycleProgress"] = 3,
        ["Control.targetUses"] = 0,
        ["Track.route"] = 6,
        ["Stop.cohortInputs"] = 0,
        ["ResolutionEffect.checks"] = 0,
        ["ResolutionEffect.createdLots"] = 0,
        ["ResolutionEffect.sources"] = 1,
        ["CompletionEffect.endLocations"] = 2,
        ["CompletionEffect.excludedUnits"] = 2,
        ["CompletionEffect.progress"] = 5,
        ["MovementEndProof.endLocations"] = 2,
        ["MovementEndProof.excludedBefore"] = 2,
    };
    private static readonly Dictionary<string, Dictionary<string, string>> Unions = new(StringComparer.Ordinal)
    {
        ["Effect"] = new(StringComparer.Ordinal) { ["movement-stopped"] = "StopEffect", ["stop-resolved"] = "ResolutionEffect", ["movement-completed"] = "CompletionEffect" },
        ["Flow"] = new(StringComparer.Ordinal) { ["idle"] = "IdleFlow", ["moving"] = "MovingFlow", ["phasing-stop"] = "PendingFlow" },
    };
}
