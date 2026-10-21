using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// A defender's accepted RBA move stays at the RBA position. The next owner cut
// decides whether to move again or explicitly stop; elapsed time is not a choice.
internal sealed record CampaignNativeCombatRetreatMoveV16(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    string PositionId,
    bool InitiallyAdjacentToKnownEnemy,
    string DefenderElementId,
    string OriginLocationId,
    string DestinationLocationId,
    string ActionId)
{
    public const int CurrentContractVersion = 16;
    public long StateVersion => checked(PriorStateVersion + 1);

    public static CampaignNativeCombatRetreatMoveV16 CreateFirst(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actionId)
    {
        var cut = CampaignContactRetreatBeforeAssaultCut.Project(selected, barrage,
            context, contacts);
        var offer = cut.MoveOffers.SingleOrDefault(value => value.ActionId == actionId)
            ?? throw new InvalidOperationException("Defender move is absent from the current RBA cut.");
        _ = CampaignNativeCombatRetreatMovement.ProjectFirst(selected, barrage,
            context, contacts, actionId);
        return new(CurrentContractVersion, selected.CampaignId, barrage.StateVersion,
            selected.Decision.ActionId, barrage.NextPositionId,
            cut.InitiallyAdjacentToKnownEnemy, cut.DefenderElementId,
            offer.OriginLocationId, offer.DestinationLocationId, actionId);
    }

    public CampaignWorldSnapshotV6 ProjectWorld(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        var expected = CreateFirst(selected, barrage, context, contacts, ActionId);
        if (this != expected)
            throw new InvalidCampaignHistoryException("RBA move differs from defender authority.");
        return CampaignNativeCombatRetreatMovement.ProjectFirst(selected, barrage,
            context, contacts, ActionId).World;
    }

    public byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || string.IsNullOrWhiteSpace(DecisionActionId) || DecisionActionId.Length > 256
            || string.IsNullOrWhiteSpace(ActionId) || ActionId.Length > 256)
            throw new JsonException("Invalid defender RBA move.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-rba-moved");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteBoolean("initiallyAdjacentToKnownEnemy", InitiallyAdjacentToKnownEnemy);
            writer.WriteString("defenderElementId", ContentContractGuards.RequireStableId(
                DefenderElementId, nameof(DefenderElementId)));
            writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(
                OriginLocationId, nameof(OriginLocationId)));
            writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(
                DestinationLocationId, nameof(DestinationLocationId)));
            writer.WriteString("actionId", ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatRetreatMoveV16 ReplayFirst(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = CreateFirst(selected, barrage, context, contacts, parsed.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("RBA move differs from defender authority.");
        return expected;
    }

    public static CampaignNativeCombatRetreatMoveV16 Deserialize(ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "initiallyAdjacentToKnownEnemy", "defenderElementId", "originLocationId",
                "destinationLocationId", "actionId");
            if (root.GetProperty("eventType").GetString() != "combat-rba-moved")
                throw new JsonException("Unknown defender RBA event.");
            var parsed = new CampaignNativeCombatRetreatMoveV16(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("initiallyAdjacentToKnownEnemy").GetBoolean(),
                root.GetProperty("defenderElementId").GetString()!,
                root.GetProperty("originLocationId").GetString()!,
                root.GetProperty("destinationLocationId").GetString()!,
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

    public byte[] SerializeStateCanonical(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        var world = ProjectWorld(selected, barrage, context, contacts);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", PositionId);
            writer.WritePropertyName("priorBarrageState");
            writer.WriteRawValue(barrage.SerializeStateCanonical(selected));
            writer.WritePropertyName("move");
            writer.WriteRawValue(SerializeCanonical());
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", world);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
