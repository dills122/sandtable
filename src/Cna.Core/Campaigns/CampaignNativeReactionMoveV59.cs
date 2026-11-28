using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed partial record CampaignNativeReactionMoveV59(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash, int GameTurn,
    CampaignReactionWindowId WindowId, LandSide ReactingSide, string PriorWorldHash, string ResultWorldHash,
    CampaignReactionOpportunityId OpportunityId, string ElementId, string OriginLocationId, string DestinationLocationId,
    CampaignMovementCost Cost, CampaignReactionWindow WindowAfter)
{
    internal const int CurrentContractVersion = 59;
    internal const string EventTypeId = "native-reacting-element-moved";
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal byte[] SerializeCanonical()
    {
        CampaignNativeReactionCodec.RequireWindowBinding(WindowAfter, WindowId, ReactingSide, GameTurn, PriorStateVersion);
        if (WindowAfter.ActiveOpportunityId != OpportunityId || OriginLocationId == DestinationLocationId
            || !WindowAfter.FrozenOpportunities.Any(x => x.OpportunityId == OpportunityId
                && x.ReactingRepresentation.BoundElementIds.Count == 1 && x.ReactingRepresentation.BoundElementIds[0] == ElementId))
            throw new JsonException("Native Reaction move does not bind its active participant.");
        return CampaignNativeReactionCodec.Write(ContractVersion, CurrentContractVersion, EventTypeId, CampaignId,
            PriorStateVersion, PriorEventHash, GameTurn, WindowId, ReactingSide, PriorWorldHash, ResultWorldHash, writer =>
            {
                writer.WriteString("opportunityId", OpportunityId.Value);
                writer.WriteString("elementId", ContentContractGuards.RequireStableId(ElementId, nameof(ElementId)));
                writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(OriginLocationId, nameof(OriginLocationId)));
                writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(DestinationLocationId, nameof(DestinationLocationId)));
                CampaignEventSerializer.WriteMovementCost(writer, Cost);
                CampaignV11CanonicalCodec.WriteReactionWindow(writer, "windowAfter", WindowAfter);
            });
    }

    internal static CampaignNativeReactionMoveV59 Deserialize(ReadOnlyMemory<byte> bytes) =>
        CampaignNativeReactionCodec.Read(bytes, EventTypeId,
            ["opportunityId", "elementId", "originLocationId", "destinationLocationId", "cost", "windowAfter"],
            root => new CampaignNativeReactionMoveV59(root.GetProperty("contractVersion").GetInt32(),
                CampaignNativeReactionCodec.String(root, "campaignId"), root.GetProperty("priorStateVersion").GetInt64(),
                CampaignNativeReactionCodec.String(root, "priorEventHash"), root.GetProperty("gameTurn").GetInt32(),
                new(CampaignNativeReactionCodec.String(root, "windowId")), CampaignSnapshotSerializer.ParseSide(CampaignNativeReactionCodec.String(root, "reactingSide")),
                CampaignNativeReactionCodec.String(root, "priorWorldHash"), CampaignNativeReactionCodec.String(root, "resultWorldHash"),
                new(CampaignNativeReactionCodec.String(root, "opportunityId")), CampaignNativeReactionCodec.String(root, "elementId"),
                CampaignNativeReactionCodec.String(root, "originLocationId"), CampaignNativeReactionCodec.String(root, "destinationLocationId"),
                CampaignEventSerializer.ParseMovementCost(root.GetProperty("cost")),
                CampaignV11CanonicalCodec.ParseReactionWindow(root.GetProperty("windowAfter"), allowSecondMovement: true)
                    ?? throw new JsonException("Native Reaction move requires a window.")), value => value.SerializeCanonical());
}
