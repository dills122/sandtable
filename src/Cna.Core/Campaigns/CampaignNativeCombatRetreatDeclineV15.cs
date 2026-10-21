using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// An explicit defender decision closes RBA without moving. Silence is never
// equivalent to this event; the selected assault remains pending until accepted.
internal sealed record CampaignNativeCombatRetreatDeclineV15(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    string PositionId,
    string NextPositionId,
    LandSide DefenderSide,
    string DefenderElementId,
    string ActionId)
{
    public const int CurrentContractVersion = 15;
    public long StateVersion => checked(PriorStateVersion + 1);

    public static CampaignNativeCombatRetreatDeclineV15 Create(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actionId)
    {
        var cut = CampaignContactRetreatBeforeAssaultCut.Project(selected, barrage,
            context, contacts);
        if (actionId != cut.DeclineOffer.ActionId)
            throw new InvalidOperationException("Defender decline is not offered at this RBA cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(
            selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(candidate => candidate.PositionId == barrage.NextPositionId);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.RetreatBeforeAssault)
            throw new InvalidOperationException("Defender decline requires the RBA position.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.SegmentId != LandSegmentIds.Combat
            || next.StepId != LandStepIds.ForceAssignment
            || next.GameTurn != position.GameTurn
            || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("RBA decline must reach same-slot Force Assignment.");
        return new(CurrentContractVersion, selected.CampaignId, barrage.StateVersion,
            selected.Decision.ActionId, position.PositionId, next.PositionId,
            cut.DefenderSide, cut.DefenderElementId, actionId);
    }

    public byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !Enum.IsDefined(DefenderSide)
            || string.IsNullOrWhiteSpace(DecisionActionId) || DecisionActionId.Length > 256
            || string.IsNullOrWhiteSpace(ActionId) || ActionId.Length > 256)
            throw new JsonException("Invalid defender RBA decline.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-rba-declined");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("defenderSide", CampaignSnapshotSerializer.FormatSide(DefenderSide));
            writer.WriteString("defenderElementId", ContentContractGuards.RequireStableId(
                DefenderElementId, nameof(DefenderElementId)));
            writer.WriteString("actionId", ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatRetreatDeclineV15 Replay(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, barrage, context, contacts, parsed.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("RBA decline differs from defender authority.");
        return expected;
    }

    public static CampaignNativeCombatRetreatDeclineV15 Deserialize(ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "nextPositionId", "defenderSide", "defenderElementId", "actionId");
            if (root.GetProperty("eventType").GetString() != "combat-rba-declined")
                throw new JsonException("Unknown defender RBA event.");
            var parsed = new CampaignNativeCombatRetreatDeclineV15(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("defenderSide").GetString()),
                root.GetProperty("defenderElementId").GetString()!,
                root.GetProperty("actionId").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical defender RBA event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid defender RBA event.", exception);
        }
    }

    public byte[] SerializeStateCanonical(CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage)
    {
        if (selected.CampaignId != CampaignId || barrage.StateVersion != PriorStateVersion
            || selected.Decision.ActionId != DecisionActionId)
            throw new InvalidOperationException("RBA decline does not bind selected Barrage.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WritePropertyName("priorBarrageState");
            writer.WriteRawValue(barrage.SerializeStateCanonical(selected));
            writer.WritePropertyName("decline");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
