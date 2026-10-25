using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Closed native repeated Movement syntax; continuation grammar remains independently bounded.</summary>
internal static class CampaignCombatActualRepeatedMovementCodec
{
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    internal static void Require([DoesNotReturnIf(false)] bool condition, int code)
    { if (!condition) throw new JsonException($"CMB-RMV-{code:000}"); }
    internal static int ByteLimit(string kind) => kind switch
    {
        "Source" => 33_554_432,
        "Control" or "World" => 262_144,
        "Event" => 32_768,
        "Input" or "Command" => 1_024,
        _ => 2_097_152,
    };
    private static int DepthLimit(string kind) => 38;
    internal static JsonObject Object(ReadOnlySpan<byte> bytes, string kind)
        => JsonNode.Parse(Parse(bytes, kind).GetRawText())!.AsObject();
    internal static JsonElement Parse(ReadOnlySpan<byte> bytes, string kind)
    {
        Require(bytes.Length > 0 && bytes.Length <= ByteLimit(kind) && bytes.IndexOfAnyInRange((byte)128, byte.MaxValue) < 0, 1);
        Preparse(bytes, DepthLimit(kind));
        try
        {
            using var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = DepthLimit(kind) + 1 });
            Bounds(doc.RootElement, 0, DepthLimit(kind));
            Require(bytes.SequenceEqual(Canonical(doc.RootElement, kind)), 8);
            return doc.RootElement.Clone();
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-RMV-", StringComparison.Ordinal))
        { throw new JsonException("CMB-RMV-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("CMB-RMV-001", e); }
    }
    internal static byte[] Canonical(JsonNode value, string kind) => Canonical(JsonSerializer.SerializeToElement(value), kind);
    internal static byte[] Canonical(JsonElement value, string kind)
    {
        Bounds(value, 0, DepthLimit(kind));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) Write(writer, value, kind);
        var bytes = AsciiEscapes(stream.ToArray());
        Require(bytes.Length <= ByteLimit(kind), 1); return bytes;
    }
    // Python ensure_ascii: lowercase escapes, literal printable ASCII, and surrogate pairs.
    internal static byte[] Encode(JsonNode value)
        => AsciiEscapes(JsonSerializer.SerializeToUtf8Bytes(value, Options));
    private static byte[] AsciiEscapes(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        var output = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                if (text[i + 1] == 'u')
                {
                    var n = int.Parse(text.AsSpan(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if (n is >= 32 and <= 126 && n is not 34 and not 92) output.Append((char)n);
                    else output.Append("\\u").Append(n.ToString("x4", CultureInfo.InvariantCulture));
                    i += 5;
                }
                else { output.Append(c).Append(text[++i]); }
            }
            else if (c > 127) output.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            else output.Append(c);
        }
        return Encoding.ASCII.GetBytes(output.ToString());
    }
    // Enforce allocation bounds before JsonDocument allocates a nested document.
    private static void Preparse(ReadOnlySpan<byte> data, int maxDepth)
    {
        Span<byte> starts = stackalloc byte[39]; Span<int> commas = stackalloc int[39]; Span<bool> occupied = stackalloc bool[39];
        var depth = 0; var quoted = false; var escaped = false;
        foreach (var c in data)
        {
            if (quoted) { if (escaped) escaped = false; else if (c == 92) escaped = true; else if (c == 34) quoted = false; continue; }
            if (c is 9 or 10 or 13 or 32) continue;
            if (c == 34) { if (depth > 0) occupied[depth - 1] = true; quoted = true; continue; }
            if (c is 123 or 91)
            {
                if (depth > 0) occupied[depth - 1] = true; Require(depth < maxDepth + 1, 1);
                starts[depth] = c; commas[depth] = 0; occupied[depth] = false; depth++; continue;
            }
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
    private static void Bounds(JsonElement value, int depth, int maxDepth)
    {
        Require(depth <= maxDepth, 1);
        if (value.ValueKind == JsonValueKind.Array)
        { Require(value.GetArrayLength() <= 512, 1); foreach (var x in value.EnumerateArray()) Bounds(x, depth + 1, maxDepth); }
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var x in value.EnumerateObject())
            {
                Require(names.Add(x.Name) && names.Count <= 128, 1);
                if (x.Value.ValueKind == JsonValueKind.Array && x.Name is "components" or "boundElementIds" or "references")
                    Require(x.Value.GetArrayLength() <= (x.Name == "references" ? 16 : 1), 1);
                Bounds(x.Value, depth + 1, maxDepth);
            }
        }
    }
    private static void Write(Utf8JsonWriter writer, JsonElement value, string kind, string parent = "", string field = "")
    {
        if (kind.EndsWith('?'))
        { if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue(); else Write(writer, value, kind[..^1], parent, field); return; }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            Require(value.ValueKind == JsonValueKind.Object, 1);
            var fields = shape.Split(' ').Select(x => x.Split(':')).ToArray();
            Require(value.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal).SetEquals(fields.Select(x => x[0])), 1);
            writer.WriteStartObject();
            foreach (var f in fields) { writer.WritePropertyName(f[0]); Write(writer, value.GetProperty(f[0]), f[1], kind, f[0]); }
            if (kind == "Effect") Require(value.GetProperty("kind").GetString() == "ordinary-move", 3);
            if (Versions.TryGetValue(kind, out var version)) Require(value.GetProperty(version.Field).GetInt64() == version.Value, 3);
            writer.WriteEndObject(); return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal) || kind == "Route")
        {
            var cap = kind == "Route" ? 9 : ArrayBounds.GetValueOrDefault(parent + "." + field, 512);
            Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= cap, 1);
            writer.WriteStartArray(); foreach (var x in value.EnumerateArray()) Write(writer, x, kind == "Route" ? "id" : kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind is "die" or "adjustment" or "differential" or "byte" or "signedInt" or "int" or "long" or "utc")
        {
            Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1);
            Require(value.TryGetInt64(out var n), 2);
            var (lo, hi) = kind switch { "die" => (1L, 6L), "adjustment" => (-1L, 1L), "differential" => (-2L, 2L), "byte" => (0L, 255L), "signedInt" => ((long)int.MinValue, int.MaxValue), "int" => (0L, int.MaxValue), "utc" => (0L, 253402300799999L), _ => (0L, long.MaxValue) };
            Require(n >= lo && n <= hi, 2); writer.WriteNumberValue(n); return;
        }
        if (kind is "carrier" or "eventCarrier")
        {
            Require(value.ValueKind == JsonValueKind.String, 1); var text = DecodeString(value.GetRawText());
            Require(text.Length > 0 && text.Length <= (kind == "carrier" ? 16_777_216 : 32_768) && text.All(c => c <= 127), 1);
            writer.WriteRawValue(Quote(text)); return;
        }
        if (kind == "ulong")
        {
            Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1);
            Require(value.TryGetUInt64(out var n), 2); writer.WriteNumberValue(n); return;
        }
        try { writer.WriteRawValue(CampaignCombatActualPostAssaultContinuationCodec.Canonical(value, kind == "ContinuationProof" ? "Proof" : kind)); }
        catch (JsonException e)
        {
            var code = e.Message.StartsWith("CMB-PAC-", StringComparison.Ordinal) && int.TryParse(e.Message.AsSpan(8, 3), out var c) ? c : 1;
            throw new JsonException($"CMB-RMV-{code:000}", e);
        }
    }
    private static string DecodeString(string raw)
    {
        var value = new StringBuilder(raw.Length);
        for (var i = 1; i < raw.Length - 1; i++)
        {
            if (raw[i] != '\\') { value.Append(raw[i]); continue; }
            var c = raw[++i];
            if (c == 'u') { value.Append((char)int.Parse(raw.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; }
            else value.Append(c switch { 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', _ => c });
        }
        return value.ToString();
    }
    private static string Quote(string text)
    {
        var value = new StringBuilder(text.Length + 2); value.Append('"');
        foreach (var c in text)
        {
            switch (c)
            {
                case '"': value.Append("\\\""); break;
                case '\\': value.Append("\\\\"); break;
                case '\b': value.Append("\\b"); break;
                case '\f': value.Append("\\f"); break;
                case '\n': value.Append("\\n"); break;
                case '\r': value.Append("\\r"); break;
                case '\t': value.Append("\\t"); break;
                default: if (c < 32 || c >= 127) value.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); else value.Append(c); break;
            }
        }
        return value.Append('"').ToString();
    }
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["Source"] = "contractVersion:int continuationSourceCanonicalUtf8:carrier movementEventCanonicalUtf8:eventCarrier[]",
        ["Base"] = "contractVersion:int dependencyManifestHash:hash continuationProof:ContinuationProof",
        ["Command"] = "contractVersion:int kind:id baseHash:hash cycleId:hash positionId:id expectedPriorVersion:long unit:UnitKey originLocationId:id destinationLocationId:id",
        ["Input"] = "command:Command actor:actor",
        ["Cost"] = "terrainCost:int breakOffCost:int totalCost:int baseCpa:int voluntaryCeiling:int beforeCp:int afterCp:int excessCpDp:int beforeCohesion:signedInt afterCohesion:signedInt",
        ["MembershipEnd"] = "relationId:id creationReceiptId:id kind:id attacker:UnitKey defender:UnitKey endingCause:id",
        ["Effect"] = "kind:id unit:UnitKey representationId:id originLocationId:id destinationLocationId:id costs:Cost endedMemberships:MembershipEnd[]",
        ["Event"] = "contractVersion:int eventType:id author:side campaignId:id rulesetHash:rawHash configurationHash:hash continuationSourceHash:hash baseHash:hash cycleId:hash positionId:id priorVersion:long stateVersion:long priorPrefix:hash input:Input effect:Effect receiptId:id",
        ["Track"] = "unit:UnitKey route:id[]",
        ["Control"] = "contractVersion:int baseHash:hash continuationSourceHash:hash stateVersion:long prefix:hash positionId:id originalCycle:Authority activeCycle:Authority closure:Closure movementEnd:MovementEndProof members:ReserveMember[] world:World randomState:Random attackHistory:AttackHistory[] targetUses:TargetUse[] nextCycleProgress:ProgressRef[] tracks:Track[] receipts:Receipt[]",
        ["Proof"] = "contractVersion:int sourceId:id sourceHash:hash base:Base control:Control",
    };
    private static readonly Dictionary<string, int> ArrayBounds = new(StringComparer.Ordinal)
    {
        ["Source.movementEventCanonicalUtf8"] = 4,
        ["Effect.endedMemberships"] = 1,
        ["Control.members"] = 1,
        ["Control.receipts"] = 4,
        ["Control.nextCycleProgress"] = 4,
        ["Control.targetUses"] = 0,
        ["Control.tracks"] = 1,
        ["Track.route"] = 5,
    };
    private static readonly Dictionary<string, (string Field, int Value)> Versions = new(StringComparer.Ordinal)
    {
        ["Source"] = ("contractVersion", 1),
        ["Base"] = ("contractVersion", 1),
        ["Command"] = ("contractVersion", 1),
        ["Event"] = ("contractVersion", 1),
        ["Control"] = ("contractVersion", 1),
        ["Proof"] = ("contractVersion", 1),
    };
    private static readonly Dictionary<string, string> Pins = new(StringComparer.Ordinal)
    {
        ["docs/research/fixtures/combat-selected-source-v1.json"] = "e225bc650c4bb942e41245a33da20c21c576ab117807a06805e2558a2c6468df",
        ["docs/research/verify-combat-rng.py"] = "f3c7c468f2be0efa583e44c4d04b8fe6d20eef51aae01554bfa535b0ecc0ea30",
        ["docs/research/verify-combat-source-freeze.py"] = "f21a97150e344337f6ee4c2c1a1752ac7d41b8b8c8bf6a61a06a1fe5a0483e7e",
        ["docs/specs/combat-actual-assault-v1.schema.json"] = "a16d852d5a4463a295e315c824e5298f4ff22b979a7c307187c2a8d926136f0d",
        ["docs/specs/combat-actual-paid-assault-v1.schema.json"] = "be1c5fcc36641ae1ee9f58b4b1c73baca098e7a8cde43d8dc568197f59399324",
        ["docs/specs/combat-actual-post-assault-continuation-v1.schema.json"] = "e948daf499acbb365d9be52b083a10d1692ccbb89fd4bf94e0b4a13601f41c36",
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
        ["docs/specs/fixtures/combat-actual-selection-v1.json"] = "019d1a3ff0f121d83f377ddfb19d274b4a8aa89172bad8aeb289c3b98228e604",
        ["docs/specs/fixtures/combat-authority-envelope-v1.json"] = "adf7b05e15f863b08f366809adf91c97b39480e404ce2efff15e838fa4043eaa",
        ["docs/specs/fixtures/combat-content-v7.canonical.json"] = "ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
        ["docs/specs/fixtures/combat-creation-ledger-v1.json"] = "4b7f87f73a5882800e4e4befb4c06c20bf19c419f92edba5028b355e37442684",
        ["docs/specs/fixtures/combat-cycle-sequence-v1.json"] = "de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f",
        ["docs/specs/fixtures/combat-positive-entry-v1.json"] = "eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e",
        ["docs/specs/fixtures/combat-reserve-designation-v1.json"] = "6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2",
        ["docs/specs/fixtures/combat-rules-inputs-v1.json"] = "5401cd9a690be8b81f19795f1f66c0c6fe24f5055a06cda5facd6193b3e0a29a",
        ["docs/specs/verify-combat-actual-assault-v1.py"] = "1ab27601eb209f48febc440ad0f1cfebd08155b9a747e06c003f11a1a16e64a9",
        ["docs/specs/verify-combat-actual-paid-assault-v1.py"] = "744dc23429b6d343743f1a4c42adcba22502f9a21d9dc433dd3ee6e49a8e4dd1",
        ["docs/specs/verify-combat-actual-post-assault-continuation-v1.py"] = "dad0b1a09219a21a82236b48da9cc67e99a2082219d8906c789e2ca4b0429a9a",
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
        ["tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.weather-tables.v1.golden.json"] = "7f134b9c5dcc356d48a9dfc51976a166b60f02fa93e73b4c0ccfdd354380c182",
    };
    private static readonly Dictionary<string, string> Delivery = new(StringComparer.Ordinal)
    {
        ["docs/specs/combat-actual-repeated-movement-v1.schema.json"] = "a3ebd21e0fd1b1d85b7335ceb2afd304511a2efd5e84bf59710a786526dfd900",
        ["docs/specs/verify-combat-actual-repeated-movement-v1.py"] = "821f860e7c9fb0702c7d04e8c996fc6e77a850eb50a5b371f8e3ff5aae995c60",
    };
    internal static byte[] Consume(string path, Func<string, byte[]> dependencyBytes)
    { Require(Pins.TryGetValue(path, out var expected), 9); return Acquire(path, expected!, dependencyBytes); }
    private static byte[] Acquire(string path, string expected, Func<string, byte[]> dependencyBytes)
    {
        try
        {
            var acquired = dependencyBytes(path); Require(acquired is not null, 9);
            var owned = acquired!.ToArray(); Require(CampaignCombatActualPaidAssaultSupport.Hash(owned) == "sha256:" + CombatEvidenceRevisions.Expected(dependencyBytes, path, expected), 9); return owned;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException)
        { throw new JsonException("CMB-RMV-009", e); }
    }
    internal static Func<string, byte[]> VerifyDependencies(Func<string, byte[]> dependencyBytes)
        => CombatEvidenceRevisions.Capture(dependencyBytes, Pins, Delivery, int.MaxValue, "CMB-RMV-009", revalidateExisting: true);
    internal static string Manifest(Func<string, byte[]> dependencies)
    {
        var value = CombatEvidenceRevisions.Manifest(Pins, dependencies);
        return "sha256:" + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-movement.dependency-manifest.v1", Encode(value));
    }
}
