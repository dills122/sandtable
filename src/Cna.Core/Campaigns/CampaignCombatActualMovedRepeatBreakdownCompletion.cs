using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedRepeatBreakdownCompletionResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    internal CombatActualMovedRepeatBreakdownCompletionResult(byte[] source, byte[] control, byte[] proof,
        byte[]? accepted = null, string? receiptId = null, bool duplicate = false)
    { this.source = source.ToArray(); this.control = control.ToArray(); this.proof = proof.ToArray(); this.accepted = accepted?.ToArray(); ReceiptId = receiptId; Duplicate = duplicate; }
    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => accepted?.ToArray();
    public string? ReceiptId { get; }
    public bool Duplicate { get; }
    public JsonElement Control { get { using var document = JsonDocument.Parse(control); return document.RootElement.Clone(); } }
}

internal static class CampaignCombatActualMovedRepeatBreakdownCompletion
{
    private const string Kind = "complete-breakdown-segment";
    private const string SchemaPath = "docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json";
    private const string VerifierPath = "docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py";
    private const string SchemaHash = "sha256:0568683ecfd1d373a4c428b2ccadf72d826b8ddfc0cc9019ba249b6eb354808a";
    private const string VerifierHash = "sha256:952c63c56f49eccb7550a1047eb1074888401b2c781c56cd034b197d16cc6c1a";
    private const string ManifestHash = "sha256:d71814815139abe7fa4d1b2e7a6aac8b8b4627953e81fbea347135fb9d2be3cb";
    private const string ActionId = "sha256:875564ad87f337593d4f9a95c006d48373f9e7f3a83dda6e232f919fed49efc0";
    private static readonly string[] SourceFields = ["contractVersion", "movementCompletionSourceCanonicalUtf8", "breakdownEventCanonicalUtf8"];
    private static readonly string[] CommandFields = ["contractVersion", "kind", "actionId", "baseHash", "cycleId", "expectedPositionId", "expectedPriorVersion", "capabilityId"];
    private static readonly string[] InputFields = ["command", "actor"];
    private static readonly string[] EventFields = ["contractVersion", "eventType", "author", "campaignId", "rulesetHash", "configurationHash", "profile", "movementCompletionSourceHash", "baseHash", "cycleId", "positionId", "priorVersion", "stateVersion", "priorPrefix", "input", "effect", "receiptId"];
    private static readonly string[] ControlFields = ["contractVersion", "profile", "baseHash", "parentSourceHash", "predecessorControlCanonicalUtf8", "stateVersion", "prefix", "sequencePosition", "phase", "activeCycle", "cycleId", "movementCompletionReceiptId", "breakdownCompletionReceiptId", "receipts", "closed"];
    private static readonly string[] EffectFields = ["kind", "movementCompletionReceiptId", "sources", "successorPosition"];
    private static readonly string[] ReceiptFields = ["commandHash", "eventHash", "receiptId", "actor", "stateVersion"];
    private static readonly string[] BaseFields = ["contractVersion", "movementCompletionSourceHash", "movementCompletionControlHash", "movementCompletionProofHash", "dependencyManifestHash", "movementCompletionReceiptId", "cycleId", "configurationHash"];
    private static readonly string[] ProofFields = ["contractVersion", "sourceId", "sourceHash", "base", "control"];
    private static readonly string[] PositionFields = ["contractVersion", "positionId", "gameTurn", "operationStage", "stageId", "phaseId", "segmentId", "stepId", "actorRole", "activeSide", "sources"];
    private static readonly string[] AuthorityFields = ["contractVersion", "campaignId", "rulesetHash", "setupId", "setupHash", "contentPackId", "contentHash", "scenarioId", "gameTurn", "operationStage", "playerPhaseSlot", "actingSide", "ordinal", "openedAuthorityVersion", "openingPrefix", "admittedPolicyBundleDigest"];
    private static readonly string[] ReferenceFields = ["sourceId", "locator"];
    private sealed record Frame(JsonObject Packet, JsonObject Base, string BaseHash, JsonObject Prior, JsonObject Cycle,
        JsonObject Boundary, JsonObject Successor, byte[][][] Ledgers, Func<string, byte[]> Dependencies, Action VerifyDependencies);

