using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

/// <summary>
/// The owner-visible part of one Core contact/action cut. Host retains the source cut and
/// binds a durable opening before this projection or any action reference can be issued.
/// </summary>
internal static class CampaignContactOwnerVisibleCutV1
{
    public const int ContractVersion = 1;

    public static byte[] SerializeCanonical(CampaignContactOwnerMovementCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        var observation = cut.ContactObservation;
        if (cut.AuthorityVersion < 1 || observation.ContractVersion
            != CampaignContactObservationV1.CurrentContractVersion)
            throw new ArgumentException("Owner cut is not an accepted contact projection.", nameof(cut));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", observation.CampaignId);
            writer.WriteString("side", observation.Side == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteString("actingFormationId", observation.ActingFormationId);
            writer.WriteNumber("contactAudienceRevision", observation.AudienceRevision);
            writer.WritePropertyName("contactObservation");
            writer.WriteRawValue(observation.SerializeCanonical());
            CampaignObservationSerializer.WriteLocations(writer, cut.Locations);
            CampaignObservationSerializer.WriteEdges(writer, cut.Edges);
            CampaignObservationSerializer.WriteOwnElements(writer, [cut.ActingElement]);
            writer.WriteStartArray("movementOffers");
            foreach (var offer in cut.MovementOffers.OrderBy(value => value.OriginLocationId,
                         StringComparer.Ordinal).ThenBy(value => value.DestinationLocationId,
                         StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("kind", "move-element");
                writer.WriteString("elementId", offer.ElementId);
                writer.WriteString("originLocationId", offer.OriginLocationId);
                writer.WriteString("destinationLocationId", offer.DestinationLocationId);
                writer.WriteNumber("totalCostNumerator", offer.CostBreakdown.TotalCost.Numerator);
                writer.WriteNumber("totalCostDenominator", offer.CostBreakdown.TotalCost.Denominator);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
