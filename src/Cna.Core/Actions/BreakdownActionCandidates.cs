using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

public sealed record StopElementMovementAction : CampaignActionCandidate
{
    internal StopElementMovementAction(string routeId)
        : base("stop-element-movement", BreakdownActionSemantics.Write("stop-element-movement", "routeId", routeId))
    {
        RouteId = ContentContractGuards.RequireSha256(routeId, nameof(routeId));
    }

    public string RouteId { get; }
}

public sealed record ResolveBreakdownStopAction : CampaignActionCandidate
{
    internal ResolveBreakdownStopAction(string stopId)
        : base("resolve-breakdown-stop", BreakdownActionSemantics.Write("resolve-breakdown-stop", "stopId", stopId))
    {
        StopId = ContentContractGuards.RequireSha256(stopId, nameof(stopId));
    }

    public string StopId { get; }
}

public sealed record CompleteBreakdownSegmentAction : CampaignActionCandidate
{
    internal CompleteBreakdownSegmentAction(int operationStage = 1)
        : base("complete-breakdown-segment", operationStage) { }
}

internal static class BreakdownActionSemantics
{
    public static byte[] Write(string kind, string property, string handle)
    {
        ContentContractGuards.RequireSha256(handle, nameof(handle));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignActionCandidate.CurrentContractVersion);
            writer.WriteString("kind", kind);
            writer.WriteString(property, handle);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