    private static JsonException Error(int code) => new($"CMB-MRB-{code:000}");
    private static void Require(bool condition, int code) { if (!condition) throw Error(code); }
    private static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static string Domain(string kind, byte[] bytes) => CampaignCombatActualMovedRepeatMovementCompletionCodec.HashWithDomain("sandtable.combat.actual-moved-repeat-breakdown-completion." + kind + ".v1", bytes)[7..];
    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static string T(JsonNode node, string name) => node[name]!.GetValue<string>();
    private static long N(JsonNode node, string name) => JsonSerializer.SerializeToElement(node[name]).GetInt64();
    private static bool B(JsonNode node, string name) => node[name]!.GetValue<bool>();
    private static byte[] Encode(JsonNode node) => CampaignCombatActualPaidAssaultCodec.Encode(node);
    private static JsonObject ParseObject(byte[] bytes, string kind)
    {
        Require(bytes.Length > 0 && bytes.Length <= Limit(kind) && bytes.All(x => x < 128), 1);
        JsonDocument document;
        try { document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 42 }); }
        catch (JsonException e) { throw new JsonException("CMB-MRB-001", e); }
        using (document)
        {
            CheckShape(document.RootElement, 0);
            Require(document.RootElement.ValueKind == JsonValueKind.Object, 1);
            var node = JsonNode.Parse(bytes)!.AsObject();
            var canonical = Canonical(node, kind);
            Require(bytes.AsSpan().SequenceEqual(canonical), 8);
            return node;
        }
    }
    private static void CheckShape(JsonElement value, int depth)
    {
        Require(depth <= 42, 1);
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in value.EnumerateObject()) { Require(names.Add(item.Name) && names.Count <= 128, 1); CheckShape(item.Value, depth + 1); }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            var count = 0;
            foreach (var item in value.EnumerateArray()) { Require(++count <= 512, 1); CheckShape(item, depth + 1); }
        }
    }
    private static int Limit(string kind) => kind switch { "Source" => 268435456, "Control" => 2097152, "Proof" => 4194304, "Event" => 32768, "Input" or "Command" => 4096, _ => 4194304 };
    private static byte[] Canonical(JsonNode value, string kind)
    {
        var ordered = Order(value, kind);
        var bytes = Encode(ordered!);
        Require(bytes.Length <= Limit(kind), 1);
        return bytes;
    }
    private static string FieldKind(string owner, string field)
    {
        if (owner == "Source" && field == "movementCompletionSourceCanonicalUtf8") return "ParentCarrier";
        if (owner == "Source" && field == "breakdownEventCanonicalUtf8") return "EventCarrier[]";
        if (owner == "Input" && field == "command") return "Command";
        if (owner == "Command" && field == "kind") return "Kind";
        if (owner == "Event" && field == "input") return "Input";
        if (owner == "Event" && field == "effect") return "Effect";
        if (owner == "Effect" && field == "successorPosition") return "Position";
        if (owner == "Control" && field == "sequencePosition") return "Position";
        if (owner == "Control" && field == "activeCycle") return "Authority";
        if (owner == "Control" && field == "predecessorControlCanonicalUtf8") return "ControlCarrier";
        if (owner == "Control" && field == "receipts") return "Receipt[]";
        if (owner == "Proof" && field == "base") return "Base";
        if (owner == "Proof" && field == "control") return "Control";
        if (field == "sources") return "Reference[]";
        if (field is "contractVersion" or "gameTurn" or "operationStage" or "ordinal") return "Int";
        if (field is "expectedPriorVersion" or "priorVersion" or "stateVersion" or "openedAuthorityVersion") return "Long";
        if (field is "actor" or "author") return "Actor";
        if (field == "profile") return "Profile";
        if (field == "phase") return "Phase";
        if (field == "closed") return "Bool";
        if (field == "rulesetHash") return "RawHash";
        if (field is "actionId" or "baseHash" or "cycleId" or "capabilityId" or
            "movementCompletionSourceHash" or "movementCompletionControlHash" or "movementCompletionProofHash" or
            "dependencyManifestHash" or "configurationHash" or "parentSourceHash" or "prefix" or "priorPrefix" or
            "openingPrefix" or "admittedPolicyBundleDigest" or "setupHash" or "contentHash" or "commandHash" or
            "eventHash" or "sourceHash") return "Hash";
        if (field == "locator") return "Locator";
        return "Id";
    }
    private static JsonNode? Order(JsonNode? node, string kind)
    {
        Require(node is not null, 1);
        if (kind.EndsWith("[]", StringComparison.Ordinal))
        {
            Require(node is JsonArray, 1); var array = node!.AsArray();
            Require(array.Count <= (kind is "EventCarrier[]" or "Receipt[]" ? 1 : kind == "Reference[]" ? 3 : 512), 1);
            return new JsonArray(array.Select(item => Order(item, kind[..^2])).ToArray());
        }
        var fields = kind switch
        {
            "Source" => SourceFields,
            "Base" => BaseFields,
            "Command" => CommandFields,
            "Input" => InputFields,
            "Event" => EventFields,
            "Effect" => EffectFields,
            "Control" => ControlFields,
            "Proof" => ProofFields,
            "Receipt" => ReceiptFields,
            "Position" => PositionFields,
            "Authority" => AuthorityFields,
            "Reference" => ReferenceFields,
            _ => null
        };
        if (fields is null)
        {
            var scalar = JsonSerializer.SerializeToElement(node);
            if (kind is "ParentCarrier" or "ControlCarrier" or "EventCarrier")
            {
                Require(scalar.ValueKind == JsonValueKind.String, 1);
                var value = scalar.GetString()!;
                Require(value.All(x => x < 128) && value.Length > 0, 1);
                Require(value.Length <= kind switch
                {
                    "ParentCarrier" => 134217728,
                    "ControlCarrier" => 1048576,
                    "EventCarrier" => 32768,
                    _ => 4096
                }, 1);
            }
            else if (kind is "Int" or "Long")
            {
                Require(scalar.ValueKind == JsonValueKind.Number && scalar.TryGetInt64(out var n) && n >= 0 &&
                    scalar.GetRawText().All(char.IsAsciiDigit) && (kind == "Long" || n <= int.MaxValue), 2);
            }
            else if (kind == "Bool") Require(scalar.ValueKind is JsonValueKind.True or JsonValueKind.False, 1);
            else
            {
                Require(scalar.ValueKind == JsonValueKind.String, kind is "Profile" or "Phase" or "Kind" ? 3 : 1);
                var value = scalar.GetString()!;
                var valid = kind switch
                {
                    "Kind" => value == Kind,
                    "Profile" => value is "paid" or "armed",
                    "Phase" => value is "breakdown" or "combat",
                    "Actor" => value is "axis" or "commonwealth" or "system",
                    "Hash" => System.Text.RegularExpressions.Regex.IsMatch(value, @"\Asha256:[0-9a-f]{64}\z"),
                    "RawHash" => System.Text.RegularExpressions.Regex.IsMatch(value, @"\A[0-9a-f]{64}\z"),
                    "Id" => System.Text.RegularExpressions.Regex.IsMatch(value, @"\A[A-Za-z0-9][A-Za-z0-9._:-]{0,127}\z"),
                    "Locator" => value.Length is >= 1 and <= 512 && value.All(c => c is >= ' ' and <= '~'),
                    _ => false
                };
                Require(valid, kind is "Profile" or "Phase" or "Kind" ? 3 : 1);
            }
            return node!.DeepClone();
        }
        Require(node is JsonObject, 1); var obj = node!.AsObject();
        Require(obj.Count == fields.Length && fields.All(obj.ContainsKey), 1);
        if (kind is "Source" or "Base" or "Command" or "Event" or "Control" or "Proof")
        {
            var version = JsonSerializer.SerializeToElement(obj["contractVersion"]);
            var n = 0;
            Require(version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out n) &&
                version.GetRawText().All(char.IsAsciiDigit), 2);
            Require(n == 1, 3);
        }
        var result = new JsonObject();
        foreach (var field in fields)
        {
            var member = obj[field];
            var nested = FieldKind(kind, field);
            var optional = (kind == "Control" && field == "breakdownCompletionReceiptId") ||
                (kind == "Position" && field is "segmentId" or "stepId" or "activeSide");
            Require(member is not null || optional, nested is "Int" or "Long" ? 2 : 1);
            result[field] = member is null ? null : Order(member, nested);
        }
        return result;
    }
    private static Func<string, byte[]> Snapshot(Func<string, byte[]> callback, out Action verify)
    {
        ArgumentNullException.ThrowIfNull(callback);
        byte[] Acquire(string path, string expected)
        {
            byte[]? data;
            try { data = callback(path); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException) { throw new JsonException("CMB-MRB-009", e); }
            Require(data is not null && data.Length <= 8_388_608 && Hash(data) == expected, 9);
            return data!.ToArray();
        }
        var schema = Acquire(SchemaPath, SchemaHash);
        var verifier = Acquire(VerifierPath, VerifierHash);
        var manifest = JsonNode.Parse(schema)!["runtimeSourceHashes"]!.AsObject();
        var retained = new Dictionary<string, byte[]>(StringComparer.Ordinal) { [SchemaPath] = schema, [VerifierPath] = verifier };
        foreach (var pair in manifest)
        {
            var path = pair.Key;
            Require(!path.StartsWith('/') && !path.Contains('\\') && !path.Split('/').Any(x => x is "" or "." or ".."), 9);
            retained[path] = Acquire(path, "sha256:" + pair.Value!.GetValue<string>());
        }
        verify = () =>
        {
            foreach (var pair in retained)
                Require(Acquire(pair.Key, Hash(pair.Value)).AsSpan().SequenceEqual(pair.Value), 9);
        };
        return path => { Require(retained.TryGetValue(path, out var data), 9); return data!.ToArray(); };
    }
    private static (Frame Frame, JsonObject State) Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    {
        var ownedSource = source.ToArray();
        IReadOnlyList<byte[]>[] supplied = [prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion];
        Require(supplied.All(x => x is not null), 1);
        var ancestors = supplied.Select(x => x.Select(b => b?.ToArray() ?? throw Error(1)).ToArray()).ToArray();
        var ownValid = own is not null && own.All(b => b is not null);
        var ownSnapshot = own is null ? [] : own.Select(b => b?.ToArray() ?? []).ToArray();
        var ledgers = ancestors.Append(ownSnapshot).ToArray();
        var retained = Snapshot(dependencies, out var verify);
        var packet = ParseObject(ownedSource, "Source");
        var parentBytes = Encoding.ASCII.GetBytes(T(packet, "movementCompletionSourceCanonicalUtf8"));
        Require(parentBytes.Length is > 0 and <= 134217728, 1);
        CombatActualMovedRepeatMovementCompletionResult parent;
        try
        {
            parent = CampaignCombatActualMovedRepeatMovementCompletion.ReplayTrustedSource(parentBytes, request,
            ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], retained);
        }
        catch (JsonException e)
        {
            var match = System.Text.RegularExpressions.Regex.Match(e.Message, @"CMB-[A-Z]+-(\d{3})");
            throw new JsonException(match.Success ? $"CMB-MRB-{match.Groups[1].Value}" : "CMB-MRB-004", e);
        }
        var prior = JsonNode.Parse(parent.ControlBytes)!.AsObject(); var cycle = prior["activeCycle"]!.AsObject();
        Require(N(cycle, "gameTurn") == 1 && N(cycle, "operationStage") == 1 && T(cycle, "playerPhaseSlot") == "first-acting-side" && N(cycle, "ordinal") == 2 && T(prior, "phase") == "breakdown" && B(prior, "closed"), 3);
        Require(T(prior["flow"]!, "kind") == "idle" && prior["interruptContext"] is null && prior["movementEnd"] is not null && N(prior["movementEnd"]!, "ordinal") == 2 && N(prior["originalMovementEnd"]!, "ordinal") == 1 && prior["closure"] is not null && N(prior["closure"]!, "ordinal") == 1 && T(prior["closure"]!, "outcome") == "repeated", 4);
        var movementReceipt = T(prior["movementEnd"]!, "completionReceiptId");
        Require(T(prior["lifecycleReceipts"]!.AsArray()[^1]!, "receiptId") == movementReceipt && ledgers[9].Length == (prior["tracks"]!.AsArray().Count == 0 ? 1 : 3), 4);
        var catalog = JsonNode.Parse(JsonNode.Parse(retained("docs/specs/fixtures/combat-cycle-sequence-v1.json"))!["predecessor"]!["canonicalUtf8"]!.GetValue<string>())!["positions"]!.AsArray();
        var boundary = prior["sequencePosition"]!.DeepClone().AsObject();
        var index = -1;
        for (var i = 0; i < catalog.Count; i++)
        {
            var position = catalog[i]!.DeepClone().AsObject(); position["contractVersion"] = 5;
            if (JsonNode.DeepEquals(position, boundary)) { index = i; break; }
        }
        Require(index >= 0 && index < catalog.Count - 1, 4);
        var successor = catalog[index + 1]!.DeepClone().AsObject(); successor["contractVersion"] = 5;
        Require(T(successor, "positionId").EndsWith(".combat.position-determination", StringComparison.Ordinal), 4);
        var basis = O(("contractVersion", 1), ("movementCompletionSourceHash", Hash(parentBytes)),
            ("movementCompletionControlHash", Hash(parent.ControlBytes)), ("movementCompletionProofHash", Hash(parent.ProofBytes)),
            ("dependencyManifestHash", ManifestHash), ("movementCompletionReceiptId", movementReceipt),
            ("cycleId", prior["cycleId"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]));
        var baseHash = "sha256:" + Domain("base", Canonical(basis, "Base"));
        Require(N(prior, "stateVersion") < long.MaxValue, 7);
        Require(ownValid && ownSnapshot.Length <= 1 && ownSnapshot.Sum(x => x.Length) <= 8192, 1);
        var ownInputs = ownSnapshot.Select(input => ParseObject(input, "Input")).ToArray();
        Require(packet["breakdownEventCanonicalUtf8"]!.AsArray().Count == ownInputs.Length, 4);
        var frame = new Frame(packet, basis, baseHash, prior, cycle.DeepClone().AsObject(), boundary, successor, ledgers, retained, verify);
        var state = Initial(frame);
        foreach (var (input, ordinal) in ownInputs.Select((x, i) => (x, i)))
        {
            var recordedBytes = Encoding.ASCII.GetBytes(packet["breakdownEventCanonicalUtf8"]![ordinal]!.GetValue<string>());
            _ = ParseObject(recordedBytes, "Event");
            var (after, ev, _) = Transition(frame, state, input, true);
            Require(ev is not null && ev.AsSpan().SequenceEqual(recordedBytes), 6);
            state = after;
        }
        frame.VerifyDependencies();
        return (frame, state);
    }
    private static JsonObject Initial(Frame f) => O(("contractVersion", 1), ("profile", f.Prior["profile"]),
        ("baseHash", f.BaseHash), ("parentSourceHash", f.Base["movementCompletionSourceHash"]),
        ("predecessorControlCanonicalUtf8", Encoding.ASCII.GetString(CampaignCombatActualMovedRepeatMovementCompletionCodec.Canonical(f.Prior, "Control"))),
        ("stateVersion", f.Prior["stateVersion"]), ("prefix", f.Prior["prefix"]), ("sequencePosition", f.Boundary),
        ("phase", "breakdown"), ("activeCycle", f.Cycle), ("cycleId", f.Prior["cycleId"]),
        ("movementCompletionReceiptId", f.Base["movementCompletionReceiptId"]), ("breakdownCompletionReceiptId", null),
        ("receipts", new JsonArray()), ("closed", false));
    private static string Capability(Frame f, JsonObject state) => "sha256:" + Domain("capability", Encode(O(
        ("baseHash", f.BaseHash), ("cycleId", state["cycleId"]), ("kind", Kind), ("stateVersion", state["stateVersion"]),
        ("sequencePosition", JsonNode.Parse(Canonical(state["sequencePosition"]!, "Position"))),
        ("movementCompletionReceiptId", state["movementCompletionReceiptId"]))));
    private static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame f, JsonObject prior, JsonObject input, bool admission)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); var cmd = input["command"]!;
        Require(T(cmd, "kind") == Kind && T(cmd, "baseHash") == f.BaseHash && T(prior, "baseHash") == f.BaseHash && T(cmd, "cycleId") == T(prior, "cycleId") && T(prior, "cycleId") == T(f.Prior, "cycleId") && T(cmd, "expectedPositionId") == T(f.Boundary, "positionId") && JsonNode.DeepEquals(prior["activeCycle"], f.Cycle), 4);
        Require(T(input, "actor") == "system", 4); Require(T(cmd, "actionId") == ActionId, 5);
        var commandHash = Hash(Canonical(cmd, "Command"));
        var existing = prior["receipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == commandHash && T(r!, "actor") == "system");
        if (existing is not null) return (prior.DeepClone().AsObject(), null, T(existing, "receiptId"));
        Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion") && JsonNode.DeepEquals(prior["sequencePosition"], f.Boundary) && T(prior, "phase") == "breakdown", 6);
        Require(!B(prior, "closed") && prior["receipts"]!.AsArray().Count == 0 && prior["breakdownCompletionReceiptId"] is null && N(prior, "stateVersion") < long.MaxValue, 7);
        Require(T(cmd, "capabilityId") == Capability(f, prior), 5); Require(admission, 7);
        var effect = O(("kind", "breakdown-completed"), ("movementCompletionReceiptId", prior["movementCompletionReceiptId"]),
            ("sources", f.Boundary["sources"]), ("successorPosition", f.Successor));
        var ev = O(("contractVersion", 1), ("eventType", "combat-actual-moved-repeat-breakdown-completed"), ("author", "system"),
            ("campaignId", f.Cycle["campaignId"]), ("rulesetHash", f.Cycle["rulesetHash"]), ("configurationHash", f.Cycle["admittedPolicyBundleDigest"]),
            ("profile", prior["profile"]), ("movementCompletionSourceHash", prior["parentSourceHash"]), ("baseHash", f.BaseHash),
            ("cycleId", prior["cycleId"]), ("positionId", prior["sequencePosition"]!["positionId"]), ("priorVersion", prior["stateVersion"]),
            ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", JsonNode.Parse(Canonical(input, "Input"))),
            ("effect", JsonNode.Parse(Canonical(effect, "Effect"))));
        var receipt = "mrb." + Domain("receipt", Encode(ev)); ev["receiptId"] = receipt; var bytes = Canonical(ev, "Event");
        var after = prior.DeepClone().AsObject(); after["stateVersion"] = ev["stateVersion"]!.DeepClone();
        after["prefix"] = CampaignCombatActualMovedRepeatMovementCompletionCodec.EventPrefix(T(prior, "prefix"), bytes);
        after["sequencePosition"] = f.Successor.DeepClone(); after["phase"] = "combat"; after["breakdownCompletionReceiptId"] = receipt; after["closed"] = true;
        after["receipts"]!.AsArray().Add(O(("commandHash", commandHash), ("eventHash", Hash(bytes)), ("receiptId", receipt), ("actor", "system"), ("stateVersion", after["stateVersion"])));
        _ = Canonical(after, "Control"); return (after, bytes, receipt);
    }
    private static CombatActualMovedRepeatBreakdownCompletionResult Result(Frame frame, JsonObject state,
        byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var source = Canonical(frame.Packet, "Source"); var control = Canonical(state, "Control"); var hash = "sha256:" + Domain("source", source);
        var proof = Canonical(O(("contractVersion", 1), ("sourceId", "mrbcomp." + hash[7..]), ("sourceHash", hash),
            ("base", frame.Base), ("control", state)), "Proof");
        return new(source, control, proof, accepted, receipt, duplicate);
    }
    public static CombatActualMovedRepeatBreakdownCompletionResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    { var (f, state) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, own, dependencies); return Result(f, state); }
    public static CombatActualMovedRepeatBreakdownCompletionResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own,
        ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var owned = currentInput.ToArray(); var (f, state) = Replay(source, request, prefix, lifecycle, breakdown,
            selection, round, paid, release, control, movement, completion, own, dependencies);
        var input = ParseObject(owned, "Input"); var (after, ev, receipt) = Transition(f, state, input, admissionEnabled);
        if (ev is null) return Result(f, state, receipt: receipt, duplicate: true);
        var packet = f.Packet.DeepClone().AsObject(); packet["breakdownEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var next = f.Ledgers.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(); next[10] = [.. next[10], owned];
        var (verified, rebuilt) = Replay(Canonical(packet, "Source"), request, next[0], next[1], next[2], next[3], next[4],
            next[5], next[6], next[7], next[8], next[9], next[10], f.Dependencies);
        Require(JsonNode.DeepEquals(after, rebuilt), 6); f.VerifyDependencies();
        return Result(verified, rebuilt, ev, receipt);
    }

    private static void RequireFrozen(Frame frame)
    {
        var schema = JsonNode.Parse(frame.Dependencies(SchemaPath))!.AsObject();
        Require(B(schema, "closureFrozen"), 7);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    {
        var (frame, state) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid,
            release, control, movement, completion, own, dependencies);
        RequireFrozen(frame); return Result(frame, state).SourceBytes;
    }
    public static CombatActualMovedRepeatBreakdownCompletionResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own,
        Func<string, byte[]> dependencies)
    {
        var owned = claim.ToArray(); var (frame, state) = Replay(source, request, prefix, lifecycle, breakdown,
            selection, round, paid, release, control, movement, completion, own, dependencies);
        RequireFrozen(frame); _ = ParseObject(owned, "Control");
        var result = Result(frame, state); Require(owned.AsSpan().SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualMovedRepeatBreakdownCompletionResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own,
        Func<string, byte[]> dependencies)
    {
        var owned = claim.ToArray(); var (frame, state) = Replay(source, request, prefix, lifecycle, breakdown,
            selection, round, paid, release, control, movement, completion, own, dependencies);
        RequireFrozen(frame); _ = ParseObject(owned, "Proof");
        var result = Result(frame, state); Require(owned.AsSpan().SequenceEqual(result.ProofBytes), 6); return result;
    }
}
