using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>
/// Private, unissuable Core-owned bytes for the pinned r012-c01 creation and first System step.
/// Storage of the canonical bytes and compare-and-swap of their head remain host responsibilities.
/// </summary>
internal static class P01CoreOpeningJournalV1
{
    private const int MaximumBytes = 1_100_000;
    private const string RequestHash = "sha256:b8c96173e432f7f8f09b68cd13e2d1e1a2d49e1a2380815b49fe23dd5510a16c";
    private static readonly string[] Fields = ["contractVersion", "campaignId", "creationRequestCanonicalUtf8",
        "createdEventCanonicalUtf8", "records", "admitted"];
    private static readonly string[] RecordFields = ["sequence", "inputCanonicalUtf8", "eventCanonicalUtf8",
        "sourceReferencesCanonicalUtf8", "receiptId", "priorPrefix", "afterPrefix"];

    internal static byte[] Create(CampaignCombatCreationRequest trustedRequest)
    {
        RequirePinnedRequest(trustedRequest);
        return Build(trustedRequest, includeInitiative: false);
    }

    internal static byte[] AppendSystemInitiative(ReadOnlySpan<byte> retainedJournal,
        CampaignCombatCreationRequest trustedRequest)
    {
        var state = Read(retainedJournal, trustedRequest);
        return state.Events.Count == 1 ? retainedJournal.ToArray() : Build(trustedRequest, includeInitiative: true);
    }

    internal static CampaignOpeningPreambleState Read(ReadOnlySpan<byte> retainedJournal,
        CampaignCombatCreationRequest trustedRequest)
    {
        RequirePinnedRequest(trustedRequest);
        if (retainedJournal.Length is < 1 or > MaximumBytes)
            throw new JsonException("P01-J1-001");
        try
        {
            using var document = JsonDocument.Parse(retainedJournal.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            RequireFields(root, Fields);
            if (root.GetProperty(Fields[0]).GetInt32() != 1 ||
                root.GetProperty(Fields[1]).GetString() != trustedRequest.CampaignId ||
                root.GetProperty(Fields[5]).GetBoolean())
                throw new JsonException("P01-J1-001");
            var records = root.GetProperty(Fields[4]);
            if (records.ValueKind != JsonValueKind.Array || records.GetArrayLength() > 1)
                throw new JsonException("P01-J1-001");
            foreach (var record in records.EnumerateArray()) RequireFields(record, RecordFields);
            var expected = Build(trustedRequest, records.GetArrayLength() == 1);
            if (!retainedJournal.SequenceEqual(expected)) throw new JsonException("P01-J1-002");
            var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(trustedRequest));
            var events = records.GetArrayLength() == 1
                ? [Initiative(trustedRequest, created).EventBytes]
                : Array.Empty<byte[]>();
            return CampaignOpeningPreamble.Replay(trustedRequest, created, events);
        }
        catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new JsonException("P01-J1-001", error);
        }
    }

    private static byte[] Build(CampaignCombatCreationRequest request, bool includeInitiative)
    {
        var creationRequest = CampaignCombatCreationRequestCodec.Serialize(request);
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }))
        {
            writer.WriteStartObject();
            writer.WriteNumber(Fields[0], 1);
            writer.WriteString(Fields[1], request.CampaignId);
            writer.WriteString(Fields[2], Encoding.ASCII.GetString(creationRequest));
            writer.WriteString(Fields[3], Encoding.ASCII.GetString(created));
            writer.WriteStartArray(Fields[4]);
            if (includeInitiative)
            {
                var before = CampaignOpeningPreamble.Replay(request, created, []);
                var input = CampaignOpeningPreamble.Command(before);
                var applied = Initiative(request, created);
                using var eventDocument = JsonDocument.Parse(applied.EventBytes);
                writer.WriteStartObject();
                writer.WriteNumber(RecordFields[0], 1);
                writer.WriteString(RecordFields[1], Encoding.ASCII.GetString(CampaignOpeningPreambleCodec.SerializeInput(input)));
                writer.WriteString(RecordFields[2], Encoding.ASCII.GetString(applied.EventBytes));
                writer.WriteString(RecordFields[3], eventDocument.RootElement.GetProperty("sources").GetRawText());
                writer.WriteString(RecordFields[4], applied.State.Receipts.Single().ReceiptId);
                writer.WriteString(RecordFields[5], before.Prefix);
                writer.WriteString(RecordFields[6], applied.State.Prefix);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteBoolean(Fields[5], false);
            writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        if (bytes.Length > MaximumBytes) throw new JsonException("P01-J1-001");
        return bytes;
    }

    private static CampaignOpeningPreambleResult Initiative(CampaignCombatCreationRequest request, byte[] created)
    {
        var before = CampaignOpeningPreamble.Replay(request, created, []);
        return CampaignOpeningPreamble.Apply(request, created, [], CampaignOpeningPreamble.Command(before));
    }

    private static void RequirePinnedRequest(CampaignCombatCreationRequest? request)
    {
        if (request is null || request.CampaignId != "rules-lab.combat-creation.1" ||
            Hash(CampaignCombatCreationRequestCodec.Serialize(request)) != RequestHash)
            throw new JsonException("P01-J1-003");
    }

    private static void RequireFields(JsonElement value, string[] fields)
    {
        if (value.ValueKind != JsonValueKind.Object ||
            !value.EnumerateObject().Select(property => property.Name).SequenceEqual(fields))
            throw new JsonException("P01-J1-001");
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
