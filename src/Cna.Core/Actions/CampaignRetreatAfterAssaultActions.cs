using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

internal sealed record RetreatAfterAssaultMoveAction : CampaignActionCandidate
{
    internal RetreatAfterAssaultMoveAction(string elementId, string originLocationId,
        string destinationLocationId)
        : base("retreat-after-assault-move", Semantics(elementId,
            originLocationId, destinationLocationId))
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        OriginLocationId = ContentContractGuards.RequireStableId(originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(destinationLocationId,
            nameof(destinationLocationId));
        if (OriginLocationId == DestinationLocationId)
            throw new ArgumentException("Retreat must leave the current hex.",
                nameof(destinationLocationId));
    }

    internal string ElementId { get; }
    internal string OriginLocationId { get; }
    internal string DestinationLocationId { get; }

    private static byte[] Semantics(string elementId, string origin, string destination)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "retreat-after-assault-move");
            writer.WriteString("elementId", ContentContractGuards.RequireStableId(elementId,
                nameof(elementId)));
            writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(origin,
                nameof(origin)));
            writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(destination,
                nameof(destination)));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}

internal sealed record RefuseRetreatAfterAssaultAction : CampaignActionCandidate
{
    internal RefuseRetreatAfterAssaultAction(string elementId)
        : base("refuse-retreat-after-assault", elementId)
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
    }

    internal string ElementId { get; }
}
