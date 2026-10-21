using System.Security.Cryptography;
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
        CampaignCombatSelectionFrame selected,
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

    public static CampaignNativeCombatRetreatMoveV16 CreateNext(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16 previous,
        CampaignWorldSnapshotV6 currentWorld,
        string initialLocationId,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actionId)
    {
        var cut = CampaignContactRetreatBeforeAssaultCut.ProjectAfterMove(
            selected, barrage, previous, currentWorld, initialLocationId,
            context, contacts);
        var offer = cut.MoveOffers.SingleOrDefault(value => value.ActionId == actionId)
            ?? throw new InvalidOperationException("Further retreat move is absent from the current cut.");
        _ = CampaignNativeCombatRetreatMovement.ProjectAfterMove(selected, barrage,
            previous, currentWorld, initialLocationId, context, contacts, actionId);
        return new(CurrentContractVersion, selected.CampaignId, previous.StateVersion,
            selected.Decision.ActionId, barrage.NextPositionId,
            previous.InitiallyAdjacentToKnownEnemy, cut.DefenderElementId,
            offer.OriginLocationId, offer.DestinationLocationId, actionId);
    }

    public CampaignWorldSnapshotV6 ProjectWorld(
        CampaignCombatSelectionFrame selected,
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

    public CampaignWorldSnapshotV6 ProjectWorldAfter(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16 previous,
        CampaignWorldSnapshotV6 currentWorld,
        string initialLocationId,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        var expected = CreateNext(selected, barrage, previous, currentWorld,
            initialLocationId, context, contacts, ActionId);
        if (this != expected)
            throw new InvalidCampaignHistoryException("Further RBA move differs from defender authority.");
        return CampaignNativeCombatRetreatMovement.ProjectAfterMove(selected, barrage,
            previous, currentWorld, initialLocationId, context, contacts, ActionId).World;
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
        CampaignCombatSelectionFrame selected,
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

    public static CampaignNativeCombatRetreatMoveV16 ReplayNext(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16 previous,
        CampaignWorldSnapshotV6 currentWorld,
        string initialLocationId,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = CreateNext(selected, barrage, previous, currentWorld,
            initialLocationId, context, contacts, parsed.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Further RBA move differs from defender authority.");
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
        CampaignCombatSelectionFrame selected,
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

    public byte[] SerializeNextStateCanonical(ReadOnlySpan<byte> priorMoveState,
        CampaignWorldSnapshotV6 world)
    {
        if (priorMoveState.IsEmpty || world is null)
            throw new ArgumentException("Further RBA move requires the retained prior state.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", PositionId);
            writer.WriteString("priorMoveHash", "sha256:" +
                Convert.ToHexStringLower(SHA256.HashData(priorMoveState)));
            writer.WritePropertyName("move");
            writer.WriteRawValue(SerializeCanonical());
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", world);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
