using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

internal sealed record SelectContactAssaultAction : CampaignActionCandidate
{
    internal SelectContactAssaultAction(string actingElementId, string contactHandle,
        string apparentLocationId)
        : base("select-contact-assault", Semantics(actingElementId, contactHandle, apparentLocationId))
    {
        ActingElementId = ContentContractGuards.RequireStableId(actingElementId, nameof(actingElementId));
        ContactHandle = ContentContractGuards.RequireStableId(contactHandle, nameof(contactHandle));
        ApparentLocationId = ContentContractGuards.RequireStableId(apparentLocationId, nameof(apparentLocationId));
    }

    public string ActingElementId { get; }
    public string ContactHandle { get; }
    public string ApparentLocationId { get; }

    private static byte[] Semantics(string actingElementId, string contactHandle,
        string apparentLocationId)
    {
        _ = ContentContractGuards.RequireStableId(actingElementId, nameof(actingElementId));
        _ = ContentContractGuards.RequireStableId(contactHandle, nameof(contactHandle));
        _ = ContentContractGuards.RequireStableId(apparentLocationId, nameof(apparentLocationId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "select-contact-assault");
            writer.WriteString("actingElementId", actingElementId);
            writer.WriteString("contactHandle", contactHandle);
            writer.WriteString("apparentLocationId", apparentLocationId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
