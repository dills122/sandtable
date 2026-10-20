using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using static Cna.Core.Campaigns.CampaignCombatActualMovedRepeatBreakdownCompletionCodec;

namespace Cna.Core.Campaigns;

/// <summary>Closed, private A1 import. These bytes are evidence, never an authorization to issue an action.</summary>
internal sealed class P01R03ImportV1
{
    internal const int MaximumBytes = 2_097_152;
    internal const string FrozenManifest = "sha256:0052acb77da04ecbbee792a0cb330fd4d8189cca1513d5d450fbb716d265cfeb";
    private static readonly string[] Fields = ["contractVersion", "creationRequestCanonicalUtf8", "predecessorInputLedgersCanonicalUtf8", "r03InputLedgerCanonicalUtf8", "r03SourceCanonicalUtf8", "dependencyManifestHash"];

    internal P01R03ImportV1(byte[] creationRequest, byte[][][] predecessors, byte[][] r03Input, byte[] r03Source)
    {
        CreationRequest = creationRequest?.ToArray() ?? throw new JsonException("P01-A1-001");
        Predecessors = predecessors?.Select(x => x?.Select(b => b?.ToArray() ?? throw new JsonException("P01-A1-001")).ToArray() ?? throw new JsonException("P01-A1-001")).ToArray() ?? throw new JsonException("P01-A1-001");
        R03Input = r03Input?.Select(x => x?.ToArray() ?? throw new JsonException("P01-A1-001")).ToArray() ?? throw new JsonException("P01-A1-001");
        R03Source = r03Source?.ToArray() ?? throw new JsonException("P01-A1-001");
        RequireShape();
    }

    internal byte[] CreationRequest { get; }
    internal byte[][][] Predecessors { get; }
    internal byte[][] R03Input { get; }
    internal byte[] R03Source { get; }

    private void RequireShape()
    {
        if (CreationRequest.Length is < 1 or > 4_096 || R03Source.Length is < 1 or > 1_048_576 ||
            Predecessors.Length != 10 || R03Input.Length != 1 ||
            Predecessors.SelectMany(x => x).Concat(R03Input).Any(x => x.Length is < 1 or > 4_096) ||
            Predecessors.Sum(x => x.Length) > 128 ||
            Predecessors.SelectMany(x => x).Concat(R03Input).Sum(x => x.Length) > 32_768)
            throw new JsonException("P01-A1-001");
        RequireAscii(CreationRequest); RequireAscii(R03Source);
        foreach (var input in Predecessors.SelectMany(x => x).Concat(R03Input)) RequireAscii(input);
    }

    private static void RequireAscii(byte[] bytes)
    {
        if (bytes.Any(x => x >= 128)) throw new JsonException("P01-A1-001");
    }

    internal byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject(); writer.WriteNumber(Fields[0], 1);
            writer.WriteString(Fields[1], Encoding.ASCII.GetString(CreationRequest));
            writer.WriteStartArray(Fields[2]);
            foreach (var ledger in Predecessors)
            {
                writer.WriteStartArray(); foreach (var input in ledger) writer.WriteStringValue(Encoding.ASCII.GetString(input)); writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteStartArray(Fields[3]); foreach (var input in R03Input) writer.WriteStringValue(Encoding.ASCII.GetString(input)); writer.WriteEndArray();
            writer.WriteString(Fields[4], Encoding.ASCII.GetString(R03Source));
            writer.WriteString(Fields[5], FrozenManifest); writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        if (bytes.Length > MaximumBytes) throw new JsonException("P01-A1-001");
        return bytes;
    }

