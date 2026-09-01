using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

internal sealed record MoveReactingElementAction : CampaignActionCandidate
{
    public MoveReactingElementAction(
        string windowId,
        string opportunityId,
        string originLocationId,
        string destinationLocationId,
        MovementActionCostBreakdown costBreakdown)
        : base(
            "move-reacting-element",
            WriteSemantics(
                windowId,
                opportunityId,
                originLocationId,
                destinationLocationId,
                costBreakdown))
    {
        WindowId = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
        OpportunityId = ContentContractGuards.RequireSha256(
            opportunityId,
            nameof(opportunityId));
        OriginLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (string.Equals(OriginLocationId, DestinationLocationId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A Reaction movement candidate must change location.",
                nameof(destinationLocationId));
        }

        CostBreakdown = costBreakdown ?? throw new ArgumentNullException(nameof(costBreakdown));
    }

    public string WindowId { get; }
    public string OpportunityId { get; }
    public string OriginLocationId { get; }
    public string DestinationLocationId { get; }
    public MovementActionCostBreakdown CostBreakdown { get; }

    private static byte[] WriteSemantics(
        string windowId,
        string opportunityId,
        string originLocationId,
        string destinationLocationId,
        MovementActionCostBreakdown costBreakdown)
    {
        _ = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
        _ = ContentContractGuards.RequireSha256(opportunityId, nameof(opportunityId));
        var origin = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        var destination = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        ArgumentNullException.ThrowIfNull(costBreakdown);
        if (string.Equals(origin, destination, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A Reaction movement candidate must change location.",
                nameof(destinationLocationId));
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "move-reacting-element");
            writer.WriteString("windowId", windowId);
            writer.WriteString("opportunityId", opportunityId);
            writer.WriteString("originLocationId", origin);
            writer.WriteString("destinationLocationId", destination);
            MovementActionJson.WriteCostBreakdown(writer, costBreakdown);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}

internal sealed record CompleteReactionParticipantAction : CampaignActionCandidate
{
    public CompleteReactionParticipantAction(
        string windowId,
        string opportunityId)
        : base(
            "complete-reaction-participant",
            ReactionActionSemantics.WriteParticipant(
                "complete-reaction-participant",
                windowId,
                opportunityId))
    {
        WindowId = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
        OpportunityId = ContentContractGuards.RequireSha256(
            opportunityId,
            nameof(opportunityId));
    }

    public string WindowId { get; }
    public string OpportunityId { get; }
}

internal abstract record ReactionWindowAction : CampaignActionCandidate
{
    protected ReactionWindowAction(string kind, string windowId)
        : base(kind, ReactionActionSemantics.WriteWindow(kind, windowId))
    {
        WindowId = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
    }

    public string WindowId { get; }
}

internal sealed record DeclineReactionWindowAction : ReactionWindowAction
{
    public DeclineReactionWindowAction(string windowId)
        : base("decline-reaction-window", windowId) { }
}

internal sealed record CloseReactionWindowUnavailableAction : ReactionWindowAction
{
    public CloseReactionWindowUnavailableAction(string windowId)
        : base("close-reaction-window-scripted-unavailable", windowId) { }
}

internal sealed record CloseReactionWindowTimeoutAction : ReactionWindowAction
{
    public CloseReactionWindowTimeoutAction(string windowId)
        : base("close-reaction-window-timeout", windowId) { }
}

internal sealed record CloseReactionWindowNoEligibleAction : ReactionWindowAction
{
    public CloseReactionWindowNoEligibleAction(string windowId)
        : base("close-reaction-window-no-eligible-reactor", windowId) { }
}

internal static class ReactionActionSemantics
{
    public static byte[] WriteWindow(string kind, string windowId)
    {
        _ = ContentContractGuards.RequireStableId(kind, nameof(kind));
        _ = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignActionCandidate.CurrentContractVersion);
            writer.WriteString("kind", kind);
            writer.WriteString("windowId", windowId);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static byte[] WriteParticipant(
        string kind,
        string windowId,
        string opportunityId)
    {
        _ = ContentContractGuards.RequireStableId(kind, nameof(kind));
        _ = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
        _ = ContentContractGuards.RequireSha256(opportunityId, nameof(opportunityId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignActionCandidate.CurrentContractVersion);
            writer.WriteString("kind", kind);
            writer.WriteString("windowId", windowId);
            writer.WriteString("opportunityId", opportunityId);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}
