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
                WriteCandidate(writer, candidate);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteCandidate(
        Utf8JsonWriter writer,
        CampaignActionCandidate candidate)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", candidate.ContractVersion);
        writer.WriteString("actionId", candidate.ActionId);
        writer.WriteString("kind", candidate.Kind);

        switch (candidate)
        {
            case DesignateReserveAction designation:
                writer.WriteString("elementId", designation.ElementId);
                break;
            case ActFirstAction:
            case ActLastAction:
                writer.WriteNumber("operationStage", candidate.OperationStage!.Value);
                break;
            case ResolveInitiativeAction:
            case ResolveNoObligationNavalConvoyScheduleAction:
            case ResolveNoObligationTacticalShippingAction:
            case ResolveWeatherAction:
            case ResolveNoObligationOrganizationAction:
            case ResolveNoObligationNavalConvoyArrivalAction:
            case ResolveNoObligationFleetAssignmentAction:
            case ResolveNoObligationFleetRepairAction:
            case CompleteReserveDesignationAction:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(candidate));
        }

        writer.WriteEndObject();
    }

    internal static string FormatAudience(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.System => "system",
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };
}
