using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCreatedV11Serializer
{
    public static byte[] Serialize(CampaignCreatedV11 created)
    {
        ArgumentNullException.ThrowIfNull(created);
        var request = created.Request;
        var setup = request.Context.Setup;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 11);
            writer.WriteString("eventType", "campaign-created");
            writer.WriteString("campaignId", request.CampaignId);
            writer.WriteNumber("stateVersion", 1);
            writer.WriteString("rulesetHash", request.Context.RulesetHash);
            writer.WritePropertyName("setup");
            writer.WriteRawValue(CampaignSetupV7Codec.Serialize(setup));
            writer.WritePropertyName("configuration");
            writer.WriteRawValue(CombatDecisionConfigurationCodec.Serialize(request.Context.Configuration));
            writer.WritePropertyName("creationRequest");
            CampaignCombatCreationRequestCodec.Write(writer, request);
            writer.WriteString("creationBinding", request.CreationBinding);
            writer.WritePropertyName("initialWorld");
            writer.WriteRawValue(CampaignWorldV7InitialCodec.Serialize(created.InitialWorld,
                setup.Artifact, setup.Scenario, setup.CombatInitialization));
            CampaignSnapshotSerializer.WriteRandomState(writer, request.RandomState);
            CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", created.SequencePosition);
            writer.WriteStartObject("breakdownFlow");
            writer.WriteString("kind", "idle");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        if (bytes.Length > 1_048_576) throw new JsonException("Created11 exceeds byte limit.");
        return bytes;
    }

    public static CampaignCreatedV11 Deserialize(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest trustedRequest)
    {
        ArgumentNullException.ThrowIfNull(trustedRequest);
        if (bytes.Length > 1_048_576) throw new JsonException("Created11 exceeds byte limit.");
        var expected = CampaignCreatedV11.Create(trustedRequest);
        // At the creation cut the entire envelope is derived. Exact comparison rejects malformed,
        // noncanonical and internally rehashed imports without trusting any embedded authority.
        if (!bytes.SequenceEqual(Serialize(expected)))
            throw new JsonException("Created11 differs from canonical trusted creation evidence.");
        return expected;
    }
}

internal sealed record CampaignCombatCreationCutResult(byte[] CreatedBytes, bool RequiresPublication);

/// <summary>Pure candidate/retry decision. The caller must atomically publish by campaign ID.</summary>
internal static class CampaignCombatCreationCut
{
    public static CampaignCombatCreationCutResult Decide(byte[]? retainedCreatedBytes,
        CampaignCombatCreationRequest request, bool admissionEnabled)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (retainedCreatedBytes is not null)
        {
            _ = CampaignCreatedV11Serializer.Deserialize(retainedCreatedBytes, request);
            return new CampaignCombatCreationCutResult(retainedCreatedBytes.ToArray(), false);
        }
        if (!admissionEnabled) throw new JsonException("Fresh Combat creation admission is disabled.");
        return new CampaignCombatCreationCutResult(
            CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request)), true);
    }
}
