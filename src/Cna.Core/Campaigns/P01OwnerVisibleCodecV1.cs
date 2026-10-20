using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cna.Core.Campaigns;

// Pure, private codec. A caller must supply an independently authenticated visible cut.
// Parsing or hashing here grants no seat authority, decision opening, or action offer.
internal sealed record P01OwnerCycleV1(int GameTurn, int OperationStage, string PlayerPhaseSlot,
    string PhasingSide, int Ordinal);

internal sealed record P01OwnerContextV1(string CampaignId, string Audience, P01OwnerCycleV1 Cycle,
    string RulesRef, string ConfigRef, string CapabilityPolicyId);

internal sealed record P01OwnerTargetSeedV1(P01OwnerContextV1 Context, string LocationId);

internal sealed record P01OwnerDecisionSeedV1(P01OwnerContextV1 Context, string CycleRef,
    string PositionId, long OpeningRevision, string Kind, string ParticipantRef, string TargetRef);

internal sealed record P01OwnerAliasSeedV1(string DecisionRef, long OpeningRevision,
    string CapabilityKind, string OwnParticipantRef, string ApparentTargetRef);

internal static class P01OwnerVisibleCodecV1
{
    private const string TargetDomain = "sandtable.combat.p01.owner-target.v1";
    private const string DecisionDomain = "sandtable.combat.p01.owner-decision.v1";
    private const string AliasDomain = "sandtable.combat.p01.owner-candidate.v1";
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal static byte[] SerializeTarget(P01OwnerTargetSeedV1 seed)
    {
        ValidateContext(seed.Context);
        ValidateId(seed.LocationId);
        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("context");
            WriteContext(writer, seed.Context);
            writer.WriteString("locationId", seed.LocationId);
            writer.WriteEndObject();
        });
    }

    internal static byte[] SerializeDecision(P01OwnerDecisionSeedV1 seed)
    {
        ValidateContext(seed.Context);
        ValidateHash(seed.CycleRef, "sha256:");
        ValidateId(seed.PositionId);
        ValidateRevision(seed.OpeningRevision);
        if (seed.Kind != "selection") throw new JsonException("Invalid decision kind.");
        ValidateHash(seed.ParticipantRef, "pub.");
        ValidateHash(seed.TargetRef, "pub.");
        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("context");
            WriteContext(writer, seed.Context);
            writer.WriteString("cycleRef", seed.CycleRef);
            writer.WriteString("positionId", seed.PositionId);
            writer.WriteNumber("openingRevision", seed.OpeningRevision);
            writer.WriteString("kind", seed.Kind);
            writer.WriteString("participantRef", seed.ParticipantRef);
            writer.WriteString("targetRef", seed.TargetRef);
            writer.WriteEndObject();
        });
    }

    internal static byte[] SerializeAlias(P01OwnerAliasSeedV1 seed)
    {
        ValidateHash(seed.DecisionRef, "pub.");
        ValidateRevision(seed.OpeningRevision);
        if (seed.CapabilityKind != "close-assault-full") throw new JsonException("Invalid capability kind.");
        ValidateHash(seed.OwnParticipantRef, "pub.");
        ValidateHash(seed.ApparentTargetRef, "pub.");
        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("decisionRef", seed.DecisionRef);
            writer.WriteNumber("openingRevision", seed.OpeningRevision);
            writer.WriteString("capabilityKind", seed.CapabilityKind);
            writer.WriteString("ownParticipantRef", seed.OwnParticipantRef);
            writer.WriteString("apparentTargetRef", seed.ApparentTargetRef);
            writer.WriteEndObject();
        });
    }

    internal static P01OwnerTargetSeedV1 ReadTarget(ReadOnlySpan<byte> bytes) => Read(bytes, root =>
        new P01OwnerTargetSeedV1(ReadContext(root.GetProperty("context")), Text(root, "locationId")),
        SerializeTarget);

    internal static P01OwnerDecisionSeedV1 ReadDecision(ReadOnlySpan<byte> bytes) => Read(bytes, root =>
        new P01OwnerDecisionSeedV1(ReadContext(root.GetProperty("context")), Text(root, "cycleRef"),
            Text(root, "positionId"), Number(root, "openingRevision"), Text(root, "kind"),
            Text(root, "participantRef"), Text(root, "targetRef")), SerializeDecision);

    internal static P01OwnerAliasSeedV1 ReadAlias(ReadOnlySpan<byte> bytes) => Read(bytes, root =>
        new P01OwnerAliasSeedV1(Text(root, "decisionRef"), Number(root, "openingRevision"),
            Text(root, "capabilityKind"), Text(root, "ownParticipantRef"),
            Text(root, "apparentTargetRef")), SerializeAlias);

    internal static string TargetRef(P01OwnerContextV1 context, string approvedVisibleLocationId) =>
        Digest(TargetDomain, "pub.", SerializeTarget(new(context, approvedVisibleLocationId)));

    internal static string DecisionRef(P01OwnerDecisionSeedV1 seed) =>
        Digest(DecisionDomain, "pub.", SerializeDecision(seed));

    internal static string CandidateRef(P01OwnerAliasSeedV1 seed) =>
        Digest(AliasDomain, "p01-owner-v1.", SerializeAlias(seed));

    internal static string ValidateOpening(P01OwnerDecisionSeedV1 decision, P01OwnerAliasSeedV1 alias,
        string approvedVisibleLocationId)
    {
        if (decision.TargetRef != TargetRef(decision.Context, approvedVisibleLocationId) ||
            alias.DecisionRef != DecisionRef(decision) ||
            alias.OpeningRevision != decision.OpeningRevision ||
            alias.OwnParticipantRef != decision.ParticipantRef ||
            alias.ApparentTargetRef != decision.TargetRef)
            throw new JsonException("Owner-visible seed binding mismatch.");
        return CandidateRef(alias);
    }

    private static T Read<T>(ReadOnlySpan<byte> bytes, Func<JsonElement, T> parse, Func<T, byte[]> serialize)
    {
        if (bytes.IsEmpty || bytes.Length > 65536 || bytes.IndexOfAnyExceptInRange((byte)0, (byte)127) >= 0)
            throw new JsonException("Invalid owner codec bytes.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            var value = parse(document.RootElement);
            if (!bytes.SequenceEqual(serialize(value))) throw new JsonException("Noncanonical owner codec bytes.");
            return value;
        }
        catch (Exception error) when (error is not JsonException and
            (KeyNotFoundException or InvalidOperationException or FormatException or OverflowException))
        {
            throw new JsonException("Invalid owner codec shape.", error);
        }
    }

    private static P01OwnerContextV1 ReadContext(JsonElement value)
    {
        var cycle = value.GetProperty("cycle");
        return new P01OwnerContextV1(Text(value, "campaignId"), Text(value, "audience"),
            new P01OwnerCycleV1(checked((int)Number(cycle, "gameTurn")),
                checked((int)Number(cycle, "operationStage")), Text(cycle, "playerPhaseSlot"),
                Text(cycle, "phasingSide"), checked((int)Number(cycle, "ordinal"))),
            Text(value, "rulesRef"), Text(value, "configRef"), Text(value, "capabilityPolicyId"));
    }

    private static string Text(JsonElement value, string name)
    {
        var property = value.GetProperty(name);
        if (property.ValueKind != JsonValueKind.String) throw new JsonException("Expected string.");
        return property.GetString()!;
    }

    private static long Number(JsonElement value, string name)
    {
        var property = value.GetProperty(name);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var number))
            throw new JsonException("Expected integer.");
        return number;
    }

    private static void ValidateContext(P01OwnerContextV1 context)
    {
        if (context is null || context.Cycle is null) throw new JsonException("Missing context.");
        ValidateId(context.CampaignId);
        if (context.Audience is not ("axis" or "commonwealth")) throw new JsonException("Invalid audience.");
        var cycle = context.Cycle;
        if (cycle.GameTurn is < 1 or > 111 || cycle.OperationStage is < 1 or > 3 ||
            cycle.PlayerPhaseSlot is not ("first-acting-side" or "second-acting-side") ||
            cycle.PhasingSide is not ("axis" or "commonwealth") || cycle.Ordinal < 1)
            throw new JsonException("Invalid owner cycle.");
        ValidateHash(context.RulesRef, "pub.");
        ValidateHash(context.ConfigRef, "pub.");
        ValidateId(context.CapabilityPolicyId);
    }

    private static void ValidateRevision(long revision)
    {
        if (revision < 1) throw new JsonException("Invalid opening revision.");
    }

    private static void ValidateId(string value)
    {
        if (value is null || value.Length is < 1 or > 128 || value.Any(character => character is < '!' or > '~'))
            throw new JsonException("Invalid visible identifier.");
    }

    private static void ValidateHash(string value, string prefix)
    {
        if (value is null || !value.StartsWith(prefix, StringComparison.Ordinal) ||
            value.Length != prefix.Length + 64 ||
            value.AsSpan(prefix.Length).IndexOfAnyExceptInRange('0', 'f') >= 0 ||
            value[prefix.Length..].Any(character => character is > '9' and < 'a'))
            throw new JsonException("Invalid owner reference.");
    }

    private static void WriteContext(Utf8JsonWriter writer, P01OwnerContextV1 context)
    {
        writer.WriteStartObject();
        writer.WriteString("campaignId", context.CampaignId);
        writer.WriteString("audience", context.Audience);
        writer.WriteStartObject("cycle");
        writer.WriteNumber("gameTurn", context.Cycle.GameTurn);
        writer.WriteNumber("operationStage", context.Cycle.OperationStage);
        writer.WriteString("playerPhaseSlot", context.Cycle.PlayerPhaseSlot);
        writer.WriteString("phasingSide", context.Cycle.PhasingSide);
        writer.WriteNumber("ordinal", context.Cycle.Ordinal);
        writer.WriteEndObject();
        writer.WriteString("rulesRef", context.RulesRef);
        writer.WriteString("configRef", context.ConfigRef);
        writer.WriteString("capabilityPolicyId", context.CapabilityPolicyId);
        writer.WriteEndObject();
    }

    private static byte[] Write(Action<Utf8JsonWriter> action)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions)) action(writer);
        return stream.ToArray();
    }

    private static string Digest(string domain, string prefix, ReadOnlySpan<byte> payload)
    {
        var domainBytes = Encoding.ASCII.GetBytes(domain);
        var input = new byte[domainBytes.Length + 1 + payload.Length];
        domainBytes.CopyTo(input, 0);
        payload.CopyTo(input.AsSpan(domainBytes.Length + 1));
        return prefix + Convert.ToHexStringLower(SHA256.HashData(input));
    }
}
