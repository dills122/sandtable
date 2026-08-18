using System.Text.Json;

namespace Cna.Core.Actions;

public static class CampaignLegalActionSerializer
{
    public static byte[] Serialize(CampaignLegalActionSet actionSet)
    {
        ArgumentNullException.ThrowIfNull(actionSet);
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
            writer.WriteString("audience", FormatAudience(actionSet.Audience));
            writer.WriteStartArray("candidates");
            foreach (var candidate in actionSet.Candidates)
            {
                writer.WriteStartObject();
                writer.WriteNumber("contractVersion", candidate.ContractVersion);
                writer.WriteString("actionId", candidate.ActionId);
                writer.WriteString("kind", candidate.Kind);
                if (candidate.OperationStage is not null)
                    writer.WriteNumber("operationStage", candidate.OperationStage.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string FormatAudience(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.System => "system",
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };
}