    internal static P01R03ImportV1 Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > MaximumBytes) throw new JsonException("P01-A1-001");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            RejectDuplicateProperties(root);
            if (root.ValueKind != JsonValueKind.Object || !root.EnumerateObject().Select(x => x.Name).SequenceEqual(Fields) ||
                root.GetProperty(Fields[0]).GetInt32() != 1 || root.GetProperty(Fields[5]).GetString() != FrozenManifest)
                throw new JsonException("P01-A1-001");
            var predecessors = root.GetProperty(Fields[2]).EnumerateArray().Select(x => x.EnumerateArray().Select(y => Ascii(y.GetString())).ToArray()).ToArray();
            var own = root.GetProperty(Fields[3]).EnumerateArray().Select(x => Ascii(x.GetString())).ToArray();
            var value = new P01R03ImportV1(Ascii(root.GetProperty(Fields[1]).GetString()), predecessors, own, Ascii(root.GetProperty(Fields[4]).GetString()));
            if (!bytes.SequenceEqual(value.Serialize())) throw new JsonException("P01-A1-001");
            return value;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("P01-A1-001", error); }
    }

    private static byte[] Ascii(string? text)
    {
        if (text is null || text.Any(c => c > 127)) throw new JsonException("P01-A1-001");
        return Encoding.ASCII.GetBytes(text);
    }

    private static void RejectDuplicateProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!keys.Add(property.Name)) throw new JsonException("P01-A1-001");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RejectDuplicateProperties(item);
    }
}

/// <summary>Immutable Core validation result; it has no journal sequence, opening, or outward identifier.</summary>
internal sealed class P01ValidatedCheckpointV1
{
    private readonly byte[] canonicalBytes;
    private readonly bool admitted;
    internal P01ValidatedCheckpointV1(byte[] bytes, bool admitted)
    {
        if (admitted) throw new JsonException("P01-A1-003");
        canonicalBytes = bytes.ToArray(); this.admitted = admitted;
    }
    internal byte[] CanonicalBytes => canonicalBytes.ToArray();
    internal bool Admitted => admitted;
}

/// <summary>Private, unissuable validation of the single independently pinned r012-c01 R03 root.</summary>
internal static class P01R03ValidationCheckpointV1
{
    private const string CampaignId = "rules-lab.combat-creation.1";
    private const string RulesetHash = "8af256c6c2bbf71cb72ea7e29922db9c4c897c2535121a68a6849bbd06e0bd18";
    private const string SetupHash = "sha256:22495e26528c2f4db8335d3b4041194aef26295f30aab0e4e6f384ff17069848";
    private const string ContentHash = "sha256:ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847";
    private const string ConfigurationHash = "sha256:3de30c451ba7e81d4fdde6493e07d4d06110209a89035d9e59bba738f0aeba34";
    private const string SourceHash = "sha256:7dd4a0860b65d8bd1db51f65cd2eadc7b57110cf95c0bf5d32447f2937573638";
    private const string EventHash = "sha256:3165ec2878d3f27235410aa4ae6a2827583fc6a27757f8e9fd2d99a492689e9d";
    private const string ControlHash = "sha256:1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c";
    private const string ProofHash = "sha256:c084755d9e970cf3bb7b751748db47a9193d51e3d2281b4f1f3a00c68e96d93f";
    private const string ReceiptId = "mrbc.af668a6a66dd890e432e4012f3690f10cfa8fb4e5a23c4ec4f228836a30f61d9";
    private const string Prefix = "sha256:d48e0c8f337c96f04b950cc026b1968c87e65cc9c91d6e7c902bab67021910f4";

