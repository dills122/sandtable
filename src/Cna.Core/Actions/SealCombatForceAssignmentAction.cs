using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

// One owner seals the currently supported full-TOE allocation for its own
// selected Combat participant. Opposing allocation never enters action identity.
internal sealed record SealCombatForceAssignmentAction : CampaignActionCandidate
{
    internal SealCombatForceAssignmentAction(string role, string elementId,
        string componentId, int committedToe)
        : base("seal-combat-force-assignment", Semantics(role, elementId,
            componentId, committedToe))
    {
        Role = role;
        ElementId = elementId;
        ComponentId = componentId;
        CommittedToe = committedToe;
    }

    public string Role { get; }
    public string ElementId { get; }
    public string ComponentId { get; }
    public int CommittedToe { get; }

    private static byte[] Semantics(string role, string elementId,
        string componentId, int committedToe)
    {
        if (role is not ("attacker" or "defender") || committedToe <= 0)
            throw new ArgumentException("Unsupported Force Assignment slot.");
        _ = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        _ = ContentContractGuards.RequireStableId(componentId, nameof(componentId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "seal-combat-force-assignment");
            writer.WriteString("role", role);
            writer.WriteString("elementId", elementId);
            writer.WriteString("componentId", componentId);
            writer.WriteNumber("committedToe", committedToe);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
