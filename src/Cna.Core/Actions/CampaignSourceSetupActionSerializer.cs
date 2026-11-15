using System.Text.Json;

namespace Cna.Core.Actions;

internal static class CampaignSourceSetupActionSerializer
{
    internal static byte[] Serialize(CampaignLegalActionSet set)
    {
        if (set.PolicyId != CampaignLegalActionSet.SourceSetupPolicyId) throw new ArgumentException("Source setup policy required.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", set.ContractVersion); writer.WriteString("policyId", set.PolicyId);
            writer.WriteString("campaignId", set.CampaignId); writer.WriteNumber("stateVersion", set.StateVersion);
            writer.WriteString("rulesetHash", set.RulesetHash); writer.WriteString("positionId", set.PositionId);
            writer.WriteString("audience", CampaignLegalActionSerializer.FormatAudience(set.Audience)); writer.WriteStartArray("candidates");
            foreach (var candidate in set.Candidates.Cast<CampaignSourceSetupAction>())
            {
                writer.WriteStartObject(); writer.WriteNumber("contractVersion", candidate.ContractVersion); writer.WriteString("actionId", candidate.ActionId);
                writer.WriteString("kind", candidate.Kind); writer.WriteNumber("stateVersion", candidate.StateVersion); writer.WriteString("family", candidate.Family);
                writer.WritePropertyName("choice"); writer.WriteRawValue(candidate.ChoiceJson); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignLegalActionSet DeserializeCanonical(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray()); var root = document.RootElement;
            CampaignLegalActionSerializer.RequireProperties(root, "contractVersion", "policyId", "campaignId", "stateVersion", "rulesetHash", "positionId", "audience", "candidates");
            var candidates = new List<CampaignActionCandidate>();
            foreach (var item in root.GetProperty("candidates").EnumerateArray())
            {
                CampaignLegalActionSerializer.RequireProperties(item, "contractVersion", "actionId", "kind", "stateVersion", "family", "choice");
                var action = new CampaignSourceSetupAction(item.GetProperty("stateVersion").GetInt64(), item.GetProperty("family").GetString()!, "", item.GetProperty("choice"));
                if (item.GetProperty("contractVersion").GetInt32() != 1 || item.GetProperty("kind").GetString() != action.Kind || item.GetProperty("actionId").GetString() != action.ActionId)
                    throw new JsonException("Source action identity differs.");
                candidates.Add(action);
            }
            var set = new CampaignLegalActionSet(root.GetProperty("campaignId").GetString()!, root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!, root.GetProperty("positionId").GetString()!,
                CampaignLegalActionSerializer.ParseAudience(root.GetProperty("audience").GetString()), candidates, CampaignLegalActionSet.SourceSetupPolicyId);
            if (root.GetProperty("contractVersion").GetInt32() != CampaignLegalActionSet.CurrentContractVersion
                || root.GetProperty("policyId").GetString() != set.PolicyId || !bytes.SequenceEqual(Serialize(set)))
                throw new JsonException("Source action set is not canonical.");
            return set;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or OverflowException)
        { throw new JsonException("Invalid source action set.", error); }
    }
}
