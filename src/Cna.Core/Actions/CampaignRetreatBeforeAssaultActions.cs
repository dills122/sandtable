using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

public sealed record RetreatBeforeAssaultMoveAction : CampaignActionCandidate
{
    internal RetreatBeforeAssaultMoveAction(string elementId, string originLocationId,
        string destinationLocationId, MovementActionCostBreakdown costBreakdown)
        : base("retreat-before-assault-move", WriteSemantics(elementId,
            originLocationId, destinationLocationId))
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        OriginLocationId = ContentContractGuards.RequireStableId(originLocationId, nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(destinationLocationId,
            nameof(destinationLocationId));
        if (OriginLocationId == DestinationLocationId)
            throw new ArgumentException("Retreat must change location.", nameof(destinationLocationId));
        CostBreakdown = costBreakdown ?? throw new ArgumentNullException(nameof(costBreakdown));
    }

    public string ElementId { get; }
    public string OriginLocationId { get; }
    public string DestinationLocationId { get; }
    public MovementActionCostBreakdown CostBreakdown { get; }

    private static byte[] WriteSemantics(string elementId, string originLocationId,
        string destinationLocationId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "retreat-before-assault-move");
            writer.WriteString("elementId", ContentContractGuards.RequireStableId(elementId, nameof(elementId)));
            writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(originLocationId,
                nameof(originLocationId)));
            writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(destinationLocationId,
                nameof(destinationLocationId)));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}

public sealed record DeclineRetreatBeforeAssaultAction : CampaignActionCandidate
{
    internal DeclineRetreatBeforeAssaultAction(string elementId)
        : base("decline-retreat-before-assault", elementId)
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
    }

    public string ElementId { get; }
}
