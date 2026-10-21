using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

internal sealed record FinishCombatWithoutAttackAction : CampaignActionCandidate
{
    internal FinishCombatWithoutAttackAction(string actingElementId)
        : base("finish-combat-without-attack", Semantics(actingElementId))
    {
        ActingElementId = ContentContractGuards.RequireStableId(actingElementId,
            nameof(actingElementId));
    }

    public string ActingElementId { get; }

    private static byte[] Semantics(string actingElementId)
    {
        _ = ContentContractGuards.RequireStableId(actingElementId, nameof(actingElementId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "finish-combat-without-attack");
            writer.WriteString("actingElementId", actingElementId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
