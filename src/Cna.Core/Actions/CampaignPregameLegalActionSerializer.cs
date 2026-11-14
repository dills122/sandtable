using System.Text.Json;

namespace Cna.Core.Actions;

// Separate policy keeps historical Observation 6/7 action sets closed to setup actions.
internal static class CampaignPregameLegalActionSerializer
{
    internal static byte[] Serialize(CampaignLegalActionSet actionSet)
    {
        ArgumentNullException.ThrowIfNull(actionSet);
        if (actionSet.PolicyId != CampaignLegalActionSet.PregamePolicyId)
            throw new ArgumentException("Expected pre-game action policy.", nameof(actionSet));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", actionSet.ContractVersion);
            writer.WriteString("policyId", actionSet.PolicyId);
            writer.WriteString("campaignId", actionSet.CampaignId);
            writer.WriteNumber("stateVersion", actionSet.StateVersion);
            writer.WriteString("rulesetHash", actionSet.RulesetHash);
            writer.WriteString("positionId", actionSet.PositionId);
            writer.WriteString("audience", CampaignLegalActionSerializer.FormatAudience(actionSet.Audience));
            writer.WriteStartArray("candidates");
            foreach (var candidate in actionSet.Candidates)
                CampaignLegalActionSerializer.WriteCandidate(writer, candidate);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignLegalActionSet DeserializeCanonical(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            CampaignLegalActionSerializer.RequireProperties(root, "contractVersion", "policyId", "campaignId",
                "stateVersion", "rulesetHash", "positionId", "audience", "candidates");
            if (root.GetProperty("contractVersion").GetInt32() != CampaignLegalActionSet.CurrentContractVersion
                || root.GetProperty("policyId").GetString() != CampaignLegalActionSet.PregamePolicyId)
                throw new JsonException("Invalid pre-game action policy.");
            var set = new CampaignLegalActionSet(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(), root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("positionId").GetString()!,
                CampaignLegalActionSerializer.ParseAudience(root.GetProperty("audience").GetString()),
                root.GetProperty("candidates").EnumerateArray().Select(CampaignLegalActionSerializer.ParseCandidate).ToArray(),
                CampaignLegalActionSet.PregamePolicyId);
            if (!bytes.SequenceEqual(Serialize(set)))
                throw new JsonException("Noncanonical pre-game action set.");
            return set;
        }
        catch (JsonException) { throw; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or FormatException or KeyNotFoundException or OverflowException)
        { throw new JsonException("Invalid pre-game action set.", error); }
    }
}
