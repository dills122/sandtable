using System.Text.Json;

namespace Cna.Core.Actions;

internal static class CampaignSourceSetupActionSerializer
{
    internal static byte[] Serialize(CampaignLegalActionSet set)
    {
        if (set.PolicyId is not (CampaignLegalActionSet.SourceSetupPolicyId or CampaignLegalActionSet.SourceTurnPolicyId))
            throw new ArgumentException("Source policy required.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", set.ContractVersion); writer.WriteString("policyId", set.PolicyId);
            writer.WriteString("campaignId", set.CampaignId); writer.WriteNumber("stateVersion", set.StateVersion);
            writer.WriteString("rulesetHash", set.RulesetHash); writer.WriteString("positionId", set.PositionId);
            writer.WriteString("audience", CampaignLegalActionSerializer.FormatAudience(set.Audience)); writer.WriteStartArray("candidates");
            foreach (var candidate in set.Candidates)
            {
                writer.WriteStartObject(); writer.WriteNumber("contractVersion", candidate.ContractVersion); writer.WriteString("actionId", candidate.ActionId);
                writer.WriteString("kind", candidate.Kind);
                if (candidate is CampaignSourceSetupAction setup)
                {
                    writer.WriteNumber("stateVersion", setup.StateVersion); writer.WriteString("family", setup.Family);
                    writer.WritePropertyName("choice"); writer.WriteRawValue(setup.ChoiceJson);
                }
                else if (candidate is CampaignSourceTurnAction turn)
                {
                    writer.WriteNumber("stateVersion", turn.StateVersion); writer.WriteString("destination", turn.Destination);
                    if (turn.Lane is int lane) { writer.WriteNumber("lane", lane); writer.WriteNumber("operationStage", turn.Stage!.Value); writer.WriteNumber("delta", turn.Delta!.Value); }
                }
                else throw new ArgumentException("Unsupported source candidate.");
                writer.WriteEndObject();
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
            var policy = root.GetProperty("policyId").GetString();
            if (policy is not (CampaignLegalActionSet.SourceSetupPolicyId or CampaignLegalActionSet.SourceTurnPolicyId))
                throw new JsonException("Unknown source action policy.");
            var candidates = new List<CampaignActionCandidate>();
            foreach (var item in root.GetProperty("candidates").EnumerateArray())
            {
                CampaignActionCandidate action;
                if (policy == CampaignLegalActionSet.SourceSetupPolicyId)
                {
                    CampaignLegalActionSerializer.RequireProperties(item, "contractVersion", "actionId", "kind", "stateVersion", "family", "choice");
                    action = new CampaignSourceSetupAction(item.GetProperty("stateVersion").GetInt64(), item.GetProperty("family").GetString()!, "", item.GetProperty("choice"));
                }
                else
                {
                    var version = item.GetProperty("stateVersion").GetInt64();
                    var kind = item.GetProperty("kind").GetString();
                    var destination = item.GetProperty("destination");
                    if (kind == "source-axis-gt3-supply-adjust")
                        CampaignLegalActionSerializer.RequireProperties(item, "contractVersion", "actionId", "kind", "stateVersion", "destination", "lane", "operationStage", "delta");
                    else
                        CampaignLegalActionSerializer.RequireProperties(item, "contractVersion", "actionId", "kind", "stateVersion", "destination");
                    action = kind switch
                    {
                        "source-fourth-indian-opening-move" when destination.ValueKind == JsonValueKind.String => CampaignSourceTurnAction.MoveFourthIndianOpening(version, destination.GetString()!),
                        "source-polish-arrival" when destination.ValueKind == JsonValueKind.String => new CampaignSourceTurnAction(version, destination.GetString()!),
                        "source-axis-gt3-supply-roll" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.RollGt3Supply(version),
                        "source-commonwealth-gt5-supply-roll" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.RollGt5Supply(version),
                        "source-tactical-shipping-decline" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.DeclineTacticalShipping(version),
                        "source-voluntary-organization-decline" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.DeclineVoluntaryOrganization(version),
                        "source-stage1-first-reserves-decline" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.DeclineFirstReserves(version),
                        "source-stage1-first-movement-finish" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.FinishFirstMovement(version),
                        "source-australian-baseline-approve" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.ApproveSixthAustralianBaseline(version),
                        "source-australian-baseline-withdraw" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.WithdrawSixthAustralianBaseline(version),
                        "source-sixth-australian-training-interrupt" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.InterruptSixthAustralianTraining(version),
                        "source-gt1-lane6-attack-decline" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.DeclineGt1Lane6Attack(version),
                        "source-gt1-lane3-attack-decline" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.DeclineGt1Lane3Attack(version),
                        "source-gt1-lane2-attack-decline" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.DeclineGt1Lane2Attack(version),
                        "source-stage1-act-first" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.ActFirst(version),
                        "source-stage1-act-last" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.ActLast(version),
                        "source-axis-gt3-supply-finish" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.FinishGt3Supply(version),
                        "source-axis-gt3-supply-adjust" when destination.ValueKind == JsonValueKind.Null => CampaignSourceTurnAction.AdjustGt3Supply(version,
                            item.GetProperty("lane").GetInt32(), item.GetProperty("operationStage").GetInt32(), item.GetProperty("delta").GetInt32()),
                        _ => throw new JsonException("Unknown source turn candidate."),
                    };
                }
                if (item.GetProperty("contractVersion").GetInt32() != 1 || item.GetProperty("kind").GetString() != action.Kind || item.GetProperty("actionId").GetString() != action.ActionId)
                    throw new JsonException("Source action identity differs.");
                candidates.Add(action);
            }
            var set = new CampaignLegalActionSet(root.GetProperty("campaignId").GetString()!, root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!, root.GetProperty("positionId").GetString()!,
                CampaignLegalActionSerializer.ParseAudience(root.GetProperty("audience").GetString()), candidates, policy);
            if (root.GetProperty("contractVersion").GetInt32() != CampaignLegalActionSet.CurrentContractVersion
                || root.GetProperty("policyId").GetString() != set.PolicyId || !bytes.SequenceEqual(Serialize(set)))
                throw new JsonException("Source action set is not canonical.");
            return set;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or OverflowException)
        { throw new JsonException("Invalid source action set.", error); }
    }
}
