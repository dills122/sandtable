using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Only facts held by the captor's accepted Combat result and current contact
// enter these bytes. Settlement hashes, victim identity and Core action IDs
// remain on the Host side of the opening fence.
internal static class CampaignContactCustodyOwnerVisibleCutV1
{
    internal static byte[] SerializeCanonical(CampaignNativeCombatCustodyCutV25 cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        if (cut.Quantity is < 1 or > 10 || cut.Choices.Count is < 1 or > 2
            || cut.Choices.Select(value => value.Kind).Distinct().Count()
                != cut.Choices.Count)
            throw new JsonException("Invalid captor custody cut.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                cut.CampaignId, nameof(cut.CampaignId)));
            writer.WriteString("captorElementId", ContentContractGuards.RequireStableId(
                cut.CaptorElementId, nameof(cut.CaptorElementId)));
            writer.WriteNumber("captiveQuantity", cut.Quantity);
            writer.WriteStartArray("offers");
            foreach (var choice in cut.Choices)
            {
                if (choice.Kind is not ("relocate-and-guard" or "leave-unguarded")
                    || choice.Route.Count is < 1 or > 16)
                    throw new JsonException("Invalid captor custody choice.");
                writer.WriteStartObject();
                writer.WriteString("kind", choice.Kind);
                writer.WriteStartArray("route");
                foreach (var location in choice.Route)
                    writer.WriteStringValue(ContentContractGuards.RequireStableId(
                        location, nameof(choice.Route)));
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