    internal static P01ValidatedCheckpointV1 Validate(ReadOnlySpan<byte> importBytes,
        CampaignCombatCreationContext trustedContext, Func<string, byte[]> trustedDependencies)
    {
        ArgumentNullException.ThrowIfNull(trustedContext);
        ArgumentNullException.ThrowIfNull(trustedDependencies);
        var import = P01R03ImportV1.Deserialize(importBytes);
        if (trustedContext.RulesetHash != RulesetHash || trustedContext.Setup.SetupHash != SetupHash ||
            trustedContext.Setup.Artifact.Identity.Hash != ContentHash || trustedContext.Configuration.Hash != ConfigurationHash ||
            ManifestHash != P01R03ImportV1.FrozenManifest || Hash(import.R03Source) != SourceHash)
            throw new JsonException("P01-A1-002");
        CampaignCombatCreationRequest request;
        try { request = CampaignCombatCreationRequestCodec.Deserialize(import.CreationRequest, trustedContext); }
        catch (JsonException error) { throw new JsonException("P01-A1-002", error); }
        if (request.CampaignId != CampaignId) throw new JsonException("P01-A1-002");
        var parents = import.Predecessors;
        var result = CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(import.R03Source, request,
            parents[0], parents[1], parents[2], parents[3], parents[4], parents[5], parents[6], parents[7], parents[8], parents[9],
            import.R03Input, trustedDependencies);
        using var source = JsonDocument.Parse(result.SourceBytes);
        using var control = JsonDocument.Parse(result.ControlBytes);
        using var proof = JsonDocument.Parse(result.ProofBytes);
        var eventBytes = Encoding.ASCII.GetBytes(source.RootElement.GetProperty("breakdownEventCanonicalUtf8")[0].GetString()!);
        var state = control.RootElement;
        if (Hash(result.SourceBytes) != SourceHash || Hash(eventBytes) != EventHash ||
            Hash(result.ControlBytes) != ControlHash || Hash(result.ProofBytes) != ProofHash ||
            state.GetProperty("stateVersion").GetInt64() != 33 || state.GetProperty("prefix").GetString() != Prefix ||
            state.GetProperty("breakdownCompletionReceiptId").GetString() != ReceiptId ||
            state.GetProperty("sequencePosition").GetProperty("positionId").GetString() !=
                "land.position.operation-1.first-player.movement-and-combat.combat.position-determination" ||
            state.GetProperty("activeCycle").GetProperty("ordinal").GetInt32() != 2 ||
            state.GetProperty("activeCycle").GetProperty("campaignId").GetString() != CampaignId ||
            !state.GetProperty("closed").GetBoolean() || proof.RootElement.GetProperty("control").GetRawText() != state.GetRawText())
            throw new JsonException("P01-A1-003");
        return new P01ValidatedCheckpointV1(CheckpointBytes(import, request, state), admitted: false);
    }

    internal static P01ValidatedCheckpointV1 ReadCheckpoint(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> importBytes,
        CampaignCombatCreationContext trustedContext, Func<string, byte[]> trustedDependencies)
    {
        if (claim.Length is < 1 or > 4_096) throw new JsonException("P01-A1-004");
        var recomputed = Validate(importBytes, trustedContext, trustedDependencies);
        if (!claim.SequenceEqual(recomputed.CanonicalBytes)) throw new JsonException("P01-A1-004");
        return recomputed;
    }

    private static byte[] CheckpointBytes(P01R03ImportV1 import, CampaignCombatCreationRequest request, JsonElement state)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", request.CampaignId); writer.WriteString("creationBinding", request.CreationBinding);
            writer.WriteString("creationRequestHash", Hash(import.CreationRequest));
            writer.WriteStartArray("orderedLedgerHashesAndLengths");
            foreach (var ledger in import.Predecessors.Append(import.R03Input))
            {
                using var ledgerStream = new MemoryStream();
                using (var ledgerWriter = new Utf8JsonWriter(ledgerStream))
                {
                    ledgerWriter.WriteStartArray(); foreach (var entry in ledger) ledgerWriter.WriteRawValue(entry, skipInputValidation: false); ledgerWriter.WriteEndArray();
                }
                var ledgerBytes = ledgerStream.ToArray();
                writer.WriteStartObject(); writer.WriteNumber("bytes", ledgerBytes.Length); writer.WriteString("hash", Hash(ledgerBytes)); writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteString("dependencyManifestHash", ManifestHash); writer.WriteString("r03SourceHash", SourceHash);
            writer.WriteString("r03ControlHash", ControlHash); writer.WriteString("r03ProofHash", ProofHash);
            writer.WriteNumber("terminalStateVersion", state.GetProperty("stateVersion").GetInt64());
            writer.WriteString("terminalPrefix", state.GetProperty("prefix").GetString());
            writer.WriteString("terminalReceiptId", state.GetProperty("breakdownCompletionReceiptId").GetString());
            writer.WriteString("positionId", state.GetProperty("sequencePosition").GetProperty("positionId").GetString());
            writer.WriteString("activeCycleId", state.GetProperty("cycleId").GetString());
            writer.WriteBoolean("admitted", false); writer.WriteEndObject();
        }
        var bytes = stream.ToArray(); if (bytes.Length > 4_096) throw new JsonException("P01-A1-003"); return bytes;
    }
}
